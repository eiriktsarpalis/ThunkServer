module ThunkServer.Naive2

//
//  Naive thunk server implementation that does not make use of Vagrant
//

open Nessos.Thespian

open System.IO
open System.Reflection

// Serializable assembly package
type AssemblyPackage =
    {
        FullName : string
        AssemblyImage : byte []
    }

// transitively traverse assembly dependencies for given value
let gatherDependencies (value:'T) : AssemblyPackage list =
    let rec aux (gathered : Map<string, Assembly>) (remaining : Assembly list) =
        match remaining with
        // ignored assembly
        | a :: rest when gathered.ContainsKey a.FullName || a.GlobalAssemblyCache -> aux gathered rest
        // came across new assembly, get location and traverse referenced assemblies
        | a :: rest ->
            let dependencies = 
                a.GetReferencedAssemblies() 
                |> Seq.choose(fun an -> try Some <| Assembly.Load an with _ -> None) 
                |> Seq.toList

            aux (Map.add a.FullName a gathered) (dependencies @ rest)
        // traversal complete, create assembly package
        | [] ->
            gathered
            |> Seq.map (fun (KeyValue(_,a)) -> 
                                { FullName = a.FullName
                                  AssemblyImage = File.ReadAllBytes a.Location })
            |> Seq.toList

    aux Map.empty [value.GetType().Assembly]

// loads raw assemblies in current application domain
let loadRawAssemblies (assemblies : AssemblyPackage list) =
    assemblies |> List.iter (fun a -> Assembly.Load a.AssemblyImage |> ignore)

type ThunkMessage = 
    | RunThunk of (unit -> obj) * IReplyChannel<Choice<obj, exn>>
    | LoadAssemblies of AssemblyPackage list

let rec serverLoop (self : Actor<ThunkMessage>) : Async<unit> = 
    async {
        let! msg = self.Receive()
        match msg with
        | RunThunk(thunk, reply) ->
            let result : Choice<obj, exn> =
                try thunk () |> Choice1Of2
                with e -> Choice2Of2 e

            do! reply.Reply result
        | LoadAssemblies assemblies ->
            loadRawAssemblies assemblies
            
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
    // traverse and upload dependencies
    let dependencies = gatherDependencies thunk
    server <-- LoadAssemblies dependencies
    let result = server <!= fun replyChannel -> RunThunk ((fun () -> thunk () :> obj), replyChannel)
    match result with
    | Choice1Of2 o -> o :?> 'T
    | Choice2Of2 e -> raise e