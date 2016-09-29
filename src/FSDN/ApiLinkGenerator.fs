namespace FSDN

open FSharpApiSearch

type ApiLinkGenerator = {
  FSharp: LinkGenerator
  MSDN: LinkGenerator
  Packages: NuGetPackage []
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ApiLinkGenerator =

  let private impl generator (result: Result) (package: NuGetPackage) =
    let generate =
      match Array.tryFind ((=) result.AssemblyName) package.Assemblies with
      | Some "FSharp.Core" -> generator.FSharp
      | Some _ when package.Standard -> generator.MSDN
      | Some _ | None -> fun _ -> None
    generate result.Api

  let generate result generator =
    generator.Packages
    |> Array.tryPick (impl generator result)

