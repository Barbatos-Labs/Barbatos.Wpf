# Barbatos.Wpf.Core

![Barbatos.Wpf.Core logo](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/build/nuget.png?raw=true)

### *A MAUI-style application host and Essentials for WPF*

**Bring .NET MAUI's `MauiApp`/`MauiAppBuilder` hosting model and Essentials APIs
(`AppInfo`, `Preferences`, `SecureStorage`, `Connectivity`, ...) to WPF, with dependency
injection, configuration, logging, and lifecycle events built in.**

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
  * [App composition](#app-composition-the-mauiprogram-pattern)
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
* **[Ecosystem](#ecosystem)**
  * [Repository layout](#repository-layout)
* **[API Reference](#api-reference)**
* **[Community](#community)**

---

## Getting Started

### Introduction

#### What is Barbatos.Wpf.Core?

Barbatos.Wpf.Core is a WPF library that ports .NET MAUI's Hosting and Essentials building
blocks to plain desktop WPF, with the same programming model and, wherever a Windows
equivalent exists, the same code shape MAUI itself uses internally.

Traditional WPF apps wire up `Application_Startup`, a hand-rolled DI container, and ad hoc
static helpers for things like app info, preferences, or connectivity. **Barbatos.Wpf.Core**
gives you `WpfApp`/`WpfAppBuilder` (mirroring `MauiApp`/`MauiAppBuilder`) — a real
`IHostApplicationBuilder` with dependency injection, configuration, logging, and lifecycle
events — plus a dozen ported Essentials modules (`AppInfo`, `Preferences`, `SecureStorage`,
`Connectivity`, `Launcher`, ...) registered in the same container.

> **Prerequisites**
>
> The rest of the documentation assumes basic familiarity with C#, .NET Dependency
> Injection, `Microsoft.Extensions.Hosting`, and WPF.

### Quick Start

Add the package via NuGet:

```powershell
dotnet add package Barbatos.Wpf.Core
```

Unlike .NET MAUI Essentials, this library ships as a single package — Hosting and every
Essentials module described below are part of `Barbatos.Wpf.Core`, there is nothing else to
install.

---

## Hosting

### App composition (the `MauiProgram` pattern)

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
`OnWindowClosing`, `OnWindowClosed`) are surfaced through the same `ILifecycleEventService`
design as .NET MAUI, and every window gets its own service scope (with
`IWpfInitializeScopedService` support).

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

Every .NET MAUI Essentials module that has a sensible Windows/WPF equivalent has been
ported with the same interface + static facade design (e.g. `AppInfo.Current`,
`Preferences.Default`) and, where the module needs OS integration, the same Windows
implementation strategy MAUI itself uses — swapped from WinRT/UWP APIs (unavailable to a
plain unpackaged WPF app) to the nearest Win32/.NET equivalent. All of it is registered in
the container by `UseEssentials()`, part of the builder defaults:

| Module | Static facade | Interface | Windows implementation |
| --- | --- | --- | --- |
| App info | `AppInfo` | `IAppInfo` | Assembly metadata/attributes; theme from the `AppsUseLightTheme` registry value; packaging via `GetCurrentPackageFullName`; `InstallDate`/`InstallLocation` from the installer's `Uninstall` registry entry, if any |
| Publisher info | `PublisherInfo` | `IPublisherInfo` | Assembly metadata, falling back to `AssemblyCompanyAttribute` |
| Device identity | `DeviceIdentity` | `IDeviceIdentity` | See [License enforcement: DeviceIdentity](#license-enforcement-deviceidentity) below — no MAUI counterpart |
| Device info | `DeviceInfo` | `IDeviceInfo` | BIOS registry (`Model`/`Manufacturer`); MAUI's own Chromium-style tablet-mode heuristic for `Idiom` |
| File system | `FileSystem` | `IFileSystem` | `%LocalAppData%\{Publisher}\{AppGuid}\{Data,Cache}`, same layout as MAUI's unpackaged path |
| Preferences | `Preferences` | `IPreferences` | Same unpackaged JSON-file strategy MAUI itself uses for non-MSIX apps |
| Secure storage | `SecureStorage` | `ISecureStorage` | DPAPI (`ProtectedData`, current-user scope) instead of the WinRT `DataProtectionProvider` |
| Version tracking | `VersionTracking` | `IVersionTracking` | Pure C#, ported near-verbatim (built on `Preferences` + `AppInfo`, no platform code) |
| Connectivity | `Connectivity` | `IConnectivity` | `System.Net.NetworkInformation` instead of the WinRT `NetworkInformation` API |
| Device display | `DeviceDisplay` | `IDeviceDisplay` | The same Win32 monitor-info P/Invoke block MAUI uses, against the active WPF window; `KeepScreenOn` via `SetThreadExecutionState`; change notifications via `SystemEvents.DisplaySettingsChanged` |
| Email | `Email` | `IEmail` | Simple MAPI (`MAPISendMail`) — ported near-verbatim, this was already pure Win32 in MAUI |
| Launcher | `Launcher` | `ILauncher` | `Process.Start(UseShellExecute: true)` instead of the WinRT `Windows.System.Launcher`; `CanOpenAsync` checks `HKEY_CLASSES_ROOT` for a registered scheme handler |
| Contacts | `Contacts` | `IContacts` | Throws `FeatureNotSupportedException` — see note below |
| Geolocation | `Geolocation` | `IGeolocation` | Throws `FeatureNotSupportedException` — see note below; `Location`'s distance math is fully functional |
| App actions | `AppActions` | `IAppActions` | The taskbar Jump List (`System.Windows.Shell.JumpList`) instead of the WinRT `Windows.UI.StartScreen.JumpList` |
| Permissions | `Permissions.CheckStatusAsync<T>()` / `RequestAsync<T>()` | *(none — generic static API, no DI, same as MAUI)* | Most permissions report `Granted` (no manifest/capability concept for an unpackaged app); `ContactsRead`/`ContactsWrite`/`LocationWhenInUse`/`LocationAlways`/`Microphone`/`Sensors` throw `FeatureNotSupportedException` — see [Permissions](#permissions) below |

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
MSBuild-property equivalent — MAUI's own `ApplicationId` is a MAUI-SDK-only concept, and
there is no built-in "website"/"support URL"/"support email" assembly attribute at all. For
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

> **Important:** `AppInfo.AppGuid` (renamed from .NET MAUI's `PackageName`, since on this
> platform it is not a store package identifier — it is simply the stable identifier used to
> derive the app's storage folder) should stay constant across releases. Changing it after
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
> `<FileVersion>` increment per build, `AppInfo.VersionString` (which reads `AssemblyVersion`,
> matching .NET MAUI's own behavior) and what your installer reports from the `.exe` file will
> diverge — set `Barbatos.Wpf.ApplicationModel.AppInfo.Version` explicitly if you need them to
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
`AppInfo`'s scope, the same way .NET MAUI's `IAppInfo` doesn't expose them either.

### App actions and version tracking

`AppActions` (taskbar Jump List shortcuts) and `VersionTracking.Track()` are wired up
through `ConfigureEssentials`, mirroring .NET MAUI's `IEssentialsBuilder`:

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
`AppActions.OnAppAction` — the same activation flow .NET MAUI uses (relaunch arguments), just
adapted to WPF's native Jump List API instead of `Windows.UI.StartScreen.JumpList`. Because
`System.Windows.Shell.JumpList` has no way to read back what the OS is currently showing
(unlike WinRT's `JumpList.LoadCurrentAsync()`), `AppActions.GetAsync()` returns the actions
last set by `SetAsync()` in this process rather than querying the OS.

### Contacts and Geolocation

.NET MAUI's real Windows implementations of `Contacts` and `Geolocation` are built on WinRT
contracts (`Windows.ApplicationModel.Contacts.ContactPicker`,
`Windows.Devices.Geolocation.Geolocator`) that require a TargetFramework with WinRT
projections (`net8.0-windows10.0.19041.0`-style) and, for some APIs, MSIX packaging —
neither of which this plain `net8.0-windows`/`net9.0-windows`/`net10.0-windows` WPF library
uses. Rather than pull in that machinery, both modules ship with their full MAUI-shaped type
surface (`IContacts`/`Contact`/`ContactEmail`/`ContactPhone`,
`IGeolocation`/`Location`/`GeolocationRequest`/...) so code compiles unchanged, but every
member of the Windows implementation throws `FeatureNotSupportedException` — the exact
exception (and message style) .NET MAUI itself throws for feature/platform combinations it
doesn't support. If you need a real implementation (for example Geolocation via the classic
Win32 Location API, or Contacts via Microsoft Graph), register your own after the builder is
created — a later `AddSingleton` call takes precedence over the `TryAddSingleton` that
`UseEssentials()` performs, so code that resolves `IContacts`/`IGeolocation` through
constructor injection will get your implementation:

```csharp
builder.Services.AddSingleton<IContacts, MyGraphContacts>();
```

The static `Contacts.PickContactAsync()`/`Geolocation.GetLocationAsync()` facades are
independent of DI (same as in .NET MAUI) and will keep using the built-in
`FeatureNotSupportedException`-throwing implementation regardless — prefer the injected
`IContacts`/`IGeolocation` interface if you register a replacement.

### Permissions

`Permissions` is the WPF counterpart of .NET MAUI's `Permissions` API, ported with the exact
same generic, DI-free static surface — there is no `IPermissions` interface to register,
because MAUI's own `Permissions` isn't service-injectable either:

```csharp
var status = await Permissions.CheckStatusAsync<Permissions.Camera>();

if (status != PermissionStatus.Granted)
    status = await Permissions.RequestAsync<Permissions.Camera>();
```

Every permission type MAUI ships (`Battery`, `Bluetooth`, `CalendarRead`/`CalendarWrite`,
`Camera`, `ContactsRead`/`ContactsWrite`, `Flashlight`, `LaunchApp`,
`LocationWhenInUse`/`LocationAlways`, `Maps`, `Media`, `Microphone`, `NearbyWifiDevices`,
`NetworkState`, `Phone`, `Photos`/`PhotosAddOnly`, `PostNotifications`, `Reminders`,
`Sensors`, `Sms`, `Speech`, `StorageRead`/`StorageWrite`, `Vibrate`) is present as a nested
type of `Permissions`, exactly as in MAUI. Two behaviors on WPF:

- **Most permissions report `PermissionStatus.Granted`.** An unpackaged desktop WPF app has
  no `AppxManifest.xml` capability declarations to check, and .NET MAUI's own Windows
  implementation already just returns `Granted` for these same permissions (it never
  actually gates `Camera`, `CalendarRead`, `Phone`, `StorageRead`, etc. on Windows either) —
  `EnsureDeclared()` is a no-op for the same reason.
- **`ContactsRead`, `ContactsWrite`, `LocationWhenInUse`, `LocationAlways`, `Microphone`, and
  `Sensors` throw `FeatureNotSupportedException`.** MAUI backs these six with real WinRT
  device-access contracts (`ContactManager`, `Geolocator`, `DeviceAccessInformation`,
  `MediaCapture`) that need WinRT projections and, for some APIs, MSIX packaging — the same
  machinery this library already opts out of for [`Contacts` and `Geolocation`](#contacts-and-geolocation).
  Rather than silently report `Granted` for a permission MAUI itself actually checks, these
  six are honest about not performing a real check.

If you need a real permission check (for example reading the Windows privacy consent
registry, or a custom capability broker), subclass `Permissions.BasePlatformPermission` and
pass your subclass as `TPermission` — the same extensibility model .NET MAUI itself offers.

### License enforcement: DeviceIdentity

.NET MAUI's `DeviceInfo` deliberately exposes no device identifier and no network address —
Apple and Google both prohibit collecting raw hardware IDs (IMEI, serial numbers) for
ordinary commercial apps, so MAUI's cross-platform surface never offers one in the first
place. `DeviceIdentity` exists for the desktop-specific case this rules out: enforcing a
per-machine license/activation limit. It has no .NET MAUI counterpart.

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
- The tray icon supports a context menu, tooltip, and click/double-click events; the
  sample uses it together with the `OnWindowClosing` lifecycle event to implement
  minimize-to-tray.
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

> **Note:** an earlier version of this library had a `ConfigureGlobalHotkeys` feature
> (system-wide keyboard shortcuts via `RegisterHotKey`, for a "Quick Entry" hotkey). It has
> been removed in favor of `IAppActions` (taskbar Jump List shortcuts — see
> [Essentials](#essentials) above), which is what .NET MAUI's own Essentials actually
> exposes as "app actions" on Windows. A global keyboard hotkey is a materially different
> capability (no MAUI equivalent); if you need one, `RegisterHotKey`/`UnregisterHotKey`
> P/Invoke is straightforward to reintroduce as your own `IWpfInitializeService`.

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

There is no .NET MAUI runtime code to port here: MAUI's splash screen is entirely a build-time
asset pipeline (`Microsoft.Maui.Resizetizer`, driven by the `<MauiSplashScreen>` MSBuild item)
that generates a native Android theme / iOS storyboard / Windows AppxManifest entry per
platform, with zero cross-platform C# logic — and on Windows it only applies to **packaged**
(MSIX) apps; the AppxManifest it relies on is stripped entirely for unpackaged apps, which is
the deployment model this library targets. So instead of porting, `WpfApplication` gets a
splash screen hook of its own, following the same shape MAUI uses elsewhere (a lifecycle hook
you override, plus a plain settings object) rather than the DI-based `Configure...()` pattern
the other Features use — a splash screen has to show before the dependency injection container
even exists.

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
register it, and it runs every `Interval` (5 seconds, 5 minutes, 1 hour, ...) on the
application dispatcher.

```csharp
public sealed class SyncService : IWpfPeriodicService
{
    public string Name => "Sync";
    public TimeSpan Interval => TimeSpan.FromMinutes(5); // default, configurable
    public async Task ExecuteAsync(IServiceProvider services, CancellationToken ct)
    {
        await Task.Run(() => { /* heavy work off the UI thread */ }, ct);
    }
}

builder.ConfigurePeriodicServices<SyncService>();
// or: builder.Services.AddSingleton<IWpfPeriodicService, SyncService>();
//     builder.ConfigurePeriodicServices();
```

The interval can be configured in three ways (file overrides code; UI wins at runtime):

1. **Code** — the service's own `Interval` property.
2. **File** — the `Barbatos:PeriodicServices` section:

   ```json
   { "Barbatos": { "PeriodicServices": { "Enabled": true, "Intervals": { "Sync": "00:05:00" } } } }
   ```

3. **UI** — through `IPeriodicServiceScheduler`:
   `scheduler.UpdateInterval("Sync", TimeSpan.FromHours(1))` reschedules immediately;
   `SetEnabled(bool)` starts/stops all services; `Services` exposes live status
   (interval, last run, run count) and `ServiceExecuted` reports every run, including failures.

Failed executions are logged and do not stop the schedule; a tick is skipped while the
previous run is still in progress; the cancellation token passed to `ExecuteAsync` is
cancelled when the host is disposed.

---

## Ecosystem

Unlike modular libraries split across many NuGet packages, Barbatos.Wpf.Core ships as a
**single package** — Hosting, all Essentials modules, and every optional desktop feature are
included; there is nothing else to install.

### Repository layout

- `src/Barbatos.Wpf.Core` — the library.
- `samples/Barbatos.Wpf.Core.Sample` — a complete sample application showing DI,
  configuration, host environment, Essentials usage, and the live lifecycle event log.
- `tests/Barbatos.Wpf.Core.UnitTests` — the unit test suite, ported from the
  .NET MAUI hosting and Essentials tests.

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
