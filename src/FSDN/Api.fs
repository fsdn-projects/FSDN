module Api

open System
open System.Runtime.Serialization
open Suave
open Suave.Operators
open Suave.Filters
open FSharpApiSearch

module Response =

  [<DataContract>]
  type Paging<'T> = {
    [<field: DataMember(Name = "values")>]
    Values: 'T []
  }

module Libraries =

  open Response
 
  [<DataContract>]
  type Library = {
    [<field: DataMember(Name = "name")>]
    Name: string
  }

  let find =
    {
      Values =
        FSharpApiSearchClient.DefaultTargets
        |> List.map (fun x -> { Name = x })
        |> List.toArray
    }

module Api =

  // copy from https://github.com/hafuu/FSharpApiSearch/blob/7acdbcf0b0a7f3331e00d8ebeea816dfab9492ea/src/FSharpApiSearch.Console/Program.fs#L58
  // The MIT License (MIT)
  // Copyright (c) 2015 MIYAZAKI Shohei
  
  let propertyKindText = function
  | PropertyKind.GetSet -> "get set"
  | PropertyKind.Set -> "set"
  | PropertyKind.Get -> "get"

  let apiKindText = function
  | ApiKind.Constructor -> "constructor"
  | ApiKind.ModuleValue -> "module value"
  | ApiKind.StaticMethod -> "static method"
  | ApiKind.StaticProperty prop -> sprintf "static property with %s" (propertyKindText prop)
  | ApiKind.InstanceMethod -> "instance method"
  | ApiKind.InstanceProperty prop -> sprintf "instance property with %s" (propertyKindText prop)
  | ApiKind.Field -> "field"

  // end

  [<DataContract>]
  type Api = {
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
    Api: Api
  }

  open Response

  let toSerializable (results: FSharpApiSearch.SearchResult seq) =
    {
      Values =
        results
        |> Seq.map (fun result ->
          {
            Distance = result.Distance
            Api =
              {
                Name = result.Api.Name
                Kind = apiKindText result.Api.Kind
                Signature = Signature.display result.Api.Signature
              }
          })
        |> Seq.toArray
    }

  let client = FSharpApiSearchClient(FSharpApiSearchClient.DefaultTargets, FSharpApiSearchClient.DefaultReferences)

  let trySearch opts (query: string) =
    try
      client.Search(query, opts)
      |> Seq.filter (fun x -> x.Distance < 3)
      |> Choice1Of2
    with e -> Choice2Of2 e

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

    let parse (req: HttpRequest) =
      match (req.queryParam "strict", req.queryParam "similarity") with
      | (Choice1Of2 strict, Choice1Of2 similarity) -> fun opts ->
        {
          StrictQueryVariable = OptionStatus.parseOrDefault Enabled strict
          SimilaritySearching = OptionStatus.parseOrDefault Disabled similarity
        }
      | (Choice1Of2 strict, Choice2Of2 _) -> fun opts ->
        { opts with StrictQueryVariable = OptionStatus.parseOrDefault Enabled strict }
      | (Choice2Of2 _, Choice1Of2 similarity) -> fun opts ->
        { opts with SimilaritySearching = OptionStatus.parseOrDefault Disabled similarity }
      | (Choice2Of2 _, Choice2Of2 _) -> id
      <| SearchOptions.defaultOptions

let validate (req: HttpRequest) key validate (f: string -> WebPart) : WebPart =
  cond (req.queryParam key)
    (fun param ->
      match validate param with
      | Choice1Of2 param -> f param
      | Choice2Of2 msg -> Suave.RequestErrors.BAD_REQUEST msg
    )
    (Suave.RequestErrors.BAD_REQUEST <| sprintf "Query parameter \"%s\" does not exist." key)

let search logger opts query =
  match Api.trySearch opts query with
  | Choice1Of2 results ->
    results
    |> Api.toSerializable
    |> Json.toJson
    |> Suave.Successful.ok
  | Choice2Of2 e ->
    Log.infoe logger "/api/search" (Logging.TraceHeader.mk None None) e "search error"
    RequestErrors.BAD_REQUEST e.Message

let app logger : WebPart =
  choose [
    GET >=> choose [
      path "/api/libraries"
        >=> (Libraries.find |> Json.toJson |> Suave.Successful.ok)
      path "/api/search" >=>
        request (fun req ->
          validate req "query"
            (fun param ->
              if String.IsNullOrEmpty(param) then Choice2Of2 "Search query require non empty string."
              else Choice1Of2 param)
            (search logger (Api.SearchOptions.parse req))
        )
    ]
  ]
