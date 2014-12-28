#I "../bin/"
#r "Thespian.dll"
#r "ThunkServer.dll"

open ThunkServer

// spawns a windowed console application that hosts a single thunk server instance
Daemon.executable <- __SOURCE_DIRECTORY__ + @"/../bin/ThunkServer.Daemon.exe"
let server = ThunkServer.spawnWindow()

// deploying code from third-party library
#r "../ThirdPartyLibrary/bin/ThirdPartyLibrary.dll"
ThunkServer.evaluate server ThirdPartyLibrary.addition

// deploying code from fsi
ThunkServer.evaluate server (fun () -> 1 + 1)
ThunkServer.evaluate server (fun () -> printfn "Remote side-effect")
ThunkServer.evaluate server (fun () -> do failwith "boom")

// Example : Fsi top-level value bindings
let cell = ref 0
for i in 1 .. 100 do
    cell := ThunkServer.evaluate server (fun () -> !cell + 1)
!cell // MAGIC

//
// Example : deploying actors remotely using fsi
//

open Nessos.Thespian

// deploy a remote actor using thunk server
let deployActor name (body : Actor<'T> -> Async<unit>) : ActorRef<'T> =
    ThunkServer.evaluate server (fun () -> let actor = Actor.Start name body in actor.Ref)

// example : counter implementation
type Counter =
    | IncrementBy of int
    | GetCount of IReplyChannel<int>

let rec body count (self : Actor<Counter>) = async {
    let! msg = self.Receive()
    match msg with
    | IncrementBy i -> return! body (count + i) self
    | GetCount rc ->
        do! rc.Reply count
        return! body count self
}

let ref = deployActor "counter" (body 0)

ref <-- IncrementBy 1 // post
ref <-- IncrementBy 2 // post
ref <-- IncrementBy 3 // post
ref <!= GetCount // post with reply