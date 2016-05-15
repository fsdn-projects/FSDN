namespace FSDN

open System.Runtime.Serialization
open FSharpApiSearch

[<DataContract>]
type TargetAssembly = {
  [<field: DataMember(Name = "name")>]
  Name: string
  [<field: DataMember(Name = "checked")>]
  Standard: bool
}

module Assemblies =

  let all asms =
    asms
    |> Array.append (FSharpApiSearchClient.DefaultTargets
      |> List.map (fun x -> { Name = x; Standard = true })
      |> List.toArray)
    |> Array.distinct
