#r @"packages/build/FAKE/tools/FakeLib.dll"
open Fake
open System
open System.IO

let project = "FSDN"

// List of author names (for NuGet package)
let authors = [ "pocketberserker" ]

// Tags for your project (for NuGet package)
let tags = "fsharp F#"

// File system information
let solutionFile  = "FSDN.sln"

// Pattern specifying assemblies to be tested using NUnit
let testAssemblies = "tests/**/bin/Release/*Tests*.dll"

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
    |>  Seq.map (fun f -> ((System.IO.Path.GetDirectoryName f) @@ "bin/Release", "bin" @@ (System.IO.Path.GetFileNameWithoutExtension f)))
    |>  Seq.iter (fun (fromDir, toDir) -> CopyDir toDir fromDir (fun _ -> true))
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
    |> MSBuildRelease "" "Rebuild"
    |> ignore
)

Target "All" DoNothing

"Clean"
  ==> "Build"
  ==> "CopyBinaries"
  ==> "All"

RunTargetOrDefault "All"
