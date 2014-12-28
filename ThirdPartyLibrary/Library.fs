module ThirdPartyLibrary

    // collection of compiled lambdas for use by the ThunkServer

    let addition = () ; fun () -> 1 + 1
    let sideEffect = () ; fun () -> printfn "Remoted side-effect"
    let failure = () ; fun () -> do failwith "boom"