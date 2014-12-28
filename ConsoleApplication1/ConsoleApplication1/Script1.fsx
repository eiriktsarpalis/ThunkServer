let inline map (alg : ^Alg) (f : ^t -> ^s) (ts : ^ts) : ^ss =
    (^Alg : (member Map : (^t -> ^s) * ^ts -> ^ss) (alg, f, ts))

let inline filter (alg : ^Alg) (f : ^t -> bool) (ts : ^ts) : ^ts =
    (^Alg : (member Filter : (^t -> bool) * ^ts -> ^ts) (alg, f, ts))

let inline reduce (alg : ^Alg) (f : ^t -> ^t -> ^t) (ts : ^ts) : ^t =
    (^Alg : (member Reduce : (^t -> ^t -> ^t) * ^ts -> ^t) (alg, f, ts))

// type signature is meaningless, to be consumed by machine only
let inline test alg xs =
    xs
    |> filter alg (fun i -> i % 2 = 0)
    |> map alg (fun i -> i + 1)
    |> map alg (fun i -> i * i)
    |> filter alg (fun i -> i < 50)
    |> reduce alg (+)

type ListAlg() =
    member __.Map  (f : 'T -> 'S, ts : 'T list) = List.map f ts
    member __.Filter (f : 'T -> bool, ts : 'T list) = List.filter f ts
    member __.Reduce (f : 'T -> 'T -> 'T, ts : 'T list) = List.reduce f ts

let list = new ListAlg()

test list [1 .. 100]