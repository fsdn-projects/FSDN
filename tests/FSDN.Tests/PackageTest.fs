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
  Standard: true
TargetGroups:
- GroupName: "Fake"
  Targets:
    - Name : "Fake.Core.Target"
      Assemblies: [ "Fake.Core.Target" ]
    - Name : "Fake.Core.String"
      Assemblies: [ "Fake.Core.String" ]
"""
  let expected : PackageInput =
    let targets : TargetPackage[] = [|
      { Name = "FSharp.Core"; Assemblies = [| "FSharp.Core" |]; Standard = false }
      { Name = "System.Core"; Assemblies = [| "System.Core" |]; Standard = true }
    |]
    let targetGroups: TargetPackageGroup[] = [|
      {
        GroupName = "Fake"
        Targets =
          [|
            { Name = "Fake.Core.Target"; Assemblies = [| "Fake.Core.Target" |]; Standard = false }
            { Name = "Fake.Core.String"; Assemblies = [| "Fake.Core.String" |]; Standard = false }
          |]
        Standard = false
      }
    |]
    {
      Languages = Map.ofList [ ("fsharp", [| "FSharp.Core"; "System.Core" |]); ("csharp", [| "System.Core" |]) ]
      Targets = targets
      TargetGroups = targetGroups
    }
  do!
    Yaml.tryLoad<PackageInput> yml
    |> assertEquals (Some expected)
}