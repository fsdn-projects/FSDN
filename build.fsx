#r @"packages/build/FAKE/tools/FakeLib.dll"
#r "System.Xml.Linq.dll"
#r ".paket/paket.exe"
open Fake
open System
open System.IO
open System.Xml
open System.Xml.Linq
open System.Xml.XPath
open Paket
open Paket.Domain

let project = "FSDN"

// List of author names (for NuGet package)
let authors = [ "pocketberserker" ]

// Tags for your project (for NuGet package)
let tags = "fsharp F#"

// File system information
let solutionFile  = "FSDN.sln"

let configuration = environVarOrDefault "configuration" "Release"

// Pattern specifying assemblies to be tested using NUnit
let testAssemblies = "tests/**/bin" @@ configuration @@ "*Tests*.dll"

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted
let gitOwner = "pocketberserker"
let gitHome = "https://github.com/" + gitOwner

// The name of the project on GitHub
let gitName = "FSDN"

// The url for the raw files hosted
let gitRaw = environVarOrDefault "gitRaw" "https://raw.github.com/pocketberserker"

// --------------------------------------------------------------------------------------
// END TODO: The rest of the file includes standard build steps
// --------------------------------------------------------------------------------------

// Copies binaries from default VS location to exepcted bin folder
// But keeps a subdirectory structure for each project in the
// src folder to support multiple project outputs
Target "CopyBinaries" (fun _ ->
    !! "src/**/*.??proj"
    |>  Seq.map (fun f -> ((System.IO.Path.GetDirectoryName f) @@ "bin" @@ configuration, "bin" @@ (System.IO.Path.GetFileNameWithoutExtension f)))
    |>  Seq.iter (fun (fromDir, toDir) -> CopyDir toDir fromDir (fun _ -> true))
)

Target "CopyWebConfig" (fun _ ->
  CopyFile ("bin" @@ project @@ "Web.config") ("config" @@ sprintf "Web.%s.config" configuration)
)

// --------------------------------------------------------------------------------------
// Clean build results

Target "Clean" (fun _ ->
    CleanDirs ["bin"; "temp"]
)

// --------------------------------------------------------------------------------------
// Build library & test project

Target "Build" (fun _ ->
    !! solutionFile
    |> MSBuild "" "Rebuild" [("Configuration", configuration)]
    |> ignore
)

open NpmHelper

Target "BuildFront" (fun _ ->
  let npm =
    let target = if isUnix then"npm" else "npm.cmd"
    match tryFindFileOnPath target with
    | Some npm -> npm
    | None -> findToolInSubPath target (currentDirectory @@ "packages/build")
  Npm (fun p ->
    {
      p with
        Command = Install Standard
        WorkingDirectory = currentDirectory
        NpmFilePath = npm
    })
  Npm (fun p ->
    {
      p with
        Command = Run "typings"
        WorkingDirectory = currentDirectory
        NpmFilePath = npm
    })
  Npm (fun p ->
    {
      p with
        Command = Run "pack"
        WorkingDirectory = currentDirectory
        NpmFilePath = npm
    })
)

let changeMonoAssemblyPath (es: XElement seq) =
  let path = getBuildParamOrDefault "mono" "/usr"
  es
  |> Seq.iter (fun e ->
    e.Attribute(XName.Get("value")).Value <- path @@ "lib/mono/4.5/"
  )

let targetFrameworks = [|
  "net45"
  "net40"
  "net4"
  "net35"
  "net20"
|]

