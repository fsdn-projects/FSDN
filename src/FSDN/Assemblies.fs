namespace FSDN

open System.Runtime.Serialization
open FSharpApiSearch

[<DataContract>]
type TargetAssembly = {
  [<field: DataMember(Name = "name")>]
  Name: string
}

module Assemblies =

  let all =
    {
      Values =
        FSharpApiSearchClient.DefaultTargets
        |> List.map (fun x -> { Name = x })
        |> List.toArray
    }
