namespace FSDN

open System.Runtime.Serialization
open FSharpApiSearch

[<DataContract>]
type TargetAssembly = {
  [<field: DataMember(Name = "name")>]
  Name: string
  [<field: DataMember(Name = "checked")>]
  Standard: bool
  [<field: DataMember(Name = "version")>]
  Version: string
  [<field: DataMember(Name = "icon_url")>]
  IconUrl: string
}

module Assemblies =

  let all asms =
    asms
    |> Array.append (FSharpApiSearchClient.DefaultTargets
      |> List.map (fun x -> { Name = x; Standard = true; Version = ""; IconUrl = "" })
      |> List.toArray)
    |> Array.distinct
