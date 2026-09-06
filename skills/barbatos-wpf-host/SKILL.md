---
name: barbatos-wpf-host
description: Compose or extend a Barbatos.Wpf.Core application host - WpfApp/WpfAppBuilder, the WpfProgram pattern, WpfApplication, configuration layering, DI service lifetimes, lifecycle events, dispatching, Essentials (AppInfo, Preferences, SecureStorage, Connectivity, VersionTracking), and the opt-in desktop features. Use this whenever the work touches app startup, registering or resolving a service, a settings screen, or any of - system tray icon, minimize to tray, toast notifications, keyboard shortcuts and global hotkeys, single instance, run on startup, keep awake, splash screen, background or scheduled jobs, dialog windows. Reach for it even when the user names a symptom rather than a feature - "the app opens twice", "my Ctrl+K shortcut does nothing", "the toast never appears", "where do I register this service", "it should keep running in the tray when I close the window". Vietnamese prompts land here too: khởi động app, đăng ký service, vòng đời ứng dụng, màn hình cài đặt, biểu tượng khay hệ thống, thu nhỏ xuống khay, thông báo toast, phím tắt, phím tắt toàn cục, chạy cùng Windows, màn hình chờ, tác vụ chạy nền hoặc định kỳ.
---

# Barbatos.Wpf.Core hosting

`Barbatos.Wpf.Core` is a single package: hosting, every Essentials module, and every optional
desktop feature ship together. Nothing else to install.

Full docs: [`src/Barbatos.Wpf.Core/README.md`](../../src/Barbatos.Wpf.Core/README.md) ·
[`API-REFERENCE.md`](../../src/Barbatos.Wpf.Core/API-REFERENCE.md).
Reference app: [`samples/Barbatos.Wpf.Core.Sample`](../../samples/Barbatos.Wpf.Core.Sample).

---

## The shape every app takes

Three files, always. Do not invent a different arrangement.

**`App.xaml`** — the `Application` root is `WpfApplication`, not `Application`:

```xml
<hosting:WpfApplication x:Class="MyApp.App"
                        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:hosting="clr-namespace:Barbatos.Wpf.Hosting;assembly=Barbatos.Wpf.Core" />
```

**`App.xaml.cs`** — hands the host over and resolves the main window from the container:

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

**`WpfProgram.cs`** — the composition root, mirroring MAUI's `MauiProgram`:

```csharp
public static class WpfProgram
{
    public static WpfApp CreateWpfApp()
    {
        var builder = WpfApp.CreateBuilder();
        // configuration -> lifecycle -> features -> services
        return builder.Build();
    }
}
```

`WpfApp.CreateBuilder()` defaults to `useDefaults: true`, which already calls
`ConfigureLifecycleEvents`, `ConfigureDispatching`, and `UseEssentials()`. Do not re-call them.

`WpfAppBuilder` implements `IHostApplicationBuilder`: `builder.Services` is a real
`IServiceCollection`, `builder.Configuration` a `ConfigurationManager`, `builder.Logging` a real
`ILoggingBuilder`. `builder.Environment` is a `WpfHostEnvironment` with settable
`EnvironmentName` and the usual `IsDevelopment()`/`IsEnvironment(...)` extensions.

Reaching the container from a static context: `IWpfPlatformApplication.Current?.Services`.

---

## Configuration layering

Order matters — last source wins. The sample's ordering is the one to copy:

```csharp
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Sample:Greeting"] = "code default",
});
builder.Configuration.AddJsonFile("appsettings.json", optional: true);
builder.Configuration.AddJsonFile(SettingsStore.FilePath, optional: true);
```

Code defaults → shipped file → the user's own saved settings.

`WpfAppBuilder` prepends a `DOTNET_`-prefixed environment-variable source as the lowest
priority entry, so `DOTNET_ENVIRONMENT=Staging` works with no code. For live reload of a
user-settings file (an `IOptionsMonitor<T>` that actually fires), pass
`reloadOnChange: true` — see `Barbatos.Wpf.AquariusRouter.Sample`'s `ShortcutSettingsStore`.

