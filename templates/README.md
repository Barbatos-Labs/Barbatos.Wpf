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
  Barbatos.Wpf.AquariusRouter/
    item-templates/
      .gitkeeper           (none yet)
    project-templates/
      .gitkeeper           (none yet)
  Barbatos.Wpf.AquariusValee/
    item-templates/
      valee-form/
    project-templates/
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
      barbatos-wpf-app/    - a starter app referencing Core, Aquarius, and whichever
                             optional modules you tick
```

An empty leaf folder keeps a `.gitkeeper` placeholder so the directory itself stays
tracked in git even with no template in it yet.

## Install

From the repo root, install whichever template(s) you need - each is an independent
folder:

```powershell
dotnet new install ./templates/Barbatos.Wpf.Aquarius/item-templates/aquarius-view
dotnet new install ./templates/Barbatos.Wpf.AquariusValee/item-templates/valee-form
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
- `DashboardView.xaml` - a `UserControl` with `aq:Setup.Enable="True"` and
  `aq:Lifecycle.Enable="True"` already set, so the ViewModel's lifecycle hooks fire.
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

## `aqv-form` - Valee form View + ViewModel (item template)

From inside any project that references `Barbatos.Wpf.AquariusValee`:

```powershell
dotnet new aqv-form -n Checkout --namespace MyApp.Features.Checkout
```

Generates, in the current directory:
- `CheckoutView.xaml` - a `UserControl` with `aq:Setup.Enable="True"` and `aqv:Valee.Form`
  already set on its container, plus a `TextBox`/`PasswordBox` per field wired through
  `aqv:Valee.Field`/`aqv:Valee.PasswordField`, an error `TextBlock` each, and a submit button.
- `CheckoutView.xaml.cs` - just `InitializeComponent()`.
- `CheckoutViewModel.cs` - the `Form`, its three `Field<T>`s, and the `HandleSubmit` command -
  plus `CheckoutModel`, the plain class carrying the `DataAnnotations` attributes.

It scaffolds **schema mode** (`Form.FromModel<T>`), not ad-hoc `DefineField` calls, because
that is the mode where a cross-field attribute like `[Compare]` resolves at all - it needs a
live model instance to read the sibling property from. The generated ViewModel also calls
`ConfirmPassword.DependsOn(Password)`: `[Compare]` gives the right verdict whenever the confirm
field is validated, but only attributes implementing `IDependsOnProperties` (e.g.
`PropertyRangeAttribute`) get re-validation wired up automatically, so without that line fixing
the password leaves a stale "don't match" error sitting under the confirm box.

`--namespace` behaves exactly as it does for `aq-view` above, and for the same reason.

## `barbatos-wpf-app` - Aquarius + Core + i18n starter app (project template)

```powershell
dotnet new barbatos-wpf-app -n MyApp
```

