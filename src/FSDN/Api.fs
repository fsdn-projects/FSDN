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

let search database logger req =
  let inner query =
    {
      Database = database
      Targets = FSharpApiSearch.FSharpApiSearchClient.DefaultTargets
      Options = (FSharpApi.SearchOptions.parse req)
      Query = query
    }
    |> FSharpApi.trySearch
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

let app database logger : WebPart =
  choose [
    GET >=> choose [
      path "/api/libraries"
        >=> (Libraries.all |> Json.toJson |> Suave.Successful.ok)
      path "/api/search" >=>
        request (search database logger)
    ]
  ]
