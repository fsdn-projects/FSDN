#I "../../packages/build/FAKE/tools/"
#load "../../packages/build/FSharp.Formatting/FSharp.Formatting.fsx"
#r "FakeLib.dll"
open Fake
open System.IO
open Fake.FileHelper
open FSharp.Literate
open FSharp.MetadataFormat

let bin = __SOURCE_DIRECTORY__ @@ "../../bin"
let output = bin @@ "FSDN"

#if RELEASE
let root = ""
#else
let root = "file://" + output
#endif

let content = __SOURCE_DIRECTORY__ @@ "../content"
let templates = __SOURCE_DIRECTORY__ @@ "templates"
let docTemplate = __SOURCE_DIRECTORY__ @@ "docpage.cshtml"
let layoutRoots = [
  templates
]

let info = [
  "project-github", "https://github.com/pocketberserker/FSDN"
]

let buildDocumentation () =
  for file in Directory.GetFiles(content, "*", SearchOption.TopDirectoryOnly) do
    let name = filename file
    let js =
      let name = if Path.GetFileNameWithoutExtension(name) = "index" then "search" else name
      Path.GetFileNameWithoutExtension(name) + ".js"
    Literate.ProcessMarkdown(
      file,
      docTemplate,
      output @@ name,
      layoutRoots = layoutRoots,
      replacements =
        ("root", root) :: ("script", js) :: info
    )

buildDocumentation()
