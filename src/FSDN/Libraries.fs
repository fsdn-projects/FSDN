namespace FSDN

open System.Runtime.Serialization
open FSharpApiSearch

[<DataContract>]
type TargetLibrary = {
  [<field: DataMember(Name = "name")>]
  Name: string
}

module Libraries =

  let find =
    {
      Values =
        FSharpApiSearchClient.DefaultTargets
        |> List.map (fun x -> { Name = x })
        |> List.toArray
    }
