#r @"../packages/build/FAKE/tools/FakeLib.dll"
#r "System.Xml.Linq.dll"
#I @"../packages/build/Chessie/lib/net40"
#r @"Chessie.dll"
#r "../packages/build/Paket.Core/lib/net45/Paket.Core.dll"
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

let paket = "../.paket/paket.exe"

Target "PaketRestore" (fun _ ->
  let cmd, args =
    let exe = paket
    let restore = "restore"
    if isMonoRuntime then ("mono", sprintf "%s %s" exe restore)
    else (exe, restore)
  let exitCode =
    ExecProcess (fun info ->
      info.FileName <- cmd
      info.Arguments <- args)
      TimeSpan.MaxValue
  if exitCode <> 0 then failwithf "failed to restore package: %d" exitCode
)



let changeMonoAssemblyPath (es: XElement seq) =
  let path = getBuildParamOrDefault "mono" "/usr"
  es
  |> Seq.iter (fun e ->
    e.Attribute(XName.Get("value")).Value <- path @@ "lib/mono/4.5/"
  )

let framework = "net46"

let generateLoadScripts() =
  let exitCode =
    ExecProcess (fun info ->
      info.FileName <- paket
      info.Arguments <- sprintf "generate-load-scripts framework %s type fsx" framework)
      TimeSpan.MaxValue
  if exitCode <> 0 then failwithf "failed to generate-load-scripts: %d" exitCode

let searchExternalAssemblies () =
  generateLoadScripts()
  
  let loadScriptDir = currentDirectory @@ (sprintf @".paket\load\%s\" framework)
  let loadScriptPath = loadScriptDir @@ @"main.group.fsx"

  File.ReadAllLines(loadScriptPath)
  |> Array.map (fun line ->
    let reference = line.Substring(4).TrimEnd('"', ' ')
    if reference.StartsWith(@"..") then
      Path.GetFullPath(loadScriptDir @@ reference)
    else
      reference
  )
  |> Array.append [|
    "System.Collections"
    "System.ComponentModel.DataAnnotations"
    "System.Data"
    "System.Diagnostics.Debug"
    "System.Diagnostics.Tracing"
    "System.Drawing"
    "System.EnterpriseServices"
    "System.Globalization"
    "System.IO"
    "System.Linq.Expressions"
    "System.Net"
    "System.Net.Http"
    "System.Numerics"
    "System.ObjectModel"
    "System.Reflection"
    "System.Reflection.Primitives"
    "System.Runtime"
    "System.Runtime.Extensions"
    "System.Runtime.InteropServices"
    "System.Runtime.Handles"
    "System.Runtime.Numerics"
    "System.Runtime.Serialization"
    "System.Security"
    "System.ServiceModel.Internals"
    "System.Text.Encoding"
    "System.Text.RegularExpressions"
    "System.Threading"
    "System.Threading.Tasks"
    "System.Transactions"
    "System.Web"
    "System.Web.ApplicationServices"
    "System.Web.Services"
    "System.Xaml"
    "System.Xml.Linq"
    "System.Xml.ReaderWriter"
    "Microsoft.Build.Framework"
  |]
  |> Array.toList

Target "Generate" (fun _ ->
  if isMonoRuntime then
    let config = findToolInSubPath "FSharpApiSearch.Database.exe.config" (currentDirectory @@ ".." @@ "packages" @@ "build")
    let doc = XDocument.Load(config)
    doc.XPathSelectElements("/configuration/appSettings/add")
    |> changeMonoAssemblyPath
    doc.Save(config)
  let exe = findToolInSubPath "FSharpApiSearch.Database.exe" (currentDirectory @@ ".." @@ "packages" @@ "build")
  let args =
    // TODO: enable external assemblies
    if isMonoRuntime then
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

let tryFindIconUrl name =
  "./packages/" @@ name
  |> FindFirstMatchingFile (name + ".nuspec")
  |> XDocument.Load
  |> fun doc ->
    let ns = doc.Root.Attribute(XName.Get("xmlns")).Value
    let manager = XmlNamespaceManager(new NameTable())
    manager.AddNamespace("x", ns)
    doc.XPathSelectElements("/x:package/x:metadata/x:iconUrl", manager)
  |> Seq.tryPick (fun e -> Some e.Value)

Target "GenerateTargetAssembliesFile" (fun _ ->
  ensureDirectory out
  let packages =
    LockFile.LoadFrom("./paket.lock")
      .GetGroup(GroupName("Main"))
      .Resolution
  let config = Package.loadTargets "./packages.yml"
  let allTargets =
    if isMonoRuntime then [||]
    else config.Targets
    |> Array.map (fun x ->
      {
        Name = x.Name
        Standard = x.Standard
        Version =
          if x.Standard then
            None
          else
            let p = packages |> Map.toSeq |> Seq.pick (fun (k, v) -> if k.ToString() = x.Name then Some v else None)
            Some p.Version
        IconUrl =
          if x.Standard then
            None
          else
            tryFindIconUrl x.Name
        Assemblies = x.Assemblies
      }
    )
    |> Array.map (fun x -> x.ToSerializablePackage)
  config.Languages
  |> Map.iter (fun lang langTargets ->
    allTargets
    |> Array.filter (fun at -> Array.contains at.Name langTargets)
    |> Package.dump (out @@ sprintf "packages.%s.yml" lang)
  )
)

"PaketRestore"
  ==> "GenerateTargetAssembliesFile"
  ==> "Generate"

RunTargetOrDefault "Generate"
