namespace ThunkServer

// ThunkServer daemon implementation
module Daemon =

    open System
    open System.IO
    open System.Diagnostics
    
    open Nessos.Thespian
    open Nessos.Thespian.Remote

    /// gets or sets location of the ThunkServer daemon
    let mutable executable : string = null

    /// Daemon initialization parameters ; actor factory and receiver
    type DaemonFactory = (unit -> ActorRef) * ActorRef<ActorRef>

    let toCommandLineArg (setup : DaemonFactory) =
        let bytes = Config.vagrant.Pickler.Pickle setup
        System.Convert.ToBase64String bytes

    let fromCommandLineArg (arg : string) =
        let bytes = System.Convert.FromBase64String arg
        Config.vagrant.Pickler.UnPickle<DaemonFactory>(bytes)

    /// Spawns a local process running a single thunk server
    let spawnWindowAsync (factory : unit -> ActorRef<'T>) = async {
        if not <| File.Exists executable then invalidOp "Invalid executable"
        use receiver = 
            Receiver.create<ActorRef> ()
            |> Receiver.publish [ Protocols.utcp() ]
            |> Receiver.start
        let! awaiter = receiver.ReceiveEvent |> Async.AwaitEvent |> Async.StartChild
        let parameters = (fun () -> factory () :> ActorRef), receiver.Ref
        let proc = Process.Start(executable, toCommandLineArg parameters)
        let! actorRef = awaiter
        return actorRef :?> ActorRef<'T>
    }