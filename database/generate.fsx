#r @"../packages/build/FAKE/tools/FakeLib.dll"
#r "System.Xml.Linq.dll"
#r "../.paket/paket.exe"
#I @"../packages/app/YamlDotNet/lib/portable-net45+netcore45+wpa81+wp8+MonoAndroid1+MonoTouch1"
#I @"../packages/app/FSharp.Configuration/lib/net40"
#r @"../packages/app/FsYaml/lib/net45/FsYaml.dll"
#r "../bin/FSDN.Package/FSDN.Package.dll"
open Fake
open System
open System.IO
open System.Xml
open System.Xml.Linq
open System.Xml.XPath
open Paket
open Paket.Domain
open FSDN

let out = "../bin/FSDN.Database/"

Target "PaketRestore" (fun _ ->
  let exitCode =
    ExecProcess (fun info ->
      info.FileName <- "../.paket/paket.exe"
      info.Arguments <- "restore")
      TimeSpan.MaxValue
  if exitCode <> 0 then failwithf "failed to restore package: %d" exitCode
)

let changeMonoAssemblyPath (es: XElement seq) =
  let path = getBuildParamOrDefault "mono" "/usr"
  es
  |> Seq.iter (fun e ->
    e.Attribute(XName.Get("value")).Value <- path @@ "lib/mono/4.5/"
  )

let changeAzureAssemblyPath (es: XElement seq) =
  es
  |> Seq.iter (fun e ->
    e.Attribute(XName.Get("value")).Value <- e.Attribute(XName.Get("value")).Value.Replace(@"C:\", @"D:\")
  )

let targetFrameworks = [|
  "net45"
  "net40"
  "net4"
  "net35"
  "net20"
|]

let searchExternalAssemblies () =
  "./packages/"
  |> directoryInfo
  |> subDirectories
  |> Array.collect (fun d ->
    let libs =
      subDirectories d
      |> Array.find (fun d -> d.Name = "lib")
      |> subDirectories
      |> Array.rev
    let withoutPortable = libs |> Array.tryFind (fun d -> targetFrameworks |> Array.exists (fun t -> d.Name.Contains(t) && not (d.Name.Contains("portable"))))
    let target =
      match withoutPortable with
      | Some w -> w
      | None -> libs |> Array.find (fun d -> targetFrameworks |> Array.exists (fun t -> d.Name.Contains(t)))
    target
    |> filesInDir
    |> Array.choose (fun f ->
      if f.Name = "FSharp.Core.dll" then None
      elif f.Extension = ".dll" then Some(f.FullName)
      else None
    )
  )
  |> Array.distinct
  |> Array.append [|
    "System.IO"
    "System.Runtime"
    "System.Diagnostics.Debug"
    "System.Collections"
    "System.Text.Encoding"
    "System.Text.RegularExpressions"
    "System.Threading"
    "System.Threading.Tasks"
    "System.Xml.ReaderWriter"
    "System.Reflection"
    "System.Globalization"
    "System.Runtime.Extensions"
    "System.Reflection.Primitives"
    "System.Xml.Linq"
    "System.Runtime.Serialization"
    "System.Net"
    "System.Numerics"
    "System.Runtime.Numerics"
    "System.Web"
    "System.Web.Services"
    "System.Web.ApplicationServices"
    "System.EnterpriseServices"
    "System.ComponentModel.DataAnnotations"
    "System.Drawing"
    "System.Data"
    "System.Transactions"
    "System.ServiceModel.Internals"
  |]
  |> Array.toList

Target "Generate" (fun _ ->
  let isAzure = getBuildParamOrDefault "platform" "" = "Azure"
  if isMono || isAzure then
    let changeAssemblyPath = if isMono then changeMonoAssemblyPath else changeAzureAssemblyPath
    let config = findToolInSubPath "FSharpApiSearch.Database.exe.config" (currentDirectory @@ ".." @@ "packages" @@ "build")
    let doc = XDocument.Load(config)
    doc.XPathSelectElements("/configuration/appSettings/add")
    |> changeAssemblyPath
    doc.Save(config)
  let exe = findToolInSubPath "FSharpApiSearch.Database.exe" (currentDirectory @@ ".." @@ "packages" @@ "build")
  let args =
    // TODO: enable external assemblies
    if isMono then
      [|
        "System.Xml.Linq"
        "System.Runtime.Serialization"
        "System.ServiceModel.Internals"
        "Mono.Security"
      |]
      |> String.concat " "
    else
      searchExternalAssemblies ()
      |> String.concat " "

  let exitCode =
    use timer = new System.Timers.Timer(1000.0)
    do timer.Elapsed |> Event.add (fun _ -> printf ".")
    timer.Start()
    let exitCode =
      ExecProcess (fun info ->
        info.FileName <- exe
        info.Arguments <- args)
        TimeSpan.MaxValue
    timer.Stop()
    printfn ""
    exitCode

  if exitCode <> 0 then failwithf "failed to generate F# API database: %d" exitCode
  MoveFile out (currentDirectory @@ "database")
)

type PackageInfo = {
  Name: string
  Standard: bool
  Version: SemVerInfo option
  IconUrl: string option
  Assemblies: string []
}
with
  member this.ToSerializablePackage =
    {
      NuGetPackage.Name = this.Name
      Standard = this.Standard
      Version = (match this.Version with | Some v -> v.AsString | None -> "")
      IconUrl = (match this.IconUrl with | Some v -> v | None -> "")
      Assemblies = this.Assemblies
    }

let standard name = {
  Name = name
  Standard = true
  Version = None
  IconUrl = None
  Assemblies = [|name|]
}

let tryFindIconUrl name =
  "./packages/" @@ name
  |> FindFirstMatchingFile (name + ".nuspec")
  |> XDocument.Load
  |> fun doc ->
    let manager = XmlNamespaceManager(new NameTable())
    manager.AddNamespace("x", "http://schemas.microsoft.com/packaging/2011/10/nuspec.xsd")
    doc.XPathSelectElements("/x:package/x:metadata/x:iconUrl", manager)
  |> Seq.tryPick (fun e -> Some e.Value)

Target "GenerateTargetAssembliesFile" (fun _ ->
  ensureDirectory out
  let packages =
    LockFile.LoadFrom("./paket.lock")
      .GetGroup(GroupName("Main"))
      .Resolution
  if isMono then [||]
  else Package.loadTargets "./packages.yml"
  |> Array.map (fun x ->
    let p = packages |> Map.toSeq |> Seq.pick (fun (k, v) -> if k.ToString() = x.Name then Some v else None)
    {
      Name = x.Name
      Standard = false
      Version = Some p.Version
      IconUrl = tryFindIconUrl x.Name
      Assemblies = x.Assemblies
    }
  )
  |> Array.append [|
    standard "System.Xml"
    standard "System.Xml.Linq"
  |]
  |> Array.map (fun x -> x.ToSerializablePackage)
  |> Package.dump (out @@ "packages.yml")
)

"PaketRestore"
  ==> "GenerateTargetAssembliesFile"
  ==> "Generate"

RunTargetOrDefault "Generate"
