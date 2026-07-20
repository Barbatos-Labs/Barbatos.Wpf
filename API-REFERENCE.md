# Barbatos.Wpf.Core API Reference

This document provides a comprehensive reference for the Barbatos.Wpf.Core library, modeled
after the official .NET API documentation.

## Namespaces

| Namespace | Description |
|-----------|-------------|
| **[`Barbatos.Wpf.Hosting`](#barbatoswpfhosting-namespace)** | The application host: `WpfApp`/`WpfAppBuilder`, initialization services, Essentials wiring, and the optional desktop features (single instance, run on startup, tray icon, keep awake, notifications, periodic services). |
| **[`Barbatos.Wpf.LifecycleEvents`](#barbatoswpflifecycleevents-namespace)** | The lifecycle event system and its WPF-specific (`WpfLifecycle`) application/window delegates. |
| **[`Barbatos.Wpf.Dispatching`](#barbatoswpfdispatching-namespace)** | Dispatcher abstractions (`IDispatcher`, `IDispatcherTimer`, `IDispatcherProvider`) backed by the WPF `Dispatcher`. |
| **[`Barbatos.Wpf.ApplicationModel`](#barbatoswpfapplicationmodel-namespace)** | App info, publisher info, version tracking, app actions, the launcher, and the shared Essentials exception types. |
| **[`Barbatos.Wpf.ApplicationModel.Communication`](#barbatoswpfapplicationmodelcommunication-namespace)** | Email composition and contacts. |
| **[`Barbatos.Wpf.Devices`](#barbatoswpfdevices-namespace)** | Device info and device display. |
| **[`Barbatos.Wpf.Devices.Sensors`](#barbatoswpfdevicessensors-namespace)** | Geolocation. |
| **[`Barbatos.Wpf.Networking`](#barbatoswpfnetworking-namespace)** | Connectivity. |
| **[`Barbatos.Wpf.Storage`](#barbatoswpfstorage-namespace)** | File system, preferences, and secure storage. |
| **[`Barbatos.Wpf.Tray`](#barbatoswpftray-namespace)** | The system tray icon optional feature. |
| **[`Barbatos.Wpf.Power`](#barbatoswpfpower-namespace)** | The keep-awake optional feature. |
| **[`Barbatos.Wpf.Startup`](#barbatoswpfstartup-namespace)** | The run-on-startup optional feature. |
| **[`Barbatos.Wpf.Notifications`](#barbatoswpfnotifications-namespace)** | The push notifications optional feature. |
| **[`Barbatos.Wpf.SingleInstance`](#barbatoswpfsingleinstance-namespace)** | The single-instance optional feature (enabled by default). |
| **[`Barbatos.Wpf.Dialogs`](#barbatoswpfdialogs-namespace)** | The dialog service: owner assignment, duplicate-open prevention, and graceful bulk-close for child windows. |

---

## `Barbatos.Wpf.Hosting` Namespace

The WPF counterpart of `Microsoft.Maui.Hosting`: the application host, its builder, and the
extension methods that wire up Essentials and every optional desktop feature.

### Classes

| Class | Description |
|-------|-------------|
| [`WpfApp`](#wpfapp-class) | A built application: configured services and configuration. |
| [`WpfAppBuilder`](#wpfappbuilder-class) | Builds a `WpfApp`; implements `IHostApplicationBuilder`. |
| [`WpfHostEnvironment`](#wpfhostenvironment-class) | The WPF `IHostEnvironment` implementation. |
| `WpfApplication` | A `System.Windows.Application` subclass that owns the built `WpfApp` and forwards lifecycle events. See [splash screen support](#wpfapplication-splash-screen-support) below. |
| `EssentialsExtensions` | `UseEssentials()` / `ConfigureEssentials(...)` — registers every Essentials service and app-action activation detection. |
| `PeriodicServiceOptions` | Options for `ConfigurePeriodicServices` (`Enabled`, `Intervals`). |
| `PeriodicServiceStatus` | Live status of a registered periodic service (`Name`, `Interval`, `LastRunTime`, `RunCount`). |
| `PeriodicServiceExecutedEventArgs` | Raised by `IPeriodicServiceScheduler.ServiceExecuted` after each run. |
| [`SplashScreenOptions`](#wpfapplication-splash-screen-support) | Configures the built-in `SplashWindow` (app name, logo, tagline, sponsor logos, related links, minimum display duration). |
| `SplashScreenLogo` | A single sponsor/developer logo entry in `SplashScreenOptions.SponsorLogos`. |
| `SplashScreenLink` | A single related-item entry in `SplashScreenOptions.RelatedLinks`, e.g. another product from the same publisher. |
| [`SplashWindow`](#wpfapplication-splash-screen-support) | The built-in splash screen window, populated from `SplashScreenOptions`. |

### Interfaces

| Interface | Description |
|-----------|-------------|
| `IWpfInitializeService` | Runs once during `WpfAppBuilder.Build()`. WPF counterpart of `IMauiInitializeService`. |
| `IWpfInitializeScopedService` | Runs once per window scope. WPF counterpart of `IMauiInitializeScopedService`. |
| `IWpfPlatformApplication` | Exposes the running `WpfApp` from `System.Windows.Application`. |
| `IEssentialsBuilder` | Configures app actions and version tracking from `ConfigureEssentials`. |
| [`IWpfPeriodicService`](#iwpfperiodicservice-interface) | A service executed on a recurring schedule. |
| [`IPeriodicServiceScheduler`](#iperiodicservicescheduler-interface) | Runtime control over registered periodic services. |
| [`ISplashScreen`](#wpfapplication-splash-screen-support) | Implemented by a splash screen window to report `MinimumDisplayDuration`. |

### Extension Methods

- **`ConfigureDialogs(this WpfAppBuilder, Action<DialogOptions>? = null)`** — registers `IDialogService` (`Barbatos.Wpf.Dialogs`).
- **`ConfigureSingleInstance(this WpfAppBuilder, Action<SingleInstanceOptions>? = null)`** — registers `ISingleInstanceService` (`Barbatos.Wpf.SingleInstance`); enabled by default the moment this is called.
- **`ConfigureRunOnStartup(this WpfAppBuilder, Action<RunOnStartupOptions>? = null)`** — registers `IRunOnStartupService` (`Barbatos.Wpf.Startup`).
- **`ConfigureTrayIcon(this WpfAppBuilder, Action<TrayIconOptions>? = null)`** — registers `ITrayIconService` (`Barbatos.Wpf.Tray`).
- **`ConfigureKeepAwake(this WpfAppBuilder, Action<KeepAwakeOptions>? = null)`** — registers `IKeepAwakeService` (`Barbatos.Wpf.Power`).
- **`ConfigureNotifications(this WpfAppBuilder, Action<NotificationOptions>? = null)`** — registers `INotificationService` (`Barbatos.Wpf.Notifications`).
- **`ConfigurePeriodicServices(this WpfAppBuilder)`** / **`ConfigurePeriodicServices<TService>(this WpfAppBuilder)`** — registers `IPeriodicServiceScheduler` and (for the generic overload) `TService` as an `IWpfPeriodicService`.
- **`UseEssentials(this WpfAppBuilder)`** — registers `IAppInfo`, `IPublisherInfo`, `IDeviceIdentity`, `IDeviceInfo`, `IFileSystem`, `IPreferences`, `ISecureStorage`, `IVersionTracking`, `IConnectivity`, `IDeviceDisplay`, `IEmail`, `IContacts`, `IGeolocation`, `IAppActions`, `ILauncher`; called by default when the builder is created with defaults.
- **`ConfigureEssentials(this WpfAppBuilder, Action<IEssentialsBuilder>? = null)`** — configures app actions (`AddAppAction`, `OnAppAction`) and `UseVersionTracking()`.

Each `Configure...` feature method binds its options section (file values override code
values) from the `Barbatos:{Feature}` configuration section — see
[Optional desktop features](README.md#optional-desktop-features) in the README.

---

### `WpfApp` Class

A built application: configured services and configuration, the WPF counterpart of .NET
MAUI's `MauiApp`.

```csharp
public sealed class WpfApp : IDisposable, IAsyncDisposable
```

#### Properties

- **`Services`** (`IServiceProvider`): the application's configured services.
- **`Configuration`** (`IConfiguration`): the application's configured configuration.

#### Methods

- **`static CreateBuilder(bool useDefaults = true)`**: creates a `WpfAppBuilder`. When
  `useDefaults` is `true` (the default), lifecycle events, dispatching, and `UseEssentials()`
  are configured automatically.

---

### `WpfAppBuilder` Class

Builds a `WpfApp`. Implements `IHostApplicationBuilder`, the WPF counterpart of .NET MAUI's
`MauiAppBuilder`.

```csharp
public sealed class WpfAppBuilder : IHostApplicationBuilder
```

#### Properties

- **`Services`** (`IServiceCollection`)
- **`Configuration`** (`ConfigurationManager`)
- **`Logging`** (`ILoggingBuilder`)
- **`Properties`** (`IDictionary<object, object>`)
- **`Environment`** (`WpfHostEnvironment`)

#### Methods

- **`ConfigureContainer<TBuilder>(IServiceProviderFactory<TBuilder>, Action<TBuilder>? = null)`**
- **`Build()`** → `WpfApp`

---

### `WpfHostEnvironment` Class

The WPF `IHostEnvironment` implementation, the WPF counterpart of .NET MAUI's
`MauiHostEnvironment` (and analogous to ASP.NET Core's `HostingEnvironment`).

```csharp
public class WpfHostEnvironment : IHostEnvironment
```

#### Properties

- **`EnvironmentName`** (`string`, settable): defaults to `Environments.Production`; set once
  by `WpfAppBuilder` from `HostDefaults.EnvironmentKey` (fed by the `DOTNET_` prefixed
  environment variables configuration source), can be overridden from any configuration
  source or directly in code. See [Configuring the hosting environment](README.md#configuring-the-hosting-environment).
- **`ApplicationName`** (`string`, read-only, throws `NotSupportedException` on set): `AppInfo.Current.Name`.
- **`ContentRootPath`** (`string`, read-only, throws `NotSupportedException` on set): `AppContext.BaseDirectory`.
- **`ContentRootFileProvider`** (`IFileProvider`, read-only, throws `NotSupportedException` on set): a `PhysicalFileProvider` rooted at `ContentRootPath`.

---

### `IWpfPeriodicService` Interface

Represents a service that is executed periodically while the application is running.

```csharp
public interface IWpfPeriodicService
{
    string Name { get; }
    TimeSpan Interval { get; }
    Task ExecuteAsync(IServiceProvider services, CancellationToken cancellationToken);
}
```

---

### `IPeriodicServiceScheduler` Interface

Schedules the registered `IWpfPeriodicService` instances.

```csharp
public interface IPeriodicServiceScheduler
```

#### Properties
- **`IsEnabled`** (`bool`)
- **`Services`** (`IReadOnlyList<PeriodicServiceStatus>`)

#### Methods
- **`SetEnabled(bool)`** — starts/stops every registered service's timer.
- **`UpdateInterval(string name, TimeSpan interval)`** — reschedules a running service immediately.

#### Events
- **`IsEnabledChanged`**
- **`ServiceExecuted`** (`PeriodicServiceExecutedEventArgs`) — raised after every run, including failed ones.

---

### `WpfApplication` splash screen support

Has no .NET MAUI counterpart to port: MAUI's splash screen is a build-time-only asset
pipeline with no cross-platform C# runtime logic, and does not apply at all to unpackaged
Windows apps. See [SplashScreen](README.md#splashscreen) in the README for the full behavior
description.

- **`WpfApplication.GetSplashScreenOptions()`** (`protected virtual SplashScreenOptions?`) —
  override to configure the built-in `SplashWindow`. Returns `null` (the default) for no splash
  screen.
- **`WpfApplication.CreateSplashScreen()`** (`protected virtual Window?`) — override instead of
  `GetSplashScreenOptions()` for full control over the splash screen's UI. Default
  implementation creates a `SplashWindow` from `GetSplashScreenOptions()`.
- **`WpfApplication.CloseSplashScreenAsync()`** (`protected Task`) — call from your own
  `OnStartup` override, after `base.OnStartup(e)` and before showing the main window. Waits out
  any remaining `MinimumDisplayDuration`, then closes the splash screen; a no-op if none was
  shown. `WpfApplication.OnStartup` shows the splash screen (if configured) before
  `CreateWpfApp()` runs, and temporarily forces `ShutdownMode.OnExplicitShutdown` for its
  lifetime so the app does not exit the moment the splash screen - briefly the only open window
  - is closed; restored once `CloseSplashScreenAsync()` actually closes it.

#### `SplashScreenOptions`

- **`AppName`** (`string?`) — defaults to `AppInfo.Name`.
- **`LogoSource`** (`string?`) — any URI a `BitmapImage` accepts; hidden when not set.
- **`Tagline`** (`string?`) — hidden when not set.
- **`Background`** (`Brush?`)
- **`ShowProgressIndicator`** (`bool`) — defaults to `true`.
- **`SponsorLogos`** (`IList<SplashScreenLogo>`) — empty by default; hidden as a row when empty.
- **`RelatedLinks`** (`IList<SplashScreenLink>`) — empty by default; hidden when empty.
- **`MinimumDisplayDuration`** (`TimeSpan`) — defaults to 1.5 seconds.

#### `ISplashScreen`

- **`MinimumDisplayDuration`** (`TimeSpan`) — implemented by `SplashWindow`; implement it on
  your own window too if you override `CreateSplashScreen()` with fully custom UI.

---

## `Barbatos.Wpf.LifecycleEvents` Namespace

The lifecycle event system, ported from .NET MAUI's `Microsoft.Maui.LifecycleEvents`.

### Classes / Interfaces

| Type | Description |
|------|-------------|
| `ILifecycleEventService` | Resolves and invokes registered delegates by event name. |
| `ILifecycleBuilder` | Registers delegates during `ConfigureLifecycleEvents`. |
| `WpfLifecycle` | Static class of delegate types: `OnStartup`, `OnActivated`, `OnDeactivated`, `OnSessionEnding`, `OnDispatcherUnhandledException`, `OnExit`, `OnWindowCreated`, `OnWindowLoaded`, `OnWindowActivated`, `OnWindowDeactivated`, `OnWindowStateChanged`, `OnWindowClosing`, `OnWindowClosed`. |
| `IWpfLifecycleBuilder` | The fluent builder passed to `AddWpf(wpf => wpf.OnStartup(...)...)`. |

### Extension Methods

- **`ConfigureLifecycleEvents(this WpfAppBuilder, Action<ILifecycleBuilder>?)`**
- **`AddWpf(this ILifecycleBuilder, Action<IWpfLifecycleBuilder>)`**
- **`AddEvent(this ILifecycleBuilder, string eventName, Delegate handler)`** — registers a custom, non-`WpfLifecycle` event.
- **`ContainsEvent(this ILifecycleEventService, string eventName)`**
- **`GetEventDelegates<TDelegate>(this ILifecycleEventService, string eventName)`**
- **`InvokeEvents(this ILifecycleEventService, string eventName, ...)`**

#### Remarks

Every window gets its own DI service scope; `IWpfInitializeScopedService` implementations
run once per scope, mirroring .NET MAUI's per-window scoping model.

---

## `Barbatos.Wpf.Dispatching` Namespace

Dispatcher abstractions backed by the WPF `Dispatcher`, the WPF counterpart of .NET MAUI's
`Microsoft.Maui.Dispatching`.

### Interfaces

| Interface | Description |
|-----------|-------------|
| `IDispatcher` | `Dispatch(Action)`, `DispatchAsync(Func<Task>)`, `IsDispatchRequired`, `CreateTimer()`. |
| `IDispatcherTimer` | `Interval`, `IsRepeating`, `Start()`, `Stop()`, `Tick` event. |
| `IDispatcherProvider` | Resolves the current `IDispatcher` (registered app-wide as singleton, and per-window as scoped). |

### Extension Methods

- **`DispatchAsync(this IDispatcher, Action)`**
- **`DispatchIfRequiredAsync(this IDispatcher, Action)`** — dispatches only when `IsDispatchRequired` is `true`, otherwise runs inline.

### Extension Methods (on `WpfAppBuilder`)

- **`ConfigureDispatching(this WpfAppBuilder)`** — registers `IDispatcherProvider`/`IDispatcher` app-wide and per-window; part of the builder defaults.

---

## `Barbatos.Wpf.ApplicationModel` Namespace

App info, publisher info, version tracking, app actions, the launcher, and the shared
Essentials exception types. WPF counterpart of .NET MAUI's `Microsoft.Maui.ApplicationModel`.

### Classes / Interfaces

| Type | Description |
|------|-------------|
| [`AppInfo`](#appinfo--ipublisherinfo) / `IAppInfo` | App identity: `AppGuid`, `Name`, `Version`, `VersionString`, `BuildString`, `RequestedTheme`, `PackagingModel`, `RequestedLayoutDirection`, `InstallDate`, `InstallLocation`, `ShowSettingsUI()`. |
| [`PublisherInfo`](#appinfo--ipublisherinfo) / `IPublisherInfo` | Publisher identity: `Name`, `Website`, `SupportUrl`, `SupportEmail`, `Copyright`. |
| [`DeviceIdentity`](#deviceidentity--ideviceidentity) / `IDeviceIdentity` | License-enforcement identifiers: `GetInstanceIdAsync()`, `GetHardwareFingerprintAsync()`. |
| `AppTheme` | `Unspecified`, `Light`, `Dark`. |
| `AppPackagingModel` | `Packaged`, `Unpackaged`. |
| `LayoutDirection` | `LeftToRight`, `RightToLeft`. |
| [`VersionTracking`](#versiontracking--iversiontracking) / `IVersionTracking` | Tracks first-launch/version/build history across runs. |
| [`AppActions`](#appactions--iappactions) / `IAppActions` | Taskbar Jump List shortcuts. |
| `AppAction` | `Id`, `Title`, `Subtitle`, `Icon`. |
| `AppActionEventArgs` | `AppAction` — raised by `AppActions.OnAppAction`. |
| [`Launcher`](#launcher--ilauncher) / `ILauncher` | Opens URIs and files via the system shell. |
| `OpenFileRequest` | `Title`, `FullPath` — used with `Launcher.OpenAsync(OpenFileRequest)`. |
| `PermissionException` | Thrown for APIs requiring a permission (`UnauthorizedAccessException` subclass). |
| `FeatureNotSupportedException` | Thrown by a feature unsupported on this platform (`NotSupportedException` subclass) — used by `Contacts` and `Geolocation`. |
| `FeatureNotEnabledException` | Thrown by a feature that exists but is not currently enabled (`InvalidOperationException` subclass). |

### `AppInfo` / `PublisherInfo`

Both follow the interface + static facade pattern (`AppInfo.Current` / `PublisherInfo.Current`).
Their properties resolve from the standard SDK-generated assembly attributes — `<Product>`,
`<Company>`, `<Version>`, `<Copyright>` in the csproj — with an explicit
`Barbatos.Wpf.ApplicationModel.*` assembly-metadata override checked first for anything that
needs a different value (or, for `AppGuid`, has no standard property at all). See
[Configuring AppInfo and PublisherInfo](README.md#configuring-appinfo-and-publisherinfo) in
the README for the full property table and fallback chain.

`AppInfo.InstallDate` (`DateTime?`) and `AppInfo.InstallLocation` (`string?`) have no .NET
MAUI counterpart. They read the Windows "Programs and Features" uninstall registry entry
(`...\Uninstall\{AppGuid}` or `{AppGuid}_is1`) for `AppGuid`, searching `HKEY_LOCAL_MACHINE`
and `HKEY_CURRENT_USER` in both the 64-bit and 32-bit registry view. Both are `null` when
`AppGuid` isn't a real GUID, or no matching entry is found (e.g. running unpublished). See
[Reading back InstallDate and InstallLocation](README.md#reading-back-installdate-and-installlocation)
in the README.

`FileSystem.AppDataDirectory`/`CacheDirectory` are derived from
`PublisherInfo.Name`/`AppInfo.AppGuid` — see [`Barbatos.Wpf.Storage`](#barbatoswpfstorage-namespace).

### `DeviceIdentity` / `IDeviceIdentity`

License-enforcement identifiers. Has no .NET MAUI counterpart — see
[License enforcement: DeviceIdentity](README.md#license-enforcement-deviceidentity) in the
README for the full rationale and privacy/compliance notes (not legal advice).

- **`GetInstanceIdAsync()`** → `Task<string>` — a random GUID generated on first use and
  persisted via `ISecureStorage`. Identifies this app installation, not the physical machine;
  resets on reinstall or if local app data is cleared.
- **`GetHardwareFingerprintAsync()`** → `Task<string>` — a 64-character uppercase hex SHA-256
  hash of a few motherboard/BIOS/CPU identifiers (read via WMI: `Win32_BaseBoard.SerialNumber`,
  `Win32_BIOS.SerialNumber`, `Win32_Processor.ProcessorId`), salted with `AppInfo.AppGuid` so
  the result is scoped to this app rather than directly comparable across unrelated apps on
  the same machine. The raw hardware serials are never stored or returned — only the hash.
  Falls back to hashing `Environment.MachineName` alone when no WMI identifier is readable
  (e.g. a locked-down environment). Cached for the lifetime of the process.

### `VersionTracking` / `IVersionTracking`

Ported near-verbatim from .NET MAUI — pure C#, built entirely on `IPreferences` + `IAppInfo`,
no platform-specific code.

- **`Track()`** — call once (typically via `ConfigureEssentials(e => e.UseVersionTracking())`) to snapshot the current version/build into history.
- **`IsFirstLaunchEver`**, **`IsFirstLaunchForCurrentVersion`**, **`IsFirstLaunchForCurrentBuild`** (`bool`)
- **`CurrentVersion`**, **`CurrentBuild`**, **`PreviousVersion`**, **`PreviousBuild`**, **`FirstInstalledVersion`**, **`FirstInstalledBuild`** (`string?`)
- **`VersionHistory`**, **`BuildHistory`** (`IReadOnlyList<string>`)
- **`IsFirstLaunchForVersion(string)`**, **`IsFirstLaunchForBuild(string)`**

### `AppActions` / `IAppActions`

Windows implementation backed by `System.Windows.Shell.JumpList` (the taskbar right-click
menu) instead of WinRT's `Windows.UI.StartScreen.JumpList`. See
[App actions and version tracking](README.md#app-actions-and-version-tracking) in the README.

- **`IsSupported`** (`bool`)
- **`GetAsync()`** → `Task<IEnumerable<AppAction>>` — returns the actions last set by `SetAsync` in this process (no OS read-back API exists for `JumpList`).
- **`SetAsync(IEnumerable<AppAction>)`** / **`SetAsync(params AppAction[])`**
- **`OnAppAction`** (`event EventHandler<AppActionEventArgs>?`)

### `Launcher` / `ILauncher`

Windows implementation backed by `Process.Start(UseShellExecute: true)` instead of WinRT's
`Windows.System.Launcher`.

- **`CanOpenAsync(Uri | string)`** → `Task<bool>` — looks up the URI's scheme under `HKEY_CLASSES_ROOT` for a registered `"URL Protocol"` value or a `shell\open\command` subkey.
- **`OpenAsync(Uri | string)`** → `Task<bool>`
- **`OpenAsync(OpenFileRequest)`** → `Task<bool>` — opens a file by full path with its associated application.
- **`TryOpenAsync(Uri | string)`** → `Task<bool>` — `CanOpenAsync` then `OpenAsync`.

```csharp
await Launcher.OpenAsync("https://example.com");
await Launcher.OpenAsync(new OpenFileRequest("Open report", @"C:\Reports\q1.pdf"));
```

---

## `Barbatos.Wpf.ApplicationModel.Communication` Namespace

Email composition and contacts.

### Classes / Interfaces

| Type | Description |
|------|-------------|
| `Email` / `IEmail` | `IsComposeSupported`, `ComposeAsync(EmailMessage?)` — backed by Simple MAPI (`MAPISendMail`). |
| `EmailMessage` | `Subject`, `Body`, `BodyFormat`, `To`, `Cc`, `Bcc`, `Attachments`. |
| `EmailBodyFormat` | `PlainText`, `Html` (HTML throws — Simple MAPI is plain-text only). |
| `EmailAttachment` | `FullPath` — a simplified counterpart of MAUI's `EmailAttachment` (no `FileBase` dependency). |
| `Contacts` / `IContacts` | `PickContactAsync()`, `GetAllAsync(CancellationToken)` — both throw `FeatureNotSupportedException` on Windows (see the README's [Contacts and Geolocation](README.md#contacts-and-geolocation) note). |
| `Contact` | `Id`, `NamePrefix`, `GivenName`, `MiddleName`, `FamilyName`, `NameSuffix`, `DisplayName`, `Emails`, `Phones`. |
| `ContactEmail` / `ContactPhone` | `Type` and value fields for a contact's email/phone entries. |

---

## `Barbatos.Wpf.Devices` Namespace

Device info and device display.

### `DeviceInfo` / `IDeviceInfo`

- **`Model`**, **`Manufacturer`** (`string`) — sourced from the BIOS registry.
- **`Name`** (`string`) — the machine name.
- **`VersionString`**, **`Version`** — the OS version.
- **`Platform`** (`DevicePlatform`), **`Idiom`** (`DeviceIdiom`), **`DeviceType`** (`DeviceType`: `Unknown`, `Physical`, `Virtual`).

### `DeviceDisplay` / `IDeviceDisplay`

- **`KeepScreenOn`** (`bool`, settable) — via `SetThreadExecutionState(ES_DISPLAY_REQUIRED)`.
- **`MainDisplayInfo`** (`DisplayInfo`) — `Width`, `Height`, `Density`, `Orientation`, `Rotation`, `RefreshRate`; computed against the active WPF window using the same Win32 monitor-info P/Invoke block MAUI uses.
- **`MainDisplayInfoChanged`** (`event EventHandler<DisplayInfoChangedEventArgs>`) — backed by `Microsoft.Win32.SystemEvents.DisplaySettingsChanged`; the listener starts on first subscription and stops when the last handler is removed.

---

## `Barbatos.Wpf.Devices.Sensors` Namespace

### `Geolocation` / `IGeolocation`

Every member throws `FeatureNotSupportedException` on Windows — see
[Contacts and Geolocation](README.md#contacts-and-geolocation) in the README.
`IsEnabled`/`IsListeningForeground` return `false` rather than throwing.

- **`GetLastKnownLocationAsync()`**, **`GetLocationAsync(...)`** → `Task<Location?>`
- **`StartListeningForegroundAsync(GeolocationListeningRequest)`** → `Task<bool>`, **`StopListeningForeground()`**
- **`LocationChanged`**, **`ListeningFailed`** events

`Location`, `GeolocationRequest`, `GeolocationAccuracy`, `GeolocationListeningRequest`,
`GeolocationError`, `DistanceUnits`, and `AltitudeReferenceSystem` retain their full
MAUI-shaped type surface. `Location.CalculateDistance(...)` (haversine distance) is fully
functional independent of the throwing platform implementation.

---

## `Barbatos.Wpf.Networking` Namespace

### `Connectivity` / `IConnectivity`

Backed by `System.Net.NetworkInformation` instead of WinRT's `NetworkInformation` API.

- **`NetworkAccess`** (`NetworkAccess`: `Unknown`, `None`, `Local`, `ConstrainedInternet`, `Internet`)
- **`ConnectionProfiles`** (`IEnumerable<ConnectionProfile>`: `Unknown`, `Bluetooth`, `Cellular`, `Ethernet`, `WiFi`) — the static `Connectivity.ConnectionProfiles` facade applies `.Distinct()`; the injected `IConnectivity.ConnectionProfiles` does not (matching .NET MAUI's own asymmetric design).
- **`ConnectivityChanged`** (`event EventHandler<ConnectivityChangedEventArgs>`)

---

## `Barbatos.Wpf.Storage` Namespace

### `FileSystem` / `IFileSystem`

- **`AppDataDirectory`**, **`CacheDirectory`** (`string`) — `%LocalAppData%\{PublisherInfo.Name}\{AppInfo.AppGuid}\{Data,Cache}`, created on first access.
- **`OpenAppPackageFileAsync(string filename)`** → `Task<Stream>`, **`AppPackageFileExistsAsync(string filename)`** → `Task<bool>` — resolved relative to the executable's own directory.

### `Preferences` / `IPreferences`

Stores key/value pairs in a local JSON file under `FileSystem.AppDataDirectory\..\Settings\preferences.dat`
(the same unpackaged strategy .NET MAUI itself uses for non-MSIX apps), guarded by a lock
around file I/O.

- **`ContainsKey`**, **`Remove`**, **`Clear`** (optionally scoped to a `sharedName`)
- **`Get<T>`/`Set<T>`** overloads for `string`, `bool`, `int`, `double`, `float`, `long`, `DateTime`, `DateTimeOffset`

### `SecureStorage` / `ISecureStorage`

Stores encrypted key/value pairs in a local file under
`FileSystem.AppDataDirectory\..\Settings\securestorage.dat`, encrypted with DPAPI
(`System.Security.Cryptography.ProtectedData`, current-user scope) instead of the WinRT
`DataProtectionProvider`.

- **`GetAsync(string key)`** → `Task<string?>`
- **`SetAsync(string key, string value)`** → `Task`
- **`Remove(string key)`** → `bool`, **`RemoveAll()`**

---

## `Barbatos.Wpf.Tray` Namespace

### `ITrayIconService`

- **`IsVisible`** (`bool`), **`SetVisible(bool)`**
- **`SetToolTip(string)`**
- **`Clicked`**, **`DoubleClicked`** events

### `TrayIconOptions`

- **`Enabled`** (`bool`), **`ToolTip`** (`string?`)
- **`MenuItems`** (`IList<TrayMenuItem>`) — each a `(string Header, Action OnClick)` pair added via `new TrayMenuItem("Open", () => { ... })`.

---

## `Barbatos.Wpf.Power` Namespace

### `IKeepAwakeService`

Prevents idle sleep via `SetThreadExecutionState`; released automatically when the host is disposed.

### `KeepAwakeOptions`

- **`Enabled`** (`bool`)
- **`KeepDisplayOn`** (`bool`) — also prevents the display from turning off, not just system sleep.

---

## `Barbatos.Wpf.Startup` Namespace

### `IRunOnStartupService`

Registers/unregisters the app in the current user's `Run` registry key.

### `RunOnStartupOptions`

- **`Enabled`** (`bool`)

---

## `Barbatos.Wpf.Notifications` Namespace

### `INotificationService`

- **`IsEnabled`** (`bool`), **`SetEnabled(bool)`** — `Show(...)` is a no-op while disabled.
- **`Show(string title, string message, NotificationSeverity severity = NotificationSeverity.Info)`** — plain text notification.
- **`Show(NotificationContent content)`** — rich notification: image, buttons, and a navigation payload.
- **`Activated`** (`event EventHandler<NotificationActivatedEventArgs>`) — raised when the user clicks the notification body or a button (unless that button used `LaunchUri`, which opens directly without raising this event).
- **`IsEnabledChanged`** event
- **`Availability`** (`NotificationAvailability`) — whether the OS currently allows this app to display notifications, read live from Windows on every access (not cached). `Show(...)` does not check this itself: Windows silently drops a toast it isn't allowed to show instead of raising an error, so this is the only way to detect it and offer an in-app fallback.
- **`OpenSystemSettings()`** — opens the Windows Settings page for notifications (`ms-settings:notifications`), so the user can act on what `Availability` reported.

Backed by `ToastContentBuilder`/`ToastNotificationManagerCompat` (Windows Community Toolkit),
rendering full adaptive Windows toast notifications (also appearing in the notification
center) for both packaged and non-packaged (plain Win32/WPF) apps, with no Start menu
shortcut or manual COM/AUMID registration required.

### `NotificationAvailability`

Mirrors `Windows.UI.Notifications.NotificationSetting`; returned by `INotificationService.Availability`.

- **`Enabled`** — notifications are allowed and will be displayed.
- **`DisabledForApplication`** — the user turned off notifications for this app specifically.
- **`DisabledForUser`** — the user turned off notifications system-wide (the main toggle in Settings > System > Notifications).
- **`DisabledByGroupPolicy`** — disabled by an administrator via Group Policy.
- **`DisabledByManifest`** — disabled by the app's manifest (packaged apps only; does not apply to Barbatos.Wpf).

### `NotificationContent`

- **`Title`** (`string`, required), **`Message`** (`string`, required)
- **`Severity`** (`NotificationSeverity`) — defaults to `Info`; `Error` marks the toast with
  the `Alarm` scenario (stays on screen until dismissed and bypasses Focus Assist), instead
  of the default scenario used for every other severity.
- **`ImagePath`** (`string?`) — a per-notification inline "hero" image.
- **`Arguments`** (`string?`) — opaque navigation payload returned via
  `NotificationActivatedEventArgs.Arguments` when the notification body is clicked.
- **`Buttons`** (`IList<NotificationButton>`) — up to five action buttons.

### `NotificationButton`

- **`NotificationButton(string text, string? arguments = null)`** — clicking raises
  `INotificationService.Activated` with `arguments`.
- **`NotificationButton(string text, Uri launchUri)`** — clicking opens `launchUri` directly
  (e.g. a website or custom protocol) without raising `Activated` or waking the app.
- **`Text`**, **`Arguments`**, **`LaunchUri`** (read-only properties mirroring the constructor used).

### `NotificationActivatedEventArgs`

- **`Title`**, **`Message`** — of the most recently shown notification.
- **`Arguments`** (`string?`) — the payload from `NotificationContent.Arguments` (body click)
  or `NotificationButton.Arguments` (button click), or `null` if none was set.

### `NotificationOptions`

- **`Enabled`** (`bool`)
- **`IconPath`** (`string?`) — a persistent circular app logo overlay shown on every
  notification; defaults to the executable's icon.
- **`Timeout`** (`TimeSpan`) — a hint (`Long` toast duration above ~7 seconds, `Short`
  otherwise); Windows may still manage actual visibility itself.

---

## `Barbatos.Wpf.SingleInstance` Namespace

Blocks duplicate launches of the app. Has no .NET MAUI counterpart (mobile platforms are
inherently single-instance).

### `ISingleInstanceService`

- **`IsPrimaryInstance`** (`bool`) — always `true` in practice: a non-primary process signals
  the primary instance and calls `Environment.Exit(0)` during `Build()`, before anything can
  observe this property being `false`.
- **`SecondInstanceLaunched`** (`event EventHandler?`) — raised on the primary instance, on
  the UI thread, when a second launch attempt is detected. Fires after the default
  window-activation behavior (if `SingleInstanceOptions.ActivateMainWindow` is `true`) has
  already run.

### `SingleInstanceOptions`

- **`Enabled`** (`bool`) — defaults to **`true`**, unlike every other optional feature.
- **`ActivateMainWindow`** (`bool`) — defaults to `true`. When a second launch is detected,
  restores (if minimized), shows, and brings `Application.Current.MainWindow` to the
  foreground. Set to `false` to only receive `SecondInstanceLaunched` and handle it yourself.

#### Remarks

Implemented with a named `Mutex` (identity — session-local, not `Global\`) and a named
`EventWaitHandle` (the wake signal), both keyed by `AppInfo.AppGuid`. See
[Optional desktop features](README.md#optional-desktop-features) in the README for the full
behavior description.

---

## `Barbatos.Wpf.Dialogs` Namespace

Centralizes showing and tracking child windows ("dialogs"). Has no .NET MAUI counterpart —
MAUI's page-based navigation model has no equivalent of WPF's multi-window/owner model. See
[Dialogs](README.md#dialogs) in the README for the full behavior description.

### `IDialogService`

- **`ActiveWindow`** (`Window?`) — the most recently activated window this service has seen
  (every dialog shown through it, plus `Application.MainWindow`, tracked opportunistically the
  first time it's observed), falling back to `Application.MainWindow` directly, falling back to
  `null` when the app has no windows yet. Used as the default owner when none is passed.
- **`ShowDialog<TWindow>(Window? owner = null, string? key = null, bool closeOthers = false)`**
  / **`ShowDialog(Window dialog, ...)`** → `bool?` — shows `TWindow` (resolved from the DI
  container) or an already-constructed window modally, blocking until closed. Returns the
  `DialogResult`; or `null` both in the normal WPF sense and when a dialog with the same `key`
  was already open, in which case that instance was activated instead.
- **`Show<TWindow>(Window? owner = null, string? key = null, bool closeOthers = false)`** /
  **`Show(Window dialog, ...)`** → `bool` — shows `TWindow`/`dialog` non-modally. `key` defaults
  to the window type's full name; if a dialog with that key is already tracked as open, that
  window is activated instead and this returns `false` — this is what makes a rapid
  double-click on a button that calls `Show` safe. Returns `true` if a new window was shown.
- **`IsOpen(string key)`** → `bool`, **`GetOpenDialog(string key)`** → `Window?`
- **`CloseAll()`** → `bool` — gracefully closes every tracked dialog; each still gets a chance
  to veto via its own `Closing` event, so this can return `false` (one or more vetoed) without
  having closed everything.
- **`Close(string key)`** → `bool` — gracefully closes the dialog tracked under `key`, if any.

### `DialogOptions`

- **`CascadeCloseOwnedDialogs`** (`bool`) — defaults to `true`. Plain WPF already closes a
  window's owned dialogs when it closes, but unconditionally, ignoring each owned dialog's own
  `Closing` veto. When enabled, this service closes a window's owned dialogs itself first,
  ahead of WPF's own cascade, so each one gets a real, respected veto — and if any refuse, the
  window's own close is cancelled too.
