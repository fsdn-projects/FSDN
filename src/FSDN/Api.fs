module FSDN.Api

open System
open System.Runtime.Serialization
open Suave
open Suave.Logging
open Suave.Logging.Message
open Suave.Operators
open Suave.Filters

type ValidationBuilder(path, logger: Logger) =
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
    | Choice2Of2 (e: exn) ->
      logger.info (eventX "validation error" >> addExn e >> setSingleName path)
      RequestErrors.BAD_REQUEST e.Message

module Search =

  [<Literal>]
  let Path = "/api/search"

  module Exclusion =

    open FParsec

    let space = pstring "+"

    let anyString = many1Chars (noneOf "+")

    let parser = (eof |>> fun _ -> []) <|> sepBy1 anyString space

    let parse str =
      match run parser str with
      | Success(result, _, _) -> Choice1Of2 result
      | Failure(msg, _, _) -> Choice2Of2 msg

    [<Literal>]
    let Literal = "exclusion"

  module Query =

    [<Literal>]
    let Literal = "query"

  let apply database generator logger (req: HttpRequest) =
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
    let validation = ValidationBuilder(Path, logger)
    let inline validateRet key = validate key validation.Return
    validation {
      let! query = validateRet Query.Literal
      let! excluded = validateOpt Exclusion.Literal Exclusion.parse
      let! respectNameDifference = validateRet SearchOptionLiteral.RespectNameDifference
      let! greedyMatching = validateRet SearchOptionLiteral.GreedyMatching
      let! ignoreParameterStyle = validateRet SearchOptionLiteral.IgnoreParameterStyle
      let! ignoreCase = validateRet SearchOptionLiteral.IgnoreCase 
      let! swapOrder = validateRet SearchOptionLiteral.SwapOrder 
      let! complement = validateRet SearchOptionLiteral.Complement
      let! language = validateOpt SearchOptionLiteral.Language validation.Return
      let! singleLetterAsVariable = validateRet SearchOptionLiteral.SingleLetterAsVariable
      let! limit = validate "limit" (fun x ->
        match Int32.TryParse(x) with
        | true, v -> Choice1Of2 v
        | false, _ -> Choice2Of2 """Query parameter "limit" should require int vaue."""
      )
      let info = {
        Targets =
          generator.Packages
          |> Array.collect (fun x -> if List.exists ((=) x.Name) excluded then [||] else x.Assemblies)
        RawOptions =
          {
            RespectNameDifference = respectNameDifference
            GreedyMatching = greedyMatching
            IgnoreParameterStyle = ignoreParameterStyle
            IgnoreCase = ignoreCase
            SwapOrder = swapOrder
            Complement = complement
            Language = language
            SingleLetterAsVariable = singleLetterAsVariable
          }
        Query = query
        Limit = limit
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
      path Search.Path >=>
        request (Search.apply database generator logger)
    ]
  ]
