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
  `Version`, never a `ProjectReference` into this repo), so it restores outside the repo. Its
  `PackageVersion` symbol's `replaces` and `defaultValue` must both equal the four `Version="…"`
  values in `BarbatosWpfApp.csproj` and track `<Version>` in `Directory.Build.props`.
- **Each optional module is a boolean `Include*` symbol**, used as `#if (IncludeXxx)` in C#, as
  `<!--#if (IncludeXxx) -->` in the `.csproj` and XAML, and — for whole files — as a
  `sources[].modifiers` `exclude` entry in `template.json`. A new symbol also needs its
  `ide.host.json` entry (Visual Studio's checkbox) and a line in `templates/README.md`. Router and Valee pull Aquarius in through
  the computed `AquariusEffective` symbol — keep that when touching either flag.
- **Templates track the public API.** When an API used by a template changes, or the scaffolded
  shape should change, update the template and `templates/README.md` in the same change.
- **Test the template, don't just read it:** `dotnet new install <folder>` (re-run after every
  edit — installed templates are cached), instantiate into a scratch folder, build it, then
  `dotnet new uninstall <folder>`. To test against unreleased `src/` changes, pack locally and
  delete `~/.nuget/packages/barbatos.wpf.*/<version>/` first, or NuGet silently reuses the old
  extracted package of the same version.