A complete, runnable WPF app in a new `MyApp/` folder, referencing `Barbatos.Wpf.Core` and,
by default, `Barbatos.Wpf.Aquarius`/`Barbatos.i18n` too - all via `PackageReference` (not a
`ProjectReference` into this repo - a real, portable starter a consumer outside this repo can
restore as-is), showing them working together, not just coexisting:
- `Barbatos.Wpf.Core`'s `WpfApp`/`WpfAppBuilder` supplies the DI container.
- `Barbatos.Wpf.Aquarius` is wired into that same container with the library's two-call
  pattern: `builder.Services.AddAquarius()` before `Build()`, then `app.Services.UseAquarius()`
  after it, which points `Setup.ServiceProvider` at the container. `MainWindow`'s
  `aq:Setup.ViewModel="{x:Type local:MainViewModel}"` therefore resolves `MainViewModel`
  through DI instead of a bare `Activator.CreateInstance` (the explicit-type form, not
  `Setup.Enable`'s naming convention - `MainWindow` doesn't end in `"View"`, so the
  convention wouldn't find anything). The second call exists because XAML has no constructor
  to inject into: markup-driven resolution needs an ambient provider.
- `Barbatos.i18n`'s `AddStringLocalizer`/`UseWpfLocalization` are wired into that same
  container too, so `Barbatos.i18n.Wpf`'s `{i18n:StringLocalizer}` markup extension has a
  provider to read from - demonstrated with an English/Vietnamese greeting and a live language
  switch.

Every package the template references is published on NuGet.org, so a freshly generated
project restores as-is. It references `Barbatos.Wpf.Core`/`Aquarius`/`AquariusRouter`/
`AquariusValee` 2.5.1 by default - pass `--PackageVersion` (see the template's own
`.template.config/template.json`) to target another release.

To try the template against unreleased changes in this repo, pack its `src/` projects and
point a `nuget.config` next to the generated project at that folder. One trap worth knowing when you do: NuGet
resolves an already-extracted package out of the global cache (`~/.nuget/packages`) purely by
id+version and never re-checks your local feed, so a `2.4.0` you packed last week keeps being
used after you rebuild `2.4.0` today. The symptom is a type the library definitely has going
missing (`The tag 'RouteTo' does not exist in XML namespace ...`); the fix is to delete
`~/.nuget/packages/barbatos.wpf.*/<version>/` before restoring, or to pack under a
`-dev.N`-suffixed version instead of overwriting the same one.

### App identity and version, out of the box

The generated `.csproj` ships a filled-in identity block, because both halves of it are things
that are painful to change *after* you ship:

```xml
<Version>1.0.0</Version>
...
<AssemblyMetadata Include="Barbatos.Wpf.ApplicationModel.AppInfo.AppId" Value="MyApp" />
```

- **`<Version>` is the only version property you bump.** The .NET SDK derives
  `AssemblyVersion` and `FileVersion` from it, keeping only the numeric part (`1.0.0` ->
  `1.0.0.0`, `1.1.0-beta.1` -> `1.1.0.0`), and keeps the full string as
  `InformationalVersion`. `AppInfo.Version` reads the assembly version, so it moves with
  `<Version>`, and so does the file version Windows Explorer and installers show (confirmed by
  building a probe project). The template deliberately does not add
  `<AssemblyVersion>$(Version)</AssemblyVersion>`: it changes nothing for `1.0.0`, and with a
  prerelease `<Version>` the build fails with `CS7034`, because an assembly version takes
  numeric parts only. This repo's samples do carry those two lines, for a reason a standalone
  app does not have: they inherit the libraries' pinned `<AssemblyVersion>` from the repo's
  `Directory.Build.props`.
- **`AppId` defaults to your project name, not a GUID.** It does not have to be one - it is the
  app's stable identifier and doubles as the storage folder name
  (`%LocalAppData%\{Company}\{AppId}\`), so a readable string is usually the better choice.
  Change `<Company>` before shipping for the same reason; both are effectively permanent once
  users have data. The template also carries a commented-out `UninstallRegistryKey` line for
  when you add an installer - see *Reading back InstallDate and InstallLocation* in
  `src/Barbatos.Wpf.Core/README.md`.

### Picking which modules to include

Beyond the packages above, `Barbatos.Wpf.Core` alone has 11 independently-optional
features (single instance, tray icon, InputSystem, MCP, ...) - the template exposes every one
of these, plus `Barbatos.Wpf.AquariusRouter` (page navigation), `Barbatos.Wpf.AquariusValee`
(form validation) and `Barbatos.i18n` (localization) themselves, as boolean `--Include*` flags
rather than baking a fixed feature set in:

```powershell
dotnet new barbatos-wpf-app -n MyApp --IncludeAquariusRouter true --IncludeValee true --IncludeMcp true
dotnet new barbatos-wpf-app -n MyApp --help   # lists every flag, its description, and its default
```

**Visual Studio's "Create a new project" wizard renders these as real checkboxes** - each
symbol's `displayName`/`description` in `template.json`, surfaced via the sibling
`.template.config/ide.host.json` (`symbolInfo[].isVisible`), is what drives that UI; this is a
genuine, long-standing Visual Studio feature, not something specific to this template.
**JetBrains Rider does not render per-flag checkboxes** even in its newest versions - only a
single raw "extra parameters" text field for the whole `dotnet new` invocation - so from Rider
(or the plain CLI) you pass the same `--Include*` flags shown above by hand instead of ticking
boxes. The generated project is identical either way; only how you choose the flags differs.

`Barbatos.Wpf.AquariusRouter` and `Barbatos.Wpf.AquariusValee` each have a hard dependency on
`Barbatos.Wpf.Aquarius` (a real `ProjectReference` in the libraries themselves), so checking
either one alone still generates a project referencing Aquarius too, via a computed symbol
(`AquariusEffective`) - the "Aquarius" checkbox itself can stay visually unchecked while its
content is included anyway. Every other flag is fully independent. Running the template with no
`--Include*` flags at all reproduces today's exact default (`Aquarius`+`i18n` on, everything
else off) - nothing about the default experience changed.

### What each of the three Aquarius-family flags adds

- **Aquarius** - `Ref<int> Count` on `MainViewModel`, bound in `MainWindow.xaml`, and the
  `AddAquarius`/`UseAquarius` bridge described above.
- **AquariusRouter** - `AppRoutes.cs`, a `RouterView` outlet, and a `RouterLink` nav bar. The
  router is registered with `builder.Services.AddAquariusRouter(_ => AppRoutes.Build())` and
  published as `Router.Current` by `app.Services.UseAquariusRouter()`, so the outlet and links
  need no `Router` of their own. The two routes deliberately demonstrate the *two different
  ways* a View finds its ViewModel: `HomeView` carries its own `aq:Setup.Enable` (naming
  convention), while `AboutView` names no ViewModel at all and is paired with `AboutViewModel`
  from the route table via `RouteRecord.ViewModel` - the option to reach for when View and
  ViewModel ship in libraries you can't edit. The nav bar also shows both
  `RouterLink.To="/"` (literal path) and `RouterLink.To="{aqr:RouteTo Name=about}"` (by route
  name), and the search box above it runs `RouteSearchIndex` over the `Search` entries declared
  in `AppRoutes.cs`. A result navigates by route `Name`, so every searchable route needs one.
- **Valee** - `SignUpView`/`SignUpViewModel`: an ad-hoc (`DefineField`) form covering all four
  `Valee.*` control bindings - `Field` on a `TextBox`, `PasswordField` on a `PasswordBox`,
  `Mask` for a phone number, and `Numeric` clamped to its `[Range]`. With AquariusRouter also
  enabled it becomes the `/signup` route; on its own it is hosted inline in `MainWindow`.
  For the schema-mode counterpart (`Form.FromModel`), scaffold an `aqv-form` next to it.

`--IncludeTrayIcon` shows the icon from launch: the library registers it hidden
(`TrayIconOptions.Enabled` defaults to `false`, so an app can offer it as an opt-in setting), and
the generated `ConfigureTrayIcon` call sets `options.Enabled = true` because ticking the flag
means you want one. `Barbatos:TrayIcon:Enabled` in configuration overrides that line, and
`ITrayIconService.SetVisible` toggles it at runtime. `--IncludeRunOnStartup` is the opposite on
purpose: it leaves `RunOnStartupOptions.Enabled` off, because setting it would register your app
with Windows startup on its very first run, before anyone was asked, and instead adds a "Launch
when Windows starts" checkbox to `MainWindow` that calls `IRunOnStartupService.SetEnabled`.
`--IncludeKeepAwake` registers its service off, with no toggle: call
`IKeepAwakeService.SetEnabled(true)` while the app needs the machine awake.

Two more Core flags are worth calling out because what they generate has grown:
`--IncludeMcp` now also configures the Microsoft Agent Framework layer (`configureAgent`), which
is registered alongside the one-shot `IAiChatService` rather than replacing it, and is left at
its default `AiToolApprovalMode.Always` so a tool call pauses for your UI to approve it;
`--IncludePushNotifications` shows the de-duplication window and the SignalR `Tags` handshake,
and points at `AppKey`/`AcknowledgeMethodName` in a comment. Neither changes what the flag is
for - only how much of the module's surface the generated code puts in front of you.

## Uninstall

```powershell
dotnet new uninstall ./templates/Barbatos.Wpf.Aquarius/item-templates/aquarius-view
dotnet new uninstall ./templates/Barbatos.Wpf.AquariusValee/item-templates/valee-form
dotnet new uninstall ./templates/Combined/project-templates/barbatos-wpf-app
```
