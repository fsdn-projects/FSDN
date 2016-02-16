#I "../../packages/build/FAKE/tools/"
#load "../../packages/build/FSharp.Formatting/FSharp.Formatting.fsx"
#r "FakeLib.dll"
open Fake
open System.IO
open Fake.FileHelper
open FSharp.Literate
open FSharp.MetadataFormat
open FSharp.Markdown

let bin = __SOURCE_DIRECTORY__ @@ "../../bin"
let output = bin @@ "FSDN"

#if RELEASE
let root = ""
#else
let root = "file://" + output
#endif

let content = __SOURCE_DIRECTORY__ @@ "../content"
let files = __SOURCE_DIRECTORY__ @@ "../files"
let templates = __SOURCE_DIRECTORY__ @@ "templates"
let docTemplate = __SOURCE_DIRECTORY__ @@ "docpage.cshtml"
let layoutRoots = [
  templates
]

let info = [
  "project-github", "https://github.com/pocketberserker/FSDN"
]

let copyFiles () =
  CopyRecursive files output true |> Log "Copying file: "

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
      replacements = ("root", root) :: ("script", js) :: info
    )

let readme = "../../paket-files/build/hafuu/FSharpApiSearch/README.md"

let generateQuerySpec () =
  let doc = Literate.ParseMarkdownFile(readme)
  let paragraphs =
    seq {
      yield Heading(3, [Literal "Search Engine"])
      yield ListBlock(Unordered, [[Span [DirectLink([Literal "FSharpApiSearch"], ("https://github.com/hafuu/FSharpApiSearch", None))]]])
      yield! doc.Paragraphs
        |> Seq.skipWhile (function | Heading(2, [Literal "クエリ仕様"]) -> false | _ -> true)
        |> Seq.takeWhile (function | Heading(2, [Literal "動作環境"]) -> false | _ -> true)
        |> Seq.map (function | Heading(n, spans) -> Heading(n + 1, spans) | other -> other)
    }
    |> Seq.toList
  printfn "%A" paragraphs
  Literate.ProcessDocument(
    doc.With(paragraphs = paragraphs),
    output @@ "query_spec.html",
    docTemplate,
    layoutRoots = layoutRoots,
    replacements = ("root", root) :: ("script", "query_spec.js") :: info
  )

copyFiles ()
buildDocumentation ()
generateQuerySpec ()
