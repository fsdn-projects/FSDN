module FSDN.Program

open System
open System.IO
open System.Net
open Suave
open Suave.Web
open Suave.Logging
open Suave.Operators
open Suave.Filters
open Suave.Files
open Argu
open FSharpApiSearch
open Suave.Writers

type Args =
  | Port of Sockets.Port
  | Home_Directory of string
  | Log_Level of string
  | FSharp_Link of string
  | DOTNET_API_Browser_Link of string
  | FParsec_Link of string
with
  interface IArgParserTemplate with
    member this.Usage =
      match this with
      | Port _ -> "specify a primary port."
      | Home_Directory _ -> "specify a home or root diretory."
      | Log_Level _ -> "specify log level."
      | FSharp_Link _ -> "specify official F# library document link."
      | DOTNET_API_Browser_Link _ -> "specify .NET API Browser link."
      | FParsec_Link _ -> "specify FParsec reference link."

let logger (args: ParseResults<Args>) =
  let level =
    args.TryPostProcessResult(<@ Log_Level @>, LogLevel.ofString)
    |> Option.defaultValue LogLevel.Warn
  Targets.create level [|"FSDN"|]

let notFound homeDir ctx = asyncOption {
  let! ctx = browseFile homeDir "404.html" ctx
  return { ctx with response = { ctx.response with status = HTTP_404.status } }
}

let fileRequest homeDir =
  let notFound = notFound homeDir
  choose [
    path "/" >=> browseFile homeDir "index.html"
    browseHome
  ]

let app database generator homeDir logger : WebPart =
  choose [
    GET >=> fileRequest homeDir
    HEAD >=> fileRequest homeDir
    Api.app database generator logger
    notFound homeDir
  ]
  >=> addHeader "Cache-Control" "no-cache, no-store"
  >=> log logger logFormat

let serverConfig port homeDir logger = {
  defaultConfig with
    bindings = [ HttpBinding.create HTTP IPAddress.Any port ]
    homeFolder = Some homeDir
    logger = logger
}

let parser = ArgumentParser.Create<Args>()

[<EntryPoint>]
let main args =
  let args = parser.Parse(args)
  let homeDir = DirectoryInfo(args.GetResult(<@ Home_Directory @>, ".")).FullName
  let logger = logger args
  let port = args.GetResult(<@ Port @>, 8083us)
  let config = serverConfig port homeDir logger
  let database =
    Path.Combine(homeDir, Database.databaseName)
    |> Database.loadFromFile
  let packages =
    Directory.GetFiles(homeDir, "packages.*.yml")
    |> Array.choose (fun path ->
      let lang =
        let fileName = Path.GetFileNameWithoutExtension(path)
        fileName.Substring(fileName.IndexOf('.') + 1)
      Package.load path
      |> Option.map (fun packages -> lang, packages)
    )
    |> Map.ofArray
  let generator = {
    FSharp =
      args.GetResult(<@ FSharp_Link @>, "https://msdn.microsoft.com/visualfsharpdocs/conceptual/")
      |> FSharpApiSearch.LinkGenerator.fsharp
    DotNetApiBrowser =
      let baseUrl = args.GetResult(<@ DOTNET_API_Browser_Link @>, "https://docs.microsoft.com/en-us/dotnet/api/")
      let view = "netframework-4.6.2"
      FSharpApiSearch.LinkGenerator.dotNetApiBrowser baseUrl view
    FParsec =
      args.GetResult(<@ FParsec_Link @>, "http://www.quanttec.com/fparsec/reference/")
      |> FSharpApiSearch.LinkGenerator.fparsec
    Packages = packages
  }
  let app = app database generator homeDir logger
  startWebServer config app
  0
