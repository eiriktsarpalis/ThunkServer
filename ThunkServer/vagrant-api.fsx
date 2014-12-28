#I "../bin/"
#r "FsPickler.dll"
#r "Vagrant.dll"

open Nessos.Vagrant

// initialize a vagrant instance
let vagrant = Vagrant.Initialize()

// create an object that depends on FSI's dynamic assembly
let f = box (fun i -> i + 1)
vagrant.ComputeObjectDependencies(f, permitCompilation = false) // fail
let deps = vagrant.ComputeObjectDependencies(f, permitCompilation = true) // success

// create a distributable assembly package
let pkg : AssemblyPackage = vagrant.CreateAssemblyPackage(deps.[0], includeAssemblyImage = true)

// load assembly package to local application domain
let result : AssemblyLoadInfo = vagrant.LoadAssemblyPackage(pkg)

// pickle using vagrant's pickler instance
vagrant.Pickler.Pickle f