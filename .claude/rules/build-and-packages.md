---
paths:
  - "**/*.csproj"
  - "Directory.Build.props"
  - "Directory.Build.targets"
  - "Directory.Packages.props"
  - "nuget.config"
  - "Barbatos.Wpf.slnx"
  - ".github/workflows/**"
---

# Build, packaging and release files

Load `barbatos-package-authoring` for the full conventions. The rules that break builds or
packages when missed:

- **Central Package Management.** Every version lives in `Directory.Packages.props`; a
  `<PackageReference>` in `src/`, `samples/` or `tests/` never carries `Version=`. (The
  `barbatos-wpf-app` template's `.csproj` is the one exception: it scaffolds a standalone app.) `Microsoft.Extensions.AI*` stays in lockstep with
  the floor `ModelContextProtocol.Core` depends on — bump them together or restore fails with
  `NU1605` (the comment above those entries says which floor).
- **Target frameworks: .NET 8, 9 and 10 only.** No `netstandard`, `net6`, `net7`, and no shims.
  Core uses the SDK-suffixed form (`net10.0-windows10.0.17763.0;…`) because toast notifications
  need the WinRT projections; Aquarius / Router / Valee use bare `netX.0-windows`.
- **`Directory.Build.props` already supplies** `LangVersion`, `Nullable`, `ImplicitUsings`,
  versioning, license/icon packaging, SourceLink and `GeneratePackageOnBuild` — a `src/` project
  file carries only its own identity (`PackageId`, `TargetFrameworks`, `GeneratePackageOnBuild`,
  `UseWPF`, `CommonTags`, `Description`, `RootNamespace`, `PackageIconSourceFile`, `NoWarn`)
  plus its references.
- **`<Version>` in `Directory.Build.props` is the single version** for all four packages, with
  `<AssemblyVersion>` beside it. A version bump also moves the copies outside it: the
  `barbatos-wpf-app` template's `PackageVersion` symbol (`replaces` **and** `defaultValue`), the
  four `Version="…"` attributes in its `BarbatosWpfApp.csproj`, and the default quoted in
  `templates/README.md`. `grep -rn "<old version>"` before committing.
- **`CommonTags` is overridden wholesale** in a non-Core package, never appended to — the shared
  value is Core-specific and would mislead NuGet search.
- **Every new `NoWarn` entry gets a comment** naming the exact reflection call and why it is
  inherently dynamic, in the tone of the existing blocks. The trim/AOT analyzers are on
  repo-wide; an unexplained suppression hides real information.
- **Each package packs its own `README.md`** (the NuGet landing page) beside its `.csproj`.
- **Release** (`.github/workflows/barbatos-wpf-cd-nuget.yml`) runs on a published GitHub Release
  or manually: it writes `src/barbatos.snk` from the `STRONG_NAME_KEY` secret, logs in to NuGet
  through OIDC trusted publishing, strips `samples/` and `tests/` from the solution, builds
  `Release` with `-p:SourceLinkEnabled=true` (which also turns on strong-name signing), and pushes
  `.nupkg` + `.snupkg` with `--skip-duplicate` — an unchanged version is silently not
  republished. Never commit a `.snk` file.
- Never add a new `src/` → `src/` `ProjectReference` without the maintainer's approval; Aquarius
  must stay free of any Core reference.
