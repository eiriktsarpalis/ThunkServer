#I "../bin/"
#r "FsPickler.dll"
#r "Vagrant.dll"

open Nessos.Vagrant

// initialize a vagrant instance
let vagrant = Vagrant.Initialize()

let f = box (fun i -> i + 1)
vagrant.ComputeObjectDependencies(f, permitCompilation = false) // fail
let deps = vagrant.ComputeObjectDependencies(f, permitCompilation = true) // success

let pkg : AssemblyPackage = vagrant.CreateAssemblyPackage(deps.[0], includeAssemblyImage = true)

let result : AssemblyLoadInfo = vagrant.LoadAssemblyPackage(pkg)

vagrant.Pickler.Pickle f