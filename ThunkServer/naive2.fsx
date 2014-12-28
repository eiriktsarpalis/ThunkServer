#I "../bin/"
#r "Thespian.dll"
#r "ThunkServer.dll"

open System.IO
open ThunkServer

// create a local server
let localServer = Naive2.createServer "thunkServer"

// evaluate locally ; should fail
Naive2.evaluate localServer (fun () -> 1 + 1)

//
//  Remote evaluation
//

// initialize a remote process hosting an instance of the same actor
Daemon.executable <- __SOURCE_DIRECTORY__ + "/../bin/ThunkServer.Daemon.exe"
let remoteServer = Naive2.spawnWindow ()

// evaluate remotely ; should fail
Naive2.evaluate remoteServer (fun () -> 1 + 1)