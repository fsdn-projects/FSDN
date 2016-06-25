#r @"packages/build/FAKE/tools/FakeLib.dll"
#r @"packages/build/FAKE.Persimmon/lib/net451/FAKE.Persimmon.dll"
open Fake
open System
open System.IO

let project = "FSDN"

let solutionFile  = "FSDN.sln"

let configuration = environVarOrDefault "configuration" "Release"

let testAssemblies = "tests/**/bin" @@ configuration @@ "*Tests*.dll"

Target "CopyBinaries" (fun _ ->
    !! "src/**/*.??proj"
    |>  Seq.map (fun f -> ((System.IO.Path.GetDirectoryName f) @@ "bin" @@ configuration, "bin" @@ (System.IO.Path.GetFileNameWithoutExtension f)))
    |>  Seq.iter (fun (fromDir, toDir) -> CopyDir toDir fromDir (fun _ -> true))
)

Target "CopyWebConfig" (fun _ ->
  CopyFile ("bin" @@ project @@ "Web.config") ("config" @@ sprintf "Web.%s.config" configuration)
)

Target "Clean" (fun _ ->
    CleanDirs ["bin"; "temp"]
)

Target "Build" (fun _ ->
    !! solutionFile
    |> MSBuild "" "Rebuild" [("Configuration", configuration)]
    |> ignore
)

Target "RunTests" (fun _ ->
    !! testAssemblies
    |> Persimmon id
)

open NpmHelper

let npm =
  let target = if isUnix then"npm" else "npm.cmd"
  match tryFindFileOnPath target with
  | Some npm -> npm
  | None -> findToolInSubPath target (currentDirectory @@ "packages/build")

Target "BuildFront" (fun _ ->
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

Target "GenerateViews" (fun _ ->
  let definitions =
    if configuration = "Release" then ["--define:RELEASE"]
    else []
  if not <| executeFSIWithArgs "views/tools" "generate.fsx" definitions [] then
    failwith "Failed: generating views"
)

Target "RevReplace" (fun _ ->
  Npm (fun p ->
    {
      p with
        Command = Run "replace"
        WorkingDirectory = currentDirectory
        NpmFilePath = npm
    })
)

// --------------------------------------------------------------------------------------
// Database

Target "CopyApiDatabase" (fun _ ->
  let dir = "./paket-files/build/github.com"
  CopyFile ("./bin" @@ project @@ "database") (dir @@ "database.fs")
  CopyFile ("./bin" @@ project) (dir @@ "packages.yml")
)

Target "GenerateApiDatabase" (fun _ ->
  FileUtils.cd "./database"
  let isAzure =
    CurrentTargetOrder
    |> List.exists (List.contains "DeployOnAzure")
  let args =
    if isAzure then " platform=Azure"
    else ""
    |> sprintf "./generate.fsx Generate%s"
  let exitCode =
    ExecProcess (fun info ->
      info.FileName <- "../packages/build/FAKE/tools/FAKE.exe"
      info.Arguments <- args)
      TimeSpan.MaxValue
  if exitCode <> 0 then failwithf "Failed to generate database: %s" args
  FileUtils.cd ".."
)

#load "paket-files/build/fsharp/FAKE/modules/Octokit/Octokit.fsx"
open Octokit

Target "PublishApiDatabase" (fun _ ->
  match environVarOrNone "GITHUB_TOKEN" with
  | Some token -> createClientWithToken token
  | None -> createClient (getBuildParamOrDefault "github-user" "") (getBuildParamOrDefault "github-pw" "")
  |> createDraft "fsdn-projects" "FSDN.Database" (DateTime.UtcNow.ToString("yyyy/MM/dd-HHmmss")) false Seq.empty
  |> uploadFiles (directoryInfo "./bin/FSDN.Database/" |> filesInDir |> Array.map (fun x -> x.FullName))
  |> releaseDraft
  |> Async.RunSynchronously
)

// --------------------------------------------------------------------------------------
// Deploy

Target "Pack" (fun _ ->
  let appDir = "./bin" @@ project
  !! (appDir + "/**/*.*")
  -- "*.zip"
  |> Zip appDir ("./bin" @@ sprintf "%s.zip" project)
)

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
  ==> "RunTests"
  ==> "CopyWebConfig"
  ==> "All"

"BuildFront"
  ==> "All"

"RunTests"
  ==> "GenerateApiDatabase"
  ==> "PublishApiDatabase"

"BuildFront"
  ==> "RevReplace"

"GenerateViews"
  ==> "RevReplace"
  ==> "All"

"CopyApiDatabase"
  ==> "All"

"All"
  ==> "Pack"

"All"
  ==> "DeployOnAzure"

RunTargetOrDefault "All"
