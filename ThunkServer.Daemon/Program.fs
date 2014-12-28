open ThunkServer

[<EntryPoint>]
let main argv = 
    try
        let factory, receiver = Daemon.fromCommandLineArg argv.[0]
        let ref = factory ()
        printfn "Initialized thunk server:\n%O." ref
        receiver.Post ref
        while true do System.Threading.Thread.Sleep 1000
        0
    with e ->
        eprintfn "Error: %O" e
        let _ = System.Console.ReadKey ()
        1