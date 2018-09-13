#r @"packages/build/FAKE/tools/FakeLib.dll"
#r @"packages/build/Fake.Persimmon/lib/net451/FAKE.Persimmon.dll"
open Fake
open Fake.Git
open System
open System.IO
open System.Text.RegularExpressions

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

[<Literal>]
let NodeEnv = "NODE_ENV"

Target "BuildFront" (fun _ ->
  let isProduction = configuration = "Release"
  let nodeEnv = environVar NodeEnv
  Npm (fun p ->
    {
      p with
        Command = Install Standard
        WorkingDirectory = currentDirectory
        NpmFilePath = npm
    })
  if isProduction then
    setEnvironVar NodeEnv "production"
  Npm (fun p ->
    {
      p with
        Command = Run "build"
        WorkingDirectory = currentDirectory
        NpmFilePath = npm
    })
  setEnvironVar NodeEnv nodeEnv
)

// --------------------------------------------------------------------------------------
// Database

let databasePackage = "FSDN.Database.zip"

Target "CopyApiDatabase" (fun _ ->
  let dir = "./paket-files/database/github.com/"
  CopyFile ("./bin" @@ project) (dir @@ "database")

  !! (dir @@ "packages.*.yml") |> CopyFiles ("./bin" @@ project)
)

Target "GenerateApiDatabase" (fun _ ->
  FileUtils.cd "./database"
  let args = "./generate.fsx Generate"
  let exitCode =
    ExecProcess (fun info ->
      info.FileName <- "../packages/build/FAKE/tools/FAKE.exe"
      info.Arguments <- args)
      TimeSpan.MaxValue
  if exitCode <> 0 then failwithf "Failed to generate database: %s" args
  FileUtils.cd ".."
)

let databaseDir = "./bin/FSDN.Database"
let databasePackagePath = "./bin/" @@ databasePackage

Target "PackApiDatabase" (fun _ ->
  !! (databaseDir @@ "/**/*")
  -- "*.zip"
  |> Zip databaseDir databasePackagePath
)

#load "paket-files/build/fsharp/FAKE/modules/Octokit/Octokit.fsx"
open Octokit

let publishApiDatabase now =

  // push empty commit
  let tempDatabaseDir = "temp/database"
  CleanDir tempDatabaseDir
  Repository.cloneSingleBranch "" ("git@github.com:fsdn-projects/FSDN.Database.git") "master" tempDatabaseDir
  sprintf "commit --allow-empty -m \"update database %s\"" now
  |> runSimpleGitCommand tempDatabaseDir
  |> trace
  Branches.push tempDatabaseDir

  // upload database file
  environVar "access_token"
  |> createClientWithToken
  |> createDraft "fsdn-projects" "FSDN.Database" now false Seq.empty
  |> uploadFile databasePackagePath
  |> releaseDraft
  |> Async.RunSynchronously

let paketDependencies = "./paket.dependencies"

let updateApiDatabase now =

  let newDatabaseUrl =
    now
    |> Uri.EscapeDataString
    |> sprintf "https://github.com/fsdn-projects/FSDN.Database/releases/download/%s/FSDN.Database.zip"
  let deps =
    Regex.Replace(
      File.ReadAllText(paketDependencies),
      "https://github.com/fsdn-projects/FSDN.Database/releases/download/[^/]+/FSDN.Database.zip",
      newDatabaseUrl
    )
  File.WriteAllText(paketDependencies, deps)

  let exitCode =
    ExecProcess (fun info ->
      info.FileName <- "./.paket/paket.exe"
      info.Arguments <- "update group Database")
      TimeSpan.MaxValue
  if exitCode <> 0 then failwithf "failed to update group Database: %d" exitCode

  // push
  sprintf "commit -am \"[skip ci]auto update database %s\"" now
  |> runSimpleGitCommand currentDirectory
  |> trace
  Branches.pushBranch currentDirectory "origin" "HEAD:master"

Target "PublishApiDatabaseFromAppVeyor" (fun _ ->

  let branch = environVar "APPVEYOR_REPO_BRANCH"
  let pr = environVar "APPVEYOR_PULL_REQUEST_NUMBER"
  if branch = "master" && String.IsNullOrEmpty(pr) then
    let now = DateTime.UtcNow.ToString("yyyy/MM/dd-HHmmss")
    publishApiDatabase now
    updateApiDatabase now
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

// require "Build"
"GenerateApiDatabase"
  ==> "PackApiDatabase"
  ==> "PublishApiDatabaseFromAppVeyor"

"CopyApiDatabase"
  ==> "All"

"All"
  ==> "Pack"

"All"
  ==> "DeployOnAzure"

RunTargetOrDefault "All"
