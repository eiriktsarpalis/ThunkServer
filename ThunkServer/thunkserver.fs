module ThunkServer.VagrantServer

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
            let info = Config.vagrant.GetAssemblyLoadInfo assemblyIds
            do! reply.Reply info

        | UploadAssemblies(pkgs, reply) ->
            let results = Config.vagrant.LoadAssemblyPackages(pkgs)
            do! reply.Reply results
            
        return! serverLoop self
    }

/// create a local thunk server instance with given name
let createServer name = Actor.create name serverLoop |> Actor.ref

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

// ThunkServer daemon implementation
module Daemon =

    open System
    open System.Diagnostics

    /// gets or sets location of the ThunkServer daemon
    let mutable executable : string = null

    let toCommandLineArg (receiver : ActorRef<ActorRef<ThunkMessage>>) =
        let bytes = Config.vagrant.Pickler.Pickle receiver
        System.Convert.ToBase64String bytes

    let fromCommandLineArg (arg : string) =
        let bytes = System.Convert.FromBase64String arg
        Config.vagrant.Pickler.UnPickle<ActorRef<ActorRef<ThunkMessage>>>(bytes)

    /// Spawns a local process running a single thunk server
    let spawnWindowAsync () = async {
        if executable = null then invalidOp "Unset 'executable'."
        use receiver = Actor.createReceiver<ActorRef<ThunkMessage>> ()
        let! awaiter = receiver.ReceiveEvent |> Async.AwaitEvent |> Async.StartChild
        let proc = Process.Start(executable, toCommandLineArg receiver.Ref)
        return! awaiter
    }

    /// Spawns a local process running a single thunk server
    let spawnWindow () = spawnWindowAsync() |> Async.RunSynchronously