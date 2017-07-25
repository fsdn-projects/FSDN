namespace FSDN

open FSharpApiSearch

type ApiLinkGenerator = {
  FSharp: LinkGenerator
  DotNetApiBrowser: LinkGenerator
  FParsec: LinkGenerator
  Packages: NuGetPackage []
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

  let generate result generator =
    generator.Packages
    |> Array.tryPick (impl generator result)

