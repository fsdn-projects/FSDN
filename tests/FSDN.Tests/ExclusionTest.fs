module FSDN.Tests.Api.ExclusionTest

open Persimmon
open UseTestNameByReflection
open FSDN.Api

let ``parse query`` = parameterize {
  source [
    ("", [])
    ("mscorlib", ["mscorlib"])
    ("mscorlib+FSharp.Core", ["mscorlib"; "FSharp.Core"])
  ]
  run (fun (value, expected) -> test {
    do! assertEquals (Choice1Of2(expected)) (Exclusion.parse value) 
  })
}

let ``fail to parse`` = parameterize {
  source [
    "+mscorlib"
  ]
  run (fun query -> test {
    do!
      match Exclusion.parse query with
      | Choice1Of2 x -> fail <| sprintf "expected fail, but was %A" x
      | Choice2Of2 _ -> pass ()
  })
}
