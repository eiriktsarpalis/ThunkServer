namespace ThunkServer

open System
open System.IO
open System.Reflection

open Nessos.FsPickler
open Nessos.Vagrant
open Nessos.Thespian
open Nessos.Thespian.Serialization
open Nessos.Thespian.Remote
open Nessos.Thespian.Remote.TcpProtocol

// Basic configuration; initializes Vagrant and Thespian in the current process

module internal Config =

    // initializes a unique vagrant instance; ignore all assemblies that depend on current assembly

    let vagrant =
        let cacheDir = Path.Combine(Path.GetTempPath(), sprintf "thunkServerCache-%O" <| Guid.NewGuid())
        let _ = Directory.CreateDirectory cacheDir
        Vagrant.Initialize(cacheDirectory = cacheDir, ignoredAssemblies = [Assembly.GetExecutingAssembly()])


module Actor =

    // initialize Thespian
    do
        TcpListenerPool.RegisterListener(IPEndPoint.any)
        // Thespian must be set to use the serializer provided by Vagrant for messaging
        let serializer = new FsPicklerMessageSerializer(Config.vagrant.Pickler) :> IMessageSerializer
        Nessos.Thespian.Serialization.defaultSerializer <- serializer

    /// <summary>
    ///     Publish a Thespian actor to the default TCP protocol
    /// </summary>
    /// <param name="name">Actor name.</param>
    /// <param name="body">Actor body.</param>
    let create name (body : Actor<'T> -> Async<unit>) : Actor<'T> =
        body
        |> Actor.bind
        |> Actor.rename name
        |> Actor.publish [ Protocols.utcp() ]
        |> Actor.start