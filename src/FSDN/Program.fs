open System.IO
open System.Net
open Suave
open Suave.Web
open Suave.Logging
open Suave.Operators
open Suave.Filters
open Suave.Files
open Argu

type Args =
  | Port of Sockets.Port
  | Home_Directory of string
  | Log_Level of string
with
  interface IArgParserTemplate with
    member this.Usage =
      match this with
      | Port _ -> "specify a primary port."
      | Home_Directory _ -> "specify a home or root diretory."
      | Log_Level _ -> "specify log level."

let configAndApp (args: ParseResults<Args>) : (SuaveConfig * WebPart) =

  let home = DirectoryInfo(args.GetResult(<@ Home_Directory @>, ".")).FullName
  let logger =
    match args.TryPostProcessResult(<@ Log_Level @>, LogLevel.FromString) with
    | Some l -> l
    | None -> LogLevel.Warn
    |> Loggers.ConsoleWindowLogger
 
  let notFound ctx = asyncOption {
    let! ctx = browseFile home "404.html" ctx
    return { ctx with response = { ctx.response with status = HTTP_404 } }
  }
  
  let app =
    choose [
      log logger logFormat >=> never
      GET >=> choose [
        path "/" >=> browseFile home "index.html"
        pathScan "/%s.html" (fun name -> tryThen (name |> sprintf "%s.html" |> browseFile home) notFound)
        pathScan "/%s.js" (browseFile home << sprintf "%s.js")
        pathScan "/%s.js.map" (browseFile home << sprintf "%s.js.map")
      ]
      Api.app
    ]

  let serverConfig = {
    defaultConfig with
      bindings = [ HttpBinding.mk HTTP IPAddress.Loopback (args.GetResult(<@ Port @>, 8083us)) ]
      homeFolder =
        args.TryGetResult(<@ Home_Directory @>)
        |> Option.map (fun d -> DirectoryInfo(d).FullName)
      logger = logger
  }

  (serverConfig, app)

let parser = ArgumentParser.Create<Args>()

[<EntryPoint>]
let main args =
  let args = parser.Parse(args)
  startWebServer <|| configAndApp args
  0
