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
  `Setup.ServiceProvider = app.Services;` after `Build()`; in `MainWindow.xaml` the `aq` and
  `local` xmlns plus `aq:Setup.ViewModel="{x:Type local:MainViewModel}"` (explicit type -
  `MainWindow` does not end in `View`, so `Setup.Enable`'s convention would find nothing) and
  the `Count` text/button; in `MainViewModel.cs` `Ref<int> Count` and `[RelayCommand] Increment`.
  When it is **off**, `MainWindow`'s constructor takes `MainViewModel` and sets `DataContext`
  itself - the only path that does not go through `Setup`.
- `IncludeAquariusRouter`: the Router `PackageReference`; the route files;
  `builder.Services.AddSingleton(_ => new Router(AppRoutes.Build()))` and
  `Router.Current = app.Services.GetRequiredService<Router>()`; in `MainWindow.xaml` a `DockPanel`
  with a nav bar (`aqr:RouterLink.To="/"` and `{aqr:RouteTo Name=about}`), a search `TextBox` +
  `ListBox`, and `<aqr:RouterView Margin="16" />`; `MainWindow.OnSourceInitialized` runs
  `await Router.Current!.Start("/")` (a memory-history router has no URL to seed the first route);
  `MainViewModel` takes `Router`, builds a `RouteSearchIndex`, and exposes `SearchQuery`,
  `SearchResults`, `HasSearchResults` and `GoToSearchResultCommand`.
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
config) or its runtime service toggles it - the template sets neither, and generates no toggle UI.

| Flag | Generated code | Behaviour of the generated app |
| --- | --- | --- |
| `IncludeSingleInstance` | `builder.ConfigureSingleInstance();` | On; keyed by `AppInfo.AppId` |
| `IncludeRunOnStartup` | `builder.ConfigureRunOnStartup();` | Off by default - nothing is written to the Run key |
| `IncludeTrayIcon` | `ConfigureTrayIcon` with "Open" (`IsDefault`), `TrayMenuItem.Separator`, "Exit", both via `IWpfPlatformApplication.Current` | Off by default - `TrayIconOptions.Enabled` is `false`, so no icon appears |
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
  `#if (IncludeI18n || IncludeDialogs || IncludeAquariusRouter)`, and each comma has its own
  condition (`IncludeI18n && IncludeDialogs`, then `(IncludeI18n || IncludeDialogs) &&
  IncludeAquariusRouter`). A fourth injected service means widening the outer condition and
  adding a comma condition that covers every earlier parameter.
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

- `<Version>1.0.0</Version>` with `<AssemblyVersion>$(Version)</AssemblyVersion>` and
  `<FileVersion>$(Version)</FileVersion>`: one value for all three, so the number Explorer and
  installers read moves with the one `AppInfo` reports. Both derived properties accept numeric
  parts only - for a prerelease `<Version>` the csproj comment says to set them to the bare
  `x.y.z`.
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
