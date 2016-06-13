module FSDN.Tests.PackageTest

open System.Xml.Linq
open Persimmon
open UseTestNameByReflection
open FSDN
open FsYaml

let ``shoud load target package`` = test {
  let yml = """- Name: FSharp.Core
  Assemblies: [FSharp.Core]"""
  let expected = {
    TargetPackage.Name = "FSharp.Core"
    Assemblies = [|"FSharp.Core"|]
  }
  do!
    Yaml.tryLoad<TargetPackage []> yml
    |> assertEquals (Some [|expected|])
}

let ``shoud load nuget package`` = test {
  let yml = """- Name: FSharp.Core
  Standard: true
  Version: 4.0.0.1
  IconUrl: http://fsharp.org/img/logo.png
  Assemblies: [FSharp.Core]"""
  let expected = {
    NuGetPackage.Name = "FSharp.Core"
    Standard = true
    Version = "4.0.0.1"
    IconUrl = "http://fsharp.org/img/logo.png"
    Assemblies = [|"FSharp.Core"|]
  }
  do!
    Yaml.tryLoad<NuGetPackage []> yml
    |> assertEquals (Some [|expected|])
}
