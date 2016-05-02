namespace FSDN

open System.Collections.Generic
open System.Runtime.Serialization
open Microsoft.FSharp.Reflection
open FSharpApiSearch

[<DataContract>]
type FSharpApi = {
  [<field: DataMember(Name = "name")>]
  Name: string
  [<field: DataMember(Name = "kind")>]
  Kind: string
  [<field: DataMember(Name = "signature")>]
  Signature: string
}

[<DataContract>]
type SearchResult = {
  [<field: DataMember(Name = "distance")>]
  Distance: int
  [<field: DataMember(Name = "api")>]
  Api: FSharpApi
}

[<DataContract>]
type SearchInformation = {
  [<field: DataMember(Name = "target_assemblies")>]
  Targets: string []
  [<field: DataMember(Name = "search_options")>]
  RawOptions: Dictionary<string, string>
  [<field: DataMember(Name = "query")>]
  Query: string
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FSharpApi =

  let toSerializable (results: FSharpApiSearch.Result seq) =
    {
      Values =
        results
        |> Seq.map (fun result ->
          {
            Distance = result.Distance
            Api =
              {
                Name = ReverseName.toString result.Api.Name
                Kind = result.Api.Kind.Print()
                Signature = result.Api.Signature.Print()
              }
          })
        |> Seq.toArray
    }

  module OptionStatus =

    let tryParse = function
    | "enabled" -> Some Enabled
    | "disabled" -> Some Disabled
    | _ -> None

    let parseOrDefault defaultValue value =
      match tryParse value with
      | Some value -> value
      | None -> defaultValue

  module SearchOptions =

    open Suave

    [<Literal>]
    let Strict = "strict"

    [<Literal>]
    let Similarity = "similarity"

    [<Literal>]
    let IgnoreArgStyle = "ignore_arg_style"

    let parse info =
      let update opt (KeyValue(name, value)) =
        match name with
        | Strict -> { opt with StrictQueryVariable = OptionStatus.parseOrDefault SearchOptions.defaultOptions.StrictQueryVariable value }
        | Similarity -> { opt with SimilaritySearching = OptionStatus.parseOrDefault SearchOptions.defaultOptions.SimilaritySearching value }
        | IgnoreArgStyle -> { opt with IgnoreArgumentStyle = OptionStatus.parseOrDefault SearchOptions.defaultOptions.IgnoreArgumentStyle value }
        | _ -> opt
      info.RawOptions
      |> Seq.fold update SearchOptions.defaultOptions

    let defaultRawOptions =
      let toString (x: OptionStatus) =
        match FSharpValue.GetUnionFields(x, typeof<OptionStatus>) with
        | case, _ -> case.Name.ToLower()
      Dictionary<string, string>(
        [
          (Strict, SearchOptions.defaultOptions.StrictQueryVariable)
          (Similarity, SearchOptions.defaultOptions.SimilaritySearching)
          (IgnoreArgStyle, SearchOptions.defaultOptions.IgnoreArgumentStyle)
        ]
        |> Seq.map (fun (name, x) -> (name, toString x))
        |> dict
      )

  let trySearch database info =
    let client = FSharpApiSearchClient(info.Targets, database)
    try
      client.Search(info.Query, SearchOptions.parse info)
      |> Seq.filter (fun x -> x.Distance < 3)
      |> Choice1Of2
    with e -> Choice2Of2 e
