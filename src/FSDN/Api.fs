module FSDN.Api

open System
open System.Runtime.Serialization
open Suave
open Suave.Operators
open Suave.Filters

module Exclusion =

  open FParsec

  let space = pstring "+"

  let anyString = many1Chars (noneOf "+")

  let parser = (eof |>> fun _ -> []) <|> sepBy1 anyString space

  let parse str =
    match run parser str with
    | Success(result, _, _) -> Choice1Of2 result
    | Failure(msg, _, _) -> Choice2Of2 msg

type ValidationBuilder(logger) =
  member __.Bind(x, f) =
    match x with
    | Choice1Of2 x -> f x
    | Choice2Of2 e -> Choice2Of2 e
  member __.Return(x) = Choice1Of2 x
  member __.Source(x: Choice<_, exn>) = x
  member __.Source(x: Choice<_, string>) =
    match x with
    | Choice1Of2 x -> Choice1Of2 x
    | Choice2Of2 msg -> ArgumentException(msg) :> exn |> Choice2Of2
  member __.Delay(f) = f
  member __.Run(f) =
    match f () with
    | Choice1Of2 result -> Suave.Successful.ok result
    | Choice2Of2 e ->
      Log.infoe logger "/api/search" (Logging.TraceHeader.mk None None) e "failed to search"
      RequestErrors.BAD_REQUEST e.Message

let search database generator logger (req: HttpRequest) =
  let validate key (f: string -> Choice<'T, string>) =
    match req.queryParam key with
    | Choice1Of2 param when String.IsNullOrEmpty(param) ->
      Choice2Of2 (sprintf """Query parameter: "%s" requires non empty string.""" key)
    | Choice1Of2 param -> f param
    | Choice2Of2 _ -> Choice2Of2 (sprintf """Query parameter: "%s" does not exist.""" key)
  let validateOpt key (f: string -> Choice<'T, string>) =
    match req.queryParam key with
    | Choice1Of2 param -> f param
    | Choice2Of2 _ -> Choice2Of2 (sprintf """Query parameter: "%s" does not exist.""" key)
  let validation = ValidationBuilder(logger)
  validation {
    let! query = validate "query" validation.Return
    let! excluded = validateOpt "exclusion" Exclusion.parse
    let! respectNameDifference = validate SearchOptionLiteral.RespectNameDifference validation.Return
    let! greedyMatching = validate SearchOptionLiteral.GreedyMatching validation.Return
    let! ignoreParameterStyle = validate SearchOptionLiteral.IgnoreParameterStyle validation.Return
    let! ignoreCase = validate SearchOptionLiteral.IgnoreCase validation.Return
    let info =
      {
        Targets =
          generator.Packages
          |> Array.collect (fun x -> if List.exists ((=) x.Name) excluded then [||] else x.Assemblies)
        RawOptions =
          {
            RespectNameDifference = respectNameDifference
            GreedyMatching = greedyMatching
            IgnoreParameterStyle = ignoreParameterStyle
            IgnoreCase = ignoreCase
          }
        Query = query
      }
    let! result = FSharpApi.trySearch database info
    return
      result
      |> FSharpApi.toSerializable generator
      |> Json.toJson
  }

let app database generator logger : WebPart =
  choose [
    GET >=> choose [
      path "/api/assemblies"
        >=> ({ Values = generator.Packages } |> Json.toJson |> Suave.Successful.ok)
      path "/api/search" >=>
        request (search database generator logger)
    ]
  ]
