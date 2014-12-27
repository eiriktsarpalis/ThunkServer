open ThunkServer.VagrantServer

[<EntryPoint>]
let main argv = 
    let receiver =
        if argv.Length > 0 then
            Some <| Daemon.fromCommandLineArg argv.[0]
        else
            None

    let ref = createServer "ThunkServer"
    printfn "Initialized thunk server %O." ref
    receiver |> Option.iter (fun r -> r.Post ref)
    while true do System.Threading.Thread.Sleep 1000
    0