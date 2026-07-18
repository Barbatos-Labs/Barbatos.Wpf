# Barbatos.Wpf.Hosting

A MAUI-style application host for WPF. It ports the hosting concept of .NET MAUI
(`MauiApp` / `MauiAppBuilder`) to WPF, giving you dependency injection, configuration,
logging, lifecycle events and dispatching with the exact same programming model.

| .NET MAUI | Barbatos.Wpf.Hosting |
| --- | --- |
| `MauiApp` | `WpfApp` |
| `MauiAppBuilder` | `WpfAppBuilder` |
| `MauiApp.CreateBuilder()` | `WpfApp.CreateBuilder()` |
| `MauiHostEnvironment` | `WpfHostEnvironment` |
| `IMauiInitializeService` / `IMauiInitializeScopedService` | `IWpfInitializeService` / `IWpfInitializeScopedService` |
| `ConfigureLifecycleEvents(...).AddWindows(...)` | `ConfigureLifecycleEvents(...).AddWpf(...)` |
| `WindowsLifecycle` delegates | `WpfLifecycle` delegates |
| `IDispatcher` / `DispatcherProvider` / `ConfigureDispatching` | `IDispatcher` / `DispatcherProvider` / `ConfigureDispatching` |
| `MauiWinUIApplication` | `WpfApplication` |
| `IPlatformApplication` | `IWpfPlatformApplication` |

## Getting started

### 1. Derive your `App` from `WpfApplication`

`App.xaml`:

```xml
<hosting:WpfApplication x:Class="MyApp.App"
                        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:hosting="clr-namespace:Barbatos.Wpf.Hosting;assembly=Barbatos.Wpf.Hosting" />
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

### 2. Compose the host (the `MauiProgram` pattern)

```csharp
public static class WpfProgram
{
    public static WpfApp CreateWpfApp()
    {
        var builder = WpfApp.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Sample:Greeting"] = "Hello from Barbatos.Wpf.Hosting!",
        });

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

## Features

- **Dependency injection** — `builder.Services` is a standard `IServiceCollection`;
  the builder implements `IHostApplicationBuilder`, so generic host extensions work too.
- **Configuration** — `builder.Configuration` is a `ConfigurationManager`; the built
  configuration is registered as `IConfiguration` and disposed together with the app.
- **Logging** — `builder.Logging` registers the real logging services; if you never touch
  logging, no-op `ILogger<T>`/`ILoggerFactory` implementations are registered so consumers
  never receive `null`.
- **Lifecycle events** — application events (`OnStartup`, `OnActivated`, `OnDeactivated`,
  `OnSessionEnding`, `OnDispatcherUnhandledException`, `OnExit`) and window events
  (`OnWindowCreated`, `OnWindowLoaded`, `OnWindowActivated`, `OnWindowDeactivated`,
  `OnWindowStateChanged`, `OnWindowClosing`, `OnWindowClosed`) are surfaced through the
  same `ILifecycleEventService` design as .NET MAUI, and every window gets its own
  service scope (with `IWpfInitializeScopedService` support).
- **Dispatching** — `IDispatcher`, `IDispatcherTimer` and `IDispatcherProvider` backed by
  the WPF `Dispatcher`, registered app-wide (singleton) and per-window (scoped), plus the
  `DispatchAsync` / `DispatchIfRequiredAsync` helpers.
- **Initialization services** — `IWpfInitializeService` runs once during `Build()`;
  `IWpfInitializeScopedService` runs once per window scope.
- **Periodic services** — `IWpfPeriodicService` runs on a recurring schedule (see below).

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

## Optional desktop features

Four opt-in features cover the typical "settings screen" of a desktop app. Each one is
only registered when its `Configure...` method is called, binds its own configuration
section (file values override code values), and exposes a service for runtime (UI) toggling:

| Feature | Builder method | Options section | Runtime service |
| --- | --- | --- | --- |
| Run on startup (registry `Run` key) | `ConfigureRunOnStartup()` | `Barbatos:RunOnStartup` | `IRunOnStartupService` |
| Global hotkeys / quick entry (`RegisterHotKey`) | `ConfigureGlobalHotkeys(...)` | `Barbatos:GlobalHotkeys` | `IGlobalHotkeyService` |
| System tray icon (`NotifyIcon`) | `ConfigureTrayIcon(...)` | `Barbatos:TrayIcon` | `ITrayIconService` |
| Keep computer awake (`SetThreadExecutionState`) | `ConfigureKeepAwake()` | `Barbatos:KeepAwake` | `IKeepAwakeService` |
| Push notifications (toast, `NotifyIcon.ShowBalloonTip`) | `ConfigureNotifications()` | `Barbatos:Notifications` | `INotificationService` |

```csharp
builder.ConfigureRunOnStartup();
builder.ConfigureKeepAwake();
builder.ConfigureTrayIcon(options =>
{
    options.MenuItems.Add(new TrayMenuItem("Open", App.ShowMainWindow));
    options.MenuItems.Add(new TrayMenuItem("Exit", App.ExitApplication));
});
builder.ConfigureGlobalHotkeys(hotkeys => hotkeys
    .Add("QuickEntry", "Control+Alt+Space", App.ShowMainWindow));
builder.ConfigureNotifications();
```

And from a configuration file:

```json
{
  "Barbatos": {
    "RunOnStartup": { "Enabled": true },
    "TrayIcon": { "Enabled": true, "ToolTip": "My app" },
    "KeepAwake": { "Enabled": true, "KeepDisplayOn": false },
    "GlobalHotkeys": { "Gestures": { "QuickEntry": "Control+Shift+K" } },
    "Notifications": { "Enabled": true }
  }
}
```

Notes:

- `KeepAwake` prevents idle sleep while still letting the display turn off
  (set `KeepDisplayOn` to also keep the display on); the sleep block is released when
  the host is disposed.
- Hotkey gestures are parsed by `HotkeyGesture` (`"Control+Alt+Space"`, `"Ctrl+Shift+K"`,
  `"Win+F12"`, ...). A failed OS registration (combination taken by another app) is
  logged as a warning.
- The tray icon supports a context menu, tooltip, and click/double-click events; the
  sample uses it together with the `OnWindowClosing` lifecycle event to implement
  minimize-to-tray.
- `RunOnStartup` state is persisted by the OS registry; the other toggles can be
  persisted by writing their configuration sections back to a user settings file that is
  loaded via `builder.Configuration.AddJsonFile(...)` — see `SettingsStore` in the sample.
- Notifications are pushed via `NotifyIcon.ShowBalloonTip`, which Windows 10/11 renders as
  a modern toast notification (it also appears in the notification center), attributed to
  the notification's identity icon — the same mechanism used by the system tray feature.
  `INotificationService.Show(title, message, severity)` is a no-op while `IsEnabled` is
  `false`, so a single settings toggle can silence every call site; `Activated` fires when
  the user clicks the notification.

## Repository layout

- `src/Barbatos.Wpf.Hosting` — the library.
- `samples/Barbatos.Wpf.Hosting.Sample` — a complete sample application showing DI,
  configuration, host environment and the live lifecycle event log.
- `tests/Barbatos.Wpf.Hosting.UnitTests` — the unit test suite, ported from the
  .NET MAUI hosting tests.
