namespace FSDN

open FSharpApiSearch

type LanguageName = string

type ApiLinkGenerator = {
  FSharp: LinkGenerator
  DotNetApiBrowser: LinkGenerator
  FParsec: LinkGenerator
  Packages: Map<LanguageName, NuGetPackageGroup []>
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ApiLinkGenerator =

  let private impl generator (result: Result) (package: NuGetPackage) =
    let generate =
      match Array.tryFind ((=) result.AssemblyName) package.Assemblies with
      | Some "FSharp.Core" -> generator.FSharp
      | Some _ when package.Standard -> generator.DotNetApiBrowser
      | Some "FParsec" -> generator.FParsec
      | Some _ | None -> fun _ -> None
    generate result.Api

  let generate result (generator: ApiLinkGenerator) language =
    generator.Packages.[language]
    |> Array.tryPick (fun group ->
      group.Packages |> Array.tryPick (impl generator result)
    )

