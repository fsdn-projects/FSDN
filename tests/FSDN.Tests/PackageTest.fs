module FSDN.Tests.PackageTest

open System.Xml.Linq
open Persimmon
open UseTestNameByReflection
open FSDN
open FsYaml

let ``shoud load target package`` = test {
  let yml = """
Languages:
  "csharp":
  - "System.Core"
  "fsharp":
  - "FSharp.Core"
  - "System.Core"
Targets:
- Name: "FSharp.Core"
  Assemblies: [ "FSharp.Core" ]
- Name: "System.Core"
  Assemblies: [ "System.Core" ]
  Standard: true"""
  let expected : PackageInput =
    let targets : TargetPackage[] = [| { Name = "FSharp.Core"; Assemblies = [| "FSharp.Core" |]; Standard = false }; { Name = "System.Core"; Assemblies = [| "System.Core" |]; Standard = true } |]
    { Languages = Map.ofList [ ("fsharp", [| "FSharp.Core"; "System.Core" |]); ("csharp", [| "System.Core" |]) ]; Targets = targets }
  do!
    Yaml.tryLoad<PackageInput> yml
    |> assertEquals (Some expected)
}