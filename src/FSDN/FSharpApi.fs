﻿namespace FSDN

open System.Collections.Generic
open System.Runtime.Serialization
open Microsoft.FSharp.Reflection
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
type FSharpApi = {
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
  Api: FSharpApi
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

  [<RequireQualifiedAccess>]
  module Name =

    let private printDisplayName = function
    | [] -> ""
    | ns -> ns |> Seq.map (fun (n: DisplayNameItem) -> n.Print()) |> Seq.rev |> String.concat "."

    let id (name: Name) =
      match name with
      | LoadingName _ -> failwith "LoadingName only use to generate database."
      | DisplayName xs -> printDisplayName (List.truncate 1 xs)

    let ``namespace`` (name: Name) =
      match name with
      | LoadingName _ -> failwith "LoadingName only use to generate database."
      | DisplayName xs ->
        match xs with
        | [] | [_] -> []
        | _ :: _  :: xs -> xs
        |> printDisplayName

    let className (name: Name) =
      match name with
      | LoadingName _ -> failwith "LoadingName only use to generate database."
      | DisplayName xs -> printDisplayName (xs |> List.skip 1 |> List.truncate 1)

  let inline private getOrEmpty value = Option.defaultValue "" value

  let toSerializable (generator: ApiLinkGenerator) (results: FSharpApiSearch.Result seq) =
    {
      Values =
        results
        |> Seq.map (fun result ->
          {
            Distance = result.Distance
            Api =
              {
                Name =
                  {
                    Id = Name.id result.Api.Name
                    Namespace = Name.``namespace`` result.Api.Name
                    Class = Name.className result.Api.Name
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
          })
        |> Seq.toArray
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
      |> applyLanguage ApiSearchOptions.Language info.RawOptions.Language

  let trySearch database info =
    let client = FSharpApiSearchClient(info.Targets, database)
    try
      client.Search(info.Query, SearchOptions.apply info)
      |> client.Sort
      |> Seq.truncate info.Limit
      |> Choice1Of2
    with e -> Choice2Of2 e
