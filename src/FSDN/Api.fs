module FSDN.Api

open System
open Suave
open Suave.Logging
open Suave.Logging.Message
open Suave.Operators
open Suave.Filters
open Utf8Json

type ValidationBuilder(path, logger: Logger) =
  member inline __.Bind(x, f) = Result.bind f x
  member inline __.Return(x) = Ok x
  member inline __.Source(x: Result<_, exn>) = x
  member __.Source(x: Result<_, string>) =
    match x with
    | Ok x -> Ok x
    | Result.Error msg -> ArgumentException(msg) :> exn |> Result.Error
  member __.Delay(f) = f
  member __.Run(f) =
    match f () with
    | Ok result -> Suave.Successful.ok result
    | Result.Error (e: exn) ->
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
      | Success(result, _, _) -> Result.Ok result
      | Failure(msg, _, _) -> Result.Error msg

    [<Literal>]
    let Literal = "exclusion"

  module Query =

    [<Literal>]
    let Literal = "query"

  let apply database generator logger (req: HttpRequest) =
    let validate key (f: string -> Result<'T, string>) =
      match req.queryParam key with
      | Choice1Of2 param when String.IsNullOrEmpty(param) ->
        Result.Error (sprintf """Query parameter: "%s" requires non empty string.""" key)
      | Choice1Of2 param -> f param
      | Choice2Of2 _ -> Result.Error (sprintf """Query parameter: "%s" does not exist.""" key)
    let validateOpt key (f: string -> Result<'T, string>) =
      match req.queryParam key with
      | Choice1Of2 param -> f param
      | Choice2Of2 _ -> Result.Error (sprintf """Query parameter: "%s" does not exist.""" key)
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
        | true, v -> Ok v
        | false, _ -> Result.Error """Query parameter "limit" should require int vaue."""
      )
      let info = {
        Targets =
          generator.Packages.[language]
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
      let! language, query, results = FSharpApi.trySearch database info
      return
        FSharpApi.toSerializable generator language query results
        |> JsonSerializer.Serialize
    }

module Assembly =
  [<Literal>]
  let Path = "/api/assemblies"

  let apply generator (req: HttpRequest) =
    let packages =
      match req.queryParamOpt SearchOptionLiteral.Language with
      | Some (_, Some lang) -> generator.Packages.[lang]
      | Some (_, None) | None -> generator.Packages |> Seq.map (fun (KeyValue(_, v)) -> v) |> Array.concat |> Array.distinct
    System.Linq.Enumerable.OrderBy(packages, fun p -> (not p.Standard, p.Name))
    |> JsonSerializer.Serialize
    |> Suave.Successful.ok

let app database generator logger : WebPart =
  choose [
    GET >=> choose [
      path Assembly.Path
        >=> request (Assembly.apply generator)
      path Search.Path >=>
        request (Search.apply database generator logger)
    ]
  ]
