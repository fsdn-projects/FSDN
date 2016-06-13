namespace FSDN

open System.IO
open System.Runtime.Serialization
open FSharpApiSearch
open FsYaml

[<DataContract>]
type NuGetPackage = {
  [<field: DataMember(Name = "name")>]
  Name: string
  [<field: DataMember(Name = "checked")>]
  Standard: bool
  [<field: DataMember(Name = "version")>]
  Version: string
  [<field: DataMember(Name = "icon_url")>]
  IconUrl: string
  [<field: DataMember(Name = "assemblies")>]
  Assemblies: string []
}

type TargetPackage = {
  Name: string
  Assemblies: string []
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
    |> Yaml.load<TargetPackage []>
