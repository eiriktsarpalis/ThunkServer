#I "../bin/"
#r "Thespian.dll"
#r "ThunkServer.dll"

open ThunkServer

// create a local server
let localServer = Naive.createServer "thunkServer"

// evaluate locally ; should succeed
Naive.evaluate localServer (fun () -> 1 + 1)

//
//  Remote evaluation
//

// initialize a remote process hosting an instance of the same actor
Daemon.executable <- __SOURCE_DIRECTORY__ + "/../bin/ThunkServer.Daemon.exe"
let remoteServer = Naive.spawnWindow ()

// remotely evaluate lambda defined in third party library; should fail
#r "../ThirdPartyLibrary/bin/ThirdPartyLibrary.dll"
Naive.evaluate remoteServer ThirdPartyLibrary.sideEffect

// remotely evaluate lambda defined in F# interactive; should fail
Naive.evaluate remoteServer (fun () -> 1 + 1)