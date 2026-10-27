# Barbatos.Wpf.Core API Reference

This document provides a comprehensive reference for the Barbatos.Wpf.Core library, modeled
after the official .NET API documentation.

## Namespaces

| Namespace | Description |
|-----------|-------------|
| **[`Barbatos.Wpf.Hosting`](#barbatoswpfhosting-namespace)** | The application host: `WpfApp`/`WpfAppBuilder`, initialization services, Essentials wiring, periodic services, the splash screen, and every optional feature's `ConfigureXxx` entry point. |
| **[`Barbatos.Wpf.LifecycleEvents`](#barbatoswpflifecycleevents-namespace)** | The lifecycle event system and its WPF-specific (`WpfLifecycle`) application/window delegates. |
| **[`Barbatos.Wpf.Dispatching`](#barbatoswpfdispatching-namespace)** | Dispatcher abstractions (`IDispatcher`, `IDispatcherTimer`, `IDispatcherProvider`) backed by the WPF `Dispatcher`. |
| **[`Barbatos.Wpf.ApplicationModel`](#barbatoswpfapplicationmodel-namespace)** | App info, publisher info, version tracking, app actions, the launcher, permissions, and the shared Essentials exception types. |
| **[`Barbatos.Wpf.ApplicationModel.Communication`](#barbatoswpfapplicationmodelcommunication-namespace)** | Email composition and contacts. |
| **[`Barbatos.Wpf.Devices`](#barbatoswpfdevices-namespace)** | Device info and device display. |
| **[`Barbatos.Wpf.Devices.Sensors`](#barbatoswpfdevicessensors-namespace)** | Geolocation. |
| **[`Barbatos.Wpf.Networking`](#barbatoswpfnetworking-namespace)** | Connectivity. |
| **[`Barbatos.Wpf.Storage`](#barbatoswpfstorage-namespace)** | File system, preferences, and secure storage. |
| **[`Barbatos.Wpf.Tray`](#barbatoswpftray-namespace)** | The system tray icon optional feature. |
| **[`Barbatos.Wpf.Power`](#barbatoswpfpower-namespace)** | The keep-awake optional feature. |
| **[`Barbatos.Wpf.Startup`](#barbatoswpfstartup-namespace)** | The run-on-startup optional feature. |
| **[`Barbatos.Wpf.Input`](#barbatoswpfinput-namespace)** | The keyboard-shortcut optional feature: in-app `KeyBinding` and OS-level `GlobalKeyBinding` unified under one `InputAction` model, plus Hold/Tap/MultiTap/Press interactions. |
| **[`Barbatos.Wpf.Notifications`](#barbatoswpfnotifications-namespace)** | The desktop (Windows toast) notifications optional feature. |
| **[`Barbatos.Wpf.PushNotifications`](#barbatoswpfpushnotifications-namespace)** | The realtime push-notification client optional feature (SignalR). |
| **[`Barbatos.Wpf.SingleInstance`](#barbatoswpfsingleinstance-namespace)** | The single-instance optional feature (enabled by default). |
| **[`Barbatos.Wpf.Dialogs`](#barbatoswpfdialogs-namespace)** | The dialog service: owner assignment, duplicate-open prevention, and graceful bulk-close for child windows. |
| **[`Barbatos.Wpf.Apsu`](#barbatoswpfapsu-namespace)** | State management: stores, getters, plugins, and a middleware-style action interceptor chain. |
| **[`Barbatos.Wpf.Mcp`](#barbatoswpfmcp-namespace)** | The MCP (Model Context Protocol) client + bring-your-own-key AI chat/agent optional feature. |

---

## `Barbatos.Wpf.Hosting` Namespace

The application host, its builder, and the extension methods that wire up Essentials and
every optional desktop feature.

### Classes

| Class | Description |
|-------|-------------|
| [`WpfApp`](#wpfapp-class) | A built application: configured services and configuration. |
| [`WpfAppBuilder`](#wpfappbuilder-class) | Builds a `WpfApp`; implements `IHostApplicationBuilder`. |
| [`WpfHostEnvironment`](#wpfhostenvironment-class) | The WPF `IHostEnvironment` implementation. |
| `WpfApplication` | A `System.Windows.Application` subclass that owns the built `WpfApp` and forwards lifecycle events. See [splash screen support](#wpfapplication-splash-screen-support) below. |
| `AppHostBuilderExtensions` | Static (partial) class holding every feature's `ConfigureXxx(this WpfAppBuilder, ...)` entry point — listed under Extension Methods below. |
| `EssentialsExtensions` | `UseEssentials()` / `ConfigureEssentials(...)` — registers every Essentials service and app-action activation detection. |
| `WpfAppExtensions` | `InitializeAppServices(this WpfApp)` runs every `IWpfInitializeService` against the root provider (`Build()` calls it); `InitializeScopedServices(this IServiceProvider)` runs every `IWpfInitializeScopedService` against a window's scoped provider (`WpfApplication` calls it). |
| `PeriodicServiceOptions` | Options for `ConfigurePeriodicServices` (`Enabled`, `Schedules`). |
| [`PeriodicSchedule`](#periodicschedule-class) | When and how often a periodic service runs: `Frequency`, `StartTime`, `EndTime`, `TimeOfDay`, `DaysOfWeek`, `DayOfMonth`, `Interval`, `Description`. |
| `PeriodicServiceStatus` | Live status of a registered periodic service (`Name`, `Schedule`, `NextRunTime`, `IsCompleted`, `LastRunTime`, `RunCount`). |
| `PeriodicServiceExecutedEventArgs` | Raised by `IPeriodicServiceScheduler.ServiceExecuted` after each run. |
| [`SplashScreenOptions`](#wpfapplication-splash-screen-support) | Configures the built-in `SplashWindow` (app name, logo, tagline, sponsor logos, related links, minimum display duration). |
| `SplashScreenLogo` | A single sponsor/developer logo entry in `SplashScreenOptions.SponsorLogos`. |
| `SplashScreenLink` | A single related-item entry in `SplashScreenOptions.RelatedLinks`, e.g. another product from the same publisher. |
| [`SplashWindow`](#wpfapplication-splash-screen-support) | The built-in splash screen window, populated from `SplashScreenOptions`. |

### Interfaces

| Interface | Description |
|-----------|-------------|
| `IWpfInitializeService` | Runs once during `WpfAppBuilder.Build()`. |
| `IWpfInitializeScopedService` | Runs once per window, on its first `Loaded`, against that window's private scope. |
| `IWpfPlatformApplication` | Exposes the running `WpfApp` from `System.Windows.Application`. |
| `IEssentialsBuilder` | Configures app actions and version tracking from `ConfigureEssentials`. |
| [`IWpfPeriodicService`](#iwpfperiodicservice-interface) | A service executed on a recurring schedule. |
| [`IPeriodicServiceScheduler`](#iperiodicservicescheduler-interface) | Runtime control over registered periodic services. |
| [`ISplashScreen`](#wpfapplication-splash-screen-support) | Implemented by a splash screen window to report `MinimumDisplayDuration`. |

### Extension Methods

All on `AppHostBuilderExtensions` unless noted; each returns the builder, for chaining.

- **`ConfigureDialogs(this WpfAppBuilder, Action<DialogOptions>? configure = null)`** — registers `IDialogService` (`Barbatos.Wpf.Dialogs`).
- **`ConfigureSingleInstance(this WpfAppBuilder, Action<SingleInstanceOptions>? configure = null)`** — registers `ISingleInstanceService` (`Barbatos.Wpf.SingleInstance`); enabled by default the moment this is called.
- **`ConfigureRunOnStartup(this WpfAppBuilder, Action<RunOnStartupOptions>? configure = null)`** — registers `IRunOnStartupService` (`Barbatos.Wpf.Startup`).
- **`ConfigureTrayIcon(this WpfAppBuilder, Action<TrayIconOptions>? configure = null)`** — registers `ITrayIconService` (`Barbatos.Wpf.Tray`).
- **`ConfigureKeepAwake(this WpfAppBuilder, Action<KeepAwakeOptions>? configure = null)`** — registers `IKeepAwakeService` (`Barbatos.Wpf.Power`).
- **`ConfigureNotifications(this WpfAppBuilder, Action<NotificationOptions>? configure = null)`** — registers `INotificationService` (`Barbatos.Wpf.Notifications`), the desktop (toast) notifications feature.
- **`ConfigurePushNotifications(this WpfAppBuilder, Action<PushNotificationOptions>? configure = null, Action<SignalRPushNotificationOptions>? configureSignalR = null)`** / **`ConfigurePushNotifications<TNotification>(...)`** (same parameters; `TNotification : class, IPushNotification`) — registers `IPushNotificationService` (`Barbatos.Wpf.PushNotifications`) and calls `ConfigureNotifications()` itself; the generic overload uses `TNotification` as the payload shape instead of the default `PushNotification`. `configureSignalR` only affects the default SignalR transport.
- **`ConfigurePeriodicServices(this WpfAppBuilder, Action<PeriodicServiceOptions>? configure = null)`** / **`ConfigurePeriodicServices<TService>(this WpfAppBuilder, Action<PeriodicServiceOptions>? configure = null)`** (`TService : class, IWpfPeriodicService`) — registers `IPeriodicServiceScheduler` and (for the generic overload) `TService` as a singleton `IWpfPeriodicService`.
- **`ConfigureInputSystem(this WpfAppBuilder, Action<InputSystemOptions>? configure = null)`** — registers `IInputSystemService` (`Barbatos.Wpf.Input`).
- **`ConfigureMcp(this WpfAppBuilder, Action<McpOptions>? configure = null, Action<AiProviderOptions>? configureProvider = null, Action<AiAgentOptions>? configureAgent = null)`** — registers the MCP client and AI services; see [`Barbatos.Wpf.Mcp`](#barbatoswpfmcp-namespace).
- **`ConfigureDispatching(this WpfAppBuilder)`** — registers the dispatching services; part of the builder defaults. See [`Barbatos.Wpf.Dispatching`](#barbatoswpfdispatching-namespace).
- **`ConfigureLifecycleEvents(this WpfAppBuilder, Action<ILifecycleBuilder>? configureDelegate)`** — declared on `WpfAppHostBuilderExtensions` in the `Barbatos.Wpf.LifecycleEvents` namespace, not here; see [`Barbatos.Wpf.LifecycleEvents`](#barbatoswpflifecycleevents-namespace).
- **`UseEssentials(this WpfAppBuilder)`** (on `EssentialsExtensions`) — registers `IAppInfo`, `IPublisherInfo`, `IDeviceIdentity`, `IDeviceInfo`, `IFileSystem`, `IPreferences`, `ISecureStorage`, `IVersionTracking`, `IConnectivity`, `IDeviceDisplay`, `IEmail`, `IContacts`, `IGeolocation`, `IAppActions`, `ILauncher`; called by default when the builder is created with defaults.
- **`ConfigureEssentials(this WpfAppBuilder, Action<IEssentialsBuilder>? = null)`** (on `EssentialsExtensions`) — configures app actions (`AddAppAction`, `OnAppAction`) and `UseVersionTracking()`.

Each `Configure...` feature method binds its options section (file values override code
values) from the `Barbatos:{Feature}` configuration section — see
[Optional desktop features](README.md#optional-desktop-features) in the README. Every
platform seam (`ITrayIconPlatform`, `INotificationPlatform`, `IHotkeyPlatform`,
`IStartupRegistrar`, `IPowerManager`, `IPushNotificationTransport`,
`IPushNotificationFallbackPresenter`) is registered with `TryAddSingleton`, so an
implementation registered *before* the `Configure...` call replaces the OS-backed default.
These features report their failures only through `ILogger`; register a logging provider to
see them.

---

### `WpfApp` Class

A built application: configured services and configuration.

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

Builds a `WpfApp`. Implements `IHostApplicationBuilder`.

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

The WPF `IHostEnvironment` implementation (analogous to ASP.NET Core's `HostingEnvironment`).

```csharp
public class WpfHostEnvironment : IHostEnvironment
```

#### Properties

- **`EnvironmentName`** (`string`, settable): defaults to `Environments.Production`. `WpfAppBuilder`
  creates the environment lazily and reads `HostDefaults.EnvironmentKey` from its configuration
  once, at that moment — the first access of `WpfAppBuilder.Environment`, or (if the builder never
  touches it) the first resolution of `IHostEnvironment` from the container. The `DOTNET_`-prefixed
  environment variables source feeds that key at the lowest priority; a configuration source that
  sets it overrides the variable only when added before that first read. Assignable directly in
  code at any time. See [Configuring the hosting environment](README.md#configuring-the-hosting-environment).
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
    PeriodicSchedule Schedule { get; }
    Task ExecuteAsync(IServiceProvider services, CancellationToken cancellationToken);
}
```

`Schedule` is read once, when the service is registered (via DI at `ConfigurePeriodicServices`
time, or via `IPeriodicServiceScheduler.Register` at any later time) — call `UpdateSchedule` to
change what's active afterwards.

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
- **`UpdateSchedule(string name, PeriodicSchedule schedule)`** — reschedules a running service immediately (throws `ArgumentException` for an unknown name, `InvalidOperationException` for an invalid schedule).
- **`Register(IWpfPeriodicService service)`** — adds a service at runtime, after the host has already been built (throws `ArgumentException` for a duplicate name). Arms it immediately if the scheduler is currently enabled. The service's `ExecuteAsync` is still ordinary developer-written code — this just removes the "only at host-build time" restriction on when it can be wired up.
- **`Unregister(string name)`** (`bool`) — removes a service, stopping it if running; returns whether one was found. An execution already in flight finishes but is not rescheduled afterwards.

#### Events
- **`IsEnabledChanged`**
- **`ServiceExecuted`** (`PeriodicServiceExecutedEventArgs`) — raised after every run, including failed ones.

---

### `PeriodicSchedule` Class

When and how often a periodic service runs. `Daily`, `Weekly` and `Monthly` are
calendar-anchored (a wall-clock time of day, specific days of the week, or a specific day of the
month — the same way a calendar reminder or a Windows Task Scheduler trigger would); `Hourly`
and `Custom` are plain fixed-duration repeats.

```csharp
public sealed class PeriodicSchedule
```

#### Properties
- **`Frequency`** (`PeriodicFrequency`) — `Once`, `Hourly`, `Daily`, `Weekly`, `Monthly`, or `Custom`. Defaults to `Once`.
- **`StartTime`** (`DateTimeOffset?`) — no occurrence before this time; `null` means occurrences are computed relative to "now" instead.
- **`EndTime`** (`DateTimeOffset?`) — no occurrence after this time (inclusive); `null` means it never expires.
- **`TimeOfDay`** (`TimeSpan?`) — wall-clock time of day; used by `Daily`/`Weekly`/`Monthly`. Defaults to `StartTime`'s (or "now"'s) time of day.
- **`DaysOfWeek`** (`WeekDays`) — used by `Weekly`. `WeekDays.None` (the default) defaults to `StartTime`'s (or "now"'s) day of week.
- **`DayOfMonth`** (`int?`, 1-31) — used by `Monthly`; clamped to the last day of shorter months. Defaults to `StartTime`'s (or "now"'s) day.
- **`Interval`** (`TimeSpan?`) — required and must be positive when `Frequency` is `Custom`; ignored otherwise.
- **`Description`** (`string?`) — human-readable, for display in a settings UI.

#### Methods
- **`Validate()`** — throws `InvalidOperationException` if the schedule isn't internally consistent (a `Custom` schedule without a positive `Interval`, an out-of-range `DayOfMonth`, or `StartTime` later than `EndTime`).
- **`GetNextOccurrence(DateTimeOffset now, DateTimeOffset? lastRun)`** (`DateTimeOffset?`) — a pure function (never reads the system clock itself) that computes the next time the schedule should run, or `null` if it has none left. Missed occurrences (the schedule was disabled, or the app was closed, across several periods) are caught up to a single next occurrence rather than replayed as a backlog. Useful on its own to preview a schedule, e.g. from a settings UI, before saving it.
- **`Clone()`** (`PeriodicSchedule`) — an independent copy.

#### `PeriodicFrequency`
`Once`, `Hourly`, `Daily`, `Weekly`, `Monthly`, `Custom` — see `Frequency` above.

#### `WeekDays`
A `[Flags]` enum (unlike `DayOfWeek`, more than one day can be combined):
`None`, `Monday`, `Tuesday`, `Wednesday`, `Thursday`, `Friday`, `Saturday`, `Sunday`, plus the
combinations `Workdays` (Monday-Friday), `Weekend` (Saturday-Sunday), and `All`.

---

### `WpfApplication` splash screen support

This is fresh work, not a port: on Windows, a splash screen for a packaged app is just a
build-time asset pipeline with no runtime C# logic at all, and it doesn't apply to unpackaged
apps in the first place. See [SplashScreen](README.md#splashscreen) in the README for the
full behavior description.

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

The lifecycle event system: application and window events surfaced as typed delegates.

### Classes / Interfaces

| Type | Description |
|------|-------------|
| `ILifecycleEventService` | The registered delegates, by event name: **`GetEventDelegates<TDelegate>(string eventName)`** (`IEnumerable<TDelegate>`, in registration order, empty when none) and **`ContainsEvent(string eventName)`** (`bool`). Registered as a singleton by `ConfigureLifecycleEvents`. |
| `ILifecycleBuilder` | Registers delegates during `ConfigureLifecycleEvents`: **`AddEvent<TDelegate>(string eventName, TDelegate action)`** (`where TDelegate : Delegate`). |
| `LifecycleEventService` | The default implementation of both interfaces; its constructor takes the `IEnumerable<LifecycleEventRegistration>` that `ConfigureLifecycleEvents` registered and applies them in order. |
| `LifecycleEventRegistration` | One `ConfigureLifecycleEvents` call's delegate (`new LifecycleEventRegistration(Action<ILifecycleBuilder>)`), registered as a singleton per call. |
| `WpfLifecycle` | Static class of delegate types: `OnStartup`, `OnActivated`, `OnDeactivated`, `OnSessionEnding`, `OnDispatcherUnhandledException`, `OnExit`, `OnWindowCreated`, `OnWindowLoaded`, `OnWindowActivated`, `OnWindowDeactivated`, `OnWindowStateChanged`, `OnWindowClosing`, `OnWindowClosed`. A built-in event's name is its delegate type's name. |
| `IWpfLifecycleBuilder` | The fluent builder passed to `AddWpf(wpf => wpf.OnStartup(...)...)`. |

### Extension Methods

- **`WpfAppHostBuilderExtensions.ConfigureLifecycleEvents(this WpfAppBuilder, Action<ILifecycleBuilder>? configureDelegate)`** —
  registers `ILifecycleEventService` once and adds `configureDelegate`'s delegates; every call's
  delegates are kept, in call order. `null` only ensures the service is registered (the builder
  defaults do this).
- **`WpfLifecycleExtensions.AddWpf(this ILifecycleBuilder, Action<IWpfLifecycleBuilder> configureDelegate)`** (`ILifecycleBuilder`)
- **`WpfLifecycleBuilderExtensions.OnStartup(this IWpfLifecycleBuilder, WpfLifecycle.OnStartup)`** … one
  method per `WpfLifecycle` delegate (`OnExit`, `OnActivated`, … `OnWindowClosed`), each returning the
  `IWpfLifecycleBuilder` for chaining.
- **`LifecycleBuilderExtensions.AddEvent(this ILifecycleBuilder, string eventName, Action action)`** /
  **`AddEvent<TDelegate>(this ILifecycleBuilder, string eventName, TDelegate action)`** (`where TDelegate : Delegate`) —
  chainable forms of `ILifecycleBuilder.AddEvent`, e.g. for an app-defined, non-`WpfLifecycle` event.
- **`LifecycleEventServiceExtensions.InvokeEvents(this ILifecycleEventService, string eventName)`** — invokes every
  parameterless `Action` registered under `eventName`, in registration order.
- **`LifecycleEventServiceExtensions.InvokeEvents<TDelegate>(this ILifecycleEventService, string eventName, Action<TDelegate> action)`** —
  passes every `TDelegate` registered under `eventName` to `action`, which decides the arguments.

#### Remarks

Window events fire for every `Window` in the application, dialogs included; `OnWindowCreated`
fires on a window's first `Loaded`, not in its constructor. At that first `Loaded`,
`WpfApplication` also creates a private DI scope for the window, used only to run the
`IWpfInitializeScopedService` implementations (and resolve the window's scoped `IDispatcher`)
and disposed when the window closes; no public API resolves from it.

---

## `Barbatos.Wpf.Dispatching` Namespace

Dispatcher abstractions backed by the WPF `Dispatcher`.

### Classes

| Class | Description |
|-------|-------------|
| `Dispatcher` | The `IDispatcher` over a WPF `System.Windows.Threading.Dispatcher` (no public constructor). **`static GetForCurrentThread()`** (`IDispatcher?`) forwards to `DispatcherProvider.Current`. `Dispatch`/`DispatchDelayed` return `false` once the WPF dispatcher has started shutting down. |
| `DispatcherTimer` | The `IDispatcherTimer` over a WPF `System.Windows.Threading.DispatcherTimer`, which its public constructor takes. `IsRepeating` defaults to `true`; `false` stops the timer after its first `Tick`. |
| `DispatcherProvider` | The default `IDispatcherProvider`. **`static Current`** (`IDispatcherProvider`, created on first use), **`static SetCurrent(IDispatcherProvider?)`** (`bool` — `true` only when it replaced an already-set provider), and **`GetForCurrentThread()`**, which creates and caches (per thread) a `Dispatcher` for `System.Windows.Threading.Dispatcher.CurrentDispatcher` — so calling it on a worker thread binds a new dispatcher to that thread, one nothing pumps unless that thread runs a dispatcher loop. |

### Interfaces

| Interface | Description |
|-----------|-------------|
| `IDispatcher` | `Dispatch(Action)`, `DispatchDelayed(TimeSpan, Action)` (both return `false` when the work could not be queued), `IsDispatchRequired`, `CreateTimer()`. |
| `IDispatcherTimer` | `Interval`, `IsRepeating`, `Start()`, `Stop()`, `Tick` event. |
| `IDispatcherProvider` | Resolves the current `IDispatcher` (registered app-wide as singleton, and per-window as scoped). |

### Extension Methods (`DispatcherExtensions`)

| Method | Returns | Meaning |
|--------|---------|---------|
| `DispatchAsync(this IDispatcher, Action)` | `Task` | Queues the action; the task completes when it has run, or faults with what it threw. |
| `DispatchAsync<T>(this IDispatcher, Func<T>)` | `Task<T>` | Same, completing with the function's result. |
| `DispatchAsync(this IDispatcher, Func<Task>)` | `Task` | Starts async work on the dispatcher; the returned task is unwrapped, so it tracks the inner task. |
| `DispatchAsync<T>(this IDispatcher, Func<Task<T>>)` | `Task<T>` | Same, for a result-returning inner task. |
| `DispatchIfRequiredAsync(this IDispatcher, Action)` | `Task` | Runs inline (returning `Task.CompletedTask`) when `IsDispatchRequired` is `false`, otherwise `DispatchAsync`. |
| `DispatchIfRequiredAsync(this IDispatcher, Func<Task>)` | `Task` | Starts the work inline when `IsDispatchRequired` is `false`, otherwise `DispatchAsync`. |

Every `DispatchAsync` overload faults its task with `InvalidOperationException` when
`IDispatcher.Dispatch` returns `false`, and throws `ArgumentNullException` for a `null`
dispatcher or delegate.

### Extension Methods (on `WpfAppBuilder`)

- **`ConfigureDispatching(this WpfAppBuilder)`** (on `Barbatos.Wpf.Hosting.AppHostBuilderExtensions`) — registers `IDispatcherProvider` (a singleton: `DispatcherProvider.Current`), an application dispatcher bound during `Build()` to the thread that builds the host (the UI thread in a `WpfApplication`), and a scoped `IDispatcher` resolved per window; part of the builder defaults. Core's own features — periodic services, input-system timeouts, keep awake, single-instance activation, toast activation — run their UI-thread work on that application dispatcher.

---

## `Barbatos.Wpf.ApplicationModel` Namespace

App info, publisher info, version tracking, app actions, the launcher, and the shared
Essentials exception types.

### Classes / Interfaces

| Type | Description |
|------|-------------|
| [`AppInfo`](#appinfo--publisherinfo) / `IAppInfo` | App identity: `AppId`, `Name`, `Version`, `VersionString`, `BuildString`, `RequestedTheme`, `PackagingModel`, `RequestedLayoutDirection`, `InstallDate`, `InstallLocation`, `ShowSettingsUI()`. (`AppGuid` is the deprecated former name of `AppId`.) |
| [`PublisherInfo`](#appinfo--publisherinfo) / `IPublisherInfo` | Publisher identity: `Name`, `Website`, `SupportUrl`, `SupportEmail`, `Copyright`. |
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
| [`Permissions`](#permissions) | Static generic API: `CheckStatusAsync<TPermission>()`, `RequestAsync<TPermission>()`, `ShouldShowRationale<TPermission>()`. No DI interface. |
| `PermissionStatus` | `Unknown`, `Denied`, `Disabled`, `Granted`, `Restricted`, `Limited`. |
| `Permissions.BasePermission` | Abstract base for every permission type; `Permissions.BasePlatformPermission` is the WPF platform base (`CheckStatusAsync`, `RequestAsync`, `EnsureDeclared`, `ShouldShowRationale`). |
| `Permissions.Battery` / `Bluetooth` / `CalendarRead` / `CalendarWrite` / `Camera` / `ContactsRead` / `ContactsWrite` / `Flashlight` / `LaunchApp` / `LocationWhenInUse` / `LocationAlways` / `Maps` / `Media` / `Microphone` / `NearbyWifiDevices` / `NetworkState` / `Phone` / `Photos` / `PhotosAddOnly` / `PostNotifications` / `Reminders` / `Sensors` / `Sms` / `Speech` / `StorageRead` / `StorageWrite` / `Vibrate` | Nested marker permission types — see [Permissions](#permissions). |
| `PermissionException` | Thrown for APIs requiring a permission (`UnauthorizedAccessException` subclass). |
| `FeatureNotSupportedException` | Thrown by a feature unsupported on this platform (`NotSupportedException` subclass) — used by `Contacts`, `Geolocation`, six of the `Permissions` types, and `Email.ComposeAsync` (HTML body, or no Simple MAPI mail client). |
| `FeatureNotEnabledException` | Thrown by a feature that exists but is not currently enabled (`InvalidOperationException` subclass). |

### `AppInfo` / `PublisherInfo`

Both follow the interface + static facade pattern (`AppInfo.Current` / `PublisherInfo.Current`).
Their properties resolve from the standard SDK-generated assembly attributes — `<Product>`,
`<Company>`, `<Version>`, `<Copyright>` in the csproj — with an explicit
`Barbatos.Wpf.ApplicationModel.*` assembly-metadata override checked first for anything that
needs a different value (or, for `AppId`, has no standard property at all). See
[Configuring AppInfo and PublisherInfo](README.md#configuring-appinfo-and-publisherinfo) in
the README for the full property table and fallback chain.

`AppInfo.Version` (`Version`) is the `...AppInfo.Version` metadata parsed with
`Version.Parse` (a non-numeric value throws `FormatException` on every read), else the entry
assembly's `AssemblyName.Version` — always four parts, so `VersionString` reads `"1.2.3.0"`
for `<Version>1.2.3</Version>` — else `0.0`. The SDK derives `AssemblyVersion` from
`<Version>` with any prerelease suffix stripped (`1.3.0-beta.1` → `1.3.0.0`), while an explicit
`<AssemblyVersion>$(Version)</AssemblyVersion>` with a prerelease `<Version>` fails the build
with `CS7034`. `BuildString` (`string`) is `Version.Revision`, the fourth part.

`AppInfo.AppId` (`string`) is the app's stable identifier; it does *not* have to be a GUID —
any stable string works. It was previously called `AppGuid`; `AppInfo.AppGuid` and
`IAppInfo.AppGuid` still exist, still return the identical value, and are marked `[Obsolete]`
pending removal in a future release. The `Barbatos.Wpf.ApplicationModel.AppInfo.AppGuid`
metadata key likewise still works as a fallback for the newer `...AppInfo.AppId` key.

`AppInfo.InstallDate` (`DateTime?`) and `AppInfo.InstallLocation` (`string?`) read the
Windows "Programs and Features" uninstall registry entry `...\Uninstall\{AppId}` — the name
used verbatim as the subkey name, GUID-shaped values normalised to the braced form — searching
`HKEY_LOCAL_MACHINE` and `HKEY_CURRENT_USER` in both the 64-bit and 32-bit registry view. Both
are `null` when no entry of that name is found (e.g. running unpublished). No suffix is ever
appended or guessed: when your installer registers itself under a different name (Inno Setup
always appends `_is1`), set the `Barbatos.Wpf.ApplicationModel.AppInfo.UninstallRegistryKey`
assembly metadata to the name it actually creates — it defaults to `AppId`. See
[Reading back InstallDate and InstallLocation](README.md#reading-back-installdate-and-installlocation)
in the README.

`FileSystem.AppDataDirectory`/`CacheDirectory` are derived from
`PublisherInfo.Name`/`AppInfo.AppId` — see [`Barbatos.Wpf.Storage`](#barbatoswpfstorage-namespace).

### `DeviceIdentity` / `IDeviceIdentity`

License-enforcement identifiers. Has no .NET MAUI counterpart — see
[License enforcement: DeviceIdentity](README.md#license-enforcement-deviceidentity) in the
README for the full rationale and privacy/compliance notes (not legal advice).

- **`GetInstanceIdAsync()`** → `Task<string>` — a random GUID generated on first use and
  persisted via `SecureStorage.Default` under the key `Barbatos.Wpf.DeviceIdentity.InstanceId`,
  i.e. in `%LocalAppData%\{PublisherInfo.Name}\{AppInfo.AppId}\Settings\securestorage.dat`.
  Identifies this app's data for this Windows user, not the physical machine. Changes when that
  file is lost (deleted, a new user profile or machine), on `SecureStorage.RemoveAll()`, or when
  `AppId` or `PublisherInfo.Name` changes; an ordinary uninstall/reinstall leaves the file, and
  the id, in place. Concurrent calls within a process are serialized, so first calls racing
  each other all receive the same id.
- **`GetHardwareFingerprintAsync()`** → `Task<string>` — a 64-character uppercase hex SHA-256
  hash of a few motherboard/BIOS/CPU identifiers (read via WMI: `Win32_BaseBoard.SerialNumber`,
  `Win32_BIOS.SerialNumber`, `Win32_Processor.ProcessorId`), salted with `AppInfo.AppId` so
  the result is scoped to this app rather than directly comparable across unrelated apps on
  the same machine. The raw hardware serials are never stored or returned — only the hash.
  Falls back to hashing `Environment.MachineName` alone when no WMI identifier is readable
  (e.g. a locked-down environment). Cached for the lifetime of the process.

### `VersionTracking` / `IVersionTracking`

Pure C#, built entirely on `IPreferences` + `IAppInfo`, no platform-specific code.

The launch is recorded when the implementation is constructed — on the first access to
`VersionTracking.Default` or the first resolve of `IVersionTracking` — because its constructor
calls `Track()`. `ConfigureEssentials(e => e.UseVersionTracking())` forces that construction
during `Build()`, so a run that never reads version tracking is still recorded. The history
lives in `Preferences` (see [`Barbatos.Wpf.Storage`](#barbatoswpfstorage-namespace)).

- **`Track()`** — idempotent: a no-op once the launch has been recorded, which construction
  already did. Kept for MAUI parity.
- **`IsFirstLaunchEver`**, **`IsFirstLaunchForCurrentVersion`**, **`IsFirstLaunchForCurrentBuild`** (`bool`) — computed once per process.
- **`CurrentVersion`**, **`CurrentBuild`** (`string`) — `AppInfo.VersionString` / `AppInfo.BuildString`.
- **`PreviousVersion`**, **`PreviousBuild`**, **`FirstInstalledVersion`**, **`FirstInstalledBuild`** (`string?`) — `null` when there is no such entry.
- **`VersionHistory`**, **`BuildHistory`** — `IReadOnlyList<string>` on `IVersionTracking`; `IEnumerable<string>` on the static `VersionTracking` facade.
- **`IsFirstLaunchForVersion(string)`**, **`IsFirstLaunchForBuild(string)`** → `bool`

### `AppActions` / `IAppActions`

Windows implementation backed by `System.Windows.Shell.JumpList` (the taskbar right-click
menu) instead of WinRT's `Windows.UI.StartScreen.JumpList`. See
[App actions and version tracking](README.md#app-actions-and-version-tracking) in the README.

- **`IsSupported`** (`bool`) — always `true`.
- **`GetAsync()`** → `Task<IEnumerable<AppAction>>` — returns the actions last set by `SetAsync` in this process (no OS read-back API exists for `JumpList`).
- **`SetAsync(IEnumerable<AppAction>)`** / **`SetAsync(params AppAction[])`** — replaces the Jump List (hiding the Recent/Frequent categories) on `Application.Current`, so it needs a running WPF `Application`. Only actions set in a process before its startup event are recognised when clicked: an action added only at runtime shows in the Jump List but raises nothing after a restart unless the next launch declares it again (`ConfigureEssentials(e => e.AddAppAction(...))` does, during `Build()`).
- **`OnAppAction`** (`event EventHandler<AppActionEventArgs>?`; `IAppActions.AppActionActivated` on the interface) — raised once, from the relaunched process's `WpfLifecycle.OnStartup` event, which runs inside `WpfApplication.OnStartup` before any main window exists. A handler added after `base.OnStartup(e)` never sees it. Under `ConfigureSingleInstance()` with an instance already running, the relaunched process exits inside `Build()` and the running instance receives only `ISingleInstanceService.SecondInstanceLaunched` (no arguments): the activation is not reported.

### `Launcher` / `ILauncher`

Windows implementation backed by `Process.Start(UseShellExecute: true)` instead of WinRT's
`Windows.System.Launcher`.

- **`CanOpenAsync(Uri | string)`** → `Task<bool>` — looks up the URI's scheme under `HKEY_CLASSES_ROOT` for a registered `"URL Protocol"` value or a `shell\open\command` subkey.
- **`OpenAsync(Uri | string)`** → `Task<bool>` — `true` when `Process.Start` did not throw, including when the shell handed the URI to an already-running or COM-activated handler and returned no `Process`; `false` when the shell refused it (any exception is caught). The `string` overload builds a `Uri`, so a relative path throws `UriFormatException`.
- **`OpenAsync(OpenFileRequest)`** → `Task<bool>` — opens a file by full path with its associated application; same `true`/`false` rule (`false` for a missing file). Throws `ArgumentNullException` for a `null` request or an empty `FullPath`.
- **`TryOpenAsync(Uri | string)`** → `Task<bool>` — `CanOpenAsync` then `OpenAsync`.
- **`LauncherExtensions`** — the `string` overloads of `CanOpenAsync`, `OpenAsync` and
  `TryOpenAsync` for an injected `ILauncher`, which itself takes only `Uri`; each builds the
  `Uri` the same way, so a relative path throws `UriFormatException`.

```csharp
await Launcher.OpenAsync("https://example.com");
await Launcher.OpenAsync(new OpenFileRequest("Open report", @"C:\Reports\q1.pdf"));
```

### `Permissions`

A static, generic, DI-free surface — there is no `IPermissions` service to register. See
[Permissions](README.md#permissions) in the README for the full design rationale.

- **`CheckStatusAsync<TPermission>()`**, **`RequestAsync<TPermission>()`** → `Task<PermissionStatus>`
- **`ShouldShowRationale<TPermission>()`** → `bool` — always `false` on WPF (a concept specific to Android's runtime permission model)
- `TPermission` is any nested `Permissions.*` type (`Permissions.Camera`, `Permissions.Microphone`, ...), each `: Permissions.BasePlatformPermission, new()`

```csharp
var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
if (status != PermissionStatus.Granted)
    status = await Permissions.RequestAsync<Permissions.Camera>();
```

Most permission types report `PermissionStatus.Granted` unconditionally, without consulting the
OS (no manifest/capability concept or consent prompt for an unpackaged app); `RequestAsync`
returns what `CheckStatusAsync` does and `EnsureDeclared()` is a no-op. `Granted` therefore
means "not checked", not "Windows allows it": the Windows privacy settings can still block a
desktop app's camera, and `PostNotifications` is `Granted` even when toasts are turned off
(use `INotificationService.Availability`). Override `CheckStatusAsync()` in a subclass of
`Permissions.BasePlatformPermission` for a real check. `ContactsRead`, `ContactsWrite`,
`LocationWhenInUse`, `LocationAlways`, `Microphone`, and `Sensors` throw
`FeatureNotSupportedException` instead, since a real check for these six needs WinRT
device-access contracts this library opts out of (same reasoning as
[`Contacts`/`Geolocation`](#barbatoswpfapplicationmodelcommunication-namespace)).

---

## `Barbatos.Wpf.ApplicationModel.Communication` Namespace

Email composition and contacts.

### Classes / Interfaces

| Type | Description |
|------|-------------|
| `Email` / `IEmail` | `IsComposeSupported` (always `true`: Simple MAPI is the mechanism, not proof a client is installed), `ComposeAsync(EmailMessage?)` — backed by Simple MAPI's Unicode `MAPISendMailW`, on a thread-pool thread. Completes once the compose window is open (or, for a modal-only client, sent or cancelled); a user cancelling is not an error. Throws `FeatureNotSupportedException` for an HTML body or when no Simple MAPI client answers (none installed or set as the default, logon failure), `FileNotFoundException` for an attachment that does not exist (checked before any client starts), and `InvalidOperationException` carrying the MAPI code for any other MAPI error. |
| `EmailMessage` | `Subject`, `Body`, `BodyFormat`, `To`, `Cc`, `Bcc`, `Attachments`. |
| `EmailBodyFormat` | `PlainText`, `Html` (HTML throws — Simple MAPI is plain-text only). |
| `EmailAttachment` | `FullPath` — the file to attach, referenced by path. |
| `EmailExtensions` | On an injected `IEmail`: `ComposeAsync()` (an empty compose window, the same as `ComposeAsync(null)`) and `ComposeAsync(string subject, string body, params string[] to)`. Both fail exactly as `ComposeAsync(EmailMessage?)` does. |
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

- **`KeepScreenOn`** (`bool`, settable from any thread) — a `SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED)` request held by a dedicated background thread while `true`; the setter returns once the request is in force. Because the request is not made on the caller's thread, it combines with `IKeepAwakeService` (which holds `ES_SYSTEM_REQUIRED` on the UI thread) and with the app's own requests instead of replacing them, and turning it off releases only the display request.
- **`MainDisplayInfo`** (`DisplayInfo`) — `Width`, `Height`, `Density`, `Orientation`, `Rotation`, `RefreshRate`; computed against the active WPF window via a Win32 monitor-info P/Invoke block.
  - `Orientation` is a `DisplayOrientation`: `Unknown`, `Portrait`, `Landscape`.
  - `Rotation` is a `DisplayRotation`: `Unknown`, `Rotation0`, `Rotation90`, `Rotation180`, `Rotation270`.
- **`MainDisplayInfoChanged`** (`event EventHandler<DisplayInfoChangedEventArgs>`) — backed by `Microsoft.Win32.SystemEvents.DisplaySettingsChanged`; the listener starts on first subscription and stops when the last handler is removed.

---

## `Barbatos.Wpf.Devices.Sensors` Namespace

### `Geolocation` / `IGeolocation`

Every member throws `FeatureNotSupportedException` on Windows — see
[Contacts and Geolocation](README.md#contacts-and-geolocation) in the README.
`IsEnabled`/`IsListeningForeground` return `false` rather than throwing.

- **`GetLastKnownLocationAsync()`**, **`GetLocationAsync(...)`** → `Task<Location?>`
- **`StartListeningForegroundAsync(GeolocationListeningRequest)`** → `Task<bool>`, **`StopListeningForeground()`**
- **`LocationChanged`** (`EventHandler<GeolocationLocationChangedEventArgs>`, carrying `Location`) and **`ListeningFailed`** (`EventHandler<GeolocationListeningFailedEventArgs>`, carrying `Error`) events — never raised, since listening cannot start
- **`GeolocationExtensions`** — `GetLocationAsync()` and `GetLocationAsync(GeolocationRequest)` on an injected `IGeolocation`, filling in a default request and no cancellation token; they throw like the interface method

`Location`, `GeolocationRequest`, `GeolocationAccuracy`, `GeolocationListeningRequest`,
`GeolocationError`, `DistanceUnits`, and `AltitudeReferenceSystem` retain their full type
surface so calling code compiles unchanged. `Location.CalculateDistance(...)` (haversine
distance) is fully functional independent of the throwing platform implementation.

---

## `Barbatos.Wpf.Networking` Namespace

### `Connectivity` / `IConnectivity`

Backed by `System.Net.NetworkInformation` instead of WinRT's `NetworkInformation` API.

- **`NetworkAccess`** (`NetworkAccess`: `Unknown`, `None`, `Local`, `ConstrainedInternet`, `Internet`)
- **`ConnectionProfiles`** (`IEnumerable<ConnectionProfile>`: `Unknown`, `Bluetooth`, `Cellular`, `Ethernet`, `WiFi`) — the static `Connectivity.ConnectionProfiles` facade applies `.Distinct()`; the injected `IConnectivity.ConnectionProfiles` does not.
- **`ConnectivityChanged`** (`event EventHandler<ConnectivityChangedEventArgs>`)

---

## `Barbatos.Wpf.Storage` Namespace

### `FileSystem` / `IFileSystem`

- **`AppDataDirectory`**, **`CacheDirectory`** (`string`) — `%LocalAppData%\{PublisherInfo.Name}\{AppInfo.AppId}\{Data,Cache}`, created on first access.
- **`OpenAppPackageFileAsync(string filename)`** → `Task<Stream>`, **`AppPackageFileExistsAsync(string filename)`** → `Task<bool>` — resolved relative to the executable's own directory.

### `Preferences` / `IPreferences`

Stores key/value pairs in a local JSON file under `FileSystem.AppDataDirectory\..\Settings\preferences.dat`,
guarded by a lock around file I/O.

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

- **`IsVisible`** (`bool`), **`SetVisible(bool)`**, **`IsVisibleChanged`** (event)
- **`SetToolTip(string)`** — the only live update; icon and menu are read from the options once,
  on the first show.
- **`ShowBalloonTip(string title, string text, TrayIconBalloonIcon icon = None)`** — shows a
  balloon notification above the tray icon (e.g. to tell the user the app is still running
  after the main window was hidden); a no-op while `IsVisible` is `false`.
- **`Clicked`**, **`DoubleClicked`** events

### `TrayIconBalloonIcon`

`None` | `Info` | `Warning` | `Error` — the OS icon shown alongside a balloon raised via
`ShowBalloonTip`.

### `TrayIconOptions`

- **`Enabled`** (`bool`, default `false`) — whether the icon is shown when the host is built.
- **`ToolTip`** (`string?`) — defaults to the entry assembly's name (not `AppInfo.Name`).
- **`IconPath`** (`string?`) — path of an `.ico` file for the tray icon; defaults to the icon associated with the application executable.
- **`MenuItems`** (`IList<TrayMenuItem>`) — code only; each item is built with `new TrayMenuItem("Open", () => { ... })`.

### `TrayMenuItem`

- **`Header`** (`string`), **`Action`** (`Action`)
- **`IconPath`** (`string?`) — path of an `.ico` file shown next to the item's text; omitted or unloadable means no icon.
- **`IsEnabled`** (`bool`, default `true`) — `false` shows the item grayed out and never invokes `Action`, for a currently-unavailable command.
- **`IsDefault`** (`bool`) — renders the item in bold, matching the convention used by e.g. the Windows Bluetooth tray icon.
- **`Separator`** (static property) — a non-clickable separator line; add it like any other item: `options.MenuItems.Add(TrayMenuItem.Separator)`.

Backed directly by the Win32 `Shell_NotifyIcon` API through a hidden `HwndSource` message
window, instead of `System.Windows.Forms.NotifyIcon` — no `UseWindowsForms` / WinForms
reference required.

### `ITrayIconPlatform`

The OS seam behind `ITrayIconService`; register your own before `ConfigureTrayIcon` to replace
the `Shell_NotifyIcon` default (e.g. a fake in tests).

- **`Show(TrayIconOptions)`** — adds the icon; icon, tooltip and menu are read from the options
  the first time only, and a later call after `Hide()` re-adds the same icon.
- **`Hide()`** — removes the icon; does nothing if it is not shown.
- **`SetToolTip(string)`**, **`ShowBalloonTip(string title, string text, TrayIconBalloonIcon icon)`** —
  the balloon is skipped silently when the icon is hidden or the shell reports the user is not
  accepting notifications right now.
- **`Clicked`**, **`DoubleClicked`** (`EventHandler?`).

---

## `Barbatos.Wpf.Power` Namespace

### `IKeepAwakeService`

Prevents idle sleep via `SetThreadExecutionState`; released automatically when the host is disposed.

- **`IsEnabled`** (`bool`), **`SetEnabled(bool)`**, **`IsEnabledChanged`** (event, raised only on
  an actual change). The state is not persisted — persist it through configuration if needed.

### `KeepAwakeOptions`

- **`Enabled`** (`bool`, default `false`) — keep the computer awake as soon as the host is built.
- **`KeepDisplayOn`** (`bool`, default `false`) — also prevents the display from turning off, not
  just system sleep. Read once; there is no runtime toggle for it.

### `IPowerManager`

The OS seam behind `IKeepAwakeService`; register your own before `ConfigureKeepAwake` to replace
the default.

- **`SetKeepAwake(bool keepAwake, bool keepDisplayOn)`** — requests (or, with `keepAwake: false`,
  releases) the keep-awake state; each call replaces the previous state rather than stacking.
  `keepDisplayOn` is ignored when `keepAwake` is `false`. The default applies it on the
  application dispatcher's thread, since `SetThreadExecutionState` state is per thread.

---

## `Barbatos.Wpf.Startup` Namespace

### `IRunOnStartupService`

Registers/unregisters the app in the current user's `Run` registry key
(`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`).

- **`IsEnabled`** (`bool`) — read from the registry on every access, so it reflects the `Run`
  value (the OS persists it across restarts). It does not see the Task Manager / Settings
  "Startup apps" switch, which Windows stores separately under `…\Explorer\StartupApproved\Run`
  while leaving the `Run` value in place: an app the user switched off there reads `true` here
  but does not start, and `SetEnabled(true)` does not switch it back on.
- **`SetEnabled(bool)`** — writes or removes the entry; a no-op when `IsEnabled` already matches,
  so it does not rewrite an existing entry that points at an old executable path.
- **`IsEnabledChanged`** (event) — raised when `SetEnabled` changes the state.

### `RunOnStartupOptions`

- **`Enabled`** (`bool`, default `false`) — register at startup when the host is built. `false`
  leaves an existing registration untouched, so the user's runtime choice survives.
- **`EntryName`** (`string?`) — the `Run` value's name; defaults to the entry assembly's name (not `AppInfo.Name`).
- **`ExecutablePath`** (`string?`) — defaults to `Environment.ProcessPath`; written quoted.
- **`Arguments`** (`string?`) — appended after the quoted executable path.

### `IStartupRegistrar`

The OS seam behind `IRunOnStartupService`; register your own before `ConfigureRunOnStartup` to
replace the registry default (e.g. a fake in tests).

- **`IsRegistered(string entryName)`** (`bool`)
- **`Register(string entryName, string command)`** — creates or overwrites the entry; `command`
  is the full command line, quoted executable path included.
- **`Unregister(string entryName)`** — does nothing if the entry does not exist.

---

## `Barbatos.Wpf.Input` Namespace

Keyboard shortcuts — in-app and true OS-level global hotkeys, unified under one `InputAction`
model conceptually ported from Unity's Input System (Actions/Bindings/Interactions), narrowed to
what a keyboard-only desktop app needs. See the root README's own "InputSystem" bullet under
[Optional desktop features](README.md#optional-desktop-features) for the full rationale of what
was ported vs. deliberately dropped.

### `IInputSystemService`

- **`IsEnabled`** (`bool`), **`SetEnabled(bool)`**, **`IsEnabledChanged`** (event) — master switch;
  disabling unregisters every global hotkey, resets every in-progress `IInputInteraction`, and
  resets every in-progress `ChordBinding` attempt back to its first step.
- **`ActionMaps`** (`IReadOnlyList<InputActionMap>`), **`AddActionMap(InputActionMap)`**,
  **`RemoveActionMap(InputActionMap)`** (`bool`).
- **`RefreshBindings()`** — re-registers every reachable `GlobalKeyBinding` with the OS and
  reclaims cached per-binding state for any binding no longer reachable from any current action.
  Only strictly required after a `GlobalKeyBinding` change made directly on an action's bindings
  (outside `AddActionMap`/`RemoveActionMap`) — a `KeyBinding`/`ChordBinding` change already takes
  effect on the very next keystroke without it, since bindings are read fresh every time, never
  cached — but calling it after any binding mutation is harmless and is what reclaims memory.
- **`FindAction(string name)`** (`InputAction?`), **`FindActionMap(string name)`** (`InputActionMap?`) —
  the usual way to attach a `.Performed` handler once app code has a dependency (e.g. a router)
  that wasn't available yet when the maps were declared in `ConfigureInputSystem`.
- **`ProcessKeyDown(Key, ModifierKeys)`** / **`ProcessKeyUp(Key, ModifierKeys)`** — what the real
  WPF key-event wiring calls internally; also directly usable to script a shortcut
  programmatically or to unit test action wiring without a real window or keyboard.

### `InputSystemOptions`

- **`Enabled`** (`bool`, default `true`).
- **`ActionMaps`** (`IList<InputActionMap>`) — code-only, like `TrayIconOptions.MenuItems`.

### `InputActionMap`

- `InputActionMap(string name, FrameworkElement? scope = null)`.
- **`Scope`** (`FrameworkElement?`) — `KeyBinding`s on this map only fire while `Scope.IsKeyboardFocusWithin`
  is `true`; `null` means "active whenever the app has focus". Never affects `GlobalKeyBinding`.
- **`Enabled`** (`bool`, default `true`) — bulk enable/disable for every action in the map.
- **`Actions`** (`IReadOnlyList<InputAction>`), **`AddAction(InputAction)`**, **`AddAction(string name)`** (`InputAction`).

### `InputAction`

- `InputAction(string name)`.
- **`Bindings`** (`IReadOnlyList<InputBinding>`), **`AddBinding(InputBinding)`**,
  **`AddKeyBinding(Key, ModifierKeys, IInputInteraction?)`**, **`AddGlobalBinding(Key, ModifierKeys)`**,
  **`AddChordBinding(params (Key, ModifierKeys)[])`**, **`AddChordBinding(TimeSpan stepTimeout, params (Key, ModifierKeys)[])`** —
  all return `this`/the action, chainable.
- **`RemoveBinding(InputBinding)`** (`bool`) — removes by reference identity; `false` if not
  present. **`ClearBindings()`** — removes every binding, returns `this`. A `KeyBinding`/
  `ChordBinding` removal takes effect on the next keystroke automatically; a `GlobalKeyBinding`
  removal additionally needs `IInputSystemService.RefreshBindings()` to actually re-register
  with the OS — the mechanism a runtime shortcut-customization UI would use to let a user rebind
  an action to a different key.
- **`Phase`** (`InputActionPhase`) — the most recent Started/Performed/Canceled transition.
- **`Started`**, **`Performed`**, **`Canceled`** (`EventHandler<InputActionContext>`) — the same
  events fire regardless of which binding (in-app, global, or chord) triggered them.

### `InputBinding` / `SingleKeyBinding` / `KeyBinding` / `GlobalKeyBinding` / `ChordBinding`

- `InputBinding` (abstract) — the common base of every binding kind; carries no members of its
  own.
- `SingleKeyBinding` (abstract, `: InputBinding`) — one key/modifier combination pressed at once;
  the base of `KeyBinding`/`GlobalKeyBinding`. **`Key`**, **`Modifiers`** (`ModifierKeys`),
  **`Interaction`** (`IInputInteraction?`), **`Matches(Key, ModifierKeys)`** (`bool`, exact match
  only — like WPF's own native `KeyGesture`).
- `KeyBinding(Key, ModifierKeys = None, IInputInteraction? = null)` — in-app, requires focus.
- `GlobalKeyBinding(Key, ModifierKeys = None)` — OS-level via `RegisterHotKey`; `Interaction` is
  always `null` (`RegisterHotKey`/`WM_HOTKEY` has no "released" message).
- `ChordBinding(params (Key, ModifierKeys)[] steps)` (`: InputBinding`) — an in-app *sequence* of
  combinations pressed one after another (VS Code's "leader key" shortcuts, e.g. `Ctrl+K` then
  `Ctrl+P`); throws `ArgumentException` for fewer than two steps or a `Key.None` step. No
  `Interaction` slot (the sequence itself is the gesture) and no global counterpart (a sequence
  needs WPF's own key events, which never arrive while unfocused).
  - **`Steps`** (`IReadOnlyList<(Key Key, ModifierKeys Modifiers)>`)
  - **`StepTimeout`** (`TimeSpan`, settable, defaults to `ChordBinding.DefaultStepTimeout` — 1.5s)
    — how long to wait for the next step before canceling the attempt; re-armed after every
    successful non-final step, not just the first.
  - **`MatchesStep(int stepIndex, Key, ModifierKeys)`** (`bool`) — `false` for an out-of-range
    index rather than throwing.
  - A bare modifier keypress (`Key.LeftCtrl`/`RightCtrl`/etc.) never counts as a step and never
    cancels a pending sequence — the same way it's already a no-op against a plain `KeyBinding`.
    An off-sequence key at any step cancels the whole attempt back to step zero.

### `IInputInteraction` / built-ins

- `IInputInteraction` — **`Process(ref InputInteractionContext)`**, **`Reset()`**.
- `InputInteractionContext` — **`IsPressed`**, **`IsTimeout`** (`bool`); **`Started()`**,
  **`Performed()`**, **`Canceled()`** — independent flags, more than one can be called per
  `Process` (e.g. Started+Performed together); **`SetTimeout(TimeSpan)`** requests a follow-up
  `Process` call.
- `DefaultInteraction` — the implicit behavior when `Interaction` is `null`: Started+Performed
  together on press, Canceled on release.
- `HoldInteraction(TimeSpan duration)` — performs only after being held ≥ `duration`.
- `TapInteraction(TimeSpan maxDuration)` — performs on a release within `maxDuration`.
- `MultiTapInteraction(int tapCount, TimeSpan tapTime, TimeSpan tapDelay)` — performs after
  `tapCount` taps, each ≤ `tapTime` held and ≤ `tapDelay` apart (e.g. a double-tap).
- `PressInteraction(PressInteractionTrigger = Press)` — `Press`/`Release`/`PressAndRelease`,
  an explicit alternative to `DefaultInteraction`'s implicit on-press behavior.

### `IHotkeyPlatform`

The OS seam behind `GlobalKeyBinding`; register your own before `ConfigureInputSystem` to replace
the `RegisterHotKey` default (e.g. a fake in tests).

- **`Register(int id, Key key, ModifierKeys modifiers)`** (`bool`) — `false` when the OS refused
  the registration, typically because another application owns the combination (the service
  logs a warning).
- **`Unregister(int id)`**
- **`HotkeyPressed`** (`EventHandler<int>?`) — the argument is the id passed to `Register`.

---

## `Barbatos.Wpf.Notifications` Namespace

Desktop (Windows toast) notifications — `ConfigureNotifications`. Not the realtime push client,
which is `Barbatos.Wpf.PushNotifications` and displays through this feature.

### `INotificationService`

- **`IsEnabled`** (`bool`), **`SetEnabled(bool)`** — `Show(...)` is a no-op while disabled.
- **`Show(string title, string message, NotificationSeverity severity = NotificationSeverity.Info)`** — plain text notification.
- **`Show(NotificationContent content)`** — rich notification: image, buttons, and a navigation payload.
- **`Activated`** (`event EventHandler<NotificationActivatedEventArgs>`) — raised when the user clicks the notification body or a button (unless that button used `LaunchUri`, which opens directly without raising this event). Raised on the UI thread: Windows reports the click on a background thread, and the service marshals it to the application dispatcher first. An activation that arrives once that dispatcher has shut down is dropped; only an app built with `useDefaults: false` and no `ConfigureDispatching()` gets it inline on the platform's thread.
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
  `INotificationService.Activated` with `arguments` (with no `arguments`, the toast's own
  `NotificationContent.Arguments`, if it has any).
- **`NotificationButton(string text, Uri launchUri)`** — clicking opens `launchUri` directly
  (e.g. a website or custom protocol) without raising `Activated` or waking the app.
- **`Text`**, **`Arguments`**, **`LaunchUri`** (read-only properties mirroring the constructor used).

### `NotificationActivatedEventArgs`

- **`NotificationActivatedEventArgs(string title, string message, string? arguments = null)`**
- **`Title`**, **`Message`** (`string`) — the clicked notification's own text. The default
  platform recovers it through an id it adds to each toast's activation arguments, because
  Windows hands back only the arguments: a toast shown by an earlier run of the app, or more than
  100 toasts ago, reports empty strings.
- **`Arguments`** (`string?`) — the payload from `NotificationContent.Arguments` (body click)
  or `NotificationButton.Arguments` (button click), or `null` if none was set. The only part
  that survives an app restart — route on it. A button created without arguments of its own
  reports the toast's `NotificationContent.Arguments` instead of `null`: the Toolkit copies a
  toast's arguments onto every button that does not set the same key itself.

### `NotificationOptions`

- **`Enabled`** (`bool`, default `true`)
- **`IconPath`** (`string?`) — a persistent circular app logo overlay shown on every
  notification; defaults to the executable's icon.
- **`Timeout`** (`TimeSpan`) — a hint (`Long` toast duration above ~7 seconds, `Short`
  otherwise); Windows may still manage actual visibility itself.

### `INotificationPlatform`

The OS seam behind `INotificationService`; register your own before `ConfigureNotifications`
(or `ConfigurePushNotifications`) to replace the toast default (e.g. a fake in tests).

- **`Show(NotificationOptions options, NotificationContent content)`**
- **`Activated`** (`EventHandler<NotificationActivatedEventArgs>?`) — may be raised on any thread;
  the service marshals it before raising its own `Activated`.
- **`GetAvailability()`** (`NotificationAvailability`), **`OpenSystemSettings()`** — back the
  service's `Availability` and `OpenSystemSettings()`.

---

## `Barbatos.Wpf.PushNotifications` Namespace

A **client** for a realtime push-notification server — listens for incoming notifications and
displays each one through `Barbatos.Wpf.Notifications.INotificationService`, falling back to
`IPushNotificationFallbackPresenter` when that's unavailable. Does not include a server. Split
into two layers: `IPushNotificationService` (transport-agnostic orchestration) depends only on
`IPushNotificationTransport` (a minimal delivery contract), never on anything SignalR-specific —
see `IPushNotificationTransport` below for how to plug in a different delivery mechanism.

### `IPushNotificationService`

- **`IsConnected`** (`bool`)
- **`ConnectionStateChanged`** (`event EventHandler<bool>`)
- **`NotificationReceived`** (`event EventHandler<PushNotificationReceivedEventArgs>`) — raised
  for every notification, whichever display path was used.
- **`RouteRequested`** (`event EventHandler<PushNotificationRouteRequestedEventArgs>`) — raised
  when the user activates a notification whose action is `PushNotificationActionType.Route`, on
  the UI thread: a toast click arrives through `INotificationService.Activated` (already
  marshalled), and the built-in fallback window is clicked on the UI thread.
- **`ConnectAsync(CancellationToken = default)`** → `Task` — starts the registered
  `IPushNotificationTransport`. The default transport throws `InvalidOperationException` if
  `SignalRPushNotificationOptions.ServerUrl`/`AppId` aren't set; a different transport may have
  its own required configuration instead.
- **`DisconnectAsync(CancellationToken = default)`** → `Task` — stops the transport.
- **`SimulateNotificationAsync(IPushNotification, CancellationToken = default)`** → `Task` —
  feeds a notification through the same display/fallback pipeline a real server push would,
  without any network involved.

### `IPushNotificationTransport`

Abstraction over however push notifications actually reach this device. Deliberately has no
concept of hub URLs, RPC method names, or handshakes — those are specific to one delivery
mechanism and belong on that mechanism's own implementation/options.

- **`IsConnected`** (`bool`)
- **`ConnectionStateChanged`** (`event EventHandler<bool>`)
- **`NotificationReceived`** (`event EventHandler<string>`) — raw JSON text; deserialized by
  `IPushNotificationService` into whichever `IPushNotification` type the app registered.
- **`StartAsync(CancellationToken = default)`** / **`StopAsync(CancellationToken = default)`** → `Task`
- **`AcknowledgeAsync(string receiptId, CancellationToken = default)`** → `Task` — confirms a
  notification arrived so a server with an offline queue stops re-sending it. Has a default
  no-op implementation, since a fire-and-forget delivery mechanism has nowhere to send one.
  Called automatically by `IPushNotificationService` for every notification with a `ReceiptId`.

Registered via `TryAddSingleton` — replace with your own implementation (registered before
`ConfigurePushNotifications`, e.g. for Firebase Cloud Messaging, Windows Notification Service, or
a raw WebSocket) in place of the built-in `SignalRPushNotificationTransport`. Note that FCM/WNS
don't map onto a client-managed persistent connection the way SignalR does (they're
token-registration-plus-OS-push-channel systems) — this interface guarantees the seam stays free
of SignalR assumptions, not a ready-made implementation for every backend.

### `IPushNotification`

The minimum shape the service needs to display a notification.

- **`Title`** (`string`), **`Body`** (`string`), **`ImageUrl`** (`string?`),
  **`Action`** (`PushNotificationAction?`)
- **`ReceiptId`** (`string?`, default interface member returning `null`) — the server's identifier
  for this notification. Used both to acknowledge receipt and to recognize a re-delivery, so the
  same notification is never shown twice. `null` means neither is possible for this payload.

### `PushNotification`

The default payload — `NotificationId` (`long`), `AppId` (`string`), `EventKey` (`string?`),
`Title`/`Body` (`string`), `ImageUrl` (`string?`), `ScheduledFor`/`ExpiresAt`
(`DateTimeOffset?`), `Action` (`PushNotificationAction?`), `Metadata`
(`IReadOnlyDictionary<string, string>?`) — implements `IPushNotification`, with `ReceiptId`
returning `NotificationId` as text (`null` when it is `0`, i.e. absent from the payload).
Implement `IPushNotification` on your own type instead (and register it via
`ConfigurePushNotifications<TNotification>()`) if your server's JSON shape differs. Incoming JSON
is matched case-insensitively, so a server using PascalCase names works without a custom type.

### `PushNotificationAction` / `PushNotificationActionType`

- **`PushNotificationAction.ActionType`** (`PushNotificationActionType`), **`ActionTarget`** (`string?`)
- **`PushNotificationActionType`**: **`Url`** (opened via `Launcher`), **`Setting`** (also via
  `Launcher` — `ActionTarget` expected to be an `ms-settings:` URI), **`Route`** (raises
  `RouteRequested` instead — app-specific), **`None`** (no-op).

### `IPushNotificationFallbackPresenter`

- **`Notify(IPushNotification, DateTimeOffset receivedAt)`**
- **`Activated`** (`event EventHandler<IPushNotification>`)

Registered via `TryAddSingleton` — replace with your own implementation (registered before
`ConfigurePushNotifications`) for something other than the built-in `FallbackNotificationWindow`
(a small, borderless, non-activating window stacked at the screen's bottom-right corner).

### `PushNotificationReceivedEventArgs`

- **`Notification`** (`IPushNotification`), **`ReceivedAt`** (`DateTimeOffset`) — the local
  wall-clock time the client processed the notification — **`UsedFallback`** (`bool`).

### `PushNotificationRouteRequestedEventArgs`

- **`Route`** (`string?`)

### `PushNotificationOptions`

Transport-agnostic — applies regardless of which `IPushNotificationTransport` is registered.

- **`Enabled`** (`bool`) — whether the client starts its transport at startup.
- **`FallbackTimeout`** (`TimeSpan`, default 5s) — how long the fallback window stays visible.
- **`DeduplicationHistorySize`** (`int`, default 256) — how many `ReceiptId`s to remember so a
  re-delivered notification isn't shown twice. Per-process and deliberately not persisted; `0`
  disables de-duplication.

### `SignalRPushNotificationTransport` / `SignalRPushNotificationOptions`

The bundled default `IPushNotificationTransport`, and its own options (irrelevant if you
register a different transport):

- **`ServerUrl`** (`string?`, required), **`AppId`** (`string?`, required)
- **`DeviceId`**/**`AppVersion`**/**`Platform`** (`string?`) — auto-populated from
  `DeviceIdentity.GetInstanceIdAsync()`/`AppInfo.VersionString`/`DeviceInfo.Platform` when left
  unset.
- **`AppKey`** (`string?`) — this application's key on the server, sent both as the
  **`AppKeyHeaderName`** header (default `"X-App-Key"`) and as SignalR's `access_token`, since
  which one arrives depends on the negotiated transport.
- **`Tags`** (`IDictionary<string, string>`) — extra labels (plan tier, permission group, machine
  role, ...) sent with the handshake, for servers that target a subset of devices.
- **`HandshakeMethodName`** (default `"RegisterDevice"`), **`NotificationMethodName`** (default
  `"ReceiveNotification"`), **`AcknowledgeMethodName`** (`string?`, default
  `"AcknowledgeNotification"`; `null` to send no acknowledgements) — the hub method names; adjust
  to match your server.

The handshake is re-sent after every automatic reconnect, not just on the first connect: SignalR
issues a new connection id each time, so a server that maps connections to devices would otherwise
treat the reconnected client as an anonymous stranger and push nothing to it until the app
restarted.

Configured via `ConfigurePushNotifications`'s `configureSignalR` parameter, or the
`Barbatos:PushNotifications:SignalR` config section.

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
`EventWaitHandle` (the wake signal), both keyed by `AppInfo.AppId`. See
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

---

## `Barbatos.Wpf.Apsu` Namespace

State management: stores, getters, plugins, and a middleware-style action interceptor chain
(design inspired by Vue's [Pinia](https://pinia.vuejs.org/) - see
[State management](README.md#state-management-barbatoswpfapsu) in the README for the full
behavior description, including the interceptor composition/ordering rules and why
genuinely asynchronous interceptor work needs a `Task`-returning action).

### Classes / Interfaces

| Type | Description |
|------|-------------|
| [`StoreBase`](#storebase-class) | Abstract base for a store: state as `ObservableObject` properties, plus `StateChanged`, `Patch`, `Reset`. |
| [`Getter<T>`](#gettert-class) | A lazily-computed, cached derived value, invalidated when any dependency store changes. |
| [`IStorePlugin`](#istoreplugin-interface) | Extends every store with cross-cutting behavior, applied once per store right after construction. |
| [`IStoreRegistry`](#istoreregistry-interface) | Exposes every store resolved so far, for cross-cutting introspection. |
| [`ActionAttribute`](#actionattribute-class) | Marks a `public virtual` method as an interceptable action. |
| [`IStoreActionInterceptor`](#istoreactioninterceptor-interface) | Middleware-style wrapper around `ActionAttribute`-marked action calls. |
| [`StoreActionContext`](#storeactioncontext-class) | Which store/action an interceptor call wraps, and the arguments it was called with. |
| [`StoreActionDelegate`](#storeactiondelegate-delegate) | `Task<object?> StoreActionDelegate()` — continues to the next interceptor, or the action itself. |
| [`StoreServiceCollectionExtensions`](#storeservicecollectionextensions-class) | `AddStore<TStore>(configureInterceptors)` / `AddStoreInterceptor<TStore, TInterceptor>()` extension methods. |
| [`StoreInterceptorBuilder<TStore>`](#storeinterceptorbuildertstore-class) | Fluent `Add(...)` surface for registering a store's local interceptors from inside `AddStore`. |
| [`InterceptorHandle`](#interceptorhandle-class) | Returned by the ejectable `AddStoreInterceptor` overloads; `Eject()` removes the interceptor at runtime. |

---

### `StoreBase` Class

Abstract base for a store. State lives as ordinary properties set via
`ObservableObject.SetProperty`.

```csharp
public abstract class StoreBase : ObservableObject
```

#### Events
- **`StateChanged`** (`event EventHandler?`) — fires once per property set outside `Patch`, or
  once per `Patch` call no matter how many properties it touched.

#### Methods
- **`Patch(Action<StoreBase> mutate)`** — applies `mutate` to this store, batching every property
  change it makes into a single `StateChanged` notification fired once `mutate` returns. Each
  property still raises `PropertyChanged` individually as it's set — only `StateChanged` is
  batched. Throws `ArgumentNullException` for a `null` `mutate`. A nested `Patch` call (an action
  that calls `Patch`, itself invoked from inside another `Patch`) lets the outermost call be the
  one that fires `StateChanged`.
- **`Reset()`** (`virtual`) — restores initial state. **Must be overridden** — the default throws
  `NotSupportedException` naming the store type, so a forgotten override fails loudly instead of
  silently doing nothing.

---

### `Getter<T>` Class

A lazily-computed, cached derived value.

```csharp
public sealed class Getter<T>
```

#### Constructor
- **`Getter(Func<T> compute, params StoreBase[] dependencies)`** — `compute` typically closes
  over the same stores listed in `dependencies` (e.g. `() => Store.Count * 2`); `dependencies`
  exists purely to tell this type when to invalidate its cache, not to make the store(s)
  reachable. Throws `ArgumentNullException` for a `null` `compute` or `dependencies`.

#### Properties
- **`Value`** (`T`) — recomputes only if invalidated (by any `dependencies` entry raising
  `StateChanged`) since the last access; otherwise returns the cached value.

#### Remarks
- **Raises no change notification.** A store property returning `Value` never raises
  `PropertyChanged` by itself, so a XAML binding to it never updates; re-raise it from a
  ViewModel on `StateChanged` of every dependency store.
- **Invalidation happens on `StateChanged`, which `Patch` defers until it returns.** Outside
  `Patch`, `PropertyChanged` subscribers that registered after the store's constructor already
  see the getter invalidated; inside `Patch` they (and reads inside the patch callback) see the
  stale cached value. Forward on `StateChanged`, not `PropertyChanged`.
- **No `Dispose`/unsubscribe.** The constructor subscribes to each dependency's `StateChanged`
  for good, so every dependency keeps the getter and everything `compute` captures alive. Create
  getters inside stores; a getter built in a short-lived object over an app-lifetime store keeps
  that object alive.

---

### `IStorePlugin` Interface

Extends every store with cross-cutting behavior.

```csharp
public interface IStorePlugin
{
    void Apply(StoreBase store);
}
```

Register with `services.AddSingleton<IStorePlugin, TPlugin>()`. Every registered plugin's
`Apply` runs once per store created via `AddStore<TStore>()`, in plugin-registration order,
right after that store's own constructor has already run.

---

### `IStoreRegistry` Interface

Tracks every store created through `AddStore<TStore>()`, for cross-cutting introspection. This
is not what makes a store a singleton or caches it — the DI container already does both; this
registry exists purely so something can ask "which stores actually exist right now".

```csharp
public interface IStoreRegistry
{
    IReadOnlyList<StoreBase> Stores { get; }
}
```

- **`Stores`** — every store actually resolved so far. A store type registered but never
  injected/resolved anywhere does not appear here yet.

Resolved automatically once any store is registered via `AddStore<TStore>()`; there is normally
no need to depend on it directly.

---

### `ActionAttribute` Class

Marks a `public virtual` method on a `StoreBase`-derived store as an action that every
applicable `IStoreActionInterceptor` wraps.

```csharp
[AttributeUsage(AttributeTargets.Method)]
public sealed class ActionAttribute : Attribute
```

Only takes effect when at least one interceptor applies to the store; the method must be
`virtual` and the store must not be `sealed` (interception generates a runtime subclass that
overrides every attributed method). Supports synchronous return values and `Task`/`Task<TResult>`
— a `ValueTask`/`ValueTask<TResult>`-returning method is *not* recognized as asynchronous by the
interceptor chain (it isn't a `Task`), so it's treated as synchronous, and the still-unfinished
`ValueTask` itself becomes the chain's "result" rather than something the chain awaits — use
`Task` instead if the method needs interception.

---

### `IStoreActionInterceptor` Interface

Wraps `ActionAttribute`-marked action calls, shaped as ASP.NET Core-style middleware: a chain
of interceptors that can run code before and after the action, retry it, or short-circuit it
entirely.

```csharp
public interface IStoreActionInterceptor
{
    Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next);
}
```

#### Methods
- **`InterceptAsync(StoreActionContext context, StoreActionDelegate next)`** — called for every
  intercepted action. Call `next` to continue the chain (its result becomes the action's return
  value), or don't call it at all to short-circuit the action entirely. Wrap it in `try`/`catch`
  to observe or translate an error; wrap it in timing code to measure it; call it again after a
  failed attempt to retry. `next` may be called after an `await`, from any thread and more than
  once, one call at a time; each call re-runs every interceptor after this one in the chain
  (globals, then locals) and then the action body, while interceptors outside this one see a
  single call. The action body runs on the thread that calls `next`.

#### Remarks

Register a **global** interceptor (every store) with
`services.AddSingleton<IStoreActionInterceptor, TInterceptor>()`, or a **local** one (one store
type only) with `StoreServiceCollectionExtensions.AddStoreInterceptor`. Both compose into a
single chain per store — every global interceptor runs, in registration order, outside every
local one, closest to the actual action call.

`InterceptAsync` is genuinely awaitable for a `Task`/`Task<TResult>`-returning action: real
asynchronous work before `next` (an async precondition) and after it (flushing a log) both
work, and so does calling `next` again to retry an attempt that failed after the action's own
first `await`. The innermost `next` proceeds Castle DynamicProxy's `IInvocation` only while
the intercepted call is still running on its own thread; any later call, or one from another
thread, reaches the action through a second call on the proxy that skips the chain. Proceeding
the invocation from a continuation instead re-entered the whole chain (`IInvocation.Proceed()`:
an `await` before `next` hung) or raced the call's own return on the thread pool
(`IInvocation.CaptureProceedInfo()`: a call occasionally ran the chain twice or never
completed).

> [!IMPORTANT]
> For a *synchronous* action, the chain still runs through this same `async` shape, but the
> proxy blocks on the chain's result (`GetAwaiter().GetResult()`) since there's no `Task` to hand
> back to a synchronous caller — safe only if every interceptor in that chain does purely
> synchronous work. An interceptor that genuinely suspends anywhere while wrapping a synchronous
> action, before or after `next`, risks the classic WPF sync-over-async deadlock; a global
> interceptor wraps synchronous actions too, so it should await nothing but `next()` itself.

---

### `StoreActionContext` Class

Identifies which store and which action an `IStoreActionInterceptor` call is currently wrapping,
and what it was called with.

```csharp
public sealed class StoreActionContext
{
    public StoreActionContext(StoreBase store, string actionName, IReadOnlyList<object?> arguments);

    public StoreBase Store { get; }
    public string ActionName { get; }
    public IReadOnlyList<object?> Arguments { get; }
}
```

- **`Arguments`** — the arguments `ActionName` was called with, in declaration order (empty for a
  parameterless action) — enough to decide whether to call `next` at all (e.g. reject an
  out-of-range argument by short-circuiting), not just to observe or log the call.

Constructed internally by the interceptor pipeline (from Castle DynamicProxy's own
`IInvocation.Arguments`) and handed to `InterceptAsync` — application code reads it, it does not
normally construct one itself.

---

### `StoreActionDelegate` Delegate

```csharp
public delegate Task<object?> StoreActionDelegate();
```

Continues an intercepted action call to the next interceptor in the chain, or to the action
itself if this is the last one. Returns the eventual (boxed) result — `null` for a
`void`/`Task`-returning action.

---

### `StoreServiceCollectionExtensions` Class

Registers `StoreBase`-derived stores with the dependency injection container — the
`defineStore()`/`useStore()` counterpart.

```csharp
public static class StoreServiceCollectionExtensions
```

#### Methods

- **`AddStore<TStore>(this IServiceCollection, Action<StoreInterceptorBuilder<TStore>>? configureInterceptors = null)`**
  (`where TStore : StoreBase`) — registers `TStore` as a singleton, created lazily on first
  resolution and cached for the container's lifetime thereafter. Bootstraps `IStoreRegistry` the
  first time it's called (no separate "enable stores" gate to call first). `configureInterceptors`
  (default: `null`) receives a [`StoreInterceptorBuilder<TStore>`](#storeinterceptorbuildertstore-class)
  for registering `TStore`'s own local interceptors in the same call, chaining as many `Add(...)`
  calls as needed instead of a separate `AddStoreInterceptor<TStore>(...)` call per interceptor
  plus a trailing `AddStore<TStore>()`.
- **`AddStoreInterceptor<TStore, TInterceptor>(this IServiceCollection)`** (`where TStore :
  StoreBase` `where TInterceptor : class, IStoreActionInterceptor`) — registers `TInterceptor` as
  a **local** interceptor via keyed DI (`AddKeyedSingleton`, keyed by `typeof(TStore)`). Call
  more than once for the same `TStore` to register several local interceptors; they run in
  registration order, innermost relative to every global interceptor.
- **`AddStoreInterceptor<TStore>(this IServiceCollection, IStoreActionInterceptor interceptor)`**
  — the same, for an already-constructed interceptor instance instead of a type DI resolves.
- **`AddStoreInterceptor<TStore>(this IServiceCollection, Func<IServiceProvider, IStoreActionInterceptor> factory)`**
  — the same, but built by `factory` the first time `TStore` is resolved — for an interceptor
  whose constructor needs a value the container can't supply on its own (a duration, a
  threshold, ...) alongside one or more services it can; `factory` receives the same
  `IServiceProvider` the store itself resolves from.
- **`AddStoreInterceptor(this IServiceCollection, IStoreActionInterceptor interceptor, out InterceptorHandle handle)`**
  — registers a **global** interceptor the same way as the plain overload above, but wraps it so
  it can be removed later via `handle` — see [`InterceptorHandle`](#interceptorhandle-class).
- **`AddStoreInterceptor<TStore>(this IServiceCollection, IStoreActionInterceptor interceptor, out InterceptorHandle handle)`**
  — the same, for a **local** interceptor.

#### Remarks

`AddStore<TStore>()` builds a Castle DynamicProxy class proxy around `TStore` only when at least
one `IStoreActionInterceptor` (global or local) applies to it — a store with none registered gets
a plain `ActivatorUtilities`-created instance, no proxy involved. Interceptors and plugins are
read at that first resolution, so registering them before or after `AddStore` makes no
difference, and the chain is fixed from then on. Building the proxy requires `TStore` to be:

- **non-`sealed`** — otherwise `InvalidOperationException` at first resolution;
- **`public`**, and every type enclosing it — otherwise Castle throws `ArgumentException`
  ("... is not accessible") at first resolution;
- constructed through **exactly one public constructor** — otherwise `InvalidOperationException`.
  Every parameter is resolved with `GetRequiredService`, so optional parameters' default values
  are not used: an unregistered parameter type throws `InvalidOperationException` ("No service for
  type ...") even where the plain `ActivatorUtilities` path would have used the default.

A global interceptor applies to every store, so registering the first one moves every store onto
the proxy path and can surface these errors in a store that has no interceptor of its own.

---

### `StoreInterceptorBuilder<TStore>` Class

Fluent surface for registering `TStore`'s own local interceptors from inside `AddStore<TStore>`'s
`configureInterceptors` callback. Never constructed directly — `AddStore` hands you one.

```csharp
public sealed class StoreInterceptorBuilder<TStore> where TStore : StoreBase
```

#### Methods

- **`Add<TInterceptor>()`** (`where TInterceptor : class, IStoreActionInterceptor`) →
  `StoreInterceptorBuilder<TStore>` — adds `TInterceptor`, DI-constructed the first time `TStore`
  is resolved. No `new` needed as long as its constructor needs nothing but other registered
  services.
- **`Add(IStoreActionInterceptor interceptor)`** → `StoreInterceptorBuilder<TStore>` — adds an
  already-constructed interceptor instance.
- **`Add(Func<IServiceProvider, IStoreActionInterceptor> factory)`** →
  `StoreInterceptorBuilder<TStore>` — adds an interceptor built by `factory` the first time
  `TStore` is resolved, for one whose constructor needs a value the container can't supply on its
  own (a threshold, a duration, ...) alongside one or more services it can. `factory` receives the
  same `IServiceProvider` the store itself resolves from.

Each `Add` overload returns the same builder, so calls chain freely and in any mix -
`i => i.Add<A>().Add(instance).Add(sp => new C(...))` registers all three as local interceptors
for `TStore`, in that order.

---

### `InterceptorHandle` Class

A handle to one registered `IStoreActionInterceptor`, returned by the `out InterceptorHandle`
overloads of `StoreServiceCollectionExtensions`'s `AddStoreInterceptor` methods — the
[`axios.interceptors.request.eject(id)`](https://axios.rest/pages/advanced/interceptors.html)
counterpart.

```csharp
public sealed class InterceptorHandle
```

#### Properties

- **`IsEjected`** (`bool`) — whether `Eject()` has been called on this handle.

#### Methods

- **`Eject()`** — removes the associated interceptor from every chain it applies to, from that
  point on. Idempotent. Does not shrink or rebuild the chain — the interceptor's slot stays in
  place but becomes a transparent pass-through straight to the next interceptor (or the action
  itself), as if it had never been registered. A call already in flight when `Eject()` runs
  keeps using the interceptor, since its own pipeline was already built by the time `Eject()`
  ran; every call that starts afterward skips it. One-way: there is no "re-attach", and
  registering the interceptor again does not help — `Build()` makes the service collection
  read-only, and each store reads its chain once, at its first resolution — so the behavior
  comes back only with a new container. For an on/off
  setting, give the interceptor its own enabled flag (return `next()` while off) instead.

---

## `Barbatos.Wpf.Mcp` Namespace

MCP (Model Context Protocol) client + bring-your-own-key (BYOK) AI chat, plus a Microsoft Agent
Framework layer on top of both. Has no .NET MAUI counterpart. See
[AI chat + MCP](README.md#ai-chat--mcp-barbatoswpfmcp) in the README for the full behavior
description, including the BYOK rationale, the provider list, and when to use `IAiChatService`
versus `IAiAgentFactory`.

All of it is registered by a single `ConfigureMcp(configure?, configureProvider?, configureAgent?)`
call — the third parameter, and everything below from `IAiAgentFactory` onwards, is additive: an
app already built on `IAiChatService` is unaffected.

### `IMcpServerRegistry`

Connects to MCP servers and aggregates their tools.

- **`Servers`** (`IReadOnlyList<McpServerStatus>`)
- **`Tools`** (`IReadOnlyList<Microsoft.Extensions.AI.AITool>`) — aggregated from every
  currently-connected server; if two servers expose a same-named tool, the one from the server
  added later (later in `Servers`) wins, logged as a warning naming both servers. A server takes
  its place in `Servers` when `AddServerAsync` starts, so this is the order servers were added,
  not the order they finished connecting.
- **`Changed`** (`event EventHandler`) — a server was added, removed, connected, failed to
  connect, or dropped off on its own. Raised on whichever thread caused the change: the caller's
  for the "added" notification at the start of `AddServerAsync`, normally a thread-pool thread
  for everything raised after an internal `ConfigureAwait(false)` await (connected, failed,
  removed, dropped off).
- **`AddServerAsync(McpServerDescriptor, CancellationToken = default)`** → `Task<McpServerStatus>`
  — throws `InvalidOperationException` for a descriptor that fails its own
  `McpServerDescriptor.Validate()`, `ArgumentException` for a duplicate `Name` (mirrors
  `IPeriodicServiceScheduler.Register`'s exception split). A connection failure is both recorded
  on the returned/tracked `McpServerStatus` and rethrown.
- **`RemoveServerAsync(string name, CancellationToken = default)`** → `Task<bool>`
- **`SignOutAsync(string name, CancellationToken = default)`** → `Task<bool>` — discards the
  server's stored OAuth tokens (see `IMcpTokenStore`) so the next connect re-prompts. Independent
  of `RemoveServerAsync`: it forgets credentials without dropping the connection, so a user can
  switch accounts without losing the server entry. Works for a name that is no longer tracked.

### `McpServerDescriptor` / `McpTransportKind` / `McpHttpTransportMode` / `McpServerStatus`

- **`McpServerDescriptor`**: **`Name`** (`string?`, required), **`TransportKind`**
  (`McpTransportKind`, default `Stdio`), **`Command`**/**`Arguments`**/**`WorkingDirectory`**/
  **`EnvironmentVariables`**/**`InheritEnvironmentVariables`** (stdio),
  **`Endpoint`**/**`AdditionalHeaders`**/**`HttpTransportMode`**/**`OAuth`** (http).
  **`Validate()`** checks internal consistency for the current `TransportKind` without attempting
  a connection (throws `InvalidOperationException`) - the same role `PeriodicSchedule.Validate()`
  plays, usable to check a server entry from a settings UI before saving/connecting it.
  - **`EnvironmentVariables`** (`IDictionary<string, string?>`) is applied on top of what
    **`InheritEnvironmentVariables`** (default `true`) lets through; a `null` value *removes* an
    inherited variable, so one secret can be withheld from one server without giving up
    inheritance entirely.
- **`McpTransportKind`**: **`Stdio`** (a local child process, `ModelContextProtocol.Client.StdioClientTransport`),
  **`Http`** (Streamable HTTP with SSE fallback, `ModelContextProtocol.Client.HttpClientTransport`).
- **`McpHttpTransportMode`**: **`AutoDetect`** (default — probe Streamable HTTP, fall back to
  SSE), **`StreamableHttp`**, **`Sse`**.
- **`McpServerStatus`**: **`Name`** (`string`), **`IsConnected`** (`bool`), **`LastError`**
  (`string?`), **`Tools`** (`IReadOnlyList<AITool>`) — as of the last successful connection.
  - **`IsConnected`** flips back to `false` on its own when a session ends without
    `RemoveServerAsync` (a stdio process that crashed or exited, an HTTP session the server tore
    down), with `LastError` set and `Changed` raised.
  - **`LastError`** for a stdio server includes the child process's exit code and the tail of its
    standard error, which is usually the real explanation behind an otherwise opaque transport
    failure. Every stderr line is also logged at `Debug`.

### `McpOAuthOptions` — OAuth for hosted servers

Set `McpServerDescriptor.OAuth` to turn the flow on. Every property is optional: an empty
`new McpOAuthOptions()` is the intended common case and is enough for any server following the MCP
authorization spec, since the authorization server, its scopes, and this app's client registration
are all discovered at connect time. HTTP transport only — `Validate()` rejects it on `Stdio`.

- **`ClientId`** / **`ClientSecret`** (`string?`) — for a server that made you register a client by
  hand. Left unset, the client registers dynamically. A secret without an id is rejected; a desktop
  binary cannot keep one secret anyway, which is why the native-app flow uses PKCE instead.
- **`Scopes`** (`IList<string>`) — *fallback* only. The spec's order is `WWW-Authenticate` →
  protected resource metadata `scopes_supported` → this list → no scope parameter, so leaving it
  empty is right for any server that publishes its own.
- **`RedirectUri`** (`Uri?`) — defaults to a free `127.0.0.1` port chosen per attempt. Must be an
  `http://` loopback URL. Set it only when the server requires a fixed registered value.
- **`ClientName`** (`string?`) — what the consent screen calls this app during dynamic
  registration. Defaults to `IAppInfo.Name`.
- **`AdditionalAuthorizationParameters`** (`IDictionary<string, string>`) — extra query parameters
  for servers needing a non-standard one (`audience`, a tenant hint).
- **`PersistTokens`** (`bool`, default `true`) — whether tokens go through `IMcpTokenStore`.
- **`Validate()`** — same pre-flight role as `McpServerDescriptor.Validate()`; called by it.

### `IMcpAuthorizationHandler` / `IMcpTokenStore`

The two halves of the flow that need a desktop. Both are registered by `ConfigureMcp` with
`TryAddSingleton`, so registering your own first replaces them, and neither is touched unless a
server sets `OAuth`.

- **`IMcpAuthorizationHandler`** — **`GetRedirectUri(McpServerDescriptor)`** → `Uri` (called once
  per attempt, before the flow starts, because the URL is registered up front) and
  **`AuthorizeAsync(McpAuthorizationContext, CancellationToken = default)`** →
  `Task<McpAuthorizationResult>`.
  - **`BrowserMcpAuthorizationHandler`** (default) opens the user's real browser via `ILauncher`
    and catches the redirect on a loopback `HttpListener` — the OAuth 2.1 / RFC 8252 native-app
    flow, so the app never handles the user's password. Incidental requests (a favicon probe) are
    answered `404` and ignored; it completes on the first one carrying `code` or `error`. Public
    **`Timeout`** (default 5 min) and **`CompletionHtml`** (the "you can close this tab" page).
    Replace the whole handler to use an embedded WebView2 instead.
  - **`McpAuthorizationContext`**: **`Descriptor`**, **`AuthorizationUri`**, **`RedirectUri`**.
  - **`McpAuthorizationResult`**: **`Code`**, **`State`**, **`Issuer`**. Pass `state` and `iss`
    back unchanged — the SDK requires an exact `state` match (CSRF) and validates `iss` per
    RFC 9207 (mix-up attacks).
  - **`McpAuthorizationException`**: **`Error`**, **`ErrorDescription`** from the server's redirect
    (`access_denied` when the user clicks Deny). Surfaces through `McpServerStatus.LastError`.
- **`IMcpTokenStore`** — **`GetAsync`** / **`SetAsync`** / **`RemoveAsync`**, keyed by server name.
  The payload is an **opaque** SDK-owned string (access + refresh token and the registration that
  produced them): treat it as a secret, don't parse it. The default
  `SecureStorageMcpTokenStore` is DPAPI-backed `SecureStorage`, the same place `IAiApiKeyProvider`
  keeps API keys. A payload that can no longer be read is discarded and the user simply
  re-authorizes rather than the connection failing.

### `McpOptions`

- **`SectionName`** = `"Barbatos:Mcp"`
- **`Enabled`** (`bool`, default `true`) — whether the seed `Servers` list is connected at
  startup; does not affect `AddServerAsync` calls made later at runtime.
- **`Servers`** (`IList<McpServerDescriptor>`) — connected once, at startup, by `ConfigureMcp`,
  in list order. A connection failure is logged and recorded on that entry's `McpServerStatus`;
  an entry that fails `McpServerDescriptor.Validate()` or reuses a `Name` is rejected before a
  status exists, so it is absent from `IMcpServerRegistry.Servers` and only the startup error log
  records it.

### `IAiApiKeyProvider`

Resolves/stores the end user's own API key, keyed per provider string (case-insensitively).

- **`GetApiKeyAsync(string provider, CancellationToken = default)`** → `Task<string?>`
- **`SetApiKeyAsync(string provider, string apiKey, CancellationToken = default)`** → `Task`

Registered via `TryAddSingleton` — replace with your own implementation (registered before
`ConfigureMcp`) to use a different secret store (Windows Credential Manager, an enterprise key
vault, ...) instead of the built-in `SecureStorageAiApiKeyProvider` (DPAPI via
`Barbatos.Wpf.Storage.ISecureStorage`).

### `AiProviderDescriptor` / `AiProviderOptions`

- **`AiProviderDescriptor`**: **`Key`** (`string?`, required) — the catalog lookup key passed to
  `IAiChatClientFactory.SelectProvider`, **`Provider`** (`string`, default `"openai"`),
  **`Model`** (`string?`), **`Endpoint`** (`string?`). **`Validate()`** checks `Key`/`Provider`
  are set (throws `InvalidOperationException`) — the same role `McpServerDescriptor.Validate()`
  plays.
- **`AiProviderOptions`** (`SectionName` = `"Barbatos:Mcp:Provider"`): **`Provider`** (`string`,
  default `"openai"`, case-insensitive — only `"anthropic"` is special-cased, every other value
  is treated as OpenAI-wire-compatible), **`Model`** (`string?`, required), **`Endpoint`**
  (`string?`, the OpenAI-wire base URL used for every provider except `"anthropic"`, which
  ignores it; `null` means the real OpenAI API, so set it for anything else — e.g. Gemini's
  `"https://generativelanguage.googleapis.com/v1beta/openai/"`), **`Providers`**
  (`IList<AiProviderDescriptor>`) — an optional catalog of provider choices the app wants to
  offer (e.g. in a settings UI), seeded the same way `McpOptions.Servers` seeds MCP servers.
  Deliberately carries no API key/secret.

### `IAiChatClientFactory`

The "which provider" seam — builds/caches the `Microsoft.Extensions.AI.IChatClient` for the
currently-configured provider/model.

- **`ConfigurationChanged`** (`event EventHandler`)
- **`IsConfiguredAsync(CancellationToken = default)`** → `Task<bool>` — true once a model is set
  and an API key has been stored for the configured provider, without building a client.
- **`GetChatClientAsync(CancellationToken = default)`** → `Task<IChatClient>` — throws
  `InvalidOperationException`, naming what's missing, if the model isn't set or no API key is
  stored for the configured provider yet. An endpoint is never required: `null` means the real
  OpenAI API (and `"anthropic"` ignores it).
- **`UpdateProvider(string provider, string model, string? endpoint = null)`** — changes the
  active provider/model, invalidating any cached client.
- **`SelectProvider(string key)`** — looks `key` up in `AiProviderOptions.Providers` and calls
  `UpdateProvider` with that entry's own `Provider`/`Model`/`Endpoint`. Throws
  `InvalidOperationException` if no catalog entry has that `Key`, if the entry fails its own
  `Validate()`, or if the entry has no `Model` set.
- **`RefreshApiKeyAsync(CancellationToken = default)`** → `Task` — invalidates any cached client
  so the next `GetChatClientAsync` re-reads the stored key immediately (already re-checked on
  every call regardless; this only makes `ConfigurationChanged` fire right away).

### `IAiModelCatalog` / `AiModelInfo`

The "which model" counterpart — asks a provider which models it currently offers, using the end
user's own stored key, so a settings UI can offer a live list instead of one hardcoded at build
time. Optional: `AiProviderOptions.Model` is a free string, so a model released after the app
shipped is always reachable by typing its name.

- **`GetModelsAsync(string provider, string? endpoint = null, CancellationToken = default)`** →
  `Task<IReadOnlyList<AiModelInfo>>` — `provider`/`endpoint` mean exactly what they do on
  `AiProviderOptions` (only `"anthropic"` is special-cased; everything else goes to
  `GET {endpoint}/models`). Throws `ArgumentException` for a blank provider and
  `InvalidOperationException`, naming the provider, if no API key has been stored for it yet.
- **`AiModelInfo`**: **`Id`** (`string`) — the string to pass to `UpdateProvider`; **`DisplayName`**,
  **`CreatedAt`**, **`MaxInputTokens`**, **`MaxOutputTokens`**, **`OwnedBy`** — all nullable, since
  only Anthropic's listing reports names/token limits and only OpenAI-compatible ones report
  `owned_by`.

Four things this deliberately does not do: it does not cache (every call is a network round trip —
drive it from a "Refresh models" button, not window activation), it does not work before the end
user has saved a key, it does not filter (an OpenAI-compatible listing includes embedding/audio
models with no reliable flag distinguishing chat ones), and it does not sort (providers return
their own order). Not every endpoint implements the route either — a narrow proxy can fail here,
so keep a typed-in model name working as the fallback.

### `IAiChatService`

The one-shot facade — merges `IMcpServerRegistry.Tools` into the caller's own
`ChatOptions` (appended, never clobbering caller-supplied tools) before delegating to
`IAiChatClientFactory`. Tool calls the model requests execute automatically, with no confirmation
step, and nothing is remembered between calls. For conversation memory and end-user tool
approval, use `IAiAgentFactory` instead; both stay registered.

- **`ConfigurationChanged`** (`event EventHandler`) — forwards `IAiChatClientFactory.ConfigurationChanged`.
- **`IsConfiguredAsync(CancellationToken = default)`** → `Task<bool>`
- **`GetResponseAsync(IEnumerable<ChatMessage>, ChatOptions? = null, CancellationToken = default)`** → `Task<ChatResponse>`
- **`GetStreamingResponseAsync(IEnumerable<ChatMessage>, ChatOptions? = null, CancellationToken = default)`** → `IAsyncEnumerable<ChatResponseUpdate>`

Both throw `InvalidOperationException` (via `IAiChatClientFactory.GetChatClientAsync`) if no
provider is fully configured yet.

### `IAiAgentFactory`

Builds a `Microsoft.Agents.AI.AIAgent` (Microsoft Agent Framework) over whichever provider
`IAiChatClientFactory` currently has configured, with every connected MCP server's tools attached,
the configured tool-approval policy applied, and logging/OpenTelemetry middleware wired in. The
returned type is the framework's own `AIAgent`, not a wrapper — `RunAsync`, `RunStreamingAsync`,
`CreateSessionAsync`, `SerializeSessionAsync` all work as the Agent Framework documents them.

- **`ConfigurationChanged`** (`event EventHandler`) — forwards *both* `IAiChatClientFactory.ConfigurationChanged`
  and `IMcpServerRegistry.Changed`, since an agent is handed its tools once, at construction.
  Raised synchronously on the forwarded event's thread, so a server connecting or dropping off
  normally arrives on a thread-pool thread (once at startup, when a seed server connects); the
  factory does not marshal — dispatch before touching UI state.
- **`IsConfiguredAsync(CancellationToken = default)`** → `Task<bool>` — forwards `IAiChatClientFactory.IsConfiguredAsync`.
- **`GetAgentAsync(CancellationToken = default)`** → `Task<AIAgent>` — the default agent described by
  `AiAgentOptions`, cached until `ConfigurationChanged` fires or `GetChatClientAsync` returns a
  different client instance. `AiAgentOptions` is read only when the agent is built, so editing it
  at runtime does not invalidate the cache.
- **`CreateAgentAsync(AiAgentDescriptor, CancellationToken = default)`** → `Task<AIAgent>` — a one-off,
  uncached agent with its own instructions/tools/approval policy.

Both throw `InvalidOperationException` if no provider is fully configured yet;
`CreateAgentAsync` also throws it for a descriptor failing `AiAgentDescriptor.Validate()`.

### `AiAgentOptions` / `AiAgentDescriptor` / `AiToolApprovalMode`

- **`AiAgentOptions`** (`SectionName` = `"Barbatos:Mcp:Agent"`): **`Name`** (`string`, default
  `"Assistant"`), **`Description`**/**`Instructions`** (`string?`), **`ToolApproval`**
  (`AiToolApprovalMode`, default `Always`), **`IncludeMcpTools`** (`bool`, default `true`),
  **`EnableLogging`** (`bool`, default `true`), **`EnableOpenTelemetry`** (`bool`, default
  `false`), **`OpenTelemetrySourceName`** (`string`, default `"Barbatos.Wpf.Mcp"`). Read when the
  default agent is built; a runtime edit shows up only at the next rebuild (provider/key change,
  server connect/disconnect), and a reloaded configuration file never reaches it.
- **`AiAgentDescriptor`**: **`Name`** (`string?`, required), **`Description`**/**`Instructions`**
  (`string?`), **`ToolApproval`** (default `Always`), **`IncludeMcpTools`** (default `true`),
  **`Tools`** (`IList<AITool>`), plus **`Validate()`** — the same pure pre-flight check
  `McpServerDescriptor.Validate()` and `AiProviderDescriptor.Validate()` provide.
- **`AiToolApprovalMode`**: **`Always`** (default — each tool call pauses the run for end-user
  approval) and **`Never`** (auto-execute, matching `IAiChatService`'s behavior).

### `AgentApprovalExtensions`

Drives the tool-approval round trip without hand-assembling `AIContent` lists.

- **`GetToolApprovalRequests(this AgentResponse)`** → `IReadOnlyList<ToolApprovalRequestContent>` —
  empty when the agent produced an ordinary answer; non-empty means the run is paused and no tool
  has run yet.
- **`CreateApprovalResponse(this IEnumerable<ToolApprovalRequestContent>, bool approved, bool alwaysApprove = false, string? reason = null)`**
  → `ChatMessage` — pass to the agent's next run on the same session. `alwaysApprove: true` records
  a standing "don't ask again" rule for that tool; it is ignored when denying. Throws
  `ArgumentException` for an empty request list.

### `IAiSessionStore`

Persists an `AgentSession` across app restarts — the "where does the serialized session live" seam
the Agent Framework deliberately leaves to the app. Default implementation writes one JSON file per
session under `IFileSystem.AppDataDirectory` (per-user, **not** encrypted — back it with
`ISecureStorage` or your own store if the conversations warrant it). Register your own before
calling `ConfigureMcp`, exactly as with `IAiApiKeyProvider`.

- **`SaveAsync(string sessionId, AIAgent, AgentSession, CancellationToken = default)`** → `Task`
- **`LoadAsync(string sessionId, AIAgent, CancellationToken = default)`** → `Task<AgentSession?>` —
  `null` when nothing was saved under that id, so a "resume last conversation" path can fall back
  to `CreateSessionAsync()` without catching anything.
- **`ListAsync(CancellationToken = default)`** → `Task<IReadOnlyList<string>>`
- **`DeleteAsync(string sessionId, CancellationToken = default)`** → `Task<bool>`

The same agent must be passed to `LoadAsync` as to `SaveAsync`. Session ids that cannot become a
file name are rejected with `ArgumentException` rather than silently sanitized.
