module ThunkServer.Naive

//
//  Naive thunk server implementation that does not make use of Vagrant
//

open Nessos.Thespian

type ThunkMessage = (unit -> obj) * IReplyChannel<Choice<obj, exn>>

let rec serverLoop (self : Actor<ThunkMessage>) : Async<unit> = 
    async {
        let! thunk, reply = self.Receive()
        let result : Choice<obj, exn> =
            try thunk () |> Choice1Of2
            with e -> Choice2Of2 e

        do! reply.Reply result
        return! serverLoop self
    }

/// create a local thunk server instance with given name
let createServer name = Actor.create name serverLoop |> Actor.ref

/// Spawns a local process running a single thunk server
let spawnWindow () =
    Daemon.spawnWindowAsync(fun () -> createServer "ThunkServer")
    |> Async.RunSynchronously

/// submit a thunk for evaluation to target actor ref
let evaluate (server : ActorRef<ThunkMessage>) (thunk : unit -> 'T) =
    let result = server <!= fun replyChannel -> (fun () -> thunk () :> obj), replyChannel
    match result with
    | Choice1Of2 o -> o :?> 'T
    | Choice2Of2 e -> raise e