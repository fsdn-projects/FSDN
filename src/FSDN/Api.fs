module FSDN.Api

open System
open System.Runtime.Serialization
open Suave
open Suave.Operators
open Suave.Filters

let validate (req: HttpRequest) key validate (f: string -> WebPart) : WebPart =
  cond (req.queryParam key)
    (fun param ->
      match validate param with
      | Choice1Of2 param -> f param
      | Choice2Of2 msg -> Suave.RequestErrors.BAD_REQUEST msg
    )
    (Suave.RequestErrors.BAD_REQUEST <| sprintf "Query parameter \"%s\" does not exist." key)

let searchSimple database (assemblies: TargetAssembly []) logger req =
  let inner query =
    {
      Targets = assemblies |> Array.map (fun x -> x.Name)
      RawOptions = FSharpApi.SearchOptions.defaultRawOptions
      Query = query
    }
    |> FSharpApi.trySearch database
    |> function
    | Choice1Of2 results ->
      results
      |> FSharpApi.toSerializable
      |> Json.toJson
      |> Suave.Successful.ok
    | Choice2Of2 e ->
      Log.infoe logger "/api/search" (Logging.TraceHeader.mk None None) e "search error"
      RequestErrors.BAD_REQUEST e.Message
  validate req "query"
    (fun param ->
      if String.IsNullOrEmpty(param) then Choice2Of2 "Search query require non empty string."
      else Choice1Of2 param)
    inner

let search database logger (req: HttpRequest) =
  req.rawForm
  |> Json.fromJson<SearchInformation>
  |> FSharpApi.trySearch database
  |> function
  | Choice1Of2 results ->
    results
    |> FSharpApi.toSerializable
    |> Json.toJson
    |> Suave.Successful.ok
  | Choice2Of2 e ->
    Log.infoe logger "/api/search" (Logging.TraceHeader.mk None None) e "search error"
    RequestErrors.BAD_REQUEST e.Message

let app database assemblies logger : WebPart =
  choose [
    GET >=> choose [
      path "/api/assemblies"
        >=> ({ Values = assemblies } |> Json.toJson |> Suave.Successful.ok)
      path "/api/search" >=>
        request (searchSimple database assemblies logger)
    ]
    POST >=> choose [
      path "/api/search" >=>
        request (search database logger)
    ]
  ]
