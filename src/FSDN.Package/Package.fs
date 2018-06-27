namespace FSDN

open System.IO
open System.Runtime.Serialization
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

type NuGetPackageGroup = {
  [<DataMember(Name = "group_name")>]
  GroupName: string
  Packages: NuGetPackage[]
  [<DataMember(Name = "checked")>]
  Standard: bool
}

type TargetPackage = {
  Name: string
  Assemblies: string []
  Standard: bool
}
with
  static member DefaultStandard = false

type TargetPackageGroup = {
  GroupName: string
  Targets: TargetPackage []
  Standard: bool
}
with
  static member DefaultStandard = false

type PackageInput = {
  Languages: Map<string, string []>
  Targets: TargetPackage []
  TargetGroups: TargetPackageGroup []
}

module PackageInput =
  let loadTargets fileName =
    File.ReadAllText(fileName)
    |> Yaml.load<PackageInput>

module Package =
  let load fileName =
    File.ReadAllText(fileName)
    |> Yaml.tryLoad<NuGetPackageGroup []>

  let dump fileName packages =
    File.WriteAllText(fileName, Yaml.dump<NuGetPackageGroup []> packages)
