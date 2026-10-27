# `barbatos-wpf-app` - what each symbol generates, file by file

Contents: [Files](#files-and-which-symbol-keeps-them) ·
[Aquarius family](#the-aquarius-family-flags) · [i18n](#includei18n) ·
[Core flags](#the-11-core-flags) · [Conditional seams](#conditional-seams-that-break-when-edited) ·
[Identity block](#the-identity-block-in-the-csproj)

All paths are under
[`templates/Combined/project-templates/barbatos-wpf-app/`](../../../templates/Combined/project-templates/barbatos-wpf-app/).
`BarbatosWpfApp` is the `sourceName`: it becomes the project name in file names, namespaces,
`<Product>`, the `AppId` metadata, `MainWindow`'s title, the push `AppId` and the agent name.

## Files and which symbol keeps them

| File | Generated when |
| --- | --- |
| `BarbatosWpfApp.csproj`, `App.xaml(.cs)`, `WpfProgram.cs`, `MainWindow.xaml(.cs)`, `MainViewModel.cs` | always |
| `AppRoutes.cs`, `HomeView.xaml(.cs)`, `HomeViewModel.cs`, `AboutView.xaml(.cs)`, `AboutViewModel.cs` | `IncludeAquariusRouter` |
| `SignUpView.xaml(.cs)`, `SignUpViewModel.cs` | `IncludeValee` |
| `AboutWindow.xaml(.cs)` | `IncludeDialogs` |
| `HeartbeatService.cs` | `IncludePeriodicServices` |

Whole-file inclusion is decided only by `sources[0].modifiers` in `template.json`
(`"condition": "(!IncludeXxx)", "exclude": [...]`). A new optional file needs an entry there.

## The Aquarius-family flags

**`AquariusEffective`** (computed: `IncludeAquarius || IncludeAquariusRouter || IncludeValee`) is
what every Aquarius-dependent site tests, never `IncludeAquarius` itself - Router and Valee depend
on Aquarius, so ticking either pulls it in while the Aquarius checkbox stays unticked.

- `AquariusEffective`: the `Barbatos.Wpf.Aquarius` `PackageReference`; in `WpfProgram.cs`
  `builder.Services.AddAquarius();` before `Build()` and `app.Services.UseAquarius();` after it
  (which points `Setup.ServiceProvider` at the container); in `MainWindow.xaml` the `aq` and
  `local` xmlns plus `aq:Setup.ViewModel="{x:Type local:MainViewModel}"` (explicit type -
  `MainWindow` does not end in `View`, so `Setup.Enable`'s convention would find nothing) and
  the `Count` text/button; in `MainViewModel.cs` `Ref<int> Count` and `[RelayCommand] Increment`.
  When it is **off**, `MainWindow`'s constructor takes `MainViewModel` and sets `DataContext`
  itself - the only path that does not go through `Setup`.
- `IncludeAquariusRouter`: the Router `PackageReference`; the route files;
  `builder.Services.AddAquariusRouter(_ => AppRoutes.Build());` before `Build()` and
  `app.Services.UseAquariusRouter();` after it (which publishes `Router.Current`); in
  `MainWindow.xaml` a `DockPanel` with a nav bar (`aqr:RouterLink.To="/"` and
  `{aqr:RouteTo Name=about}`), a search `TextBox` + `ListBox`, and
  `<aqr:RouterView Margin="16" />`; `MainWindow.OnSourceInitialized` runs
  `await Router.Current!.Start("/")` (a memory-history router has no URL to seed the first route);
  `MainViewModel` takes `Router`, builds a `RouteSearchIndex`, and exposes `SearchQuery`,
  `SearchResults`, `HasSearchResults` and `GoToSearchResultCommand`, which pushes
  `RouteSearchResult.To` - always the record's `Name`, and an `InvalidOperationException` for a
  searchable route without one.
  `HomeView` finds `HomeViewModel` by `aq:Setup.Enable`; `AboutView` names no ViewModel and is
  paired with `AboutViewModel` by `RouteRecord.ViewModel` - the two wirings are deliberate.
- `IncludeValee`: the Valee `PackageReference`; the SignUp files (ad-hoc `DefineField` mode with
  `Valee.Field`, `Valee.PasswordField`, `Valee.Mask`, `Valee.Numeric="Integer"`);
  `builder.Services.AddTransient<SignUpViewModel>()` (transient so a revisit starts blank).
  Without the router it is hosted inline (`<local:SignUpView />`, gated
  `IncludeValee && !IncludeAquariusRouter`); with it, `AppRoutes.cs` adds the `/signup` route
  named `signup` and `MainWindow.xaml` adds a "Sign up" `RouterLink`.

## `IncludeI18n`

Default on. `Barbatos.i18n.Wpf` + `Barbatos.i18n.DependencyInjection` references (their own
version, not `PackageVersion`); `AddStringLocalizer` with in-memory `en-US`/`vi-VN` dictionaries;
`app.Services.UseWpfLocalization().SetLocalizationCulture(new CultureInfo("en-US"))`; in
`MainWindow.xaml` the `i18n` xmlns (`http://schemas.barbatos.co/i18n/2026/xaml`) and
`{i18n:StringLocalizer Text='Greeting', BindArg={Binding CurrentCultureName}}`; in
`MainViewModel` an `ILocalizationCultureManager` parameter, `CurrentCultureName` and
`SwitchLanguageCommand`. Keep `BindArg`, not the static `Arg`: Barbatos.i18n raises nothing when
the culture changes, so only a live binding argument re-evaluates the text.

## The 11 Core flags

All but the splash screen add their `ConfigureXxx` call to `WpfProgram.cs` between
`CreateBuilder()` and `Build()`. The `ConfigureXxx` methods live in `Barbatos.Wpf.Hosting`; the
per-flag `using` lines bring in the feature's own types. "Off by default" means the service is
registered but does nothing until its options `Enabled` is set (code or `Barbatos:<Feature>`
config, which overrides code) or its runtime service toggles it.

| Flag | Generated code | Behaviour of the generated app |
| --- | --- | --- |
| `IncludeSingleInstance` | `builder.ConfigureSingleInstance();` | On; keyed by `AppInfo.AppId` |
| `IncludeRunOnStartup` | `builder.ConfigureRunOnStartup();` with `Enabled` left `false`; `MainViewModel` takes `IRunOnStartupService` and exposes `RunOnStartupEnabled` (get `IsEnabled`, set `SetEnabled`); `MainWindow.xaml` binds a "Launch when Windows starts" `CheckBox` to it | Nothing is written to the Run key until the user ticks the box - never register a first run unasked |
| `IncludeTrayIcon` | `ConfigureTrayIcon` setting `options.Enabled = true`, with "Open" (`IsDefault`; shows, un-minimizes and activates the main window), `TrayMenuItem.Separator`, "Exit", both via `IWpfPlatformApplication.Current` | On - icon shown from launch; `Barbatos:TrayIcon:Enabled` in configuration overrides the code |
| `IncludeKeepAwake` | `builder.ConfigureKeepAwake();` | Off by default |
| `IncludeNotifications` | `builder.ConfigureNotifications();` | On |
| `IncludePushNotifications` | `ConfigurePushNotifications` setting `options.Enabled = false` and `DeduplicationHistorySize = 256`, and `configureSignalR:` setting `ServerUrl`, `AppId`, `Tags["channel"]` (a comment points at `AppKey` / `AcknowledgeMethodName`) | Deliberately disabled - no server exists |
| `IncludePeriodicServices` | `ConfigurePeriodicServices<HeartbeatService>()` + `HeartbeatService.cs` (`PeriodicFrequency.Custom`, 5 s, `Debug.WriteLine`) | On |
| `IncludeInputSystem` | `InputActionMap("AppShortcuts")`, action `ShowWindow`, `AddKeyBinding(Key.F9, ModifierKeys.Control)`, `Performed` shows the main window | On (in-app binding only) |
| `IncludeDialogs` | `ConfigureDialogs()`, `AddTransient<AboutWindow>()`, `IDialogService` injected into `MainViewModel`, `ShowAboutCommand` → `Show<AboutWindow>()`, an About button | On; transient because a closed `Window` cannot be shown again |
| `IncludeMcp` | `ConfigureMcp` with a stdio `McpServerDescriptor` (`dotnet dnx NuGet.Mcp.Server --version <pinned> --yes`) and `configureAgent` (`Name`, `Instructions`) | No provider or key - BYOK; `ToolApproval` left at `AiToolApprovalMode.Always` |
| `IncludeSplashScreen` | In `App.xaml.cs`: `GetSplashScreenOptions()` override (`Tagline`, `MinimumDisplayDuration`), and `OnStartup` becomes `async void` with `await CloseSplashScreenAsync()` before the main window is resolved | On |

## Conditional seams that break when edited

- **`MainViewModel`'s constructor** is a comma ladder: the parameter list exists only under
  `#if (IncludeI18n || IncludeDialogs || IncludeAquariusRouter || IncludeRunOnStartup)`, and each
  comma has its own condition (`IncludeI18n && IncludeDialogs`, then `(IncludeI18n ||
  IncludeDialogs) && IncludeAquariusRouter`, then `(IncludeI18n || IncludeDialogs ||
  IncludeAquariusRouter) && IncludeRunOnStartup`). A fifth injected service means widening the
  outer condition and adding a comma condition that covers every earlier parameter.
- **`MainWindow.xaml` opens and closes its layout in two places**: the router branch opens a
  `DockPanel` + inner `StackPanel` at the top and closes them (plus `RouterView`) at the bottom
  under a second `<!--#if (IncludeAquariusRouter) -->` / `<!--#else -->`. Change one half, change
  the other.
- **Conditionals sit inside the `<Window ...>` start tag** (xmlns lines and `aq:Setup.ViewModel`),
  so the raw file is not well-formed XAML. It is only valid after the template engine runs.
- **`using` lines are gated with the code that needs them**, including
  `#if (IncludeTrayIcon || IncludeInputSystem)` for `System.Windows`. Code you add under another
  flag that touches `Window` must widen that condition.
- **`MainWindow.xaml.cs`** wraps the constructor parameter and the `DataContext` assignment in
  `#if (!AquariusEffective)`, and `OnSourceInitialized` in `#if (IncludeAquariusRouter)`.

## The identity block in the csproj

- `<Version>1.0.0</Version>` alone. The SDK derives `AssemblyVersion` and `FileVersion` from it
  with any prerelease suffix stripped (`1.1.0-beta.1` -> `1.1.0.0`), so `AppInfo.Version` (the
  assembly version) and the file version Explorer shows move with it. Never add
  `<AssemblyVersion>$(Version)</AssemblyVersion>`: it fails a prerelease build with `CS7034`.
  The repo's samples do carry that line, because they inherit `Directory.Build.props`' pinned
  library `<AssemblyVersion>` - a generated app has no such file.
- `<Product>` (= project name), `<Company>Your Company</Company>`, `<Copyright>`: feed `AppInfo` /
  `PublisherInfo`. `Company` + `AppId` decide `%LocalAppData%\{Company}\{AppId}\`, so changing
  either after shipping strands user data.
- `<AssemblyMetadata Include="Barbatos.Wpf.ApplicationModel.AppInfo.AppId" Value="BarbatosWpfApp" />`:
  a readable string, not a GUID. A commented-out `...AppInfo.UninstallRegistryKey` line is there
  for an installer whose key name differs from `AppId` (Inno Setup appends `_is1`). Details belong
  to `barbatos-wpf-essentials`.
- `TargetFramework` is `net10.0-windows10.0.17763.0`; keep the SDK-suffixed form (CLAUDE.md -
  toast notifications need it). `Nullable` / `ImplicitUsings` are set explicitly because the
  generated project has no `Directory.Build.props` to inherit them from.
