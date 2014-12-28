#I "../bin/"
#r "Thespian.dll"
#r "ThunkServer.dll"

open ThunkServer

// initialize a remote process hosting an instance of the actor
Daemon.executable <- __SOURCE_DIRECTORY__ + "/../bin/ThunkServer.Daemon.exe"
let remoteServer = Naive2.spawnWindow ()

// remotely evaluate lambda defined in third party library; should succeed
#r "../ThirdPartyLibrary/bin/ThirdPartyLibrary.dll"
Naive2.evaluate remoteServer ThirdPartyLibrary.sideEffect

// remotely evaluate lambda defined in F# interactive; should fail
Naive2.evaluate remoteServer (fun () -> 1 + 1)