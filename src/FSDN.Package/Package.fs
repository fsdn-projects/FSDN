namespace FSDN

open System.IO
open System.Runtime.Serialization
open FSharpApiSearch
open FsYaml

type NuGetPackage = {
  Name: string
  [<DataMember(Name = "checked")>]
  Standard: bool
  Version: string
  [<DataMember(Name = "icon_url")>]
  IconUrl: string
  [<DataMember(Name = "assemblies")>]
  Assemblies: string []
}

type TargetPackage = {
  Name: string
  Assemblies: string []
  Standard: bool
}
with
  static member DefaultStandard = false

type PackageInput = {
  Languages: Map<string, string []>
  Targets: TargetPackage []
}

module Package =

  let all asms =
    asms
    |> Array.append (FSharpApiSearchClient.DefaultTargets
      |> List.map (fun x -> { Name = x; Standard = true; Version = ""; IconUrl = ""; Assemblies = [|x|] })
      |> List.toArray)
    |> Array.distinct

  let load fileName =
    File.ReadAllText(fileName)
    |> Yaml.tryLoad<NuGetPackage []>

  let dump fileName packages =
    File.WriteAllText(fileName, Yaml.dump<NuGetPackage []> packages)

  let loadTargets fileName =
    File.ReadAllText(fileName)
    |> Yaml.load<PackageInput>
