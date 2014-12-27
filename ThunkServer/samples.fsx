#I "../bin/"
#r "Thespian.dll"
#r "ThunkServer.dll"

open ThunkServer.VagrantServer

Daemon.executable <- __SOURCE_DIRECTORY__ + @"/../bin/ThunkServer.Daemon.exe"
let server = Daemon.spawnWindow()

evaluate server (fun () -> 1 + 1)
evaluate server (fun () -> printfn "Remote side-effect")
evaluate server (fun () -> do failwith "boom")