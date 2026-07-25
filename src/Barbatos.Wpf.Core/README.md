# Barbatos.Wpf.Core

![Barbatos.Wpf.Core logo](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/build/core-logo.svg?raw=true)

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
  * [Full customization](#full-customization)
  * [Showing it conditionally](#showing-it-conditionally)
  * [Avoiding flicker: minimum display duration](#avoiding-flicker-minimum-display-duration)
* **[Periodic services](#periodic-services)**
* **[State management (Barbatos.Wpf.Apsu)](#state-management-barbatoswpfapsu)**
  * [Stores](#stores)
  * [Getters](#getters)
  * [Actions and action interceptors](#actions-and-action-interceptors)
    * [Removing an interceptor at runtime](#removing-an-interceptor-at-runtime)
  * [Plugins](#plugins)
  * [Registry](#registry)
* **[AI chat + MCP (Barbatos.Wpf.Mcp)](#ai-chat--mcp-barbatoswpfmcp)**
  * [Providers](#providers)
  * [Configuration](#configuration)
* **[Ecosystem](#ecosystem)**
  * [Repository layout](#repository-layout)
* **[API Reference](#api-reference)**
* **[Community](#community)**

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

### Lifecycle events

Application events (`OnStartup`, `OnActivated`, `OnDeactivated`, `OnSessionEnding`,
`OnDispatcherUnhandledException`, `OnExit`) and window events (`OnWindowCreated`,
`OnWindowLoaded`, `OnWindowActivated`, `OnWindowDeactivated`, `OnWindowStateChanged`,
`OnWindowClosing`, `OnWindowClosed`) are surfaced through `ILifecycleEventService`, and every
window gets its own service scope (with `IWpfInitializeScopedService` support).

```csharp
builder.ConfigureLifecycleEvents(events => events.AddWpf(wpf => wpf
    .OnStartup((app, args) => logger.LogInformation("Started with args: {Args}", args.Args))
    .OnWindowClosing((window, args) => { /* prompt to save, or cancel via args.Cancel */ })));
```

`IWpfInitializeService` runs once during `Build()`; `IWpfInitializeScopedService` runs once
per window scope — both are the WPF counterparts of `IMauiInitializeService`/
`IMauiInitializeScopedService`.

### Dispatching

`IDispatcher`, `IDispatcherTimer`, and `IDispatcherProvider`, backed by the WPF `Dispatcher`,
registered app-wide (singleton) and per-window (scoped), plus the `DispatchAsync` /
`DispatchIfRequiredAsync` extension helpers.

### Configuring the hosting environment

`WpfHostEnvironment` (the WPF counterpart of `MauiHostEnvironment`/ASP.NET Core's
`HostingEnvironment`) resolves `EnvironmentName` the same way
`Microsoft.Extensions.Hosting.HostBuilder` does: `WpfAppBuilder` adds an environment
variables configuration source with the `DOTNET_` prefix as the very first (lowest-priority)
entry in `builder.Configuration`, then reads `HostDefaults.EnvironmentKey` from it once,
before any of your own configuration sources run. That means:

* Setting the `DOTNET_ENVIRONMENT` environment variable (e.g. `DOTNET_ENVIRONMENT=Staging`)
  before launching the app sets `builder.Environment.EnvironmentName`, with no code required.
* Any configuration source you add that also sets the `environment` key overrides it —
  including sources added *before* you first read `builder.Environment`:

  ```csharp
  var builder = WpfApp.CreateBuilder();
  builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
  {
      [HostDefaults.EnvironmentKey] = "Staging",
  });

  // builder.Environment.EnvironmentName == "Staging" from here on.
  ```
* Unlike the WPF counterpart's previous behavior, `EnvironmentName` is a normal settable
  property — `builder.Environment.EnvironmentName = "Staging";` works directly, and the value
  is fixed once `Build()` has read it (it does not keep re-reading the environment variable).
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
| File system | `FileSystem` | `IFileSystem` | `%LocalAppData%\{Publisher}\{AppGuid}\{Data,Cache}`, created on first access |
| Preferences | `Preferences` | `IPreferences` | A local JSON file, guarded by a lock around file I/O |
| Secure storage | `SecureStorage` | `ISecureStorage` | DPAPI (`ProtectedData`, current-user scope) instead of the WinRT `DataProtectionProvider` |
| Version tracking | `VersionTracking` | `IVersionTracking` | Pure C#, no platform-specific code (built on `Preferences` + `AppInfo`) |
| Connectivity | `Connectivity` | `IConnectivity` | `System.Net.NetworkInformation` instead of the WinRT `NetworkInformation` API |
| Device display | `DeviceDisplay` | `IDeviceDisplay` | A Win32 monitor-info P/Invoke block against the active WPF window; `KeepScreenOn` via `SetThreadExecutionState`; change notifications via `SystemEvents.DisplaySettingsChanged` |
| Email | `Email` | `IEmail` | Simple MAPI (`MAPISendMail`) |
| Launcher | `Launcher` | `ILauncher` | `Process.Start(UseShellExecute: true)` instead of the WinRT `Windows.System.Launcher`; `CanOpenAsync` checks `HKEY_CLASSES_ROOT` for a registered scheme handler |
| Contacts | `Contacts` | `IContacts` | Throws `FeatureNotSupportedException` — see note below |
| Geolocation | `Geolocation` | `IGeolocation` | Throws `FeatureNotSupportedException` — see note below; `Location`'s distance math is fully functional |
| App actions | `AppActions` | `IAppActions` | The taskbar Jump List (`System.Windows.Shell.JumpList`) instead of the WinRT `Windows.UI.StartScreen.JumpList` |
| Permissions | `Permissions.CheckStatusAsync<T>()` / `RequestAsync<T>()` | *(none — generic static API, no DI)* | Most permissions report `Granted` (no manifest/capability concept for an unpackaged app); `ContactsRead`/`ContactsWrite`/`LocationWhenInUse`/`LocationAlways`/`Microphone`/`Sensors` throw `FeatureNotSupportedException` — see [Permissions](#permissions) below |

```csharp
var name = AppInfo.Name;
var publisher = PublisherInfo.Name;
var isOnline = Connectivity.NetworkAccess == NetworkAccess.Internet;

Preferences.Set("launch_count", Preferences.Get("launch_count", 0) + 1);
await SecureStorage.SetAsync("api_token", token);

await Email.ComposeAsync("Subject", "Body", "someone@example.com");
await Launcher.OpenAsync("https://example.com");
```

### Configuring AppInfo and PublisherInfo

`AppInfo` and `PublisherInfo` never require any setup — their fallback chains always resolve
to *something* — but you should configure them explicitly for any app you plan to ship,
because `FileSystem.AppDataDirectory` and `FileSystem.CacheDirectory` are derived from
`PublisherInfo.Name` and `AppInfo.AppGuid` (`%LocalAppData%\{PublisherInfo.Name}\{AppInfo.AppGuid}\...`).

This library targets .NET 8+ SDK-style projects only, so configuration is a plain
`<PropertyGroup>` in the csproj — the same well-known properties Visual Studio's *Project ▸
Properties ▸ Package* page already exposes, with no `AssemblyInfo.cs` file or ClickOnce
settings involved:

```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <TargetFramework>net8.0-windows</TargetFramework>
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
| `AppInfo.Version`/`VersionString` | The assembly's own `Version` | `<Version>` (or `<AssemblyVersion>` for finer control) |
| `PublisherInfo.Name` | `AssemblyCompanyAttribute` | `<Company>` |
| `PublisherInfo.Copyright` | `AssemblyCopyrightAttribute` | `<Copyright>` |

`AppInfo.AppGuid` and `PublisherInfo.Website`/`SupportUrl`/`SupportEmail` have no standard
MSBuild-property equivalent — there is no built-in "app id"/"website"/"support URL"/"support
email" assembly attribute at all. For
those (or to override any of the properties above with a value that differs from
`<Product>`/`<Company>`/`<Copyright>`), set the `Barbatos.Wpf.ApplicationModel.*` assembly
metadata directly — it is always checked *before* the standard fallback:

```xml
<ItemGroup>
  <AssemblyMetadata Include="Barbatos.Wpf.ApplicationModel.AppInfo.AppGuid" Value="{C6BB69DE-7E6B-43E3-83AD-AC46E1B0570D}" />
  <AssemblyMetadata Include="Barbatos.Wpf.ApplicationModel.PublisherInfo.Website" Value="https://contoso.example" />
  <AssemblyMetadata Include="Barbatos.Wpf.ApplicationModel.PublisherInfo.SupportUrl" Value="https://contoso.example/support" />
  <AssemblyMetadata Include="Barbatos.Wpf.ApplicationModel.PublisherInfo.SupportEmail" Value="support@contoso.example" />
</ItemGroup>
```

| Property | Metadata key (explicit override) |
| --- | --- |
| `AppInfo.AppGuid` | `Barbatos.Wpf.ApplicationModel.AppInfo.AppGuid` |
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

> **Important:** `AppInfo.AppGuid` (not a store package identifier on this platform — it is
> simply the stable identifier used to derive the app's storage folder) should stay constant
> across releases. Changing it after
> shipping moves `Preferences`, `SecureStorage`, and `FileSystem.AppDataDirectory` to a new
> folder, effectively discarding every existing user's stored data. If you ship an installer,
> set it to a real GUID matching the installer's own application identifier — see
> [Publishing with an installer](#publishing-with-an-installer) below.
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

| `[Setup]` directive | Registry value under `...\Uninstall\{AppId}_is1` | Barbatos.Wpf.Core source |
| --- | --- | --- |
| `AppId` | *(the key name itself)* | `AppInfo.AppGuid` — **use the same GUID for both**, see the note above |
| `AppName` | `DisplayName` | `AppInfo.Name` (`<Product>`) |
| `AppVersion` | `DisplayVersion` | `AppInfo.VersionString` (`<Version>`) |
| `AppPublisher` | `Publisher` | `PublisherInfo.Name` (`<Company>`) |
| `AppPublisherURL` | `URLInfoAbout` | `PublisherInfo.Website` |
| `AppSupportURL` | `HelpLink` | `PublisherInfo.SupportUrl` |
| `AppUpdatesURL` | `URLUpdateInfo` | *(no dedicated property — reuse `PublisherInfo.Website`, or a custom metadata key of your own)* |

```pascal
; setup.iss
#define AppGUID "C6BB69DE-7E6B-43E3-83AD-AC46E1B0570D"   ; matches AppInfo.AppGuid, without braces
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
> defaults `<FileVersion>` from `<Version>`. If you ever set `<AssemblyVersion>` to something
> fixed (a common practice to avoid binding-redirect churn) while letting `<Version>`/
> `<FileVersion>` increment per build, `AppInfo.VersionString` (which reads `AssemblyVersion`)
> and what your installer reports from the `.exe` file will diverge — set `Barbatos.Wpf.ApplicationModel.AppInfo.Version` explicitly if you need them to
> stay in lockstep.

### Reading back InstallDate and InstallLocation

`AppInfo.InstallDate` and `AppInfo.InstallLocation` read the *installed copy's* own uninstall
registry entry (the one from the table above) back — the same entry `AppInfo.AppGuid` is
recommended to match:

```csharp
var when = AppInfo.InstallDate;         // DateTime? — null if not found
var where = AppInfo.InstallLocation;    // string? — null if not found
```

Both try every plausible location an installer might have used: `HKEY_LOCAL_MACHINE` (a
machine-wide install) and `HKEY_CURRENT_USER` (a per-user install, e.g. Inno Setup's
`PrivilegesRequired=lowest`), each in both the 64-bit and 32-bit registry view (the 32-bit
view transparently redirects to `WOW6432Node`), trying both the bare-GUID subkey name (the
MSI/WiX convention) and the `_is1`-suffixed one (the Inno Setup convention). They resolve to
`null` — never throw — whenever:

- `AppInfo.AppGuid` isn't a valid GUID (the common case for an app that never overrode it —
  see the note above), or
- the app hasn't actually been installed through a matching installer yet (running from the
  IDE/`dotnet run`, or a different `AppId` than what `AppInfo.AppGuid` reports).

Other fields the installer's uninstall entry carries — `EstimatedSize`, `DisplayIcon`,
`NoModify`/`NoRepair`, the `Inno Setup: *` bookkeeping keys — describe the *installed copy*
rather than something an app typically needs to read back about itself, so they stay outside
`AppInfo`'s scope.

### App actions and version tracking

`AppActions` (taskbar Jump List shortcuts) and `VersionTracking.Track()` are wired up
through `ConfigureEssentials`:

```csharp
builder.ConfigureEssentials(essentials => essentials
    .AddAppAction("open", "Open", "Show the main window")
    .OnAppAction(action =>
    {
        if (action.Id == "open")
            App.ShowMainWindow();
    })
    .UseVersionTracking());
```

Clicking a Jump List entry launches a new process with an encoded command-line argument;
`UseEssentials()` checks for it on the `WpfLifecycle.OnStartup` event and raises
`AppActions.OnAppAction` — a relaunch-argument activation flow adapted to WPF's native Jump
List API instead of `Windows.UI.StartScreen.JumpList`. Because
`System.Windows.Shell.JumpList` has no way to read back what the OS is currently showing
(unlike WinRT's `JumpList.LoadCurrentAsync()`), `AppActions.GetAsync()` returns the actions
last set by `SetAsync()` in this process rather than querying the OS.

### Contacts and Geolocation

A real Windows implementation of `Contacts`/`Geolocation` needs WinRT contracts
(`Windows.ApplicationModel.Contacts.ContactPicker`,
`Windows.Devices.Geolocation.Geolocator`) that require a TargetFramework with WinRT
projections (`net8.0-windows10.0.19041.0`-style) and, for some APIs, MSIX packaging —
neither of which this plain `net8.0-windows`/`net9.0-windows`/`net10.0-windows` WPF library
uses. Rather than pull in that machinery, both modules still ship their full type surface
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

- **Most permissions report `PermissionStatus.Granted`.** An unpackaged desktop WPF app has
  no `AppxManifest.xml` capability declarations to check, and Windows itself doesn't gate
  `Camera`, `CalendarRead`, `Phone`, `StorageRead`, etc. behind a runtime consent prompt
  outside a packaged/store app — `EnsureDeclared()` is a no-op for the same reason.
- **`ContactsRead`, `ContactsWrite`, `LocationWhenInUse`, `LocationAlways`, `Microphone`, and
  `Sensors` throw `FeatureNotSupportedException`.** A real check for these six needs WinRT
  device-access contracts (`ContactManager`, `Geolocator`, `DeviceAccessInformation`,
  `MediaCapture`) that need WinRT projections and, for some APIs, MSIX packaging — the same
  machinery this library already opts out of for [`Contacts` and `Geolocation`](#contacts-and-geolocation).
  Rather than silently report `Granted` for something it can't actually verify, these six are
  honest about it instead.

If you need a real permission check (for example reading the Windows privacy consent
registry, or a custom capability broker), subclass `Permissions.BasePlatformPermission` and
pass your subclass as `TPermission`.

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

| Member | What it is | Survives reinstall? |
| --- | --- | --- |
| `GetInstanceIdAsync()` | A random GUID generated on first use, persisted via `SecureStorage`. Identifies "this install", not the physical machine. | No |
| `GetHardwareFingerprintAsync()` | A SHA-256 hash of a few motherboard/BIOS/CPU identifiers (read via WMI), salted with `AppInfo.AppGuid`. | Yes |

Two design choices keep this narrower than the raw hardware-ID approach a lot of licensing
code reaches for by default:

- **The hardware serials themselves are never stored or transmitted** — only a one-way
  SHA-256 hash of them. Your license server can tell "this is the same machine as last time"
  without ever holding a reversible hardware identifier.
- **The hash is salted with `AppInfo.AppGuid`**, so it's scoped to *this app* — the same
  machine produces a different fingerprint for a different app, the same way Apple's
  `IdentifierForVendor` is scoped to a developer rather than being a single ID usable to
  correlate a device across unrelated apps.

Use `GetInstanceIdAsync()` alone if you don't need to survive a reinstall (the least invasive
option). Use `GetHardwareFingerprintAsync()` (or both, keyed together) if a user should not be
able to reset a per-machine activation count by simply reinstalling — this is the same
technique commercial license managers (FlexNet, Reprise, ...) use.

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
| Run on startup (registry `Run` key) | `ConfigureRunOnStartup()` | `Barbatos:RunOnStartup` | `IRunOnStartupService` | No |
| System tray icon (`Shell_NotifyIcon`) | `ConfigureTrayIcon(...)` | `Barbatos:TrayIcon` | `ITrayIconService` | No |
| Keep computer awake (`SetThreadExecutionState`) | `ConfigureKeepAwake()` | `Barbatos:KeepAwake` | `IKeepAwakeService` | No |
| Push notifications (adaptive toast, images/buttons/navigation) | `ConfigureNotifications()` | `Barbatos:Notifications` | `INotificationService` | Yes |
| Periodic background services | `ConfigurePeriodicServices<T>()` | `Barbatos:PeriodicServices` | `IPeriodicServiceScheduler` | Yes |
| Realtime push notification listener (SignalR) | `ConfigurePushNotifications()` | `Barbatos:PushNotifications` | `IPushNotificationService` | Yes |

"Enabled by default" means what happens the moment you call the `Configure...` method with no
further setup — every feature is still opt-in at the *builder* level (call the method, or the
service isn't registered at all).

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
```

And from a configuration file:

```json
{
  "Barbatos": {
    "SingleInstance": { "Enabled": true, "ActivateMainWindow": true },
    "RunOnStartup": { "Enabled": true },
    "TrayIcon": { "Enabled": true, "ToolTip": "My app" },
    "KeepAwake": { "Enabled": true, "KeepDisplayOn": false },
    "Notifications": { "Enabled": true }
  }
}
```

Notes:

- **Single instance** is keyed by `AppInfo.AppGuid` (see
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
  hidden so a hidden window doesn't look identical to a closed app.
- `RunOnStartup` state is persisted by the OS registry; the other toggles can be
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
    your app knows its own screens/navigation.
  - The default transport's own `HandshakeMethodName`/`NotificationMethodName` (defaulting to
    `"RegisterDevice"`/`"ReceiveNotification"`) are configurable via `ConfigurePushNotifications`'s
    `configureSignalR` parameter (or the `Barbatos:PushNotifications:SignalR` config section) to
    match whatever hub method names your specific server actually uses.

> **Note:** an earlier version of this library had a `ConfigureGlobalHotkeys` feature
> (system-wide keyboard shortcuts via `RegisterHotKey`, for a "Quick Entry" hotkey). It has
> been removed in favor of `IAppActions` (taskbar Jump List shortcuts — see
> [Essentials](#essentials) above) instead, which better matches what "app actions" means on
> Windows. A global keyboard hotkey is a materially different capability; if you need one,
> `RegisterHotKey`/`UnregisterHotKey` P/Invoke is straightforward to reintroduce as your own
> `IWpfInitializeService`.

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
(if your "slow work" is synchronous, an `IWpfInitializeService` - covered by the splash the same
way, since it also runs before `CreateWpfApp()` - is the better fit; see "Hosting" above).

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
resolved by the container the same way any other constructor dependency is. Register its local
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
        Console.WriteLine($"[{context.Store.GetType().Name}] {context.ActionName} starting");
        var result = await next(); // call first - see the warning below
        Console.WriteLine($"[{context.Store.GetType().Name}] {context.ActionName} done");
        return result;
    }
}
```

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

> [!IMPORTANT]
> **Call `next` as your first meaningful step if you're going to call it at all - never `await`
> anything genuinely asynchronous before calling it.** Castle DynamicProxy's underlying
> `Proceed()` (what `next` eventually reaches) is not safe to call from a continuation that
> already crossed a real `await` suspension - doing so hangs the action forever instead of
> throwing (confirmed by isolating and killing the stuck process during this feature's own
> testing). Awaiting real asynchronous work *after* `next()` returns (post-processing, e.g.
> flushing a log) is fully supported - only work ordered *before* it is unsafe. A synchronous
> action's interceptor chain still runs through this same `async` shape, but the proxy blocks on
> it (`GetAwaiter().GetResult()`) since there's no `Task` to hand back to a synchronous caller -
> safe as long as every interceptor in that chain only does synchronous work, but risks the
> classic WPF sync-over-async deadlock if one doesn't. Pair genuinely asynchronous interceptor
> logic with a `Task`-returning action to avoid this entirely.

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
            Report(context.Store.GetType().Name, context.ActionName, ex); // same sink OnDispatcherUnhandledException uses
            throw;
        }
    }
}
```

A store using interceptors must be **non-`sealed`** with **exactly one public constructor**
(interception works by generating a runtime subclass via Castle DynamicProxy that overrides every
`[Action]`-marked method) - `AddStore<TStore>()` throws a descriptive `InvalidOperationException`
at first resolution otherwise, rather than failing silently.

#### Removing an interceptor at runtime

Register with the `out InterceptorHandle` overload instead of the plain one to be able to
remove an interceptor later without rebuilding the host - `axios`'s
[`interceptors.request.eject(id)`](https://axios.rest/pages/advanced/interceptors.html):

```csharp
builder.Services.AddStoreInterceptor(new LoggingInterceptor(), out var loggingHandle);
// Local interceptors have the same overload: AddStoreInterceptor<TStore>(interceptor, out handle)

// Later, e.g. from a settings toggle:
loggingHandle.Eject();
```

Ejecting doesn't remove the interceptor from the chain structurally - its slot just becomes a
transparent pass-through straight to the next interceptor (or the action itself) from that call
onward, as if it had never been registered. A call already in flight when `Eject()` runs keeps
using the interceptor; every call that starts afterward skips it. There's no "re-attach" -
construct and register a new interceptor if you need the behavior back. Most interceptors don't
need this - it's opt-in per interceptor, not automatic.

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

### Registry

`IStoreRegistry` (resolved automatically - there's normally no need to depend on it directly)
exposes every store actually resolved so far, for cross-cutting introspection (e.g. a debug
panel listing live stores). It exists purely for introspection, not caching - the DI container
itself is already what caches and singleton-izes each store.

See the `Barbatos.Wpf.Apsu` namespace in the [API Reference](#api-reference) for the full member
list. Two worked examples ship with this repository:

- **`samples/Barbatos.Wpf.Core.Sample`** - `CounterStore` (state, a getter, two actions) plus
  `SampleActionLoggingInterceptor`, a single global interceptor also called directly from the
  `OnDispatcherUnhandledException` lifecycle hook, showing the two error paths share one policy.
- **`samples/Barbatos.Wpf.Aquarius.Sample`** (`ApsuInteropDemoView`/`ApsuInteropDemoViewModel`) -
  a richer scenario against a real API (jsonplaceholder.typicode.com): `UserStore`/`OrderStore`
  watching each other's state and calling across stores, a getter combining both, and a full
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

| Type | Role |
| --- | --- |
| `IMcpServerRegistry` | Connects to MCP servers (stdio child process or HTTP), aggregates their tools. Runtime-mutable (`AddServerAsync`/`RemoveServerAsync`), not just a fixed config list - build your own "add an MCP server" settings UI on top if your app should let the end user do that themselves, the same way Claude Desktop/Cursor do. |
| `IAiApiKeyProvider` | Resolves/stores the end user's own API key, keyed per provider string (case-insensitively) so switching providers never loses a different provider's key. The default implementation is `SecureStorage`-backed; swap in Credential Manager/a key vault by registering your own before calling `ConfigureMcp`. |
| `IAiChatClientFactory` | The "which provider" seam (structurally the same role `IPushNotificationTransport` plays for push notifications) - builds/caches the `Microsoft.Extensions.AI.IChatClient` for whichever provider/model is currently selected. Inject this directly instead of `IAiChatService` if you want a raw chat client with no MCP tools merged in. |
| `IAiChatService` | The facade most app code actually calls - merges every connected server's tools into `ChatOptions.Tools` automatically before delegating to `IAiChatClientFactory`, so callers never wire MCP tools in by hand. `GetResponseAsync`/`GetStreamingResponseAsync`. |

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
      "Provider": { "Provider": "gemini", "Model": "gemini-3.5-flash" }
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

- **Tool calls execute automatically, with no confirmation step** - `IAiChatService` enables
  `Microsoft.Extensions.AI`'s function-invocation middleware by default, the same way Claude
  Desktop/Cursor auto-execute MCP tool calls. If your app needs a confirmation step before a tool
  actually runs (for example because end users can add their own untrusted MCP servers), bypass
  `IAiChatService` and build your own `ChatOptions` from `IMcpServerRegistry.Tools`/
  `IAiChatClientFactory` directly instead.
- **Tool name collisions**: if two connected servers expose a same-named tool, the one from
  whichever server connected most recently wins - a warning is logged when this happens.
- **A failed server connection is still tracked** - `IMcpServerRegistry.Servers` includes it with
  `IsConnected = false` and `LastError` set, rather than disappearing; `AddServerAsync` also
  rethrows the exception, so a caller doesn't have to poll `Servers` just to notice a failure.
- **Runtime-mutable state does not persist itself** - like every other runtime-mutable feature in
  this library (`ITrayIconService`, `IPeriodicServiceScheduler`), an `IMcpServerRegistry.AddServerAsync`/
  `IAiChatClientFactory.UpdateProvider` call made from a settings UI is not written back to a
  config file automatically; persist it yourself (see `SettingsStore` in the sample) if it should
  survive a restart.

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
