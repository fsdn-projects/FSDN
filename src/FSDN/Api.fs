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

let search client logger opts query =
  match FSharpApi.trySearch client opts query with
  | Choice1Of2 results ->
    results
    |> FSharpApi.toSerializable
    |> Json.toJson
    |> Suave.Successful.ok
  | Choice2Of2 e ->
    Log.infoe logger "/api/search" (Logging.TraceHeader.mk None None) e "search error"
    RequestErrors.BAD_REQUEST e.Message

let app client logger : WebPart =
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
            (search client logger (FSharpApi.SearchOptions.parse req))
        )
    ]
  ]
