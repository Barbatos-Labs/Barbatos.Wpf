# Barbatos.Wpf templates

`dotnet new` templates for this repo's packages - the closest equivalent to how a Vue
project scaffolds a new `.vue` single-file component, or how `npm create vue@latest`
scaffolds a whole starter app.

## Layout

Organized by which package(s) a template belongs to, then by template kind:

```
templates/
  Barbatos.Wpf.Aquarius/
    item-templates/       - generates files into an existing project (e.g. a View + ViewModel)
      aquarius-view/
    project-templates/    - generates a whole new project
      .gitkeeper           (none yet)
  Barbatos.Wpf.Core/
    item-templates/
      .gitkeeper           (none yet)
    project-templates/
      .gitkeeper           (none yet)
  Combined/                - templates that span more than one package
    item-templates/
      .gitkeeper           (none yet)
    project-templates/
      barbatos-wpf-app/    - a starter app referencing both Core and Aquarius
```

An empty leaf folder keeps a `.gitkeeper` placeholder so the directory itself stays
tracked in git even with no template in it yet.

## Install

From the repo root, install whichever template(s) you need - each is an independent
folder:

```powershell
dotnet new install ./templates/Barbatos.Wpf.Aquarius/item-templates/aquarius-view
dotnet new install ./templates/Combined/project-templates/barbatos-wpf-app
```

Re-run the same command after editing a template's files to pick up the change
(`dotnet new` caches installed templates).

## `aq-view` - Aquarius View + ViewModel (item template)

From inside any project that references `Barbatos.Wpf.Aquarius`:

```powershell
dotnet new aq-view -n Dashboard --namespace MyApp.Features.Dashboard
```

Generates, in the current directory:
- `DashboardView.xaml` - a `UserControl` with `aq:Setup.Enable="True"` already set.
- `DashboardView.xaml.cs` - just `InitializeComponent()`.
- `DashboardViewModel.cs` - an `ObservableObject` with one example `[ObservableProperty]`,
  picked up automatically by `Setup`'s naming convention (`DashboardView` ->
  `DashboardViewModel`) - no manual `DataContext = new DashboardViewModel()` needed.

`--namespace` is a plain required-in-practice parameter, not auto-detected from the
consuming project: `dotnet new`'s CLI does not evaluate the target project's MSBuild
`RootNamespace` the way Visual Studio's own "Add New Item" dialog can for its built-in
item templates (confirmed by testing - the CLI path silently skips a `msbuild:`-bound
symbol rather than resolving it). Omitting `--namespace` still generates working files, just
under the obviously-fake placeholder namespace `ChangeMe.Namespace`, so a forgotten override
is easy to spot rather than silently landing in the wrong namespace.

## `barbatos-wpf-app` - Aquarius + Core + i18n starter app (project template)

```powershell
dotnet new barbatos-wpf-app -n MyApp
```

A complete, runnable WPF app in a new `MyApp/` folder, referencing all three packages via
`PackageReference` (not a `ProjectReference` into this repo - a real, portable starter a
consumer outside this repo can restore once `Barbatos.Wpf.Core`/`Barbatos.Wpf.Aquarius` are
published too) and showing them working together, not just coexisting:
- `Barbatos.Wpf.Core`'s `WpfApp`/`WpfAppBuilder` supplies the DI container.
- `Barbatos.Wpf.Aquarius`'s `Setup.ServiceProvider` is pointed at that same container, so
  `MainWindow`'s `aq:Setup.ViewModel="{x:Type local:MainViewModel}"` resolves `MainViewModel`
  through it instead of a bare `Activator.CreateInstance` (the explicit-type form, not
  `Setup.Enable`'s naming convention - `MainWindow` doesn't end in `"View"`, so the
  convention wouldn't find anything).
- `Barbatos.i18n`'s `AddStringLocalizer`/`UseWpfLocalization` are wired into that same
  container too, so `Barbatos.i18n.Wpf`'s `{i18n:StringLocalizer}` markup extension has a
  provider to read from - demonstrated with an English/Vietnamese greeting and a live language
  switch.

`Barbatos.Wpf.Core`/`Barbatos.Wpf.Aquarius` aren't published to NuGet.org yet as of this
writing - restoring a freshly generated project will fail with NU1101 for those two until
they are (see the template's own `.template.config/template.json` for the `--PackageVersion`
parameter, if the version needs to move once they do publish). `Barbatos.i18n.Wpf`/
`Barbatos.i18n.DependencyInjection` are already published and resolve normally.

## Uninstall

```powershell
dotnet new uninstall ./templates/Barbatos.Wpf.Aquarius/item-templates/aquarius-view
dotnet new uninstall ./templates/Combined/project-templates/barbatos-wpf-app
```
