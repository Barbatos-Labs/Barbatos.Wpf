# Build, packing and release mechanics

Contents:

1. [How a project is classified](#how-a-project-is-classified)
2. [What every project inherits](#what-every-project-inherits)
3. [What lands in a package](#what-lands-in-a-package)
4. [SourceLink and symbols](#sourcelink-and-symbols)
5. [Strong naming](#strong-naming)
6. [Dependencies and restore](#dependencies-and-restore)
7. [Trim and AOT suppressions](#trim-and-aot-suppressions)
8. [The release workflow](#the-release-workflow)
9. [Checklist: cutting a release](#checklist-cutting-a-release)
10. [Checklist: adding a package](#checklist-adding-a-package)

Everything here is read from [`Directory.Build.props`](../../../Directory.Build.props),
[`Directory.Build.targets`](../../../Directory.Build.targets),
[`Directory.Packages.props`](../../../Directory.Packages.props),
[`nuget.config`](../../../nuget.config) and
[`.github/workflows/barbatos-wpf-cd-nuget.yml`](../../../.github/workflows/barbatos-wpf-cd-nuget.yml).
None of it can be run on Linux or macOS; on those, say what you could not verify.

## How a project is classified

`Directory.Build.props` sets `IsTestProject` to `$(MSBuildProjectName.Contains('Test'))` and
`IsCoreProject` to its opposite. Both are decided before the project file's own body is read, so
the project **name** decides, not the `<IsTestProject>true</IsTestProject>` the test projects
also set. `IsCoreProject` is a misleading name: it means "not a test", and the samples are
"core" too.

| | Not a test (`src/`, `samples/`) | Name contains `Test` (`tests/`) |
| --- | --- | --- |
| `GeneratePackageOnBuild` | `true` | `false` |
| `IsPackable` / `IsPublishable` | `true` (samples set `IsPackable=false` themselves) | `false` |
| `PackageReadmeFile` | `README.md` | - |
| `GenerateDocumentationFile` | `true` | - |
| Extra `NoWarn` | - | `CS8002;SA0001` |

Consequences: a `src/` project whose name contains `Test` silently stops packing and signing,
and the samples also meet the signing condition below - CI strips them from the solution before
building.

## What every project inherits

- `LangVersion` `14.0`, `Nullable` and `ImplicitUsings` enabled, `AllowUnsafeBlocks`,
  `NoWarn` `CS8500` (with the reason in a comment), `CheckEolWorkloads` `false`.
- Package metadata: `Authors`, `Product`, `Copyright`, a fallback `Description`,
  `PackageProjectUrl`, `PackageReleaseNotes` (the GitHub releases page - release notes live in
  the GitHub Release, there is no changelog file), `PackageLicenseFile` `LICENSE.md`,
  `PackageIcon` `Icon.png`.
- `CommonTags` holds Core's tag list; `Directory.Build.targets` turns it into `PackageTags`
  (prepended to any `PackageTags` a project sets). Core keeps it; the other three replace it
  wholesale with a comment saying why.
- `PackageIconSourceFile` falls back to `nuget.png`; each package names its own file in
  [`build/`](../../../build) (`core-logo.png`, `aquarius-logo.png`, `aquariusrouter-logo.png`,
  `aquariusvalee-logo.png`, each with an `.svg` beside it).
- `IsTrimmable`, `EnableTrimAnalyzer`, `EnableAotAnalyzer`, `EnableSingleFileAnalyzer` are `true`
  for every project.
- `ContinuousIntegrationBuild` is `true` under GitHub Actions (`GITHUB_ACTIONS`) or Azure
  Pipelines (`TF_BUILD`).
- `<Version>` and `<AssemblyVersion>` come from the first `PropertyGroup` of
  `Directory.Build.props`. Samples override both (plus `FileVersion`) so they report their own
  version; a library must not.

## What lands in a package

For a project with `GeneratePackageOnBuild` `true`, `Directory.Build.targets` packs:

| Source | Path in the `.nupkg` |
| --- | --- |
| `build/$(PackageIconSourceFile)` | `Icon.png` |
| `LICENSE.md` (repo root) | `LICENSE.md` |
| `ThirdPartyNotices.txt` (repo root) | root |
| `README.md` beside the `.csproj` | `README.md` - the nuget.org landing page |

`API-REFERENCE.md` is **not** packed. The packed README is read on nuget.org, where a relative
link resolves to nothing, so cross-file links in a package README must be absolute
`https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/...` URLs (the Valee README still has
three relative `API-REFERENCE.md#...` links that are dead there - do not copy them).

A plain `dotnet build` already produces the `.nupkg` and `.snupkg` (`GeneratePackageOnBuild`), so
packaging problems show up in an ordinary local build.

When code is ported (not just its API shape) from another project, its notice belongs in
`ThirdPartyNotices.txt`, which every package ships.

## SourceLink and symbols

- `Microsoft.SourceLink.GitHub` is referenced, with `PublishRepositoryUrl` and
  `EmbedUntrackedSources`, unless a build passes `-p:SourceLinkEnabled=false`. A normal local
  build includes it.
- `-p:SourceLinkEnabled=true` (what CI passes) additionally turns on `DeterministicSourcePaths`
  and strong naming.
- Symbols ship as a separate `.snupkg` (`IncludeSymbols`, `SymbolPackageFormat` `snupkg`). Do not
  also embed the PDB in the `.nupkg`: nuget.org rejects the `.snupkg` when PDBs are duplicated.
- The `WpfSourceLinkWorkaround` target adds a placeholder `SourceRoot` for every `UseWPF`
  project, because WPF's markup compile builds a temporary project without the SourceLink
  package and the source root would otherwise be empty. Leave it in place.
- The `AddCommitHashToAssemblyAttributes` target stamps
  `[AssemblyMetadata("CommitHash", ...)]` from `SourceRevisionId` when one is known.

## Strong naming

`Directory.Build.targets` sets `SignAssembly` and
`AssemblyOriginatorKeyFile` = `$(RepositoryDirectory)\src\barbatos.snk` only when
`SourceLinkEnabled` is `'true'` **and** `GeneratePackageOnBuild` is `'true'` **and**
`IsCoreProject` is `'true'`.

- Local Debug and Release builds are unsigned; the released packages are signed.
- The key exists only on the CI runner, written from the `STRONG_NAME_KEY` secret. A local
  `-p:SourceLinkEnabled=true` build fails for lack of it - that is expected.
- `.gitignore` has its `*.snk` line commented out, so a key written into the tree shows up in
  `git status`. Never commit it.
- Only test projects suppress `CS8002` (referenced assembly has no strong name), and only CI
  signs, so an unsigned new dependency surfaces as a warning in the release build alone.

## Dependencies and restore

- `ManagePackageVersionsCentrally` is on: every `PackageVersion` lives in
  `Directory.Packages.props` (sample and test packages too), a `PackageReference` never carries
  `Version=`. `CentralPackageTransitivePinningEnabled` is off. One version applies to all three
  TFMs - there are no per-TFM conditions.
- `NuGetAudit` is on at level `moderate`: a vulnerable package warns at restore.
- `Microsoft.Extensions.AI`, `.AI.Abstractions` and `.AI.OpenAI` are pinned to the floor
  `ModelContextProtocol.Core` depends on; the comment above them names it. Bump them together.
- `nuget.config` lists only nuget.org and maps `*` to it under `packageSourceMapping`. A package
  from any other feed needs a new source **and** a mapping entry.
- What each package references today, which is what a consumer inherits transitively:

| Package | Package references | Project references |
| --- | --- | --- |
| Core | `Anthropic`, `Castle.Core`, `CommunityToolkit.Mvvm`, `Microsoft.Agents.AI`, `Microsoft.AspNetCore.SignalR.Client`, `Microsoft.Extensions.AI*`, `Microsoft.Extensions.Configuration` + `.Binder` + `.EnvironmentVariables` (not `.Json` - apps add it), `.DependencyInjection`, `.Diagnostics.Abstractions`, `.FileProviders.Physical`, `.Hosting.Abstractions`, `.Logging`, `.Options*`, `Microsoft.Toolkit.Uwp.Notifications`, `ModelContextProtocol.Core`, `OpenAI`, `System.ClientModel`, `System.Management` | - |
| Aquarius | `CommunityToolkit.Mvvm`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Options` | - |
| AquariusRouter | same three as Aquarius | Aquarius |
| AquariusValee | `CommunityToolkit.Mvvm` (DI types arrive through Aquarius) | Aquarius |

  Adding to the Aquarius family's list, or any `src/` → `src/` reference, is a maintainer
  decision: each one becomes a dependency of every app that installs the package. Each existing
  `ProjectReference` carries a comment recording that approval; a new one does too.

## Trim and AOT suppressions

WPF apps are never trimmed, but the analyzers run, so every suppression is a statement about a
specific call. Existing blocks to match in tone:

| Project | `NoWarn` codes | What the comment names |
| --- | --- | --- |
| `Barbatos.Wpf.Core` | `IL2026;IL3050;IL2075;IL2060` | Configuration binding; Apsu's action-interceptor reflection (unwrapping `Task<T>.Result`, `MakeGenericMethod`), on top of Castle DynamicProxy's own runtime codegen |
| `Barbatos.Wpf.Aquarius` | `IL2026;IL3050;IL2075;IL2067` | `Directives.Event`'s `Type.GetEvent`; `Setup`'s `Activator.CreateInstance(Type)` / `Assembly.GetType(string)` |
| `Barbatos.Wpf.AquariusRouter` | `IL2026;IL3050;IL2075;IL2067` | Constructing routed Views through `Setup`'s resolution path |
| `Barbatos.Wpf.AquariusValee` | `IL2026;IL3050;IL2075;IL2067;IL2090;IL2060;IL2070` | `Form.FromModel`'s `PropertyInfo`/`ValidationAttribute` reflection, its `MakeGenericMethod`, `WireDeclaredDependencies`' `GetProperty` |

Before adding a code, try annotating instead: a generic parameter that gets constructed takes
`[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]`
(`ConfigurePeriodicServices<TService>`, `AddStore<TStore>`, `AddStoreInterceptor<TStore, TInterceptor>`).

## The release workflow

`.github/workflows/barbatos-wpf-cd-nuget.yml`, job `deploy`, `runs-on: windows-latest`,
`environment: production`, `permissions: id-token: write, contents: read`.

| Step | What it does |
| --- | --- |
| Trigger | `release: types: [published]`, or `workflow_dispatch` from the Actions tab (runs against whichever branch is picked) |
| 1. SDK | `actions/setup-dotnet@v4` with `dotnet-version: 10.x`, plus `microsoft/setup-msbuild@v2` |
| 2. Key | "Fetch the certificate": base64-decodes `secrets.STRONG_NAME_KEY` into `src\barbatos.snk`; fails if the secret is empty or decodes to nothing |
| 3. Login | `NuGet/login@v1` - nuget.org trusted publishing over OIDC; it hands the push steps a short-lived key, so no long-lived API key is stored. The nuget.org policy identifies the workflow (and environment), so renaming this file or the `production` environment breaks it |
| 4. Trim the solution | Regex-removes the `/samples/` and `/tests/` `<Folder>` blocks from `Barbatos.Wpf.slnx`. A project missing from the solution is never built or pushed; one in any folder other than `/samples/` or `/tests/` is built in Release with signing on |
| 5. Restore | `dotnet workload restore`, `dotnet restore` |
| 6. Build | `dotnet build --configuration Release --no-restore -p:SourceLinkEnabled=true` - packs, signs, deterministic paths |
| 7. Push | `dotnet nuget push **\*.nupkg` then `**\*.snupkg` with `--skip-duplicate` |
| 8. Artifact | Uploads every `.nupkg` as `nuget-packages` |

What it does **not** do: run tests, read the release tag, or bump anything. The version pushed is
the `<Version>` in `Directory.Build.props` at the built commit; if that version is already on
nuget.org the push is skipped and the run still succeeds.

## Checklist: cutting a release

1. Every test project passes, one project and one TFM at a time (`barbatos-wpf-testing`) - CI
   will not catch a failure.
2. Bump `<Version>` and `<AssemblyVersion>` in `Directory.Build.props` (`AssemblyVersion` stays
   numeric; a prerelease suffix goes on `Version` only).
3. Move the copies: the `barbatos-wpf-app` template's `template.json` (`replaces` and
   `defaultValue`), the four `Version="..."` in its `BarbatosWpfApp.csproj`, the default in
   `templates/README.md`. Then `grep -rn "<old version>" --exclude-dir=.git` finds nothing
   (`barbatos-templates`).
4. Each package's `README.md` / `API-REFERENCE.md` describe what ships (`barbatos-docs-sync`).
5. Merge, then publish a GitHub Release on that commit; its notes are the package release notes.
6. In the run, both push steps report pushed packages, not "already exists"; the
   `nuget-packages` artifact holds four `.nupkg`.

## Checklist: adding a package

A new package is a maintainer decision - ask before starting.

1. `src/Barbatos.Wpf.Xyz/Barbatos.Wpf.Xyz.csproj`, shaped like `Barbatos.Wpf.Aquarius.csproj`:
   `PackageId`, `TargetFrameworks` (`net10.0-windows;net9.0-windows;net8.0-windows`, or Core's
   SDK-suffixed form if it needs WinRT), `GeneratePackageOnBuild`, `UseWPF`, a wholesale
   `CommonTags` with its comment, `Description`, `RootNamespace`, `PackageIconSourceFile`, a
   commented `NoWarn`. The name must not contain `Test`.
2. `README.md` and `API-REFERENCE.md` beside the `.csproj` (`barbatos-docs-sync`); without the
   README the pack step fails.
3. `build/xyz-logo.png` (and its `.svg`).
4. `AssemblyInfo.cs` with the MIT header, one `XmlnsDefinition` per CLR namespace and one
   `XmlnsPrefix`, following the `http://schemas.barbatos.co/<package>/2026/xaml` pattern with a
   prefix distinct from `aq`/`aqr`/`aqv`.
5. DI entry point: `AddXyz`/`UseXyz` (Aquarius family) or `ConfigureXyz` (Core-dependent).
6. Add the project under the `/src/` folder of `Barbatos.Wpf.slnx` - that is what CI builds and
   pushes.
7. `tests/Barbatos.Wpf.Xyz.UnitTests` (TFMs `net8.0-windows;net10.0-windows`, `IsPackable`
   `false`, `IsTestProject` `true`, `OutputType` `Exe`) under `/tests/`, and
   `samples/Barbatos.Wpf.Xyz.Sample` under `/samples/`.
8. Root `README.md` package table, root `API-REFERENCE.md` package list, the package and XAML
   tables in `CLAUDE.md`, and the project template if it should offer the package
   (`barbatos-templates`).
