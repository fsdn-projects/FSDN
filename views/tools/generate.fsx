#I "../../packages/build/FAKE/tools/"
#load "../../packages/build/FSharp.Formatting/FSharp.Formatting.fsx"
#r "FakeLib.dll"
open Fake
open System.IO
open System.Net
open Fake.FileHelper
open FSharp.Literate
open FSharp.MetadataFormat
open FSharp.Markdown

let src = __SOURCE_DIRECTORY__ @@ "../../src"
let bin = __SOURCE_DIRECTORY__ @@ "../../bin"
let output = bin @@ "FSDN"

let root = ""

let specFileName = "query_spec"

let content = __SOURCE_DIRECTORY__ @@ "../content"
let files = __SOURCE_DIRECTORY__ @@ "../files"
let icons =
  ["fsdn_mono.png"; "favicon.ico"]
  |> List.map (fun x -> __SOURCE_DIRECTORY__ @@ "../../paket-files/build/fsdn-projects/fsdn-logo" @@ x)
let templates = __SOURCE_DIRECTORY__ @@ "templates"
let docTemplate = __SOURCE_DIRECTORY__ @@ "docpage.cshtml"
let layoutRoots = [
  templates
]

let info = [
  "project-github", "https://github.com/fsdn-projects/FSDN"
]

let copyFiles () =
  CopyRecursive files output true |> Log "Copying file: "
  CopyTo output icons

let configReplacements name =
  let exists = fileExists (src @@ "front" @@ sprintf "%s.ts" name) || fileExists (sprintf "../files/%s.js" name)
  let script =
    if exists then
      Path.GetFileNameWithoutExtension(name) + ".js"
      |> sprintf """<script src="%s%s"></script>""" (if name = specFileName then "../" else root)
    else ""
  ("root", root) :: ("script", script) :: info

let buildDocumentation () =
  for file in Directory.GetFiles(content, "*", SearchOption.TopDirectoryOnly) do
    let name = filename file
    let replacements =
      if Path.GetFileNameWithoutExtension(name) = "index" then "search"
      else Path.GetFileNameWithoutExtension(name)
      |> configReplacements
    Literate.ProcessMarkdown(
      file,
      docTemplate,
      output @@ name,
      layoutRoots = layoutRoots,
      replacements = replacements
    )

module QuerySpec =

  open FSharp.CodeFormat

  let rec format = function
  | Heading(n, spans) -> Heading(n + 1, spans)
  | EmbedParagraphs(cmd) as embed ->
    match cmd with
    | :? LiterateParagraph as p ->
      match p with
      | FormattedCode lines ->
        lines
        |> List.map (fun (Line spans) ->
          spans
          |> List.fold (fun l -> function
            | Token(_, v, _)
            | Error(_, v, _)
            | Omitted(_, v)
            | Output v -> l + v
            ) ""
        )
        |> String.concat System.Environment.NewLine
        |> WebUtility.HtmlEncode
        |> sprintf """<pre><code class="markdown">%s</code></pre>"""
        |> InlineBlock
      | _ -> embed
    | _ -> embed
  | other -> other

type Language =
  | English
  | Japanese
with
  override this.ToString() =
    match this with
    | English -> "en"
    | Japanese -> "ja"

let (|Begin|_|) heading = function
| Heading(2, [Literal ("Query format specifications" | "クエリ仕様")]) -> Some ()
| _ -> None

let (|End|_|) heading = function
| Heading(2, [Literal ("Current Build Status" | "動作環境")]) -> Some ()
| _ -> None

let generateQuerySpec (language, readme) =
  let doc = Literate.ParseMarkdownFile(readme)
  let paragraphs =
    seq {
      yield Heading(3, [Literal "Search Engine"])
      yield ListBlock(Unordered, [[Span [DirectLink([Literal "FSharpApiSearch"], ("https://github.com/hafuu/FSharpApiSearch", None))]]])
      yield! doc.Paragraphs
        |> Seq.skipWhile (function | Begin () -> false | _ -> true)
        |> Seq.takeWhile (function | End () -> false | _ -> true)
        |> Seq.map QuerySpec.format
    }
    |> Seq.toList
  let outDir = output @@ language.ToString()
  ensureDirectory outDir
  Literate.ProcessDocument(
    doc.With(paragraphs = paragraphs),
    outDir @@ sprintf "%s.html" specFileName,
    docTemplate,
    lineNumbers = false,
    layoutRoots = layoutRoots,
    replacements = ("root", ".." @@ root) :: ("script", "") :: info
  )

copyFiles ()
buildDocumentation ()

[
  (English, "../../README.md")
  (Japanese, "../../paket-files/doc/hafuu/FSharpApiSearch/README.md")
]
|> List.iter generateQuerySpec
