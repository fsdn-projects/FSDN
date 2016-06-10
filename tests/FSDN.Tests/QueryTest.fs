module FSDN.Tests.Api.QueryTest

open Persimmon
open UseTestNameByReflection
open FSDN.Api

let ``parse query`` = parameterize {
  source [
    ("'a", ("'a", []))
    ("Name: _", ("Name: _", []))
    ("Name: _+-mscorlib", ("Name: _", ["mscorlib"]))
    ("Name: _+-mscorlib+-FSharp.Core", ("Name: _", ["mscorlib"; "FSharp.Core"]))
  ]
  run (fun (query, expected) -> test {
    do! assertEquals (Choice1Of2(expected)) (Query.parse query) 
  })
}

let ``fail to parse`` = parameterize {
  source [
    ""
    "+-mscorlib"
  ]
  run (fun query -> test {
    do!
      match Query.parse query with
      | Choice1Of2 x -> fail <| sprintf "expected fail, but was %A" x
      | Choice2Of2 _ -> pass ()
  })
}
