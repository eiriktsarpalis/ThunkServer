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

module internal Config =

    // initializes a unique vagrant instance; ignore all assemblies that depend on current assembly

    let vagrant =
        let cacheDir = Path.Combine(Path.GetTempPath(), sprintf "thunkServerCache-%O" <| Guid.NewGuid())
        let _ = Directory.CreateDirectory cacheDir
        Vagrant.Initialize(cacheDirectory = cacheDir, ignoredAssemblies = [Assembly.GetExecutingAssembly()])


module Actor =

    // initialize Thespian ; plugs vagrant pickler instance to configuration

    do
        let serializer = new FsPicklerMessageSerializer(Config.vagrant.Pickler) :> IMessageSerializer
        Nessos.Thespian.Serialization.defaultSerializer <- serializer
        TcpListenerPool.RegisterListener(IPEndPoint.any)

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

    /// <summary>
    ///     Publish a new Thespian receiver to the default TCP protocol
    /// </summary>
    let createReceiver<'T> () =
        Receiver.create<'T> ()
        |> Receiver.publish [ Protocols.utcp() ]
        |> Receiver.start

module XmlPickler =
    let private xmlPickler = FsPickler.CreateXml(indent = true, typeConverter = Config.vagrant.TypeConverter)
    
    let toFile<'T> (path : string) (value : 'T) =
        use fs = File.OpenWrite path
        xmlPickler.Serialize(fs, value)

    let fromFile<'T> (path : string) =
        use fs = File.OpenRead path
        xmlPickler.Deserialize<'T>(fs)