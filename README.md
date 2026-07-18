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

## Repository layout

- `src/Barbatos.Wpf.Hosting` — the library.
- `samples/Barbatos.Wpf.Hosting.Sample` — a complete sample application showing DI,
  configuration, host environment and the live lifecycle event log.
- `tests/Barbatos.Wpf.Hosting.UnitTests` — the unit test suite, ported from the
  .NET MAUI hosting tests.
