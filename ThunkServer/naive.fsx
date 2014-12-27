#I "../bin/"
#r "Thespian.dll"
#r "ThunkServer.dll"

open System.IO
open Nessos.Thespian
open ThunkServer
open ThunkServer.Naive

// file used to pass actor refs between processes
let actorRefPickle = Path.Combine(Path.GetTempPath(), "actorRefPickle.xml")

// create a local server
let localServer = Naive.createServer "thunkServer"

// evaluate locally ; should succeed
evaluate localServer (fun () -> 1 + 1)

//
//  Remote evaluation
//

// persist actor ref to file
XmlPickler.toFile actorRefPickle localServer

// read actor ref from file
let remoteServer = XmlPickler.fromFile<ActorRef<ThunkMessage>> actorRefPickle

// evaluate to remote server ; should fail
evaluate remoteServer (fun () -> 1 + 1)