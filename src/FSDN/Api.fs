module Api

open System.Runtime.Serialization
open Suave
open Suave.Operators
open Suave.Filters
open FSharpApiSearch

[<DataContract>]
type Library = {
  [<field: DataMember(Name = "name")>]
  Name: string
}

[<DataContract>]
type Libraries = {
  [<field: DataMember(Name = "libraries")>]
  Values: Library []
}

let findLibraries () =
  {
    Values =
      FSharpApiSearchClient.DefaultTargets
      |> List.map (fun x -> { Name = x })
      |> List.toArray
  }
  |> Json.toJson
  |> Suave.Successful.ok

let app: WebPart =
  choose [
    GET >=> choose [
      path "/api/libraries"
        >=> (findLibraries ())
    ]
  ]
