
open System.Net
open Suave
open Suave.Web
open Suave.Logging
open Suave.Operators
open Suave.Filters
open Suave.Files

// TODO: get from config
let logger = Loggers.ConsoleWindowLogger LogLevel.Verbose

let app : WebPart =
  choose [
    log logger logFormat >=> never
    GET >=> path "/" >=> file "index.html"
  ]

let serverConfig =
  // TODO: get from config
  let port = "80" |> Sockets.Port.Parse
  { defaultConfig with bindings = [ HttpBinding.mk HTTP IPAddress.Loopback port ] }

[<EntryPoint>]
let main argv = 
  startWebServer serverConfig app
  0
