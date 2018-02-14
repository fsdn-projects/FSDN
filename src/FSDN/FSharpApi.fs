namespace FSDN

open System.Runtime.Serialization
open FSharpApiSearch
open System

module ApiSearchOptions = FSharpApiSearch.SearchOptions

module SearchOptionLiteral =

  [<Literal>]
  let RespectNameDifference = "respect_name_difference"

  [<Literal>]
  let GreedyMatching = "greedy_matching"

  [<Literal>]
  let IgnoreParameterStyle = "ignore_parameter_style"

  [<Literal>]
  let IgnoreCase = "ignore_case"

  [<Literal>]
  let SwapOrder = "swap_order"

  [<Literal>]
  let Complement = "complement"

  [<Literal>]
  let Language = "language"

  [<Literal>]
  let SingleLetterAsVariable = "single_letter_as_variable"

type ApiName = {
  Id: string
  [<DataMember(Name = "class_name")>]
  Class: string
  Namespace: string
}

type TypeName = {
  Name: string
  ColorId: int option
}

type LanguageApi = {
  Name: ApiName
  Kind: string
  Signature: TypeName []
  TypeConstraints: string
  Assembly: string
  XmlDoc: string
  Link: string
}

type SearchResult = {
  Distance: int
  Api: LanguageApi
}

type ResultResponse = {
  Values: SearchResult []
  Query: TypeName []
}

type SearchOptions = {
  RespectNameDifference: string
  GreedyMatching: string
  IgnoreParameterStyle: string
  IgnoreCase: string
  SwapOrder: string
  Complement: string
  Language: string
  SingleLetterAsVariable: string
}

type SearchInformation = {
  [<DataMember(Name = "target_assemblies")>]
  Targets: string []
  [<DataMember(Name = "search_options")>]
  RawOptions: SearchOptions
  Query: string
  Limit: int
}

module FSharpApi =
  let inline private getOrEmpty value = Option.defaultValue "" value

  let languageToString = function
    | FSharp -> "fsharp"
    | CSharp -> "csharp"

  let private toTypeName (name, color) = {
    Name = name
    ColorId = color
  }

  let private toLanguageApi generator language (result: FSharpApiSearch.Result) =
    match language with
    | FSharp ->
      {
        Name =
          {
            Id = StringPrinter.FSharp.printApiName result.Api
            Namespace = StringPrinter.FSharp.printAccessPath None result.Api
            Class = StringPrinter.FSharp.printAccessPath (Some 1) result.Api
          }
        Kind = StringPrinter.FSharp.printKind result.Api
        Signature =
          (HtmlPrintHelper.signature result (Printer.FSharp.printSignature result.Api)).Text
          |> Array.map toTypeName
        TypeConstraints =
          StringPrinter.FSharp.tryPrintTypeConstraints result.Api
          |> getOrEmpty
        Assembly = result.AssemblyName
        XmlDoc = getOrEmpty result.Api.Document
        Link = ApiLinkGenerator.generate result generator (languageToString language) |> getOrEmpty
      }
    | CSharp ->
      {
        Name =
          {
            Id = StringPrinter.CSharp.printApiName result.Api
            Namespace = StringPrinter.CSharp.printAccessPath None result.Api
            Class = StringPrinter.CSharp.printAccessPath (Some 1) result.Api
          }
        Kind = StringPrinter.CSharp.printKind result.Api
        Signature =
          (HtmlPrintHelper.signature result (Printer.CSharp.printSignature result.Api)).Text
          |> Array.map toTypeName
        TypeConstraints =
          StringPrinter.CSharp.tryPrintTypeConstraints result.Api
          |> getOrEmpty
        Assembly = result.AssemblyName
        XmlDoc = getOrEmpty result.Api.Document
        Link = ApiLinkGenerator.generate result generator (languageToString language) |> getOrEmpty
      }

  let toSerializable (generator: ApiLinkGenerator) language (query: FSharpApiSearch.Query) (results: FSharpApiSearch.Result seq) =
    {
      Values =
        results
        |> Seq.map (fun result ->
          {
            Distance = result.Distance
            Api = toLanguageApi generator language result
          })
        |> Seq.toArray
      Query =
        (HtmlPrintHelper.query (QueryPrinter.print query)).Text
        |> Array.map toTypeName
    }

  module OptionStatus =

    let tryParse = function
    | "enabled" -> Some Enabled
    | "disabled" -> Some Disabled
    | _ -> None

  module SearchOptions =

    open Suave
    open SearchOptionLiteral

    let private applyOrDefault tryParse (lens: Lens<_, _>) value =
      match tryParse value with
      | Some value -> lens.Set value
      | None -> id

    let private applyStatus lens value =
      applyOrDefault OptionStatus.tryParse lens value

    let private applyLanguage lens value =
      applyOrDefault FSharpApiSearch.Language.tryParse lens value

    let apply info =
      SearchOptions.defaultOptions
      |> SearchOptions.Parallel.Set Enabled
      |> applyStatus ApiSearchOptions.RespectNameDifference info.RawOptions.RespectNameDifference
      |> applyStatus ApiSearchOptions.GreedyMatching info.RawOptions.GreedyMatching
      |> applyStatus ApiSearchOptions.IgnoreParameterStyle info.RawOptions.IgnoreParameterStyle
      |> applyStatus ApiSearchOptions.IgnoreCase info.RawOptions.IgnoreCase
      |> applyStatus ApiSearchOptions.SwapOrder info.RawOptions.SwapOrder
      |> applyStatus ApiSearchOptions.Complement info.RawOptions.Complement
      |> applyStatus ApiSearchOptions.SingleLetterAsVariable info.RawOptions.SingleLetterAsVariable
      |> applyLanguage ApiSearchOptions.Language info.RawOptions.Language

  let trySearch database info =
    let client = FSharpApiSearchClient(info.Targets, database)
    let options = SearchOptions.apply info
    try
      let query, results = client.Search(info.Query, options)
      let actual =
        results
        |> client.Sort
        |> Seq.truncate info.Limit
      let language = ApiSearchOptions.Language.Get options
      Ok(language, query, actual)
    with e -> Error e
