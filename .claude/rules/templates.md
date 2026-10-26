---
paths:
  - "templates/**"
---

# `dotnet new` templates under `templates/`

Load `barbatos-templates` for the full template map, the symbols and the test loop. What is easy
to break:

- **Layout is package → kind → template:** `templates/<Package or Combined>/{item-templates,
  project-templates}/<template>/.template.config/template.json`. An empty leaf folder keeps a
  `.gitkeeper` so it stays tracked.
- **Generated code is the user's code.** No MIT header, no repo-only conventions. Item templates
  use the obviously-fake `ChangeMe.Namespace` default for `--namespace` on purpose — the CLI
  cannot read the target project's `RootNamespace`.
- **The project template references published packages** (`PackageReference` with an explicit
  `Version`, never a `ProjectReference` into this repo), so it restores outside the repo. Never
  remove those `Version=` attributes. Its `PackageVersion` symbol's `replaces` and `defaultValue`
  must both equal the four `Barbatos.Wpf.*` `Version="…"` values in `BarbatosWpfApp.csproj` and
  track `<Version>` in `Directory.Build.props` (the two `Barbatos.i18n.*` references pin their own
  version and follow `Directory.Packages.props` instead).
- **Each optional module is a boolean `Include*` symbol**, used as `#if (IncludeXxx)` in C#, as
  `<!--#if (IncludeXxx) -->` in the `.csproj` and XAML, and — for whole files — as a
  `sources[].modifiers` `exclude` entry in `template.json`. A new symbol also needs its
  `ide.host.json` entry (Visual Studio's checkbox), and a new Core flag bumps the "11
  independently-optional features" count in `templates/README.md`. Router and Valee pull Aquarius
  in through the computed `AquariusEffective` symbol — keep that when touching either flag.
- **Templates track the public API.** When an API used by a template changes, or the scaffolded
  shape should change, update the template and `templates/README.md` in the same change. The
  install and usage commands are repeated in the root `README.md` Templates section and the
  Aquarius README Quick Start — change all three together.
- **Test the template, don't just read it.** After every edit run `dotnet new uninstall <folder>`
  then `dotnet new install <folder>` (installed templates are cached). Instantiate into a scratch
  folder **outside this clone** — inside it the repo's `Directory.Packages.props` turns the
  template's `Version=` attributes into `NU1008`, and the repo's `Directory.Build.props` and
  `nuget.config` apply too. Build it, then uninstall.
- **Testing against unreleased `src/` changes:** pack each package to a local folder under a fresh
  `<next>-dev.<n>` version, put a `nuget.config` listing that folder beside the generated project,
  and instantiate with `--PackageVersion <next>-dev.<n>`. Never re-pack the same version: NuGet
  silently reuses the extracted `~/.nuget/packages/barbatos.wpf.*/<version>/`.
