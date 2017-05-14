﻿namespace FSDN

open System.Runtime.Serialization
open FSharpApiSearch
open FSharpApiSearch.Printer
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

[<DataContract>]
type ApiName = {
  [<field: DataMember(Name = "id")>]
  Id: string
  [<field: DataMember(Name = "class_name")>]
  Class: string
  [<field: DataMember(Name = "namespace")>]
  Namespace: string
}

[<DataContract>]
type LanguageApi = {
  [<field: DataMember(Name = "name")>]
  Name: ApiName
  [<field: DataMember(Name = "kind")>]
  Kind: string
  [<field: DataMember(Name = "signature")>]
  Signature: string
  [<field: DataMember(Name = "type_constraints")>]
  TypeConstraints: string
  [<field: DataMember(Name = "assembly")>]
  Assembly: string
  [<field: DataMember(Name = "xml_doc")>]
  XmlDoc: string
  [<field: DataMember(Name = "link")>]
  Link: string
}

[<DataContract>]
type SearchResult = {
  [<field: DataMember(Name = "distance")>]
  Distance: int
  [<field: DataMember(Name = "api")>]
  Api: LanguageApi
}

[<DataContract>]
type SearchOptions = {
  [<field: DataMember(Name = SearchOptionLiteral.RespectNameDifference)>]
  RespectNameDifference: string
  [<field: DataMember(Name = SearchOptionLiteral.GreedyMatching)>]
  GreedyMatching: string
  [<field: DataMember(Name = SearchOptionLiteral.IgnoreParameterStyle)>]
  IgnoreParameterStyle: string
  [<field: DataMember(Name = SearchOptionLiteral.IgnoreCase)>]
  IgnoreCase: string
  [<field: DataMember(Name = SearchOptionLiteral.SwapOrder)>]
  SwapOrder: string
  [<field: DataMember(Name = SearchOptionLiteral.Complement)>]
  Complement: string
  [<field: DataMember(Name = SearchOptionLiteral.Language)>]
  Language: string
}

[<DataContract>]
type SearchInformation = {
  [<field: DataMember(Name = "target_assemblies")>]
  Targets: string []
  [<field: DataMember(Name = "search_options")>]
  RawOptions: SearchOptions
  [<field: DataMember(Name = "query")>]
  Query: string
  [<field: DataMember(Name = "limit")>]
  Limit: int
}

module FSharpApi =
  let inline private getOrEmpty value = Option.defaultValue "" value

  let private toLanguageApi generator language (result: FSharpApiSearch.Result) =
    match language with
    | FSharp ->
      {
        Name =
          {
            Id = FSharp.printApiName result.Api
            Namespace = FSharp.printAccessPath None result.Api
            Class = FSharp.printAccessPath (Some 1) result.Api
          }
        Kind = FSharp.printKind result.Api
        Signature = FSharp.printSignature result.Api
        TypeConstraints =
          FSharp.tryPrintTypeConstraints result.Api
          |> getOrEmpty
        Assembly = result.AssemblyName
        XmlDoc = getOrEmpty result.Api.Document
        Link = ApiLinkGenerator.generate result generator |> getOrEmpty
      }
    | CSharp ->
      {
        Name =
          {
            Id = CSharp.printApiName result.Api
            Namespace = CSharp.printAccessPath None result.Api
            Class = CSharp.printAccessPath (Some 1) result.Api
          }
        Kind = CSharp.printKind result.Api
        Signature = CSharp.printSignature result.Api
        TypeConstraints =
          CSharp.tryPrintTypeConstraints result.Api
          |> getOrEmpty
        Assembly = result.AssemblyName
        XmlDoc = getOrEmpty result.Api.Document
        Link = ApiLinkGenerator.generate result generator |> getOrEmpty
      }

  let toSerializable (generator: ApiLinkGenerator) language (results: FSharpApiSearch.Result seq) =
    {
      Values =
        results
        |> Seq.map (fun result ->
          {
            Distance = result.Distance
            Api = toLanguageApi generator language result
          })
        |> Seq.toArray
    }

  module OptionStatus =

    let tryParse = function
    | "enabled" -> Some Enabled
    | "disabled" -> Some Disabled
    | _ -> None

  module SearchOptions =

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
      |> applyLanguage ApiSearchOptions.Language info.RawOptions.Language

  let trySearch database info =
    let client = FSharpApiSearchClient(info.Targets, database)
    let options = SearchOptions.apply info
    try
      let actual =
        client.Search(info.Query, options)
        |> client.Sort
        |> Seq.truncate info.Limit
      Choice1Of2(ApiSearchOptions.Language.Get options, actual)
    with e -> Choice2Of2 e
