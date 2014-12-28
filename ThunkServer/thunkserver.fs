module ThunkServer.ThunkServer

//
//  Correct thunk server implementation that uses Vagrant
//

open Nessos.Thespian
open Nessos.Vagrant

type ThunkMessage = 
    // Evaluate thunk
    | RunThunk of (unit -> obj) * IReplyChannel<Choice<obj, exn>>
    // Query remote process on assembly load state
    | GetAssemblyLoadState of AssemblyId list * IReplyChannel<AssemblyLoadInfo list>
    // Submit assembly packages for loading at remote party
    | UploadAssemblies of AssemblyPackage list * IReplyChannel<AssemblyLoadInfo list>

let rec serverLoop (self : Actor<ThunkMessage>) : Async<unit> = 
    async {
        let! msg = self.Receive()
        match msg with
        | RunThunk(thunk, reply) ->
            let result : Choice<obj, exn> =
                try thunk () |> Choice1Of2
                with e -> Choice2Of2 e

            do! reply.Reply result
        | GetAssemblyLoadState(assemblyIds, reply)  ->
            // query local vagrant object on load state
            let info = Config.vagrant.GetAssemblyLoadInfo assemblyIds
            do! reply.Reply info

        | UploadAssemblies(pkgs, reply) ->
            // load packages using local vagrant object
            let results = Config.vagrant.LoadAssemblyPackages(pkgs)
            do! reply.Reply results
            
        return! serverLoop self
    }

/// create a local thunk server instance with given name
let createServer name = Actor.Start name serverLoop |> Actor.ref

/// Spawns a local process running a single thunk server
let spawnWindow () =
    Daemon.spawnWindowAsync(fun () -> createServer "ThunkServer")
    |> Async.RunSynchronously

/// submit a thunk for evaluation to target actor ref
let evaluate (server : ActorRef<ThunkMessage>) (thunk : unit -> 'T) =
    // receiver implementation ; only specifies how to communicate with remote party
    let receiver =
        {
            new IRemoteAssemblyReceiver with
                member __.GetLoadedAssemblyInfo(ids: AssemblyId list) = 
                    server.PostWithReply(fun reply -> GetAssemblyLoadState(ids, reply))
                
                member __.PushAssemblies (pkgs: AssemblyPackage list) = 
                    server.PostWithReply(fun reply -> UploadAssemblies(pkgs, reply))
        }

    // submit assemblies using the receiver implementation and the built-in upload protocol
    Config.vagrant.SubmitObjectDependencies(receiver, thunk, permitCompilation = true) 
    |> Async.RunSynchronously
    |> ignore

    let result = server <!= fun replyChannel -> RunThunk ((fun () -> thunk () :> obj), replyChannel)
    match result with
    | Choice1Of2 o -> o :?> 'T
    | Choice2Of2 e -> raise e