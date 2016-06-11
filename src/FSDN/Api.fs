module FSDN.Api

open System
open System.Runtime.Serialization
open Suave
open Suave.Operators
open Suave.Filters

module Query =

  let erroroMessage = "Search query requires non empty string."

  open FParsec

  let space = pstring "+"

  let anyString = many1Chars (noneOf "+" <|> (attempt (pchar '+' .>> notFollowedBy (pchar '-'))))

  let query = notEmpty anyString <?> erroroMessage

  let parser = query .>>. (many (space >>. pstring "-" >>. anyString))

  let parse str =
    match run parser str with
    | Success(result, _, _) -> Choice1Of2 result
    | Failure(msg, _, _) -> Choice2Of2 msg

let validate (req: HttpRequest) key (validate: string -> Choice<'T, string>) (f: 'T -> WebPart) : WebPart =
  cond (req.queryParam key)
    (fun param ->
      match validate param with
      | Choice1Of2 param -> f param
      | Choice2Of2 msg -> Suave.RequestErrors.BAD_REQUEST msg
    )
    (Suave.RequestErrors.BAD_REQUEST <| sprintf "Query parameter \"%s\" does not exist." key)

let search database (assemblies: TargetAssembly []) logger (req: HttpRequest) =
  let getOrEmpty name =
    match req.queryParam name with
    | Choice1Of2 param -> param
    | Choice2Of2 _ -> ""
  let inner (query, excluded) =
    {
      Targets = assemblies |> Array.choose (fun x -> if List.exists ((=) x.Name) excluded then None else Some x.Name)
      RawOptions =
        {
          Strict = getOrEmpty SearchOptionLiteral.Strict
          Similarity = getOrEmpty SearchOptionLiteral.Similarity
          IgnoreArgStyle = getOrEmpty SearchOptionLiteral.IgnoreArgStyle
        }
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
      if String.IsNullOrEmpty(param) then Choice2Of2 Query.erroroMessage
      else Query.parse param)
    inner

let app database assemblies logger : WebPart =
  choose [
    GET >=> choose [
      path "/api/assemblies"
        >=> ({ Values = assemblies } |> Json.toJson |> Suave.Successful.ok)
      path "/api/search" >=>
        request (search database assemblies logger)
    ]
  ]