**Runtime-mutable features never persist themselves.** Toggling `ITrayIconService`,
`IPeriodicServiceScheduler`, `IMcpServerRegistry`, or `IAiChatClientFactory` from a settings UI
changes the running app only. To survive a restart, write the same
`Barbatos:<Feature>` section back to a JSON file that is loaded above.

---

## Lifecycle events

```csharp
builder.ConfigureLifecycleEvents(events => events.AddWpf(wpf => wpf
    .OnStartup((app, args) => { })
    .OnDispatcherUnhandledException((app, args) => { /* args.Handled = true to swallow */ })
    .OnWindowCreated(window => { })
    .OnWindowClosing((window, args) => { /* args.Cancel = true to keep it open */ })
    .OnWindowClosed((window, args) => { })));
```

Application events: `OnStartup`, `OnActivated`, `OnDeactivated`, `OnSessionEnding`,
`OnDispatcherUnhandledException`, `OnExit`. Window events: `OnWindowCreated`, `OnWindowLoaded`,
`OnWindowActivated`, `OnWindowDeactivated`, `OnWindowStateChanged`, `OnWindowClosing`,
`OnWindowClosed`.

`WpfApplication` already subscribes to `Application.DispatcherUnhandledException` and forwards
it here — this is the idiomatic place to observe it. Do not add a second direct subscription in
`App.xaml.cs`.

Every window gets its own DI scope. `IWpfInitializeService` runs once during `Build()`;
`IWpfInitializeScopedService` runs once per window scope. Use `IWpfInitializeService` for
"wire this up after the container exists" work instead of manual `GetRequiredService` calls in
`CreateWpfApp` — that is how `ConfigureShortcutRouting` and `ConfigureAppNavigationGuards` are
built in the router sample. **`IWpfInitializeService` is synchronous**; if the work is async,
do it in `OnStartup` behind the splash screen instead (see below).

Minimize-to-tray is `OnWindowClosing` + `ITrayIconService`:

```csharp
.OnWindowClosing((window, args) =>
{
    var tray = IWpfPlatformApplication.Current?.Services.GetService<ITrayIconService>();
    if (tray?.IsVisible == true && !App.IsExiting)
    {
        args.Cancel = true;
        window.Hide();
        tray.ShowBalloonTip("My app", "Still running in the system tray.");
    }
})
```

The balloon is not optional polish — a hidden window is indistinguishable from a closed app.

---

## Optional desktop features

Each is opt-in at the builder level, binds `Barbatos:<Feature>` from configuration (file
overrides code), and exposes a runtime service.

| Feature | Builder method | Section | Service | On by default once called |
| --- | --- | --- | --- | --- |
| Single instance | `ConfigureSingleInstance(...)` | `Barbatos:SingleInstance` | `ISingleInstanceService` | **Yes** |
| Run on startup | `ConfigureRunOnStartup()` | `Barbatos:RunOnStartup` | `IRunOnStartupService` | No |
| Tray icon | `ConfigureTrayIcon(...)` | `Barbatos:TrayIcon` | `ITrayIconService` | No |
| Keep awake | `ConfigureKeepAwake()` | `Barbatos:KeepAwake` | `IKeepAwakeService` | No |
| Notifications (toast) | `ConfigureNotifications()` | `Barbatos:Notifications` | `INotificationService` | Yes |
| Periodic services | `ConfigurePeriodicServices<T>()` | `Barbatos:PeriodicServices` | `IPeriodicServiceScheduler` | Yes |
| Push notifications | `ConfigurePushNotifications(...)` | `Barbatos:PushNotifications` | `IPushNotificationService` | Yes |
| Input system | `ConfigureInputSystem(...)` | `Barbatos:InputSystem` | `IInputSystemService` | Yes |
| Dialogs | `ConfigureDialogs(...)` | — | `IDialogService` | — |
| MCP + AI | `ConfigureMcp(...)` | `Barbatos:Mcp` | see `barbatos-mcp-ai` | Yes |

Per-feature notes that are easy to get wrong: see
[`reference.md`](reference.md).