let searchExternalAssemblies () =
  "./packages/database/"
  |> directoryInfo
  |> subDirectories
  |> Array.collect (fun d ->
    subDirectories d
    |> Array.find (fun d -> d.Name = "lib")
    |> subDirectories
    |> Array.rev
    |> Array.find (fun d -> targetFrameworks |> Array.exists (fun t -> d.Name.Contains(t)))
    |> filesInDir
    |> Array.choose (fun f ->
      if f.Extension = ".dll" then Some(f.FullName)
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
  |]
  |> Array.toList

Target "GenerateApiDatabase" (fun _ ->
  if isMono then
    let config = findToolInSubPath "FSharpApiSearch.Database.exe.config" (currentDirectory @@ "packages" @@ "build")
    let doc = XDocument.Load(config)
    doc.XPathSelectElements("/configuration/appSettings/add")
    |> changeMonoAssemblyPath
    doc.Save(config)
  let exe = findToolInSubPath "FSharpApiSearch.Database.exe" (currentDirectory @@ "packages" @@ "build")
  let args =
    // TODO: enable external assemblies
    if isMono then "System.Xml.Linq System.Runtime.Serialization"
    else
      @"--lib:""C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5""" :: searchExternalAssemblies ()
      |> String.concat " "
  let exitCode =
    ExecProcess (fun info ->
      info.FileName <- exe
      info.Arguments <- args)
      TimeSpan.MaxValue
  if exitCode <> 0 then failwithf "failed to generate F# API database: %d" exitCode
  MoveFile ("bin" @@ project) (currentDirectory @@ "database")
)

type TargetAssemblyInfo = {
  Name: string
  Standard: bool
  Version: SemVerInfo option
  IconUrl: string option
}
with
  override this.ToString() =
    [
      this.Name
      string this.Standard
      (match this.Version with | Some v -> v.AsString | None -> "")
      (match this.IconUrl with | Some v -> v | None -> "")
    ]
    |> String.concat ","

let standard name = {
  Name = name
  Standard = true
  Version = None
  IconUrl = None
}

let tryFindIconUrl name =
  "./packages/database/" @@ name
  |> FindFirstMatchingFile (name + ".nuspec")
  |> XDocument.Load
  |> fun doc ->
    let manager = XmlNamespaceManager(new NameTable())
    manager.AddNamespace("x", "http://schemas.microsoft.com/packaging/2011/10/nuspec.xsd")
    doc.XPathSelectElements("/x:package/x:metadata/x:iconUrl", manager)
  |> Seq.tryPick (fun e -> Some e.Value)

Target "GenerateTargetAssembliesFile" (fun _ ->
  let packages =
    LockFile.LoadFrom("./paket.lock")
      .GetGroup(GroupName("Database"))
      .Resolution
  let targets =
    if isMono then [||]
    else [|
      "FSharp.Compiler.Service"
      "FSharp.Data"
      "FsUnit"
      "FsPickler"
      "FParsec"
      "Argu"
    |]
    |> Array.map (fun name ->
      let p = packages |> Map.toSeq |> Seq.pick (fun (k, v) -> if k.ToString() = name then Some v else None)
      {
        Name = name
        Standard = false
        Version = Some p.Version
        IconUrl = tryFindIconUrl name
      }
    )
    |> Array.append [|
      standard "System.Xml"
      standard "System.Xml.Linq"
    |]
    |> Array.map (sprintf "%O")
  File.WriteAllLines("bin" @@ project @@ "assemblies", targets)
)

Target "GenerateViews" (fun _ ->
  let definitions =
    if configuration = "Release" then ["--define:RELEASE"]
    else []
  if not <| executeFSIWithArgs "views/tools" "generate.fsx" definitions [] then
    failwith "Failed: generating views"
)

// --------------------------------------------------------------------------------------
// Deploy

Target "DeployOnAzure" (fun _ ->
  let artifacts = currentDirectory @@ ".." @@ "artifacts"
  let kuduSync = findToolInSubPath "KuduSync.NET.exe" (currentDirectory @@ "packages")
  let deploymentSource =
    currentDirectory @@ "bin" @@ project
  let deploymentTarget =
    match environVarOrNone "DEPLOYMENT_TARGET" with
    | Some v -> v
    | None -> artifacts @@ "wwwroot"
  let nextManifestPath =
    match environVarOrNone "NEXT_MANIFEST_PATH" with
    | Some v -> v
    | None -> artifacts @@ "manifest"
  let previousManifestPath =
    match environVarOrNone "PREVIOUS_MANIFEST_PATH" with
    | Some v -> v
    | None -> nextManifestPath
  if environVarOrNone "IN_PLACE_DEPLOYMENT" <> Some "1" then
    let args =
      sprintf "-v 50 -f \"%s\" -t \"%s\" -n \"%s\" -p \"%s\" -i \".git;.hg;.deployment;\""
        deploymentSource deploymentTarget nextManifestPath previousManifestPath
    let exitCode =
      ExecProcess (fun info ->
        info.FileName <- kuduSync
        info.Arguments <- args)
        TimeSpan.MaxValue
    if exitCode <> 0 then failwithf "Failed KuduSync: %s" args
    environVarOrNone "POST_DEPLOYMENT_ACTION"
    |> Option.iter (fun c ->
      let exitCode = ExecProcess (fun info -> info.FileName <- c) TimeSpan.MaxValue
      if exitCode <> 0 then failwithf "Failed: post deployment action"
    )
)

Target "All" DoNothing

"Clean"
  ==> "Build"
  ==> "CopyBinaries"
  ==> "CopyWebConfig"
  ==> "All"

"All"
  ==> "DeployOnAzure"

"BuildFront"
  ==> "All"

"GenerateApiDatabase"
  ==> "All"

"GenerateApiDatabase"
 <== ["GenerateTargetAssembliesFile"]

"GenerateViews"
  ==> "All"

RunTargetOrDefault "All"
