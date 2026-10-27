# Barbatos.Wpf.Core

![Barbatos.Wpf.Core logo](https://raw.githubusercontent.com/Barbatos-Labs/Barbatos.Wpf/main/build/core-logo.svg)

### *A modern application host and ready-made platform services for WPF*

**`WpfApp`/`WpfAppBuilder` give plain desktop WPF a real application host - dependency
injection, configuration, logging, and lifecycle events - plus a broad set of ready-made
platform services (`AppInfo`, `Preferences`, `SecureStorage`, `Connectivity`, ...)
registered in the same container.**

[![NuGet](https://img.shields.io/nuget/v/Barbatos.Wpf.Core.svg)](https://www.nuget.org/packages/Barbatos.Wpf.Core)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Barbatos.Wpf.Core.svg)](https://www.nuget.org/packages/Barbatos.Wpf.Core)
[![GitHub stars](https://img.shields.io/github/stars/Barbatos-Labs/Barbatos.Wpf?style=social)](https://github.com/Barbatos-Labs/Barbatos.Wpf/stargazers)
[![License](https://img.shields.io/github/license/Barbatos-Labs/Barbatos.Wpf)](https://github.com/Barbatos-Labs/Barbatos.Wpf/tree/main/LICENSE.md)

---

## 📖 Documentation Menu

* **[Getting Started](#getting-started)**
  * [Introduction](#introduction)
  * [Quick Start](#quick-start)
* **[Hosting](#hosting)**
  * [App composition](#app-composition-the-wpfprogram-pattern)
  * [Lifecycle events](#lifecycle-events)
  * [Dispatching](#dispatching)
  * [Configuring the hosting environment](#configuring-the-hosting-environment)
* **[Essentials](#essentials)**
  * [Configuring AppInfo and PublisherInfo](#configuring-appinfo-and-publisherinfo)
  * [Publishing with an installer](#publishing-with-an-installer)
  * [Reading back InstallDate and InstallLocation](#reading-back-installdate-and-installlocation)
  * [App actions and version tracking](#app-actions-and-version-tracking)
  * [Contacts and Geolocation](#contacts-and-geolocation)
  * [Permissions](#permissions)
  * [License enforcement: DeviceIdentity](#license-enforcement-deviceidentity)
* **[Optional desktop features](#optional-desktop-features)**
* **[Dialogs](#dialogs)**
  * [Owner assignment](#owner-assignment)
  * [Closing other dialogs, without losing in-progress work](#closing-other-dialogs-without-losing-in-progress-work)
  * [Preventing a double-click from opening a dialog twice](#preventing-a-double-click-from-opening-a-dialog-twice)
* **[SplashScreen](#splashscreen)**
  * [Loading important data before the main window ever appears](#loading-important-data-before-the-main-window-ever-appears)
  * [Full customization](#full-customization)
  * [Showing it conditionally](#showing-it-conditionally)
  * [Avoiding flicker: minimum display duration](#avoiding-flicker-minimum-display-duration)
* **[Periodic services](#periodic-services)**
  * [Registering services after startup](#registering-services-after-startup)
* **[State management (Barbatos.Wpf.Apsu)](#state-management-barbatoswpfapsu)**
  * [Stores](#stores)
  * [Getters](#getters)
  * [Actions and action interceptors](#actions-and-action-interceptors)
    * [Removing an interceptor at runtime](#removing-an-interceptor-at-runtime)
  * [Plugins](#plugins)
  * [Registry](#registry)
* **[AI chat + MCP (Barbatos.Wpf.Mcp)](#ai-chat--mcp-barbatoswpfmcp)**
  * [Hosted servers that need a sign-in (OAuth)](#hosted-servers-that-need-a-sign-in-oauth)
  * [Agents (Microsoft Agent Framework)](#agents-microsoft-agent-framework)
  * [Providers](#providers)
  * [Models (IAiModelCatalog)](#models-iaimodelcatalog)
  * [Attachments (images, documents)](#attachments-images-documents)
  * [Configuration](#configuration)
* **[Ecosystem](#ecosystem)**
  * [Repository layout](#repository-layout)
* **[API Reference](#api-reference)**
* **[Community](#community)**
  * [Maintainers](#maintainers)
  * [Support](#support)
  * [License](#license)

---

## Getting Started

### Introduction

#### What is Barbatos.Wpf.Core?

Traditional WPF apps wire up `Application_Startup`, a hand-rolled DI container, and ad hoc
static helpers for things like app info, preferences, or connectivity. **Barbatos.Wpf.Core**
gives you `WpfApp`/`WpfAppBuilder` — a real `IHostApplicationBuilder` with dependency
injection, configuration, logging, and lifecycle events — plus a dozen ready-made platform
services (`AppInfo`, `Preferences`, `SecureStorage`, `Connectivity`, `Launcher`, ...)
registered in the same container.

> **Familiar from .NET MAUI?** The hosting model and Essentials-style services below are
> deliberately shaped like MAUI's own `MauiApp`/`MauiAppBuilder` and Essentials APIs wherever
> a Windows equivalent exists - a head start if you've used them before, though nothing here
> requires that background.

> **Prerequisites**
>
> The rest of the documentation assumes basic familiarity with C#, .NET Dependency
> Injection, `Microsoft.Extensions.Hosting`, and WPF.

### Quick Start

Add the package via NuGet:

```powershell
dotnet add package Barbatos.Wpf.Core
```

This library ships as a single package — Hosting and every module described below are part
of `Barbatos.Wpf.Core`, there is nothing else to install.

> [!IMPORTANT]
> **Your app's `<TargetFramework>` must include the Windows SDK version suffix** —
> `net8.0-windows10.0.17763.0` / `net9.0-windows10.0.17763.0` / `net10.0-windows10.0.17763.0` —
> not the bare `net8.0-windows` form. A Target Framework Moniker (TFM) like
> `net10.0-windows10.0.17763.0` has three parts: `net10.0` (the .NET version), `-windows` (opts
> into Windows-only APIs — Win32, WPF, the registry, ... — beyond the cross-platform BCL), and
> `10.0.17763.0` (the Windows SDK version the `-windows` APIs are projected from). That last
> part isn't cosmetic: dropping it silently falls back to the `windows7.0` baseline, which has
> **no WinRT projections at all** — and this library's toast notification feature
> (`Microsoft.Toolkit.Uwp.Notifications`, which needs `Windows.UI.Notifications`) fails to
> compile without them. A mismatched TFM here surfaces as a NuGet restore error at build time
> (`NU1201: Project Barbatos.Wpf.Core is not compatible with net10.0-windows7.0 ...`), not a
> runtime issue — confirmed by trying it. `Barbatos.Wpf.Aquarius` has no such requirement (no
> WinRT dependency) and works fine with the bare `net8.0-windows`/etc. form.

---

## Hosting

### App composition (the `WpfProgram` pattern)

#### 1. Derive your `App` from `WpfApplication`

`App.xaml`:

```xml
<hosting:WpfApplication x:Class="MyApp.App"
                        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:hosting="clr-namespace:Barbatos.Wpf.Hosting;assembly=Barbatos.Wpf.Core" />
```

`App.xaml.cs`:

```csharp
public partial class App : WpfApplication
{
    protected override WpfApp CreateWpfApp() => WpfProgram.CreateWpfApp();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }
}
```

#### 2. Compose the host

```csharp
public static class WpfProgram
{
    public static WpfApp CreateWpfApp()
    {
        var builder = WpfApp.CreateBuilder();

        builder.Configuration.AddJsonFile("appsettings.json", optional: true);

        builder.ConfigureLifecycleEvents(events => events.AddWpf(wpf => wpf
            .OnStartup((app, args) => { /* ... */ })
            .OnWindowCreated(window => { /* ... */ })
            .OnWindowClosed((window, args) => { /* ... */ })));

        builder.Services.AddSingleton<IGreetingService, GreetingService>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        return builder.Build();
    }
}
```

`WpfAppBuilder` implements `IHostApplicationBuilder`, so `builder.Services` is a standard
`IServiceCollection`, `builder.Configuration` is a `ConfigurationManager` registered as
`IConfiguration`, and `builder.Logging` registers real logging services (or a no-op
`ILogger<T>`/`ILoggerFactory` pair if you never touch it, so consumers never receive
`null`).

**Add a logging provider.** Every optional desktop feature reports its failures only as log
entries — a global hotkey another app already owns, a periodic run that threw, a push
connection that could not start — and with no provider registered (the no-op pair, or
`builder.Logging` touched but given no provider) those entries go nowhere. Core deliberately
ships no provider of its own, so add one; during development the debug provider from the
`Microsoft.Extensions.Logging.Debug` package, which writes to the debugger's Output window, is
enough (the Core sample's `WpfProgram.cs` does exactly this):

```csharp
builder.Logging.AddDebug();
```

A shipped app usually also wants a provider that persists, such as a file or the Windows event
log.

### Lifecycle events

Application events (`OnStartup`, `OnActivated`, `OnDeactivated`, `OnSessionEnding`,
`OnDispatcherUnhandledException`, `OnExit`) and window events (`OnWindowCreated`,
`OnWindowLoaded`, `OnWindowActivated`, `OnWindowDeactivated`, `OnWindowStateChanged`,
`OnWindowClosing`, `OnWindowClosed`) are surfaced through `ILifecycleEventService`. Window
events fire for every `Window` in the app — the splash screen, dialogs and push-notification
fallback popups included — so check which window it is before acting: the sample's
minimize-to-tray handler returns early unless `window is MainWindow`, since cancelling
`Closing` on any other window would leave it hidden but alive.

```csharp
builder.ConfigureLifecycleEvents(events => events.AddWpf(wpf => wpf
    .OnStartup((app, args) => logger.LogInformation("Started with args: {Args}", args.Args))
    .OnWindowClosing((window, args) => { /* prompt to save, or cancel via args.Cancel */ })));
```

`IWpfInitializeService` runs once during `Build()`; `IWpfInitializeScopedService` runs once
per window, the first time it raises `Loaded` — both are the WPF counterparts of
`IMauiInitializeService`/`IMauiInitializeScopedService`. The per-window DI scope behind the
latter is private to `WpfApplication`: it exists to run those initializers (and resolve that
window's scoped `IDispatcher`) and is disposed when the window closes. App code cannot resolve
from it — `Services` is always the root provider, and `Build()` creates the container without
scope validation, so a service registered with `AddScoped` resolves there as one app-wide
instance, not one per window. Register per-window state as transient instead, or create your
own scope.

### Dispatching

`IDispatcher`, `IDispatcherTimer`, and `IDispatcherProvider`, backed by the WPF `Dispatcher`,
registered app-wide (singleton) and per-window (scoped), plus the `DispatchAsync` /
`DispatchIfRequiredAsync` extension helpers.

### Configuring the hosting environment

`WpfHostEnvironment` (the WPF counterpart of `MauiHostEnvironment`/ASP.NET Core's
`HostingEnvironment`) resolves `EnvironmentName` the same way
`Microsoft.Extensions.Hosting.HostBuilder` does: `WpfAppBuilder` adds an environment
variables configuration source with the `DOTNET_` prefix as the very first (lowest-priority)
entry in `builder.Configuration`, then reads `HostDefaults.EnvironmentKey` from the whole
configuration once, lazily — the first time you access `builder.Environment` (or, if you never
do, when `IHostEnvironment` is first resolved from the built container). That means:

* Setting the `DOTNET_ENVIRONMENT` environment variable (e.g. `DOTNET_ENVIRONMENT=Staging`)
  before launching the app sets `builder.Environment.EnvironmentName`, with no code required.
* Any configuration source that also sets the `environment` key overrides it, as long as it
  is added *before* you first read `builder.Environment` — a source added after that first
  read is never seen:

  ```csharp
  var builder = WpfApp.CreateBuilder();
  builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
  {
      [HostDefaults.EnvironmentKey] = "Staging",
  });

  // builder.Environment.EnvironmentName == "Staging" from here on.
  ```
* `EnvironmentName` is a normal settable property — `builder.Environment.EnvironmentName =
  "Staging";` works directly, at any time. Apart from that assignment the value stays what the
  first read produced: it does not keep re-reading the environment variable or configuration.
* `WpfHostEnvironment` also exposes `HostEnvironmentEnvExtensions` from
  `Microsoft.Extensions.Hosting` for free — `builder.Environment.IsDevelopment()`,
  `.IsProduction()`, `.IsEnvironment("Staging")`, etc.

A common pattern is loading an environment-specific settings file:

```csharp
var builder = WpfApp.CreateBuilder();

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);
```

---

## Essentials

A broad set of platform services, each following the same interface + static facade design
(e.g. `AppInfo.Current`, `Preferences.Default`) so you can inject the interface for
testability or just call the static facade directly. Where a module needs OS integration,
the Windows implementation goes straight to the nearest Win32/.NET equivalent rather than
WinRT/UWP APIs (unavailable to a plain unpackaged WPF app). All of it is registered in the
container by `UseEssentials()`, part of the builder defaults:

| Module | Static facade | Interface | Windows implementation |
| --- | --- | --- | --- |
| App info | `AppInfo` | `IAppInfo` | Assembly metadata/attributes; theme from the `AppsUseLightTheme` registry value; packaging via `GetCurrentPackageFullName`; `InstallDate`/`InstallLocation` from the installer's `Uninstall` registry entry, if any |
| Publisher info | `PublisherInfo` | `IPublisherInfo` | Assembly metadata, falling back to `AssemblyCompanyAttribute` |
| Device identity | `DeviceIdentity` | `IDeviceIdentity` | See [License enforcement: DeviceIdentity](#license-enforcement-deviceidentity) below — a desktop-specific addition |
| Device info | `DeviceInfo` | `IDeviceInfo` | BIOS registry (`Model`/`Manufacturer`); a Chromium-style tablet-mode heuristic for `Idiom` |
| File system | `FileSystem` | `IFileSystem` | `%LocalAppData%\{Publisher}\{AppId}\{Data,Cache}`, created on first access |
| Preferences | `Preferences` | `IPreferences` | A local JSON file, guarded by a lock around file I/O |
| Secure storage | `SecureStorage` | `ISecureStorage` | DPAPI (`ProtectedData`, current-user scope) instead of the WinRT `DataProtectionProvider` |
| Version tracking | `VersionTracking` | `IVersionTracking` | Pure C#, no platform-specific code (built on `Preferences` + `AppInfo`) |
| Connectivity | `Connectivity` | `IConnectivity` | `System.Net.NetworkInformation` instead of the WinRT `NetworkInformation` API |
| Device display | `DeviceDisplay` | `IDeviceDisplay` | A Win32 monitor-info P/Invoke block against the active WPF window; `KeepScreenOn` via `SetThreadExecutionState`, held on a thread of its own; change notifications via `SystemEvents.DisplaySettingsChanged` |
| Email | `Email` | `IEmail` | Simple MAPI, Unicode entry point (`MAPISendMailW`); a missing mail client is reported as `FeatureNotSupportedException` |
| Launcher | `Launcher` | `ILauncher` | `Process.Start(UseShellExecute: true)` instead of the WinRT `Windows.System.Launcher`, `true` whenever the shell accepts the target; `CanOpenAsync` checks `HKEY_CLASSES_ROOT` for a registered scheme handler |
| Contacts | `Contacts` | `IContacts` | Throws `FeatureNotSupportedException` — see note below |
| Geolocation | `Geolocation` | `IGeolocation` | Throws `FeatureNotSupportedException` — see note below; `Location`'s distance math is fully functional |
| App actions | `AppActions` | `IAppActions` | The taskbar Jump List (`System.Windows.Shell.JumpList`) instead of the WinRT `Windows.UI.StartScreen.JumpList` |
| Permissions | `Permissions.CheckStatusAsync<T>()` / `RequestAsync<T>()` | *(none — generic static API, no DI)* | Most permissions report `Granted` without checking anything (no manifest/capability concept for an unpackaged app); `ContactsRead`/`ContactsWrite`/`LocationWhenInUse`/`LocationAlways`/`Microphone`/`Sensors` throw `FeatureNotSupportedException` — see [Permissions](#permissions) below |

```csharp
var name = AppInfo.Name;
var publisher = PublisherInfo.Name;
var isOnline = Connectivity.NetworkAccess == NetworkAccess.Internet;

Preferences.Set("launch_count", Preferences.Get("launch_count", 0) + 1);
await SecureStorage.SetAsync("api_token", token);

await Email.ComposeAsync("Subject", "Body", "someone@example.com");
await Launcher.OpenAsync("https://example.com");
```

A few modules behave differently from their .NET MAUI originals on purpose, or have limits
worth knowing before you rely on them:

**`Email` reports failure.** `ComposeAsync` calls Simple MAPI's Unicode entry point
`MAPISendMailW`, so a subject or body outside the system code page (Vietnamese on an
English-locale machine, for one) reaches the mail client intact; the ANSI `MAPISendMail` the
MAUI port uses turned those characters into `?`. When no mail client answers (none installed,
or none set as the default), the call throws `FeatureNotSupportedException` instead of
completing as if a compose window had opened; an attachment that does not exist throws
`FileNotFoundException` before any client starts; a user closing the compose window is not an
error. `IsComposeSupported` stays `true` — it says Simple MAPI is the mechanism, and only
calling it tells whether a client answers — so offer a `mailto:` fallback:

```csharp
try
{
    await Email.ComposeAsync(subject, body, supportAddress);
}
catch (FeatureNotSupportedException)
{
    await Launcher.OpenAsync($"mailto:{supportAddress}?subject={Uri.EscapeDataString(subject)}");
}
```

**`Launcher` returns `true` when the shell accepts the target.** `Process.Start` with shell
execution returns no `Process` when the shell hands the target to an application that is
already running or to a COM-activated handler, although the target opens. Counting that as a
failure made a working browser hand-off look like an error, so `OpenAsync` now returns `false`
only when the shell refuses (no handler, a missing file).

**`DeviceDisplay.KeepScreenOn` stacks with keep-awake.** The
`SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED)` request is held by a background
thread of its own for as long as the property is `true`. That state is per thread and each
call replaces the calling thread's previous one, so applying it on the caller's thread — as
earlier versions did — cancelled `ConfigureKeepAwake`'s `ES_SYSTEM_REQUIRED` (held on the UI
thread) the moment the screen request was turned off. Windows combines every thread's request,
so the two now add up, and `KeepScreenOn` can be set and cleared from any thread. For a
persisted "keep the display on" setting prefer `KeepAwakeOptions.KeepDisplayOn`; use
`KeepScreenOn` for a temporary "while presenting" request.

**`Permissions` and `AppActions`** have Windows-specific limits of their own:
[`Granted` means "not checked"](#permissions), and a Jump List click is
[not reported while the app is already running under single instance](#app-actions-and-version-tracking).

### Configuring AppInfo and PublisherInfo

`AppInfo` and `PublisherInfo` never require any setup — their fallback chains always resolve
to *something* — but you should configure them explicitly for any app you plan to ship,
because `FileSystem.AppDataDirectory` and `FileSystem.CacheDirectory` are derived from
`PublisherInfo.Name` and `AppInfo.AppId` (`%LocalAppData%\{PublisherInfo.Name}\{AppInfo.AppId}\...`).

This library targets .NET 8+ SDK-style projects only, so configuration is a plain
`<PropertyGroup>` in the csproj — the same well-known properties Visual Studio's *Project ▸
Properties ▸ Package* page already exposes, with no `AssemblyInfo.cs` file or ClickOnce
settings involved:

```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <TargetFramework>net8.0-windows10.0.17763.0</TargetFramework>
  <UseWPF>true</UseWPF>

  <!-- AppInfo.Name / PublisherInfo.Name / AppInfo.Version / PublisherInfo.Copyright -->
  <Product>My App</Product>
  <Company>Contoso</Company>
  <Version>1.2.3</Version>
  <Copyright>Copyright © 2026 Contoso</Copyright>
</PropertyGroup>
```

The SDK turns each of these into the matching assembly attribute at build time (`<Product>`
→ `AssemblyProductAttribute`, `<Company>` → `AssemblyCompanyAttribute`, `<Version>` →
`AssemblyVersionAttribute`, `<Copyright>` → `AssemblyCopyrightAttribute`), which is exactly
what the fallback chains below read — so setting these four properties is normally all you
need:

| Property | Standard fallback | Standard csproj property |
| --- | --- | --- |
| `AppInfo.Name` | `AssemblyProductAttribute`, then `AssemblyTitleAttribute` (`<Title>`), then the assembly name | `<Product>` |
| `AppInfo.Version`/`VersionString` | The entry assembly's `AssemblyName.Version` (always four parts) | `<Version>` (or `<AssemblyVersion>` for finer control) |
| `PublisherInfo.Name` | `AssemblyCompanyAttribute` | `<Company>` |
| `PublisherInfo.Copyright` | `AssemblyCopyrightAttribute` | `<Copyright>` |

**`AppInfo.Version` always has four parts.** It reads the entry assembly's
`AssemblyName.Version`, so `<Version>1.2.3</Version>` reads back as `1.2.3.0` — show
`AppInfo.Version.ToString(3)` in an About box. The SDK derives `AssemblyVersion` from
`<Version>` with any prerelease suffix stripped (`<Version>1.3.0-beta.1</Version>` gives
`1.3.0.0`; verified by building a probe project), and `FileVersion` defaults to that same
value, so `<Version>` alone is enough for a prerelease too. Set `<AssemblyVersion>` only to a
purely numeric value: `<AssemblyVersion>$(Version)</AssemblyVersion>` with a prerelease
`<Version>` fails the build with `CS7034`. The one case that needs an explicit value is a shared
`Directory.Build.props` that already sets `<AssemblyVersion>` (this repository's does, which is
why its samples override it) — override it with the numeric part. The
`Barbatos.Wpf.ApplicationModel.AppInfo.Version` metadata override below goes through
`Version.Parse`, so it must be numeric as well; a value such as `1.2.0-beta` throws
`FormatException` on every read.

`AppInfo.AppId` and `PublisherInfo.Website`/`SupportUrl`/`SupportEmail` have no standard
MSBuild-property equivalent — there is no built-in "app id"/"website"/"support URL"/"support
email" assembly attribute at all. For
those (or to override any of the properties above with a value that differs from
`<Product>`/`<Company>`/`<Copyright>`), set the `Barbatos.Wpf.ApplicationModel.*` assembly
metadata directly — it is always checked *before* the standard fallback:

```xml
<ItemGroup>
  <AssemblyMetadata Include="Barbatos.Wpf.ApplicationModel.AppInfo.AppId" Value="{C6BB69DE-7E6B-43E3-83AD-AC46E1B0570D}" />
  <AssemblyMetadata Include="Barbatos.Wpf.ApplicationModel.PublisherInfo.Website" Value="https://contoso.example" />
  <AssemblyMetadata Include="Barbatos.Wpf.ApplicationModel.PublisherInfo.SupportUrl" Value="https://contoso.example/support" />
  <AssemblyMetadata Include="Barbatos.Wpf.ApplicationModel.PublisherInfo.SupportEmail" Value="support@contoso.example" />
</ItemGroup>
```

`AppId` does not have to be a GUID — `com.contoso.myapp` or `Contoso.MyApp` work just as
well. Use a GUID when you want it to double as an MSI product code (see
[Publishing with an installer](#publishing-with-an-installer)); use a readable string
otherwise, since it also becomes the app's storage folder name.

| Property | Metadata key (explicit override) |
| --- | --- |
| `AppInfo.AppId` | `Barbatos.Wpf.ApplicationModel.AppInfo.AppId` |
| *(the `Uninstall` subkey `AppInfo.InstallDate`/`InstallLocation` read from)* | `Barbatos.Wpf.ApplicationModel.AppInfo.UninstallRegistryKey` *(defaults to `AppId` — see [Reading back InstallDate and InstallLocation](#reading-back-installdate-and-installlocation))* |
| `AppInfo.Name` | `Barbatos.Wpf.ApplicationModel.AppInfo.Name` |
| `AppInfo.Version`/`VersionString` | `Barbatos.Wpf.ApplicationModel.AppInfo.Version` |
| `PublisherInfo.Name` | `Barbatos.Wpf.ApplicationModel.PublisherInfo.Name` |
| `PublisherInfo.Website` | `Barbatos.Wpf.ApplicationModel.PublisherInfo.Website` *(no standard fallback — `null` when unset)* |
| `PublisherInfo.SupportUrl` | `Barbatos.Wpf.ApplicationModel.PublisherInfo.SupportUrl` *(no standard fallback — `null` when unset)* |
| `PublisherInfo.SupportEmail` | `Barbatos.Wpf.ApplicationModel.PublisherInfo.SupportEmail` *(no standard fallback — `null` when unset)* |
| `PublisherInfo.Copyright` | `Barbatos.Wpf.ApplicationModel.PublisherInfo.Copyright` |

Reading the values back — for example on an About screen — goes through the facades instead
of raw reflection:

```csharp
var appName = AppInfo.Name;
var version = AppInfo.VersionString;
var publisher = PublisherInfo.Name;
var copyright = PublisherInfo.Copyright;
```

> **Important:** `AppInfo.AppId` (not a store package identifier on this platform — it is
> simply the stable identifier used to derive the app's storage folder) should stay constant
> across releases. Changing it after
> shipping moves `Preferences`, `SecureStorage`, and `FileSystem.AppDataDirectory` to a new
> folder, effectively discarding every existing user's stored data. If you ship an installer,
> consider matching it to the installer's own application identifier — see
> [Publishing with an installer](#publishing-with-an-installer) below.
>
> **`AppGuid` is deprecated:** this property used to be called `AppInfo.AppGuid`, back when it
> was expected to always hold a GUID. `AppGuid` (and the
> `Barbatos.Wpf.ApplicationModel.AppInfo.AppGuid` metadata key) still resolve to the exact same
> value, so nothing breaks and no storage folder moves — but the property is marked
> `[Obsolete]` and will be removed in a future release. Rename both to `AppId` when convenient.
>
> **Multi-project solutions:** `<Product>`/`<Company>`/`<Copyright>` are easy to accidentally
> set in a shared `Directory.Build.props` — every app in the solution would then report the
> same `AppInfo.Name`/`PublisherInfo.Name`/`PublisherInfo.Copyright`. Set them per-project
> (or use the `Barbatos.Wpf.ApplicationModel.AppInfo.Name`/`PublisherInfo.Name` metadata
> override per-project) if that's not what you want.

### Publishing with an installer

`AppInfo`/`PublisherInfo` map cleanly onto the fields a Windows installer needs, because both
are ultimately backed by the same standard assembly attributes an installer's version-reading
tooling already understands. For [Inno Setup](https://jrsoftware.org/isinfo.php) 6.4
specifically (the same mapping applies to WiX/MSI, which uses the identical Windows
`Uninstall` registry convention):

| `[Setup]` directive | Registry value under the installer's `Uninstall` key | Barbatos.Wpf.Core source |
| --- | --- | --- |
| `AppId` | *(the key name itself, **plus `_is1`** — see below)* | `AppInfo.AppId`, plus the `...AppInfo.UninstallRegistryKey` metadata key for the suffixed form — see [Reading back InstallDate and InstallLocation](#reading-back-installdate-and-installlocation) |
| `AppName` | `DisplayName` | `AppInfo.Name` (`<Product>`) |
| `AppVersion` | `DisplayVersion` | `AppInfo.VersionString` (`<Version>`) |
| `AppPublisher` | `Publisher` | `PublisherInfo.Name` (`<Company>`) |
| `AppPublisherURL` | `URLInfoAbout` | `PublisherInfo.Website` |
| `AppSupportURL` | `HelpLink` | `PublisherInfo.SupportUrl` |
| `AppUpdatesURL` | `URLUpdateInfo` | *(no dedicated property — reuse `PublisherInfo.Website`, or a custom metadata key of your own)* |

```pascal
; setup.iss
#define AppGUID "C6BB69DE-7E6B-43E3-83AD-AC46E1B0570D"   ; matches AppInfo.AppId, without braces
#define Publisher "Contoso"                              ; matches PublisherInfo.Name
#define AppName "My App"                                 ; matches AppInfo.Name

[Setup]
AppId={{{#AppGUID}}
AppName={#AppName}
AppPublisher={#Publisher}
```

> **`AssemblyVersion` vs. the file's native version resource:** Inno Setup's
> `GetVersionNumbers()` (used above to auto-extract `AppVersion` from the built `.exe`) reads
> the Win32 `FileVersion` resource embedded in the PE file, not .NET's `AssemblyVersion` —
> these are two different concepts that usually carry the same value only because the SDK
> defaults `<FileVersion>` to the `AssemblyVersion` it derives from `<Version>`. If you ever set
> `<AssemblyVersion>` to something fixed (a common practice to avoid binding-redirect churn)
> while letting `<Version>` increment per build (and setting `<FileVersion>` explicitly, since it
> otherwise follows the fixed `AssemblyVersion`), `AppInfo.VersionString` (which
> reads `AssemblyVersion`) and what your installer reports from the `.exe` file will diverge —
> set `Barbatos.Wpf.ApplicationModel.AppInfo.Version` explicitly if you need them to stay in
> lockstep.

### Reading back InstallDate and InstallLocation

`AppInfo.InstallDate` and `AppInfo.InstallLocation` read the *installed copy's* own uninstall
registry entry (the one from the table above) back:

```csharp
var when = AppInfo.InstallDate;         // DateTime? — null if not found
var where = AppInfo.InstallLocation;    // string? — null if not found
```

The subkey looked up is `...\Uninstall\{AppInfo.AppId}` by default, using the name **verbatim**
— no suffix is appended or guessed (a GUID-shaped name is normalised to the braced `{...}` form
the registry convention uses, so `C6BB…` and `{C6BB…}` both work). Both properties try every
plausible location an installer might have used: `HKEY_LOCAL_MACHINE` (a machine-wide install)
and `HKEY_CURRENT_USER` (a per-user install, e.g. Inno Setup's `PrivilegesRequired=lowest`),
each in both the 64-bit and 32-bit registry view (the 32-bit view transparently redirects to
`WOW6432Node`). They resolve to `null` — never throw — when no entry of that exact name exists
anywhere, which includes the ordinary "not installed through an installer yet" case (running
from the IDE/`dotnet run`).

When your installer registers itself under a name that *isn't* `AppId`, point the lookup at the
real name with the `UninstallRegistryKey` metadata key instead — `AppId` stays clean, and this
library never has to guess installer-specific naming conventions:

```xml
<AssemblyMetadata Include="Barbatos.Wpf.ApplicationModel.AppInfo.AppId" Value="{C6BB69DE-7E6B-43E3-83AD-AC46E1B0570D}" />
<AssemblyMetadata Include="Barbatos.Wpf.ApplicationModel.AppInfo.UninstallRegistryKey" Value="{C6BB69DE-7E6B-43E3-83AD-AC46E1B0570D}_is1" />
```

> **Inno Setup users, this is you.** Inno Setup does not name its uninstall key after its
> `AppId` directly — it appends `_is1` and offers no directive to change or remove that suffix
> (`AppId=MyProgram` → `...\Uninstall\MyProgram_is1`; if you never set `AppId` it defaults to
> `AppName`). This has been its behaviour since version 1.3. So an Inno-installed app needs the
> `UninstallRegistryKey` override above — set it to your `AppId` plus `_is1`.
>
> Putting the suffix here rather than in `AppInfo.AppId` matters: `AppId` also becomes the
> storage folder name (`%LocalAppData%\{Publisher}\{AppId}\`), and it can never be changed after
> shipping without stranding every existing user's stored data.
>
> Source: the `[Setup]: AppId` and `[Setup]: CreateUninstallRegKey` topics in `ISetup.chm`,
> shipped with Inno Setup itself
> ([help source](https://github.com/jrsoftware/issrc/blob/is-6_4_3/ISHelp/isetup.xml),
> [web version](https://jrsoftware.org/ishelp/topic_setup_appid.htm)).

MSI/WiX needs no override: its uninstall key *is* the product code, so setting `AppInfo.AppId`
to that GUID is all it takes.

Other fields the installer's uninstall entry carries — `EstimatedSize`, `DisplayIcon`,
`NoModify`/`NoRepair`, the `Inno Setup: *` bookkeeping keys — describe the *installed copy*
rather than something an app typically needs to read back about itself, so they stay outside
`AppInfo`'s scope.

### App actions and version tracking

`AppActions` (taskbar Jump List shortcuts) and `VersionTracking.Track()` are wired up
through `ConfigureEssentials`:

```csharp
// WpfProgram.cs
builder.ConfigureEssentials(essentials => essentials
    .AddAppAction("new-note", "New note", "Start a blank note")
    .OnAppAction(action => App.LaunchActionId = action.Id)
    .UseVersionTracking());

// App.xaml.cs
internal static string? LaunchActionId { get; set; }

protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);                  // a Jump List activation is raised in here

    MainWindow = Services.GetRequiredService<MainWindow>();
    MainWindow.Show();

    if (LaunchActionId == "new-note")
        Services.GetRequiredService<MainViewModel>().NewNote();   // the app's own command
}
```

Clicking a Jump List entry launches a new process with an encoded command-line argument;
`UseEssentials()` checks for it on the `WpfLifecycle.OnStartup` event and raises
`AppActions.OnAppAction` — a relaunch-argument activation flow adapted to WPF's native Jump
List API instead of `Windows.UI.StartScreen.JumpList`. Because
`System.Windows.Shell.JumpList` has no way to read back what the OS is currently showing
(unlike WinRT's `JumpList.LoadCurrentAsync()`), `AppActions.GetAsync()` returns the actions
last set by `SetAsync()` in this process rather than querying the OS.

That event fires inside `WpfApplication.OnStartup` — that is, inside your override's
`base.OnStartup(e)` call, before any main window exists. Hence the shape above: the handler
only records the id, and the app acts on it once `MainWindow` is shown. A handler that tries
to show or activate the main window itself finds no window on a cold start, and an
`AppActions.OnAppAction +=` placed *after* `base.OnStartup(e)` never sees the activation at all
(the Core sample subscribes before it, in `App.xaml.cs`). Two further limits follow from a
click being a relaunch:

- **Under `ConfigureSingleInstance()`, only cold starts are reported.** While the app is
  already running, the relaunched process exits inside `Build()` — before the startup event —
  and the running instance receives only `ISingleInstanceService.SecondInstanceLaunched`,
  which carries no command-line arguments. The action id is lost; the running instance can
  bring its window forward (the default `ActivateMainWindow` behaviour), not tell which entry
  was clicked.
- **Actions must be declared at startup to be recognised.** The relaunched process matches
  the clicked id against the actions set *in that process* before its startup event, which
  `ConfigureEssentials` does during `Build()`. An action added later with
  `AppActions.SetAsync(...)` appears in the Jump List but raises nothing after a restart unless
  the next launch declares it the same way.

### Contacts and Geolocation

A real Windows implementation of `Contacts`/`Geolocation` needs WinRT contracts
(`Windows.ApplicationModel.Contacts.ContactPicker`,
`Windows.Devices.Geolocation.Geolocator`) and, for some APIs, MSIX packaging. Although this
package targets `net*-windows10.0.17763.0`, it uses those WinRT projections only for toast
notifications and keeps every Essentials module on Win32/.NET APIs. Rather than pull in that
machinery, both modules still ship their full type surface
(`IContacts`/`Contact`/`ContactEmail`/`ContactPhone`,
`IGeolocation`/`Location`/`GeolocationRequest`/...) — useful if you share code with a .NET
MAUI project, since it compiles unchanged either way — but every member of the Windows
implementation throws `FeatureNotSupportedException` instead of doing a real lookup. If you
need a real implementation (for example Geolocation via the classic
Win32 Location API, or Contacts via Microsoft Graph), register your own after the builder is
created — a later `AddSingleton` call takes precedence over the `TryAddSingleton` that
`UseEssentials()` performs, so code that resolves `IContacts`/`IGeolocation` through
constructor injection will get your implementation:

```csharp
builder.Services.AddSingleton<IContacts, MyGraphContacts>();
```

The static `Contacts.PickContactAsync()`/`Geolocation.GetLocationAsync()` facades are
independent of DI and will keep using the built-in
`FeatureNotSupportedException`-throwing implementation regardless — prefer the injected
`IContacts`/`IGeolocation` interface if you register a replacement.

### Permissions

`Permissions` is a generic, DI-free static surface — there is no `IPermissions` interface to
register; each permission type (`Permissions.Camera`, `Permissions.Microphone`, ...) is just
a type argument to `CheckStatusAsync<TPermission>()`/`RequestAsync<TPermission>()`:

```csharp
var status = await Permissions.CheckStatusAsync<Permissions.Camera>();

if (status != PermissionStatus.Granted)
    status = await Permissions.RequestAsync<Permissions.Camera>();
```

Every permission type below is present as a nested type of `Permissions`: `Battery`,
`Bluetooth`, `CalendarRead`/`CalendarWrite`, `Camera`, `ContactsRead`/`ContactsWrite`,
`Flashlight`, `LaunchApp`, `LocationWhenInUse`/`LocationAlways`, `Maps`, `Media`,
`Microphone`, `NearbyWifiDevices`, `NetworkState`, `Phone`, `Photos`/`PhotosAddOnly`,
`PostNotifications`, `Reminders`, `Sensors`, `Sms`, `Speech`, `StorageRead`/`StorageWrite`,
`Vibrate`. Two behaviors on WPF:

- **Most permissions report `PermissionStatus.Granted` without checking anything.** An
  unpackaged desktop WPF app has no `AppxManifest.xml` capability declarations to check and no
  runtime consent prompt to show, so `EnsureDeclared()` is a no-op and `RequestAsync` returns
  what `CheckStatusAsync` does. Read `Granted` as "not gated by this API, not checked", not as
  "Windows allows it": the Windows privacy settings (Settings ▸ Privacy & security ▸ Camera)
  can still block a desktop app's camera, and `PostNotifications` is `Granted` even when the
  user or a policy has turned this app's toasts off — read
  `INotificationService.Availability` (from `ConfigureNotifications()`) before relying on a
  toast.
- **`ContactsRead`, `ContactsWrite`, `LocationWhenInUse`, `LocationAlways`, `Microphone`, and
  `Sensors` throw `FeatureNotSupportedException`.** A real check for these six needs WinRT
  device-access contracts (`ContactManager`, `Geolocator`, `DeviceAccessInformation`,
  `MediaCapture`) that need WinRT projections and, for some APIs, MSIX packaging — the same
  machinery this library already opts out of for [`Contacts` and `Geolocation`](#contacts-and-geolocation).
  Rather than silently report `Granted` for something it can't actually verify, these six are
  honest about it instead.

If you need a real permission check (for example reading the Windows privacy consent
registry, or a custom capability broker), subclass `Permissions.BasePlatformPermission` (or
one of the nested types), override `CheckStatusAsync()` — `RequestAsync()` forwards to it —
and pass your subclass as `TPermission`:

```csharp
sealed class CameraPrivacyPermission : Permissions.BasePlatformPermission
{
    // ReadCameraConsent() is the app's own check, e.g. of the Windows privacy settings.
    public override Task<PermissionStatus> CheckStatusAsync() =>
        Task.FromResult(ReadCameraConsent() ? PermissionStatus.Granted : PermissionStatus.Denied);
}

var status = await Permissions.RequestAsync<CameraPrivacyPermission>();
```

### License enforcement: DeviceIdentity

Apple and Google both prohibit collecting raw hardware IDs (IMEI, serial numbers) for
ordinary commercial apps, so cross-platform mobile frameworks (including .NET MAUI's own
`DeviceInfo`) never expose a device identifier or network address in the first place.
`DeviceIdentity` exists for the desktop-specific case this rules out: enforcing a per-machine
license/activation limit.

> [!IMPORTANT]
> This section explains what the API does and why it's shaped this way — it is **not legal
> advice**. Both members below are still personal data under most privacy laws (GDPR, EU/EEA;
> Vietnam's Decree 13/2023/NĐ-CP; CCPA, California; ...) once your license server associates
> them with a customer record (an email, a purchase). Using this API still requires a privacy
> policy disclosure and a lawful basis for processing — have your specific case reviewed by
> counsel, especially if you sell internationally.

```csharp
string installId = await DeviceIdentity.GetInstanceIdAsync();
string fingerprint = await DeviceIdentity.GetHardwareFingerprintAsync();
```

| Member | What it is | Changes when |
| --- | --- | --- |
| `GetInstanceIdAsync()` | A random GUID generated on first use, persisted via `SecureStorage` under the key `Barbatos.Wpf.DeviceIdentity.InstanceId`. Identifies this app's data for this Windows user, not the physical machine. | `%LocalAppData%\{Publisher}\{AppId}\Settings\securestorage.dat` is lost (deleted, a new Windows user profile or machine); `SecureStorage.RemoveAll()` runs; `AppInfo.AppId` or `PublisherInfo.Name` changes |
| `GetHardwareFingerprintAsync()` | A SHA-256 hash of a few motherboard/BIOS/CPU identifiers (read via WMI), salted with `AppInfo.AppId`. | The hardware is replaced, or `AppInfo.AppId` changes |

The instance id does **not** reset on an ordinary reinstall: uninstallers (Inno Setup, MSI)
remove what they installed and, unless told otherwise, leave the per-user `%LocalAppData%`
data behind — and the id lives there. To reset it on purpose, remove the
`Barbatos.Wpf.DeviceIdentity.InstanceId` key from `SecureStorage` (an uninstaller can delete
`securestorage.dat`, at the cost of every other secret stored there). Concurrent first calls
within one process are serialized, so a startup licence check and a background job asking at
the same moment receive the same id; two processes starting at the same moment can still each
create one, and the last write wins on disk.

Two design choices keep this narrower than the raw hardware-ID approach a lot of licensing
code reaches for by default:

- **The hardware serials themselves are never stored or transmitted** — only a one-way
  SHA-256 hash of them. Your license server can tell "this is the same machine as last time"
  without ever holding a reversible hardware identifier.
- **The hash is salted with `AppInfo.AppId`**, so it's scoped to *this app* — the same
  machine produces a different fingerprint for a different app, the same way Apple's
  `IdentifierForVendor` is scoped to a developer rather than being a single ID usable to
  correlate a device across unrelated apps.

Use `GetInstanceIdAsync()` alone if an activation may reset along with the app's data (the
least invasive option). Use `GetHardwareFingerprintAsync()` (or both, keyed together) if a
user should not be able to reset a per-machine activation count by deleting that data or
signing in as another Windows user — this is the same technique commercial license managers
(FlexNet, Reprise, ...) use.

**Network address:** for the same purpose (e.g. flagging one license activating from an
unusual number of locations), resist the urge to call a third-party IP-lookup service (like
`api.ipify.org`) from the client — that shares "this app, this machine, right now" with an
extra party you don't control, on top of whatever your own license server already sees. Your
license-check API already receives the caller's public IP as part of the HTTP request itself
(e.g. `HttpContext.Connection.RemoteIpAddress` in ASP.NET Core) — no client-side code needed.
Treat it as a soft signal, not a hard block: NAT/CGNAT means multiple legitimate users can
share one public IP, and VPNs/mobile networks/dynamic IPs mean the same legitimate user's IP
changes over time.

---

## Optional desktop features

Opt-in features cover the typical "settings screen" of a desktop app. Each one is only
registered when its `Configure...` method is called, binds its own configuration section
(file values override code values), and exposes a service for runtime (UI) toggling:

| Feature | Builder method | Options section | Runtime service | Enabled by default? |
| --- | --- | --- | --- | --- |
| Single instance (block duplicate launches) | `ConfigureSingleInstance(...)` | `Barbatos:SingleInstance` | `ISingleInstanceService` | **Yes** |
| Run on startup (registry `Run` key) | `ConfigureRunOnStartup(...)` | `Barbatos:RunOnStartup` | `IRunOnStartupService` | No |
| System tray icon (`Shell_NotifyIcon`) | `ConfigureTrayIcon(...)` | `Barbatos:TrayIcon` | `ITrayIconService` | No |
| Keep computer awake (`SetThreadExecutionState`) | `ConfigureKeepAwake(...)` | `Barbatos:KeepAwake` | `IKeepAwakeService` | No |
| Desktop notifications (adaptive Windows toast, images/buttons/navigation) | `ConfigureNotifications(...)` | `Barbatos:Notifications` | `INotificationService` | Yes |
| Periodic background services | `ConfigurePeriodicServices<T>(...)` | `Barbatos:PeriodicServices` | `IPeriodicServiceScheduler` | Yes |
| Realtime push notification listener (SignalR) | `ConfigurePushNotifications(...)` | `Barbatos:PushNotifications` | `IPushNotificationService` | Yes |
| Keyboard shortcuts (in-app + `RegisterHotKey` global hotkeys) | `ConfigureInputSystem(...)` | `Barbatos:InputSystem` | `IInputSystemService` | Yes |
| Dialogs (owner assignment, no duplicate opens, graceful close) — see [Dialogs](#dialogs) | `ConfigureDialogs(...)` | `Barbatos:Dialogs` | `IDialogService` | n/a (no on/off switch) |

"Enabled by default" means what happens the moment you call the `Configure...` method with no
further setup — every feature is still opt-in at the *builder* level (call the method, or the
service isn't registered at all).

These features report what goes wrong — a refused global hotkey, a failed periodic run, a push
connection error — only through `ILogger`, so register a logging provider (see
[Compose the host](#2-compose-the-host)) or those failures stay invisible.

```csharp
builder.ConfigureSingleInstance();
builder.ConfigureRunOnStartup();
builder.ConfigureKeepAwake();
builder.ConfigureTrayIcon(options =>
{
    options.MenuItems.Add(new TrayMenuItem("Open", App.ShowMainWindow));
    options.MenuItems.Add(new TrayMenuItem("Exit", App.ExitApplication));
});
builder.ConfigureNotifications();
builder.ConfigureInputSystem(options =>
{
    var shortcuts = new InputActionMap("AppShortcuts");
    shortcuts.AddAction("QuickEntry")
        .AddKeyBinding(Key.K, ModifierKeys.Control)      // in-app only
        .AddGlobalBinding(Key.Space, ModifierKeys.Control | ModifierKeys.Alt); // works even minimized
    options.ActionMaps.Add(shortcuts);
});
// after Build(): app.Services.GetRequiredService<IInputSystemService>().FindAction("QuickEntry")!.Performed += (_, _) => ShowQuickEntry();
```

And from a configuration file:

```json
{
  "Barbatos": {
    "SingleInstance": { "Enabled": true, "ActivateMainWindow": true },
    "RunOnStartup": { "Enabled": true },
    "TrayIcon": { "Enabled": true, "ToolTip": "My app" },
    "KeepAwake": { "Enabled": true, "KeepDisplayOn": false },
    "Notifications": { "Enabled": true },
    "InputSystem": { "Enabled": true }
  }
}
```

Notes:

- **Single instance** is keyed by `AppInfo.AppId` (see
  [Configuring AppInfo and PublisherInfo](#configuring-appinfo-and-publisherinfo)) via a
  named `Mutex`, scoped to the current user's session (not machine-wide). A second launch
  detects the running instance during `Build()` — *before any window is created* — signals
  it, and terminates immediately via `Environment.Exit(0)`; the first instance's
  `SecondInstanceLaunched` event fires on the UI thread, and (unless you set
  `ActivateMainWindow = false`) its `Application.Current.MainWindow` is automatically
  restored, shown, and brought to the foreground. This has no .NET MAUI counterpart — mobile
  platforms are inherently single-instance.
- `KeepAwake` prevents idle sleep while still letting the display turn off
  (set `KeepDisplayOn` to also keep the display on); the sleep block is released when
  the host is disposed.
- The tray icon supports a context menu, tooltip, click/double-click events, and balloon
  notifications (`ShowBalloonTip`); the sample uses it together with the `OnWindowClosing`
  lifecycle event to implement minimize-to-tray, showing a balloon each time the window is
  hidden so a hidden window doesn't look identical to a closed app. That handler acts only when
  `window is MainWindow`: lifecycle window events fire for every window, and cancelling a
  dialog's or a fallback popup's close would hide it instead (with `IDialogService` still
  tracking the hidden dialog as open). It matches the window's type rather than
  `Application.MainWindow` because WPF makes the first window created — the splash screen —
  `Application.MainWindow` until it closes (checked in dotnet/wpf's `Window.cs`), so a
  reference check would hide the splash instead of closing it whenever the tray icon is
  already visible at startup.
- `RunOnStartup` state is persisted by the OS registry. Its `IsEnabled` reflects only the `Run`
  value the service writes, not the switch in Task Manager's Startup tab or Settings > Apps >
  Startup: Windows stores that one separately (`StartupApproved`) and keeps the `Run` value, so
  after a user turns the app off there, `IsEnabled` still reads `true` and Windows does not
  start it. The service leaves that key alone on purpose (it is undocumented, and it records a
  choice the user made in the OS's own UI); a settings screen can say so next to its checkbox.
  The other toggles can be
  persisted by writing their configuration sections back to a user settings file that is
  loaded via `builder.Configuration.AddJsonFile(...)` — see `SettingsStore` in the sample.
- Notifications are pushed as full adaptive Windows toast notifications (via the Windows
  Community Toolkit's `ToastContentBuilder`/`ToastNotificationManagerCompat`), which also
  appear in the notification center — no Start menu shortcut or manual COM registration
  required, even for a plain, non-packaged WPF app. `INotificationService.Show(title,
  message, severity)` is a no-op while `IsEnabled` is `false`, so a single settings toggle
  can silence every call site.
  - For richer content, use `Show(NotificationContent)`: `ImagePath` renders an inline
    "hero" image, and `Buttons` adds up to five action buttons — each one either raises
    `Activated` with an opaque `Arguments` payload (`NotificationButton(text, arguments)`)
    for the app to navigate on, or opens a URL/protocol directly
    (`NotificationButton(text, launchUri)`), without waking the app at all.
  - `NotificationContent.Arguments` is the same navigation payload, but for clicking the
    notification body itself (as opposed to a button); read it back from
    `NotificationActivatedEventArgs.Arguments` in the `Activated` handler to route the user
    to the relevant place in the app.
  - `Activated` is raised on the UI thread. Windows reports toast clicks on a background thread,
    and the service marshals each one to the application dispatcher first, so a handler can
    navigate or append to a bound collection directly — the sample's handler does the latter.
  - `NotificationActivatedEventArgs.Title`/`Message` are the clicked toast's own text, looked up
    by an id the default toast platform adds to each toast's activation arguments. Windows hands
    back only those arguments, never the text, so a toast left in the notification center by an
    *earlier run* of the app (or one more than 100 toasts old) reports empty strings. Route on
    `Arguments`, which survives restarts; treat `Title`/`Message` as display text.
  - Per-project branding: `NotificationOptions.IconPath` sets a persistent circular app
    logo overlay shown on every notification (defaults to the executable's icon); per-call
    visuals (image, buttons, navigation) are set per `NotificationContent`/`Show` call, so
    each feature of the app can shape its own notifications independently.
  - Windows silently drops a toast it isn't allowed to show (e.g. the user turned
    notifications off in Settings > System > Notifications) instead of raising an error, so
    `Show(...)` alone can't tell you it was blocked. Check `INotificationService.Availability`
    (a live `NotificationAvailability` read, not cached — call it whenever you need current
    state, e.g. when the window is activated) to detect this and show your own in-app
    fallback, and call `OpenSystemSettings()` to deep-link the user to the fix. The sample's
    "Notifications" row demonstrates this: the description turns into a warning and an "Open
    notification settings" button appears whenever `Availability != Enabled`.
- `IPushNotificationService` is a **client** for a realtime push-notification server — it
  listens for incoming notifications and displays each one through `INotificationService` above
  (the primary path), falling back to a small in-app `IPushNotificationFallbackPresenter` window
  whenever `Availability != Enabled`, `IsEnabled == false`, or showing the real toast throws. It
  does not include a server.
  - **Two layers, deliberately split**: `IPushNotificationService` itself knows nothing about
    *how* notifications actually arrive — it only depends on `IPushNotificationTransport`, a
    minimal contract (`StartAsync`/`StopAsync`/`NotificationReceived` raw JSON text, no hub URLs
    or RPC method names). The bundled default, `SignalRPushNotificationTransport`, owns every
    SignalR-specific detail (hub URL, method names, the handshake payload) itself, configured
    through its own `SignalRPushNotificationOptions` — none of that leaks into the
    transport-agnostic `PushNotificationOptions`/`IPushNotificationService` surface. That split
    is what makes swapping in a different delivery mechanism possible without reshaping
    anything else: implement `IPushNotificationTransport` yourself and register it *before*
    calling `ConfigurePushNotifications` —
    `services.AddSingleton<IPushNotificationTransport, MyTransport>()` (`TryAddSingleton` inside
    `ConfigurePushNotifications` then skips the SignalR default), same swap-in pattern as
    `IPushNotificationFallbackPresenter`/`INotificationPlatform` elsewhere in this library.
    Worth being explicit about the limits of this: **Firebase Cloud Messaging and Windows
    Notification Service don't map onto a client-managed persistent connection the way SignalR
    does** — they're token-registration-plus-OS-push-channel systems, not something an app calls
    `StartAsync` on and receives RPC-style callbacks from. Plugging one in still means writing a
    real `IPushNotificationTransport` for that mechanism's actual delivery model (token
    exchange, platform channel subscription, ...); this library only guarantees the *seam*
    exists and stays free of SignalR assumptions, not a ready-made implementation for every
    backend.
  - The default payload type, `PushNotification`, implements `IPushNotification` (`Title`,
    `Body`, `ImageUrl`, `Action`) — deserialize your own server's JSON shape into your own type
    instead by implementing `IPushNotification` and registering it with
    `ConfigurePushNotifications<TNotification>()`.
  - `NotificationReceived` always reports `ReceivedAt` (the local wall-clock time the client
    processed the notification) and `UsedFallback`, regardless of which display path fired —
    so you always know when something arrived even if you never touch the UI event.
  - A notification's `Action` (`Url`/`Setting`/`Route`/`None`) is dispatched automatically for
    `Url`/`Setting` (opened via `Launcher`); `Route` raises `RouteRequested` instead, since only
    your app knows its own screens/navigation. `RouteRequested` is raised on the UI thread for
    both a toast click (it comes through `INotificationService.Activated`) and a click on the
    built-in fallback window, so the handler can navigate directly.
  - The default transport's own `HandshakeMethodName`/`NotificationMethodName`/
    `AcknowledgeMethodName` (defaulting to `"RegisterDevice"`/`"ReceiveNotification"`/
    `"AcknowledgeNotification"`) are configurable via `ConfigurePushNotifications`'s
    `configureSignalR` parameter (or the `Barbatos:PushNotifications:SignalR` config section) to
    match whatever hub method names your specific server actually uses. Set `AppKey` there too if
    your server authenticates clients.
  - **Delivery is at-least-once, and the client makes that invisible.** Every notification with a
    `ReceiptId` is acknowledged back to the server as soon as it has been displayed, which is what
    lets a server keep an offline queue and stop re-sending. Because an acknowledgement can be lost
    to a dropped connection, the server will occasionally re-deliver something already shown, so
    the service remembers the last `PushNotificationOptions.DeduplicationHistorySize` receipt ids
    and shows each notification only once — while still re-acknowledging the repeat, since the
    repeat is itself evidence the first acknowledgement never landed.
  - **The handshake is re-sent after every automatic reconnect.** SignalR issues a new connection
    id on reconnect, so a server that maps connections to devices sees the reconnected client as an
    anonymous stranger until it identifies itself again. Without this, a client that briefly lost
    its network would silently stop receiving anything until the app was restarted.
- InputSystem unifies three binding kinds under one `InputAction`: `KeyBinding` (in-app, requires
  keyboard focus — optionally scoped to a specific `FrameworkElement` via `InputActionMap.Scope`
  instead of the whole app), `GlobalKeyBinding` (a true OS-level hotkey via `RegisterHotKey`,
  fires even while unfocused/minimized), and `ChordBinding` (an in-app *sequence* of combinations
  pressed one after another within a timeout — VS Code's "leader key" shortcuts, e.g. `Ctrl+K`
  then `Ctrl+P`). An action can carry a `KeyBinding` and a `GlobalKeyBinding` for the same
  combination — the same `Performed` event fires from either source. In practice only the global
  path ever actually fires from a real keystroke while the app is focused: once `RegisterHotKey`
  claims a combination, Windows routes every press of it into `WM_HOTKEY` exclusively,
  system-wide, for as long as the registration holds — *including* while the registering app's
  own window has focus — so the ordinary `WM_KEYDOWN`/`WM_KEYUP` sequence the `KeyBinding` half
  relies on simply never arrives for that exact combination in the first place; there is no
  double-firing to suppress because the two paths structurally cannot both fire for one physical
  keystroke. The `KeyBinding` half is really a fallback for the (rare) case the global
  registration itself fails, e.g. another app already owns that exact combination. Matching is
  always exact (`ModifierKeys.None` never matches with a modifier held, like WPF's own native
  `KeyGesture`), and `IInputInteraction` (`HoldInteraction`, `TapInteraction`,
  `MultiTapInteraction`, `PressInteraction`) reinterprets raw press/release into gestures like
  "held for 2 seconds" — conceptually ported from Unity's Input System (Actions/Bindings/
  Interactions), narrowed to what a keyboard-only desktop app actually needs: no Composite
  Bindings (WPF's own `ModifierKeys` flags enum already collapses a modifier+key chord into one
  binding), no Control Schemes (there's only one device family), no Processors/Device abstraction
  layer/`.inputactions` file format/haptics/XR/mobile — none of that carries over.
  `GlobalKeyBinding` specifically cannot carry an interaction, and there is no global counterpart
  of `ChordBinding` either: `RegisterHotKey`/`WM_HOTKEY` has no "released" message and delivers a
  single edge-triggered notification per press, a hard OS constraint rather than a design choice
  — a sequence is tracked entirely from WPF's own key events, which never arrive while unfocused
  in the first place. Bindings can be changed at runtime — e.g. for a user-facing "customize your
  shortcuts" screen — via `InputAction.RemoveBinding`/`ClearBindings`; a `KeyBinding`/`ChordBinding`
  change takes effect on the very next keystroke automatically, while a `GlobalKeyBinding` change
  additionally needs `IInputSystemService.RefreshBindings()` to actually re-register with the OS.

---

## Dialogs

`IDialogService` (registered by `ConfigureDialogs()`) centralizes showing and tracking child
windows ("dialogs") so owner assignment, duplicate-open prevention, and graceful bulk-close
behave consistently no matter where in the app a dialog is opened from. It has no .NET MAUI
counterpart — MAUI's page-based navigation model has no equivalent of WPF's multi-window/owner
model — and closes three well-known WPF footguns:

```csharp
builder.ConfigureDialogs();

builder.Services.AddTransient<AboutWindow>();
```

```csharp
// In a button's click handler, resolved via constructor injection of IDialogService:
_dialogService.Show<AboutWindow>();          // non-modal, double-click-safe
_dialogService.ShowDialog<AboutWindow>();     // modal, blocks until closed
_dialogService.Show(dialog, owner: someWindow, key: "customer-42", closeOthers: true);
```

### Owner assignment

Not setting `Window.Owner` explicitly lets Windows decide Z-order/activation on its own, which
is what causes dialogs to end up "under" an unrelated foreground application, or two
concurrently-open dialogs fighting over which one visually owns the other. `Show`/`ShowDialog`
always resolve and set `Owner` exactly once, before a fresh window is ever shown:

- Pass `owner:` explicitly when you know it (e.g. always owned by `MainWindow`, or by the
  window a dialog was opened from).
- Otherwise it defaults to `IDialogService.ActiveWindow` — the most recently *activated*
  window this service has seen (every dialog shown through it, plus `Application.MainWindow`,
  tracked opportunistically the first time it's observed) — never the operating system's
  notion of the active window, which is what lets an unrelated external application end up as
  a dialog's owner. This is also what makes "a dialog opened from another dialog" work
  correctly without any extra code: since the dialog you opened *from* is the currently active
  window, a new dialog shown from inside it is owned by it, not by `MainWindow`.

### Closing other dialogs, without losing in-progress work

`Show`/`ShowDialog`'s `closeOthers: true` closes every other dialog currently tracked by this
service before showing the new one; `CloseAll()`/`Close(key)` do the same on demand (e.g. from
a "Close all windows" menu command). All of these are *graceful*: each dialog still gets a
chance to veto via its own `Closing` event (`e.Cancel = true`), so in-progress/unsaved work is
never silently discarded — `CloseAll()`/`Close(key)` return `false` if anything vetoed, and
`closeOthers`/`CloseAll()` leave a vetoing dialog open rather than forcing it shut.

This graceful behavior also applies to `DialogOptions.CascadeCloseOwnedDialogs` (default:
`true`). Plain WPF already closes a window's owned dialogs when *it* closes — but
unconditionally, **ignoring each owned dialog's own `Closing` veto** (verified: an owned
window's `e.Cancel = true` does not stop it from being force-closed once its owner closes).
That is exactly the data-loss risk this option prevents: with it enabled, closing a window
first closes the dialogs it owns itself, giving each one (recursively, however many owned
dialogs deep) a real, respected veto — and if any of them refuse, the owner's own close is
cancelled too, so the whole chain stays open together instead of tearing down partway through.

### Preventing a double-click from opening a dialog twice

Calling `Window.Show()` (not `ShowDialog()`) from a button's click handler is what opens a
second instance on a rapid double-click, since `Show()` doesn't block — this is the most common
way this bug happens in practice. `Show`/`ShowDialog` key each dialog by `key` (defaults to the
window type's full name) and, if a dialog with that key is already tracked as open, activate
the existing instance instead of showing a duplicate — `Show` returns `false` in that case so
you can tell the two apart if you need to. Pass a more specific `key` (e.g. including an entity
id) when you deliberately want several instances of the same window type open at once, one per
key — an "edit customer" dialog, for example, where different customers should be editable
simultaneously but the same customer twice should just refocus the existing window.

---

## SplashScreen

This is fresh work, not a port: on Windows, a splash screen for a **packaged** (MSIX) app is
just a build-time asset pipeline generating an AppxManifest entry, with zero runtime C# logic
needed — but that manifest is stripped entirely for unpackaged apps, the deployment model this
library targets, so there's nothing to build on from that approach here. `WpfApplication` gets
a splash screen hook of its own instead (a lifecycle hook you override, plus a plain settings
object) rather than the DI-based `Configure...()` pattern the other features use — a splash
screen has to show before the dependency injection container even exists.

```csharp
public partial class App : WpfApplication
{
    protected override SplashScreenOptions GetSplashScreenOptions() => new()
    {
        AppName = "My App",                                   // defaults to AppInfo.Name
        LogoSource = "pack://application:,,,/Assets/logo.png",
        Tagline = "Loading your workspace...",
        SponsorLogos =
        {
            new SplashScreenLogo("pack://application:,,,/Assets/sponsor1.png", "Sponsor Inc.", "https://sponsor.example.com"),
        },
        RelatedLinks =
        {
            new SplashScreenLink("My Other App", "Also by this publisher", "https://example.com/other-app"),
        },
        MinimumDisplayDuration = TimeSpan.FromSeconds(1.5),   // default
    };

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        await CloseSplashScreenAsync(); // waits out MinimumDisplayDuration, then closes the splash

        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }
}
```

`GetSplashScreenOptions()` returning a non-null value is what turns the splash screen on — the
built-in `SplashWindow` is shown immediately in `OnStartup`, *before* `CreateWpfApp()` runs, so
it actually covers slow startup work (a slow `IWpfInitializeService`, for example). Sponsor
logos and related links are each individually clickable (opens their `LinkUrl`/`Url` via
`Launcher`) when one is provided, and hidden entirely when their list is empty.

> [!IMPORTANT]
> Purely *synchronous* startup work still blocks the UI thread as usual, so the splash screen
> (and its progress indicator) will not animate while it runs — same as any WPF window. Move
> slow work to an async continuation, awaited before `CloseSplashScreenAsync()`, if you need the
> splash to stay responsive/animated while it happens.

### Loading important data before the main window ever appears

A slow query for something the main window needs (a workspace summary, a license check, ...)
often ends up wired to `MainWindow`'s own `Loaded` event or its ViewModel's mount hook — which
means the window pops up empty first, then that content flashes in a moment later. The splash
screen is already sitting there for exactly this: resolve the ViewModel and await its slow work
*before* closing the splash and showing the window, instead of after:

```csharp
protected override async void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    // MainViewModel is registered as a singleton, so MainWindow's own constructor resolves
    // this exact same, already-loaded instance below - nothing to pass through by hand.
    var mainViewModel = Services.GetRequiredService<MainViewModel>();
    await mainViewModel.LoadWorkspaceSummaryAsync();

    await CloseSplashScreenAsync(); // a no-op wait - the query above already took longer
                                    // than MinimumDisplayDuration, so nothing left to wait out

    MainWindow = Services.GetRequiredService<MainWindow>();
    MainWindow.Show();
}
```

`samples/Barbatos.Wpf.Core.Sample` demonstrates this end to end: `MainViewModel.LoadWorkspaceSummaryAsync()`
simulates a slow query (a 3-second delay standing in for a real database read/license check/cache
warm-up) and sets `WorkspaceSummary`, which `MainWindow.xaml`'s header displays - already there
the instant the window appears, never an empty flash followed by a pop-in.

This is a straightforward reordering, not a new API - the only two things that make it work are
already true of any DI-registered ViewModel: it must be a **singleton** (a scoped/transient
registration would hand `MainWindow`'s constructor a second, still-unloaded instance instead of
the one already awaited above), and the slow work has to be something you can genuinely `await`
(if your "slow work" is synchronous, an `IWpfInitializeService` is the better fit - it runs
inside `CreateWpfApp()`, during `Build()`, after the splash screen is already showing, so the
splash covers it too, though without animating; see [Lifecycle events](#lifecycle-events)).

Weigh this against just showing the window immediately and letting that one section show its
own loading state instead (a spinner, a skeleton) - blocking the *entire* app's startup on one
query is the right call when nothing else in the window is meaningfully usable without it, but
if most of the window works fine without that data, an in-place loading indicator for just that
section keeps the rest of the app interactive sooner.

### Full customization

For full control over the UI instead of the built-in layout, override `CreateSplashScreen()`
and return any `Window` you like — implement `ISplashScreen` on it too if you still want
`MinimumDisplayDuration` support:

```csharp
protected override Window CreateSplashScreen() => new MyOwnSplashWindow();
```

The default implementation of `CreateSplashScreen()` is what creates the built-in
`SplashWindow` from `GetSplashScreenOptions()`; overriding `GetSplashScreenOptions()` is enough
for most apps; overriding `CreateSplashScreen()` bypasses it entirely.

### Showing it conditionally

`GetSplashScreenOptions()`/`CreateSplashScreen()` are just plain methods called once per launch
in `OnStartup` — returning `null` from either one means no splash screen for that launch, so any
condition you can express in code works, with no extra API needed:

```csharp
protected override SplashScreenOptions? GetSplashScreenOptions()
{
    // Only on this device's very first-ever launch.
    if (!VersionTracking.IsFirstLaunchEver)
        return null;

    return new SplashScreenOptions { Tagline = "Welcome!", MinimumDisplayDuration = TimeSpan.FromSeconds(3) };
}
```

A few other common conditions, same pattern:

```csharp
// Once per app update, not every launch.
if (!VersionTracking.IsFirstLaunchForCurrentVersion) return null;

// Opt out via a command-line flag (e.g. unattended/automation runs). GetSplashScreenOptions()
// takes no parameters, so use Environment.GetCommandLineArgs() rather than OnStartup's own
// StartupEventArgs.Args.
if (Environment.GetCommandLineArgs().Contains("--no-splash")) return null;

// Opt out via a user-facing setting.
if (!Preferences.Get("ShowSplashScreen", true)) return null;
```

> [!IMPORTANT]
> Both methods run *before* `CreateWpfApp()` — the dependency injection container does not
> exist yet at this point, so only the static Essentials facades (`VersionTracking`, `AppInfo`,
> `PublisherInfo`, `Preferences`, ...) are usable inside them, not `Services.GetRequiredService<...>()`.
> `VersionTracking`/`Preferences` work standalone for exactly this reason - they only depend on
> each other and on `AppInfo`, never on the host.

### Avoiding flicker: minimum display duration

`SplashScreenOptions.MinimumDisplayDuration` (default: 1.5 seconds) keeps the splash screen
visible for at least that long from the moment it is shown, regardless of how quickly the rest
of startup finishes — this is what avoids a jarring flash on a fast machine, at the deliberate
cost of the splash screen acting like an "ad slot" for at least that long. The clock starts
before `CreateWpfApp()` runs, so a *slow* startup is only ever waited out, never delayed
further: `CloseSplashScreenAsync()` computes the remaining time and only awaits if there still
is any.

---

## Periodic services

`IWpfPeriodicService` is the recurring counterpart of `IWpfInitializeService`: implement it,
register it, and it runs on the application dispatcher according to its `PeriodicSchedule` -
a class carrying a start time, an end time, a recurrence frequency, and a description.

```csharp
public sealed class SyncService : IWpfPeriodicService
{
    public string Name => "Sync";

    public PeriodicSchedule Schedule => new()
    {
        Frequency = PeriodicFrequency.Custom,
        Interval = TimeSpan.FromMinutes(5), // default, configurable
        Description = "Synchronizes local data with the server.",
    };

    public async Task ExecuteAsync(IServiceProvider services, CancellationToken ct)
    {
        await Task.Run(() => { /* heavy work off the UI thread */ }, ct);
    }
}

builder.ConfigurePeriodicServices<SyncService>();
// or: builder.Services.AddSingleton<IWpfPeriodicService, SyncService>();
//     builder.ConfigurePeriodicServices();
```

`Frequency` is `Once`, `Hourly`, `Daily`, `Weekly`, `Monthly`, or `Custom`. `Daily`, `Weekly` and
`Monthly` are calendar-anchored - they run at a specific wall-clock `TimeOfDay`, on specific
`DaysOfWeek` or a specific `DayOfMonth`, the same way a calendar reminder or a Windows Task
Scheduler trigger would - not simply "every N days from whenever the app happened to start":

```csharp
public PeriodicSchedule Schedule => new()
{
    Frequency = PeriodicFrequency.Weekly,
    DaysOfWeek = WeekDays.Monday | WeekDays.Thursday,
    TimeOfDay = new TimeSpan(9, 0, 0), // 09:00
    Description = "Sends the weekly digest email.",
};
```

`Hourly` and `Custom` are plain fixed-duration repeats instead (`Custom` requires a positive
`Interval`). `StartTime`/`EndTime` bound when a schedule is allowed to produce an occurrence at
all - both are optional; `Once` runs a single time at `StartTime` (or immediately if unset).

The schedule can be configured in three ways (file overrides code, as a whole schedule per
service; UI wins at runtime):

1. **Code** — the service's own `Schedule` property.
2. **File** — the `Barbatos:PeriodicServices` section:

   ```json
   {
     "Barbatos": {
       "PeriodicServices": {
         "Enabled": true,
         "Schedules": {
           "Sync": { "Frequency": "Custom", "Interval": "00:05:00" }
         }
       }
     }
   }
   ```

3. **UI** — through `IPeriodicServiceScheduler`:
   `scheduler.UpdateSchedule("Sync", new PeriodicSchedule { ... })` reschedules immediately;
   `SetEnabled(bool)` starts/stops all services; `Services` exposes live status (schedule, next
   run time, last run, run count, whether it has completed) and `ServiceExecuted` reports every
   run, including failures.

Failed executions are logged and do not stop the schedule; a tick is skipped while the
previous run is still in progress; the cancellation token passed to `ExecuteAsync` is
cancelled when the host is disposed.

### Registering services after startup

Every example above registers `SyncService` at host-build time via DI, but
`IPeriodicServiceScheduler` also exposes `Register`/`Unregister`, which work at any time after
the host has already started - not only while the builder is being configured:

```csharp
scheduler.Register(new SyncService());
scheduler.Unregister("Sync");
```

This doesn't let a settings UI conjure new behavior out of nothing - `ExecuteAsync` is still
ordinary code written by a developer - but that code no longer has to be wired up only at
startup. A plugin loaded later, or a factory parameterized by something the end user picked in a
settings UI, can hand the scheduler a new `IWpfPeriodicService` at any time.

---

## State management (`Barbatos.Wpf.Apsu`)

A lightweight state-management layer for WPF: a **store** is an `ObservableObject`-derived
class holding state as ordinary properties, plus batched change notification, an explicit
reset hook, computed getters that can span several stores, plugins, and a middleware-style
action interceptor chain that can observe, time, retry, or outright block an action call.

> **Familiar from Vue's Pinia?** The design borrows its shape from Pinia's own
> `defineStore`/getters/actions/plugins model - a head start if you've used it before, though
> nothing here requires that background.

```csharp
public class CounterStore : StoreBase
{
    int _count;
    readonly Getter<string> _summary;

    public CounterStore() => _summary = new Getter<string>(() => $"Count is {Count}", this);

    public int Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }

    public string Summary => _summary.Value; // computed(), cached until StateChanged fires

    [Action]
    public virtual void Increment() => Count++;

    public override void Reset() => Count = 0;
}
```

```csharp
builder.Services.AddStore<CounterStore>();
```

```csharp
// Resolved via DI (a ViewModel's constructor, typically) like any other registered service:
counterStore.Increment();
var summary = counterStore.Summary; // "Count is 1"
```

### Stores

`StoreBase` (abstract, derives from `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`) is
the base every store derives from:

| Member | Role |
| --- | --- |
| State | Ordinary properties set via `SetProperty` |
| `StateChanged` (`event EventHandler?`) | Fires once per property set outside `Patch`, or once per `Patch` call no matter how many properties it touched |
| `Patch(Action<StoreBase> mutate)` | Batches several property changes into a single `StateChanged` notification |
| `Reset()` (`virtual`) | Restores initial state - **must be overridden**; the default throws `NotSupportedException` so a forgotten override fails loudly instead of silently doing nothing |

Register a store with **`AddStore<TStore>()`** - it becomes a singleton, created lazily on
first resolution and cached for the container's lifetime after that, keyed by `TStore` itself.
A store's constructor can take further dependencies - other services, or other stores,
resolved from the container (once interceptors apply, with the stricter rules listed at the end
of [Actions and action interceptors](#actions-and-action-interceptors)). Register its local
interceptors in the same call by chaining `Add` on the builder it hands you - one `AddStore`
call handles the store and every local interceptor it needs, instead of a separate
`AddStoreInterceptor<TStore>(...)` call per interceptor plus a trailing `AddStore<TStore>()`:

```csharp
builder.Services.AddStore<OrderStore>(i => i
    .Add<NonEmptyCartGuardInterceptor>()
    .Add(sp => new CheckoutCooldownInterceptor(TimeSpan.FromSeconds(3), sp.GetRequiredService<ApsuActivityLog>())));
```

Omit the callback entirely (the default) for a store that only needs global interceptors, or
none at all - see [Actions and action interceptors](#actions-and-action-interceptors) below
for what each `Add` overload does.

### Getters

`Getter<T>` is a lazily-computed, cached value, invalidated whenever any of its declared
dependency stores raises `StateChanged`.

```csharp
readonly Getter<string> _summary;

public OrderStore(UserStore userStore)
{
    _summary = new Getter<string>(
        () => $"{OrderCount} order(s) for {userStore.UserName}",
        this, userStore); // combines two stores - list every store the callback reads
}

public string Summary => _summary.Value;
```

Every store the callback reads must be listed explicitly as a `dependencies` argument, since
C# has no proxy-based reactivity to auto-track that on its own - this is a coarse invalidation
(any state change on any listed dependency recomputes the getter, not just a change to the
specific property it actually reads), a deliberate simplification over a full
dependency-tracking system.

Three things a getter does not do, each of which bites when it is shown in XAML:

- **It raises no `PropertyChanged`.** `Summary` above is a plain property read, so
  `{Binding OrderStore.Summary}` reads it once and never updates. Expose it through a ViewModel
  that re-raises it whenever any store the getter reads changes:

  ```csharp
  orderStore.StateChanged += (_, _) => OnPropertyChanged(nameof(Summary));
  userStore.StateChanged += (_, _) => OnPropertyChanged(nameof(Summary));

  public string Summary => _orderStore.Summary;
  ```

  Raise it from a ViewModel, not from the store's own `StateChanged` handler - a store raising its
  own `PropertyChanged` there fires `StateChanged` again, recursively.
- **Forwarding on `PropertyChanged` goes stale inside `Patch`.** Outside a `Patch`, the store's
  own `PropertyChanged` handler (subscribed first, in `StoreBase`'s constructor) raises
  `StateChanged` before any later subscriber runs, so the getter is already invalidated. Inside a
  `Patch`, `StateChanged` waits until the patch returns: a `PropertyChanged` handler that reads
  the getter gets the old cached value, and nothing notifies again afterwards. Forward on
  `StateChanged`, as above.
- **It has no `Dispose`.** Its constructor subscribes to every dependency's `StateChanged` and
  never unsubscribes. Inside a store that costs nothing - store and getter share one lifetime -
  but a `Getter<T>` built in a short-lived ViewModel over an app-lifetime store keeps that
  ViewModel, and everything the callback captures, alive for the life of the app. Create getters
  inside stores; for a per-screen derived value use something the ViewModel can dispose (for
  example Aquarius's `Computed<T>`).

### Actions and action interceptors

A `[Action]`-marked `public virtual` method is interceptable, shaped as ASP.NET Core-style
middleware: a chain of interceptors that can run code before and after the action, retry it,
or short-circuit it entirely. Interception only activates when at least one
`IStoreActionInterceptor` applies to that store; a store with no interceptors registered never
pays for the proxy.

```csharp
public sealed class LoggingInterceptor : IStoreActionInterceptor
{
    public async Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
    {
        // context.Store is the generated proxy subclass; its BaseType is the store's own type.
        var store = context.Store.GetType().BaseType!.Name;
        Console.WriteLine($"[{store}] {context.ActionName} starting");
        var result = await next(); // the rest of the chain, then the action itself
        Console.WriteLine($"[{store}] {context.ActionName} done");
        return result;
    }
}
```

An interceptor only ever runs on a store built behind the proxy (below), so
`context.Store.GetType()` is always the generated subclass - `OrderStoreProxy` or similar - and
`GetType().BaseType` is the type you registered.

```csharp
// Global - wraps every store's actions. DI-constructed - no `new` needed, since
// LoggingInterceptor's own constructor only needs other registered services (if any):
builder.Services.AddSingleton<IStoreActionInterceptor, LoggingInterceptor>();

// Local - wraps only CounterStore's actions, registered together with the store in one call.
// Add<T>() is DI-constructed the same way the global registration above is:
builder.Services.AddStore<CounterStore>(i => i.Add<ThrottleInterceptor>());
```

An interceptor that needs a value the container can't supply on its own (a duration, a
threshold, ...) alongside a service it can isn't a special case - `Add` has a factory overload
too, still getting the service DI-resolved, explicit about the one input that has to come from
the call site instead of the container:

```csharp
builder.Services.AddStore<CounterStore>(i => i.Add(sp =>
    new ThrottleInterceptor(TimeSpan.FromSeconds(1), sp.GetRequiredService<IClock>())));
```

Chain as many `Add` calls as the store needs local interceptors - `Add<TInterceptor>()`
(DI-constructed), `Add(instance)` (already-built), and `Add(factory)` (built lazily, a service
plus a raw value) mix freely in the same chain. For registering a local interceptor separately
from `AddStore` instead - e.g. from a different composition step - the standalone
`AddStoreInterceptor<TStore, TInterceptor>()` (and its instance/factory overloads) remain
available too.

Both kinds compose into a single chain per store: every **global** interceptor runs, in
registration order, outside every **local** one - the same nesting a global ASP.NET Core
middleware has relative to endpoint-specific behavior. Registering more than one interceptor for
the same store (global or local) stacks them, innermost by registration order. Not calling
`next` short-circuits the whole chain - the action itself never runs, not just observed after
the fact:

```csharp
public Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
{
    if (context.ActionName == nameof(UserStore.SelectUser) &&
        context.Arguments[0] is int userId && userId is < 1 or > 10)
    {
        return Task.FromResult<object?>(null); // blocked - SelectUser's body never runs
    }

    return next();
}
```

`StoreActionContext.Arguments` carries the actual call arguments in declaration order, so a
condition can be about *what an action was called with*, not just the store's current state.

`next` can be called after other asynchronous work, and called again to retry. An async
precondition (a server-side permission check) can be awaited *before* `next()`, and a retry
interceptor can call `next()` again after an attempt failed - even one that failed after the
action's own first `await`. A local retry interceptor on a store whose actions return `Task`
(see the note below for why that matters):

```csharp
public async Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
{
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            return await next(); // re-runs the interceptors after this one, then the action
        }
        catch (HttpRequestException) when (attempt < 3)
        {
            await Task.Delay(TimeSpan.FromSeconds(attempt)); // back off, then try again
        }
    }
}
```

Each call of `next` re-runs the rest of the chain - every interceptor after this one in the
chain order above, then the action body - while interceptors outside this one see a single
call. Make one call at a time (await it before starting the next), and keep the awaits before
`next` on the caller's context (no `ConfigureAwait(false)`): the action body runs on whichever
thread calls `next`.

Both shapes used to hang or misbehave, because Castle DynamicProxy's `IInvocation` - its
interceptor index and its `ReturnValue` - is only safe to proceed while the proxied call is still
running on its own thread. Proceeded from a later continuation with `IInvocation.Proceed()`, it
re-entered the whole chain instead of running the action: an `await` before `next` suspended
again on every re-entry, a hang with no exception (confirmed by killing a stuck test host), and a
retry ran the outer interceptors again, nested. `IInvocation.CaptureProceedInfo()` fixes that only
where continuations wait for the call to return, as on a UI thread; a thread-pool continuation can
run while the call is still returning, and a stress test then saw calls run the chain twice or
never complete. So the pipeline proceeds Castle's invocation only from inside the call, and any
later `next` - or one from another thread - reaches the action through a second call on the proxy
that skips the chain. `StoreActionInterceptorTests` pins each shape, the thread-pool race included.

> [!IMPORTANT]
> **Genuinely asynchronous interceptor work needs a `Task`-returning action.** A synchronous
> action's interceptor chain still runs through the same `async` shape, but the proxy blocks on
> it (`GetAwaiter().GetResult()`) since there's no `Task` to hand back to a synchronous caller.
> That is safe as long as every interceptor in the chain only does synchronous work (an
> `async` method awaiting already-completed tasks counts), but an interceptor that genuinely
> suspends anywhere in it - before or after `next` - waits for a continuation on the UI thread
> the proxy is blocking: the classic WPF sync-over-async deadlock. A global interceptor wraps
> every store's synchronous actions too, so it should await nothing but `next()` itself.

Combine with `OnDispatcherUnhandledException` (see [Lifecycle events](#lifecycle-events)) to give
a store action's error and any other unhandled exception one shared reporting path, since an
interceptor can `catch` around `next()` and still let the exception propagate:

```csharp
public sealed class ErrorReportingInterceptor : IStoreActionInterceptor
{
    public async Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            Report(context.Store.GetType().BaseType!.Name, context.ActionName, ex); // same sink OnDispatcherUnhandledException uses
            throw;
        }
    }
}
```

Interception works by generating a runtime subclass via Castle DynamicProxy that overrides every
`[Action]`-marked method, and that asks more of a store than the plain path does. A store with
at least one interceptor applying to it - and a **global** interceptor applies to every store, so
adding one can break a store that has none of its own - must be:

- **non-`sealed`** - otherwise `InvalidOperationException` ("must not be sealed") at first
  resolution;
- **`public`**, along with every type it is nested in - Castle's generated assembly cannot
  subclass anything less visible, and throws `ArgumentException` ("is not accessible");
- built through **exactly one public constructor** - otherwise `InvalidOperationException`
  ("must have exactly one public constructor"). Each of its parameters is resolved with
  `GetRequiredService`, so an optional parameter's default value is *not* used the way
  `ActivatorUtilities` (the plain path) uses it: a parameter whose type isn't registered throws
  "No service for type ..." even when it has a default.

All three fail loudly at first resolution rather than silently skipping interception.

#### Removing an interceptor at runtime

Register with the `out InterceptorHandle` overload instead of the plain one to be able to
remove an interceptor later without rebuilding the host - `axios`'s
[`interceptors.request.eject(id)`](https://axios.rest/pages/advanced/interceptors.html):

```csharp
builder.Services.AddStoreInterceptor(new LoggingInterceptor(), out var loggingHandle);
// Local interceptors have the same overload: AddStoreInterceptor<TStore>(interceptor, out handle)

// Later, e.g. once a diagnostics session is over for good:
loggingHandle.Eject();
```

Ejecting doesn't remove the interceptor from the chain structurally - its slot just becomes a
transparent pass-through straight to the next interceptor (or the action itself) from that call
onward, as if it had never been registered. Each call checks at the moment it reaches that slot:
a call already inside the interceptor when `Eject()` runs finishes with it, while a call still in
an interceptor further out (awaiting before `next()`, say) skips it when it gets there, as does
every call that starts afterward. Most interceptors don't need
this - it's opt-in per interceptor, not automatic.

Ejecting is one-way. There's no "re-attach", and registering the interceptor again is no way back
either: `Build()` makes the service collection read-only, and each store reads its chain once,
when it is first resolved - getting the behavior back takes a new container. For behavior a
user can switch on and off (a settings toggle), give the interceptor its own flag instead:

```csharp
public sealed class ToggleableLoggingInterceptor : IStoreActionInterceptor
{
    public bool Enabled { get; set; } = true; // flipped by a settings screen, any number of times

    public Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next) =>
        Enabled ? LogAroundAsync(context, next) : next();

    async Task<object?> LogAroundAsync(StoreActionContext context, StoreActionDelegate next)
    {
        Console.WriteLine($"{context.ActionName} starting");
        var result = await next();
        Console.WriteLine($"{context.ActionName} done");
        return result;
    }
}

// Register the instance so the settings screen can reach the same object:
var logging = new ToggleableLoggingInterceptor();
builder.Services.AddSingleton(logging);
builder.Services.AddSingleton<IStoreActionInterceptor>(logging);
```

### Plugins

`IStorePlugin` extends every store with cross-cutting behavior:

```csharp
public sealed class PersistencePlugin : IStorePlugin
{
    public void Apply(StoreBase store)
    {
        if (store is CounterStore counter)
            counter.StateChanged += (_, _) => Preferences.Set("count", counter.Count);
    }
}

builder.Services.AddSingleton<IStorePlugin, PersistencePlugin>();
```

Every registered plugin's `Apply` runs once per store, right after that store's own
constructor has already run, in plugin-registration order. A plugin can read/write state or
subscribe to `StateChanged`, but cannot transparently intercept an action call (use an
`IStoreActionInterceptor` for that instead).

`Apply` runs inside that store's own DI resolution, so state a plugin *restores* is set before
anything else can have subscribed to the store - the change notifications for it reach nobody.
Whatever reacts to that state has to read the current value when it subscribes, the way Vue's
`watch(..., { immediate: true })` does: the Aquarius sample's `OrderStore` checks for a restored
`UserStore.SelectedUserId` right after wiring its watch, and loads that user's orders.

### Registry

`IStoreRegistry` (resolved automatically - there's normally no need to depend on it directly)
exposes every store actually resolved so far, for cross-cutting introspection (e.g. a debug
panel listing live stores). It exists purely for introspection, not caching - the DI container
itself is already what caches and singleton-izes each store.

See the `Barbatos.Wpf.Apsu` namespace in the [API Reference](#api-reference) for the full member
list. Two worked examples ship with this repository:

- **`samples/Barbatos.Wpf.Core.Sample`** - `CounterStore` (state, a getter, two actions) plus
  `SampleActionLoggingInterceptor`, a single global interceptor that logs into
  `SampleActivityLog`; the `OnDispatcherUnhandledException` lifecycle hook reports into that same
  log, so a failed action and any other unhandled exception share one sink and one policy.
- **`samples/Barbatos.Wpf.Aquarius.Sample`** (`ApsuInteropDemoView`/`ApsuInteropDemoViewModel`) -
  a richer scenario against a real API (jsonplaceholder.typicode.com): `UserStore`/`OrderStore`
  watching each other's state and calling across stores, a getter combining both (forwarded to
  XAML by the ViewModel), a plugin that restores the last selected user, and a full
  3-global + 3-local interceptor chain (logging, timing, audit trail, argument-based validation,
  a state-based guard, and a cooldown) demonstrating short-circuiting at every layer.

---

## AI chat + MCP (`Barbatos.Wpf.Mcp`)

Every modern AI-capable desktop app now ships MCP ([Model Context
Protocol](https://modelcontextprotocol.io)) support, so it can call out to external tools through
a standard, LLM-agnostic protocol. `ConfigureMcp()` gives any WPF app built on Barbatos.Wpf.Core
the same capability - connect to any number of MCP servers, and chat with an LLM that
automatically gets every connected server's tools.

**Bring-your-own-key (BYOK), deliberately**: this feature exists so the *application publisher
never pays for LLM usage* - the application's own end user supplies their own Gemini, Claude,
ChatGPT, or other-provider API key at runtime (typically through a settings screen you build),
and `IAiApiKeyProvider` stores it via `SecureStorage` (DPAPI-encrypted, never written to a config
file or sent anywhere but that provider).

```csharp
builder.ConfigureMcp(
    options => options.Servers.Add(new McpServerDescriptor
    {
        Name = "MyTools",
        TransportKind = McpTransportKind.Stdio,
        Command = "dotnet",
        Arguments = { "dnx", "NuGet.Mcp.Server", "--version", "1.4.16", "--yes" },
        // Most real servers are configured through the environment, like the `env` block of a
        // Claude Desktop/Cursor server entry. Set InheritEnvironmentVariables = false to start
        // the server with an empty environment instead, so none of this process's own secrets
        // reach a third-party server binary.
        EnvironmentVariables = { ["NUGET_SOURCE"] = "https://api.nuget.org/v3/index.json" },
    }),
    configureProvider: options =>
    {
        // The providers this app wants to offer - a settings UI provider picker would read
        // options.Providers back out (see the sample's MainViewModel.AiProviders). Any string
        // works as Key/Provider, not just these four - see "Providers" below.
        options.Providers.Add(new AiProviderDescriptor { Key = "openai", Provider = "openai", Model = "gpt-5.2" });
        options.Providers.Add(new AiProviderDescriptor { Key = "gemini", Provider = "gemini", Model = "gemini-3.5-flash", Endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/" });
        options.Providers.Add(new AiProviderDescriptor { Key = "anthropic", Provider = "anthropic", Model = "claude-opus-4-8" });

        options.Provider = "gemini"; // active on startup
        options.Model = "gemini-3.5-flash";
        options.Endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/";
    });
```

```csharp
// Anywhere in the app, after the end user has entered their own key (e.g. from a settings screen):
await apiKeyProvider.SetApiKeyAsync("gemini", theUsersOwnApiKey);

// Switch to a different catalog entry (looks up Provider/Model/Endpoint from options.Providers):
aiChatClientFactory.SelectProvider("anthropic");

// Then just chat - every connected MCP server's tools are merged in automatically:
var response = await aiChatService.GetResponseAsync([new ChatMessage(ChatRole.User, "What's new in Newtonsoft.Json?")]);
Console.WriteLine(response.Text);
```

### Hosted servers that need a sign-in (OAuth)

The interesting MCP servers are hosted ones - your issue tracker, your notes, your repos - and
those want the end user to sign in rather than paste a token. `McpServerDescriptor.OAuth` turns on
the full MCP authorization flow, and because everything in it is discoverable, an empty options
object is genuinely all most servers need:

```csharp
await mcpServerRegistry.AddServerAsync(new McpServerDescriptor
{
    Name = "Linear",
    TransportKind = McpTransportKind.Http,
    Endpoint = "https://mcp.example.com/mcp",
    OAuth = new McpOAuthOptions(),   // discovered: auth server, scopes, client registration
});
```

That single call gets the server's `401` challenge, finds its authorization server, registers this
app as an OAuth client (no client ID to provision by hand), opens the **user's real browser** for
consent, catches the redirect on a loopback port, and exchanges the code - the OAuth 2.1 / RFC 8252
native-app flow, so your app never handles the user's password. The tokens are then persisted
DPAPI-encrypted, so the next app start reconnects without asking again.

Fill properties in only where a particular server forces you to - `ClientId` for one that has no
dynamic registration, `Scopes` for one that publishes none, `RedirectUri` for one that demands a
fixed registered URL. Two seams are swappable by registering your own before `ConfigureMcp()`:

| Type | Default | Replace it when |
| --- | --- | --- |
| `IMcpAuthorizationHandler` | `BrowserMcpAuthorizationHandler` - system browser + loopback listener | you want consent in an embedded WebView2 window instead of the user's browser |
| `IMcpTokenStore` | `SecureStorageMcpTokenStore` - DPAPI, current-user scope | tokens belong in Credential Manager or a key vault |

```csharp
// "Sign out of this server": forget the tokens so the next connect asks again. Independent of
// RemoveServerAsync, so a user can switch accounts without losing the server entry.
await mcpServerRegistry.SignOutAsync("Linear");

// Denied consent, a closed browser tab, or a timeout arrives as McpAuthorizationException -
// an ordinary outcome to report, not a crash. It also lands in McpServerStatus.LastError.
```

The sample's "AI Chat + MCP" tab has this wired to a text box and two buttons, so you can point it
at a real hosted server and watch the whole round trip.

| Type | Role |
| --- | --- |
| `IMcpServerRegistry` | Connects to MCP servers (stdio child process or HTTP), aggregates their tools. Runtime-mutable (`AddServerAsync`/`RemoveServerAsync`/`SignOutAsync`), not just a fixed config list - build your own "add an MCP server" settings UI on top if your app should let the end user do that themselves, the same way Claude Desktop/Cursor do. |
| `IAiApiKeyProvider` | Resolves/stores the end user's own API key, keyed per provider string (case-insensitively) so switching providers never loses a different provider's key. The default implementation is `SecureStorage`-backed; swap in Credential Manager/a key vault by registering your own before calling `ConfigureMcp`. |
| `IMcpAuthorizationHandler` | The interactive half of a server's OAuth flow - sends the user to consent and returns what the authorization server redirects back. Defaults to the system browser plus a loopback listener; see "Hosted servers that need a sign-in" above. |
| `IMcpTokenStore` | Persists each server's OAuth tokens (DPAPI-encrypted by default) so a consented server reconnects silently after a restart. `IMcpServerRegistry.SignOutAsync` is what clears them. |
| `IAiChatClientFactory` | The "which provider" seam (structurally the same role `IPushNotificationTransport` plays for push notifications) - builds/caches the `Microsoft.Extensions.AI.IChatClient` for whichever provider/model is currently selected. Inject this directly instead of `IAiChatService` if you want a raw chat client with no MCP tools merged in. |
| `IAiModelCatalog` | The "which model" counterpart - asks a provider which models it currently offers, using the end user's own stored key, so a settings UI can fill a picker instead of hardcoding a list that ages (see "Models" below). Optional: a model name typed by hand always works, since `Model` is a free string. |
| `IAiChatService` | The one-shot facade - merges every connected server's tools into `ChatOptions.Tools` automatically before delegating to `IAiChatClientFactory`, so callers never wire MCP tools in by hand. `GetResponseAsync`/`GetStreamingResponseAsync`. No memory between calls, and tools run without asking. |
| `IAiAgentFactory` | The agent layer (see below) - builds a Microsoft Agent Framework `AIAgent` over the same provider and the same MCP servers, adding conversation memory and end-user tool approval. |
| `IAiSessionStore` | Persists an agent's conversation across app restarts. File-backed by default; swap in your own the same way as `IAiApiKeyProvider`. |

### Agents (Microsoft Agent Framework)

`IAiChatService` is a single request/response call. That is fine for "summarize this", but a
conversational assistant needs two things it does not have: it forgets everything between calls,
and it runs whatever tool the model asks for the instant it is asked. `IAiAgentFactory` builds a
[Microsoft Agent Framework](https://github.com/microsoft/agent-framework) `AIAgent` over the *same*
BYOK provider and the *same* connected MCP servers, adding exactly those two things.

Both layers stay registered and neither replaces the other - keep using `IAiChatService` for
one-shot requests, and use an agent for the conversational surface.

```csharp
builder.ConfigureMcp(configureAgent: options =>
{
    options.Name = "Assistant";
    options.Instructions = "You help the user manage their project files. Be concise.";
});
```

```csharp
var agent = await agentFactory.GetAgentAsync();
var session = await agent.CreateSessionAsync();   // carries history across turns

var response = await agent.RunAsync("What's new in Newtonsoft.Json?", session);

// Under the default AiToolApprovalMode.Always, a run that wants a tool comes back paused
// instead of answering - nothing has run yet.
while (response.GetToolApprovalRequests() is { Count: > 0 } pending)
{
    // ToolCall is typed as the ToolCallContent base; the name lives on FunctionCallContent,
    // which is what every MCP tool call actually is.
    var name = (pending[0].ToolCall as FunctionCallContent)?.Name ?? pending[0].ToolCall.CallId;

    var allow = MessageBox.Show(
        $"Let the assistant run '{name}'?", "Tool approval", MessageBoxButton.YesNo) == MessageBoxResult.Yes;

    response = await agent.RunAsync(pending.CreateApprovalResponse(allow), session);
}

Console.WriteLine(response.Text);

// Same conversation next launch:
await sessionStore.SaveAsync("last-chat", agent, session);
var restored = await sessionStore.LoadAsync("last-chat", agent) ?? await agent.CreateSessionAsync();
```

The returned `AIAgent` is Microsoft's own type, not a Barbatos wrapper, so everything the Agent
Framework documents about running agents applies directly - `RunAsync`, `RunStreamingAsync`,
structured output via `RunAsync<T>`, and so on.

- **Tool approval defaults to on** (`AiToolApprovalMode.Always`). An MCP server is third-party
  code whose tools this library cannot inspect for side effects, and in an app that lets end users
  add their own servers it is not even code the publisher chose. Passing `alwaysApprove: true`
  to `CreateApprovalResponse` (a "Don't ask again" checkbox) records a standing rule for the rest
  of the session, so this costs one prompt per distinct tool rather than one per call. Set
  `options.ToolApproval = AiToolApprovalMode.Never` for the older auto-execute behavior.
- **The agent is rebuilt when its inputs change** - a different provider/model/API key, or an MCP
  server connecting or disconnecting. Ask `GetAgentAsync()` per conversation rather than holding
  one forever; an `AgentSession` survives those rebuilds. `AiAgentOptions` itself is not one of
  those inputs: it is read when the agent is built, so editing it at runtime only shows up at the
  next rebuild. For different instructions or tools at runtime, use `CreateAgentAsync` (below) or
  per-run instructions in `ChatClientAgentRunOptions`.
- **`IAiAgentFactory.ConfigurationChanged` can arrive on a thread-pool thread.** It forwards
  `IMcpServerRegistry.Changed`, which the registry raises after awaiting the connection - so a
  seed server finishing its connect at startup reaches your handler off the UI thread. The MCP
  layer is UI-agnostic and does not marshal; dispatch before touching bindings or a bound
  collection (the sample's `MainViewModel` does).
- **`CreateAgentAsync(descriptor)`** builds a second, differently-scoped agent (a summarizer, a
  code reviewer) with its own instructions, its own tools, and its own approval policy, without
  disturbing the default one.
- **Saved sessions contain the conversation itself**, which for a desktop assistant can easily
  include personal data. The default store writes plain JSON under
  `IFileSystem.AppDataDirectory` - per-user, but not encrypted. Back `IAiSessionStore` with
  `ISecureStorage` or your own encrypted store if that is not enough for your app.

### Providers

`AiProviderOptions.Provider` is a plain string, not a fixed enum - baking every provider name an
app might ever want into a shared library enum would need a new release for each one. This
library only ever draws one real technical distinction: (case-insensitively) `"anthropic"` uses
the official Anthropic .NET SDK (that SDK's own documentation notes it is still versioned 10+ but
currently in beta, with possible breaking changes in minor/patch releases - this repository pins
an exact version in `Directory.Packages.props`, no floating ranges, so a breaking change is only
ever absorbed when this repository deliberately bumps that pin); every other string is assumed
OpenAI-wire-compatible, covering:

- **`"openai"`** (or any other spelling you like) - ChatGPT, via the official OpenAI API. Leave
  `Endpoint` unset.
- **Google Gemini** - via [Gemini's own OpenAI-compatible endpoint](https://ai.google.dev/gemini-api/docs/openai) -
  set `Endpoint` to `"https://generativelanguage.googleapis.com/v1beta/openai/"`. No separate
  Gemini SDK dependency.
- **Any other OpenAI-wire-compatible endpoint** - a self-hosted model via Ollama/LM Studio/vLLM,
  OpenRouter, a proxy, or a future provider this library has never heard of - set `Endpoint`
  yourself. Does not cover Azure OpenAI, which needs its own client for correct auth/routing.

**`AiProviderOptions.Providers`** is an optional catalog of provider choices your app wants to
offer (e.g. in a settings UI's provider picker) - seeded the same way `McpOptions.Servers` seeds
MCP servers. It is purely a catalog: `Barbatos.Wpf.Mcp` itself never reads it automatically -
`IAiChatClientFactory.SelectProvider(key)` is what actually looks an entry up and switches to it,
calling `UpdateProvider` with that entry's own `Provider`/`Model`/`Endpoint`. Leave it empty if
your app hardcodes its own list instead, or only ever supports one provider - `Provider`/`Model`/
`Endpoint` directly on `AiProviderOptions` work with or without a `Providers` catalog behind them.

### Models (`IAiModelCatalog`)

Models are released far more often than most desktop apps ship, which is why
`AiProviderOptions.Model` is a free string: a model published this morning is reachable by typing
its name, with no new release of your app. `IAiModelCatalog` is the optional upgrade on top of
that - it asks the provider which models it currently offers, using the end user's own stored key,
so a settings screen can populate a picker instead of hardcoding a list that ages:

```csharp
// "Refresh models" in a settings screen. The endpoint is whatever this provider chats through -
// listing has to be pointed at the same place, or it describes a different service.
var models = await modelCatalog.GetModelsAsync("openai");
var geminiModels = await modelCatalog.GetModelsAsync(
    "gemini", "https://generativelanguage.googleapis.com/v1beta/openai/");

foreach (var model in models)
    Console.WriteLine(model.DisplayName ?? model.Id);   // model.Id is what UpdateProvider wants
```

Behind the string it splits exactly like the chat path: `"anthropic"` goes through Anthropic's own
SDK (paginated, and rich enough to fill in `DisplayName`/`MaxInputTokens`/`MaxOutputTokens`),
everything else through `GET {endpoint}/models`, a route OpenRouter, Ollama, LM Studio, vLLM and
Gemini's OpenAI-compatible endpoint all answer.

Worth knowing before wiring it to a picker:

- **An API key must already be stored** for the provider being asked - this is a real authenticated
  call, so the list can only be fetched *after* the end user saves their key, not while the fields
  are blank. Without one it throws `InvalidOperationException` naming the provider, rather than
  surfacing a raw 401.
- **Nothing is cached** - every call is a network round trip. Unlike a chat client, a model list has
  no event that could invalidate it (a provider can publish a model at any moment), so caching here
  would mostly serve stale answers to the one call a user makes explicitly to get fresh ones. Drive
  it from a button and keep the result in your view model.
- **The list is raw** - unfiltered and unsorted, the same way `IAiChatService` passes message content
  through untransformed. An OpenAI-compatible endpoint lists embedding, audio and image models next
  to chat ones with no reliable flag telling them apart, and providers return their own order.
- **Not every endpoint implements it** - a narrow proxy that only serves chat completions will throw
  here. Keep a free-text model entry as the fallback (the sample's model box is an *editable*
  ComboBox for exactly this reason): the typed name is what always works.

### Attachments (images, documents)

`GetResponseAsync`/`GetStreamingResponseAsync` take `Microsoft.Extensions.AI.ChatMessage` as-is,
so sending an image or a document alongside the text is just building a `ChatMessage` with more
than one `AIContent` in it - `Barbatos.Wpf.Mcp` has no separate "attachment" API to learn:

```csharp
using Microsoft.Extensions.AI;

// A local file, embedded inline as base64 - works for any provider, no hosting/upload step.
// MediaType is a plain MIME type; it's sent through as-is, not inspected or validated.
var imageBytes = await File.ReadAllBytesAsync(screenshotPath);
var response = await aiChatService.GetResponseAsync(
[
    new ChatMessage(ChatRole.User,
    [
        new TextContent("What's in this screenshot?"),
        new DataContent(imageBytes, "image/png"),
    ]),
]);

// A document works the same way - just a different media type:
var pdfBytes = await File.ReadAllBytesAsync(reportPath);
var docResponse = await aiChatService.GetResponseAsync(
[
    new ChatMessage(ChatRole.User,
    [
        new TextContent("Summarize this document."),
        new DataContent(pdfBytes, "application/pdf"),
    ]),
]);

// Already-hosted content can be referenced by URL instead of embedding bytes - prefer
// DataContent above for local files picked from the end user's machine.
var byUrl = new ChatMessage(ChatRole.User,
[
    new TextContent("What's in this image?"),
    new UriContent("https://example.com/photo.jpg", "image/jpeg"),
]);
```

A couple of things worth knowing before relying on this:

- **Which media types a given provider/model actually accepts varies** (e.g. PDF support isn't
  universal) - check that provider's own docs; `Barbatos.Wpf.Mcp` doesn't transform, downscale, or
  reject anything, it passes `ChatMessage.Contents` straight to the underlying SDK.
- **`ChatMessage.Text` only ever reflects `TextContent`** - if you log or display `message.Text`
  (e.g. building a transcript UI), an attached `DataContent`/`UriContent` won't show up in it; walk
  `message.Contents` yourself if the UI needs to indicate "this message had an attachment."

### Configuration

```json
{
  "Barbatos": {
    "Mcp": {
      "Enabled": true,
      "Provider": { "Provider": "gemini", "Model": "gemini-3.5-flash" },
      "Agent": { "Instructions": "Be concise.", "ToolApproval": "Always" }
    }
  }
}
```

`Barbatos:Mcp:Servers` and `Barbatos:Mcp:Provider:Providers` aren't practical to express as flat
config keys (each server/provider entry has several fields, and array-index config keys like
`Providers:0:Key` are unwieldy to hand-author), so both are more commonly seeded from code via
`options.Servers.Add(...)`/`options.Providers.Add(...)`, as shown above - the configuration
section still exists for the parts that are practical to override from a file (`Enabled`,
`Provider`, `Model`, `Endpoint`).

A few behaviors worth knowing before relying on this feature:

- **`IAiChatService` executes tool calls automatically, with no confirmation step** - it enables
  `Microsoft.Extensions.AI`'s function-invocation middleware by default, the same way Claude
  Desktop/Cursor auto-execute MCP tool calls. If your app needs a confirmation step before a tool
  actually runs (for example because end users can add their own untrusted MCP servers), use
  `IAiAgentFactory` instead - its default `AiToolApprovalMode.Always` pauses the run and hands the
  pending call back to you, which is what the "Agents" section above is about.
- **Tool name collisions**: if two connected servers expose a same-named tool, the one from the
  server *added* later wins - the one later in `IMcpServerRegistry.Servers` - and a warning naming
  both servers is logged. A server takes its place in that list when `AddServerAsync` starts,
  before it connects, so precedence follows the order servers were added (the seed list in
  order, then runtime additions), not the order they happened to finish connecting.
- **A failed server connection is still tracked** - `IMcpServerRegistry.Servers` includes it with
  `IsConnected = false` and `LastError` set, rather than disappearing; `AddServerAsync` also
  rethrows the exception, so a caller doesn't have to poll `Servers` just to notice a failure.
  An entry rejected *before* connecting is different: a descriptor failing `Validate()`, or one
  reusing a tracked `Name`, throws before any status exists, so a seed entry like that is missing
  from `Servers` entirely and only the startup error log mentions it.
- **Runtime-mutable state does not persist itself** - like every other runtime-mutable feature in
  this library (`ITrayIconService`, `IPeriodicServiceScheduler`), an `IMcpServerRegistry.AddServerAsync`/
  `IAiChatClientFactory.UpdateProvider` call made from a settings UI is not written back to a
  config file automatically; persist it yourself if it should survive a restart. The sample
  deliberately doesn't: its `SettingsStore` shows the pattern (a JSON file the host loads back into
  configuration on the next start) for the tray, keep-awake, periodic-service and notification
  settings only, so its AI provider/model and any server added at runtime start from the seeded
  defaults on every launch. What does survive is held by the secret stores: the API key
  (`IAiApiKeyProvider`) and a signed-in server's OAuth tokens (`IMcpTokenStore`).

---

## Ecosystem

Unlike modular libraries split across many NuGet packages, Barbatos.Wpf.Core ships as a
**single package** — Hosting, all Essentials modules, and every optional desktop feature are
included; there is nothing else to install.

### Repository layout

- `src/Barbatos.Wpf.Core` — the library.
- `samples/Barbatos.Wpf.Core.Sample` — a complete sample application showing DI,
  configuration, host environment, Essentials usage, and the live lifecycle event log.
- `tests/Barbatos.Wpf.Core.UnitTests` — the unit test suite covering hosting and Essentials.

---

## API Reference

The library contains a rich set of primitives spanning Hosting (`Barbatos.Wpf.Hosting`,
`Barbatos.Wpf.LifecycleEvents`, `Barbatos.Wpf.Dispatching`) and Essentials
(`Barbatos.Wpf.ApplicationModel`, `Barbatos.Wpf.Devices`, `Barbatos.Wpf.Storage`,
`Barbatos.Wpf.Networking`, ...).

Due to the extensive nature of the library's interfaces, classes, and properties, the full
API Reference has been moved to a dedicated document modeled after Microsoft's official
.NET documentation format.

👉 **[Read the Full API Reference](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Core/API-REFERENCE.md)** 👈

In the full reference, you will find comprehensive documentation for every namespace,
interface, static facade, and options class described above.

---

## Community

### Maintainers

- Pham The Hung ([@StHung](https://github.com/StHung))

### Support

For support, please open a [GitHub issue](https://github.com/Barbatos-Labs/Barbatos.Wpf/issues/new). We welcome bug reports, feature requests, and questions.

### License

This project is licensed under the terms of the **MIT** open source license. Please refer to the [LICENSE](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/LICENSE.md) file for the full terms.

You can use it in private and commercial projects. Keep in mind that you must include a copy of the license in your project.