---

## Essentials

Every module follows *interface + static facade*: inject `IAppInfo` for testability, or call
`AppInfo.Current` directly. `UseEssentials()` (a builder default) registers `IAppInfo`,
`IPublisherInfo`, `IDeviceIdentity`, `IDeviceInfo`, `IFileSystem`, `IPreferences`,
`ISecureStorage`, `IVersionTracking`, `IConnectivity`, `IDeviceDisplay`, `IEmail`, `IContacts`,
`IGeolocation`, `IAppActions`, `ILauncher`.

```csharp
builder.ConfigureEssentials(e => e
    .AddAppAction("open", "Open", "Show the main window")
    .OnAppAction(action => { /* route on action.Id */ })
    .UseVersionTracking());
```

Identity comes from standard SDK properties, with an assembly-metadata override checked first:

| Value | Standard property | Explicit override |
| --- | --- | --- |
| `AppInfo.Name` | `<Product>` | `Barbatos.Wpf.ApplicationModel.AppInfo.Name` |
| `AppInfo.Version` | `<Version>` | … |
| `PublisherInfo.Name` | `<Company>` | … |
| `PublisherInfo.Copyright` | `<Copyright>` | … |
| `AppInfo.AppId` | *(none)* | `Barbatos.Wpf.ApplicationModel.AppInfo.AppId` |
| `PublisherInfo.Website`/`SupportUrl`/`SupportEmail` | *(none)* | `…PublisherInfo.Website` etc. |

`AppInfo.AppId` need not be a GUID — any stable string works. It keys the single-instance
mutex and the `%LocalAppData%\{Publisher}\{AppId}` data folder, so changing it orphans user
data. An app project should also set `<AssemblyVersion>$(Version)</AssemblyVersion>` and
`<FileVersion>$(Version)</FileVersion>`, or it reports `Directory.Build.props`' library version
to File Explorer.

Not available on Windows and throwing `FeatureNotSupportedException` by design: `IContacts`,
`IGeolocation` (except `Location.CalculateDistance`), and six `Permissions` types
(`ContactsRead/Write`, `LocationWhenInUse/Always`, `Microphone`, `Sensors`). Do not "fix" these.

---

## Splash screen and slow startup

Override `GetSplashScreenOptions()` on `App` for the built-in window, or `CreateSplashScreen()`
for a fully custom one. To load data before the main window ever appears:

```csharp
protected override async void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    var vm = Services.GetRequiredService<MainViewModel>();   // must be a singleton
    await vm.LoadWorkspaceSummaryAsync();

    await CloseSplashScreenAsync();

    MainWindow = Services.GetRequiredService<MainWindow>();
    MainWindow.Show();
}
```

The preloaded ViewModel **must be registered as a singleton** — otherwise `MainWindow`'s own
constructor resolves a second, unloaded instance. `WpfApplication` forces
`ShutdownMode.OnExplicitShutdown` while the splash screen is the only open window and restores
it in `CloseSplashScreenAsync()`; keep `MinimumDisplayDuration` (default 1.5s) so fast machines
don't get a flash.

---

## Checklist for a new hosted feature

1. `ConfigureXxx(this WpfAppBuilder, Action<XxxOptions>? configure = null)` in
   `Essentials/Xxx/AppHostBuilderExtensions.cs`.
2. `XxxOptions` with `const string SectionName = "Barbatos:Xxx"`, bound so file values override
   code values.
3. Register the runtime service with `TryAdd*` so an app can substitute its own.
4. If the feature touches the OS, put the P/Invoke behind an `IXxxPlatform` interface so the
   service stays unit-testable (see `ITrayIconPlatform`, `INotificationPlatform`,
   `IHotkeyPlatform`, `IStartupRegistrar`, `IPowerManager`).
5. Expose runtime toggling (`IsEnabled` + `SetEnabled` + `IsEnabledChanged`) if a settings
   screen would want it.
6. Update `README.md` (feature table + notes) and `API-REFERENCE.md`, then add a row to the
   sample's settings screen.
