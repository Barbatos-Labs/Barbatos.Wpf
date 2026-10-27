# Desktop features: behaviour that is easy to get wrong

Read when using, configuring or debugging one specific `ConfigureXxx` feature. Composition order,
threading and persistence rules shared by all of them are in [SKILL.md](SKILL.md). Prose docs:
[Optional desktop features](../../../src/Barbatos.Wpf.Core/README.md#optional-desktop-features).

Contents

- [Single instance](#single-instance)
- [Tray icon](#tray-icon)
- [Toast notifications](#toast-notifications)
- [Push notifications client](#push-notifications-client)
- [Periodic services](#periodic-services)
- [Input system](#input-system)
- [Dialogs](#dialogs)
- [Keep awake](#keep-awake)
- [Run on startup](#run-on-startup)
- [Feature symptoms](#feature-symptoms)
- [Adding or changing a Core ConfigureXxx feature](#adding-or-changing-a-core-configurexxx-feature)

## Single instance

`ConfigureSingleInstance(o => { o.Enabled = true; o.ActivateMainWindow = true; })` - both default
`true`. Source: `Essentials/SingleInstance/SingleInstanceService.cs`.

- A named `Mutex` (`"Barbatos.Wpf.SingleInstance." + AppInfo.AppId`) plus a wake
  `EventWaitHandle` (the same name + `".Wake"`), session-local (no `Global\` prefix): one instance
  per user session, not per machine.
- The check runs in the feature's `IWpfInitializeService`, inside `Build()`. A duplicate sets the
  wake event and calls `Environment.Exit(0)` - before any window, before `Build()` returns, and
  without disposing the host. Anything that must run in the duplicate (forwarding its arguments
  over your own IPC) has to run before that initializer: in `CreateWpfApp` before `Build()`, or
  in an `IWpfInitializeService` registered ahead of `ConfigureSingleInstance()`.
- The primary's `SecondInstanceLaunched` is dispatched to the UI thread. With
  `ActivateMainWindow` it first un-minimizes, shows and foregrounds `Application.Current.MainWindow`
  (Topmost toggle, no `SetForegroundWindow`). Set it `false` to handle activation yourself.
- The event is `EventHandler` with `EventArgs.Empty`: the duplicate's command line is not
  forwarded. "Open this file in the running instance" needs your own IPC.
- Options are read once; there is no runtime toggle. `Enabled = false` skips the check and
  `IsPrimaryInstance` stays `true`.
- Changing `AppInfo.AppId` between versions lets old and new builds run side by side (and moves
  the per-user data folder - see `barbatos-wpf-essentials`).

## Tray icon

Win32 `Shell_NotifyIcon` behind a hidden `HwndSource` - no WinForms. Source:
`Essentials/TrayIcon/ITrayIconPlatform.cs` (`Win32TrayIconPlatform`).

```csharp
// samples/Barbatos.Wpf.Core.Sample/WpfProgram.cs
builder.ConfigureTrayIcon(options =>
{
    options.MenuItems.Add(new TrayMenuItem("Open", App.ShowMainWindow) { IsDefault = true });
    options.MenuItems.Add(TrayMenuItem.Separator);
    options.MenuItems.Add(new TrayMenuItem("About", App.ShowAboutWindow));
    options.MenuItems.Add(new TrayMenuItem("Check for Updates", () => { }) { IsEnabled = false });
    options.MenuItems.Add(TrayMenuItem.Separator);
    options.MenuItems.Add(new TrayMenuItem("Exit", App.ExitApplication));
});
```

- `TrayIconOptions`: `Enabled` (default `false` - the icon is hidden until enabled by config or
  `SetVisible(true)`), `ToolTip` (default: entry-assembly name; truncated to 127 chars), `IconPath`
  (`.ico` file loaded with `LoadImage` - anchor it on `AppContext.BaseDirectory`; unloadable falls
  back to the executable's icon, then the system default), `MenuItems` (code only).
- `TrayMenuItem(header, action)`: `IconPath`, `IsEnabled` (grayed, never invoked), `IsDefault`
  (bold, rendering hint only), static `Separator` (a fresh instance each access).
- **Icon, menu and tooltip are read from options once, on the first `Show`.** Mutating
  `MenuItems` later does nothing; `SetToolTip(string)` is the only live update. A dynamic menu
  needs your own `ITrayIconPlatform`.
- `ITrayIconService`: `IsVisible`, `SetVisible(bool)`, `IsVisibleChanged`, `SetToolTip`,
  `ShowBalloonTip(title, text, icon = TrayIconBalloonIcon.None)`, `Clicked` (left button up),
  `DoubleClicked`. Right-click opens the menu. All on the UI thread.
- The icon is re-added automatically when Explorer restarts (`TaskbarCreated`).
- `ShowBalloonTip` is a no-op while hidden, and is skipped when `SHQueryUserNotificationState`
  says the user is away, in quiet time (first hour after login) or full-screen. The user can also
  turn balloons off per app; nothing reports that. Title/text truncate at 63/255 chars.
- The sample wires double-click to reopen the window in `App.OnStartup`
  (`trayIcon.DoubleClicked += (sender, args) => ShowMainWindow();`).
- Minimize-to-tray: [SKILL.md](SKILL.md#lifecycle-events). Filter to the main window's type.

## Toast notifications

Adaptive toasts through the Windows Community Toolkit (`ToastContentBuilder`,
`ToastNotificationManagerCompat`); works unpackaged with no Start-menu shortcut or AUMID.
Requires the SDK-suffixed TFM. Source: `Essentials/Notifications/INotificationPlatform.cs`.

```csharp
notifications.Show("Saved", "Your changes were saved.");                 // severity defaults to Info
notifications.Show(new NotificationContent
{
    Title = "Build finished",
    Message = "3 warnings",
    ImagePath = Path.Combine(AppContext.BaseDirectory, "Assets", "notification-hero.png"),   // as the sample does
    Arguments = "route:/builds/42",               // returned when the body is clicked
    Buttons = { new NotificationButton("Open", "route:/builds/42"),
                new NotificationButton("Docs", new Uri("https://example.com/docs")) },
});
```

- `Show(...)` is a no-op while `IsEnabled` is `false` (`SetEnabled`, or `Barbatos:Notifications:Enabled`).
- Windows drops a blocked toast silently. `Availability` (`NotificationAvailability`: `Enabled`,
  `DisabledForApplication`, `DisabledForUser`, `DisabledByGroupPolicy`, `DisabledByManifest`) is
  read live on every access - check it when the window activates (the sample does) and offer
  `OpenSystemSettings()` (opens `ms-settings:notifications`).
- `ImagePath` and `NotificationOptions.IconPath` (circular logo override on every toast) go
  through `Path.GetFullPath` + `new Uri(...)`: local files only; an http(s) URL throws, and a
  relative path resolves against the process's current directory, which need not be the app
  folder (a shortcut's "Start in", a `Run`-key launch). Anchor paths on `AppContext.BaseDirectory`.
- `NotificationSeverity.Error` uses the `Alarm` scenario: stays until dismissed, bypasses Focus
  Assist. `NotificationOptions.Timeout` above 7 seconds selects the long duration; Windows may
  ignore it.
- `NotificationButton(text, arguments)` raises `Activated` with those arguments - and a button
  given none reports the body's `NotificationContent.Arguments` (the Toolkit copies the toast's
  arguments onto every button lacking that key), so give each button its own when the handler
  must tell them apart. `NotificationButton(text, launchUri)` opens the URI without raising
  anything. Keep to five buttons (the toast limit).
- `Activated` (`NotificationActivatedEventArgs`): route on `Arguments`, the only part that
  survives an app restart. `Title`/`Message` are the clicked toast's own text, looked up by an id
  the platform adds to each toast's activation arguments - empty for a toast an earlier run
  showed, or one more than 100 toasts old (Windows hands back only the arguments). Raised on the
  UI thread: the service marshals the Toolkit's background activation callback to the application
  dispatcher, so handlers may touch bound collections directly.

## Push notifications client

A client only; no server ships. Two layers: `IPushNotificationService` (display, dedup,
actions) over `IPushNotificationTransport` (`StartAsync`/`StopAsync`, raw-JSON
`NotificationReceived`, optional `AcknowledgeAsync`). Default transport:
`SignalRPushNotificationTransport`. Source: `Essentials/PushNotifications/`.

```csharp
// templates/Combined/project-templates/barbatos-wpf-app/WpfProgram.cs, trimmed
builder.ConfigurePushNotifications(
    options => options.Enabled = false,           // true (the default) connects during Build()
    configureSignalR: options =>
    {
        options.ServerUrl = "https://localhost:5443/hubs/notifications";
        options.AppId = "BarbatosWpfApp";
        options.Tags["channel"] = "stable";
    });
```

- `ConfigurePushNotifications` calls `ConfigureNotifications()` itself (toasts are the primary
  display path).
- `Enabled` (default `true`) starts the transport fire-and-forget from the initializer. Without
  `ServerUrl`/`AppId` the SignalR transport throws `InvalidOperationException` - logged, not
  fatal. Connect later with `ConnectAsync()`.
- Display: a toast when `Availability == Enabled` and `IsEnabled`; otherwise, or if the toast
  throws, the in-app `IPushNotificationFallbackPresenter` window (bottom-right, `FallbackTimeout`
  default 5 s). `IPushNotification.ImageUrl` appears only in the fallback window - the toast
  deliberately leaves `ImagePath` unset because a URL there throws.
- `NotificationReceived` (UI thread) reports `ReceivedAt` and `UsedFallback` for every
  displayed notification.
- At-least-once delivery, kept invisible: every notification with a `ReceiptId` is acknowledged
  after display; the last `DeduplicationHistorySize` (default 256, `0` disables) ids are
  remembered so a re-delivery shows once - and is still re-acknowledged, because the repeat
  proves the first ack was lost. Malformed JSON is logged and left unacknowledged on purpose.
- The SignalR transport re-sends its handshake after every automatic reconnect (new connection
  id = anonymous client otherwise). Do not remove it.
- Actions: `Url`/`Setting` open through `ILauncher`; `Route` raises `RouteRequested` (only the app
  knows its screens); `None` does nothing. `RouteRequested` arrives on the UI thread for a toast
  click and a fallback-window click alike.
- Custom payload: implement `IPushNotification` (`Title`, `Body`, `ImageUrl`, `Action`,
  optional `ReceiptId`) and call `ConfigurePushNotifications<TNotification>()`. JSON is read with
  web defaults (case-insensitive names, numbers in strings).
- Custom transport: register it before `ConfigurePushNotifications` (`TryAddSingleton` then skips
  SignalR) and implement `IDisposable` as well as `IAsyncDisposable`. FCM/WNS are
  token-registration systems; the seam exists, an implementation does not.
- `SimulateNotificationAsync(notification)` runs the display pipeline with no ack and no dedup -
  for testing the UI path.
- SignalR options: `ServerUrl`, `AppId`, `DeviceId` (default `DeviceIdentity` instance id),
  `AppVersion`, `Platform`, `AppKey` + `AppKeyHeaderName` (default `X-App-Key`), `Tags`,
  `HandshakeMethodName` (`RegisterDevice`), `NotificationMethodName` (`ReceiveNotification`),
  `AcknowledgeMethodName` (`AcknowledgeNotification`; `null` sends no acks). Section
  `Barbatos:PushNotifications:SignalR`.

## Periodic services

```csharp
// samples/Barbatos.Wpf.Core.Sample/HeartbeatService.cs
public sealed class HeartbeatService : IWpfPeriodicService
{
    public string Name => "Heartbeat";

    public PeriodicSchedule Schedule => new()
    {
        Frequency = PeriodicFrequency.Custom,
        Interval = TimeSpan.FromSeconds(5),
        Description = "Logs a heartbeat into the lifecycle event list.",
    };

    public Task ExecuteAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

builder.ConfigurePeriodicServices<HeartbeatService>();   // registers it as IWpfPeriodicService (singleton)
```

- `ExecuteAsync` runs **on the UI thread** (a `DispatcherTimer`): touching UI is safe, CPU or
  blocking work freezes the app - push it into `Task.Run(..., cancellationToken)`. The token is
  cancelled when the host is disposed.
- `PeriodicFrequency`: `Once`, `Hourly`, `Custom` (needs a positive `Interval`) are fixed
  durations; `Daily`, `Weekly` (`DaysOfWeek`, a `[Flags] WeekDays`), `Monthly` (`DayOfMonth`
  clamped to short months) are wall-clock anchored at `TimeOfDay`. `StartTime`/`EndTime` bound
  the window.
- **Run history is per process.** With no `StartTime`, `Once`/`Hourly`/`Custom` run immediately at
  startup; `Once` runs once per launch, not once ever; an overdue `StartTime` also runs at once.
  An unset `TimeOfDay` takes `StartTime`'s time of day, or now's when `StartTime` is unset too -
  so a calendar schedule with neither runs at startup. A calendar occurrence missed while the app
  was closed is skipped, not caught up; periods missed while running (sleep, a long run) never
  replay as a backlog - the next run is the single next slot.
- A tick that arrives while the previous run is still executing is skipped. Failures are logged
  and reported through `ServiceExecuted` (`Error`), and the schedule continues.
- `Schedule` is read once, at registration. Change it with `UpdateSchedule(name, schedule)`
  (validated, cloned; applies after an in-flight run finishes). `Register(service)` /
  `Unregister(name)` work after startup; an in-flight run finishes but is not rescheduled.
- A `Barbatos:PeriodicServices:Schedules:<Name>` entry replaces the whole schedule - restate
  `Description` or it goes blank (the sample's `SettingsStore` does).
- An invalid schedule or a duplicate `Name` among DI-registered services throws from `Build()`
  (`PeriodicServiceFeatureTests`).
- Settings UI: `Services` lists the live `PeriodicServiceStatus` objects (`NextRunTime`,
  `LastRunTime`, `RunCount`, `IsCompleted`; their `Schedule` is a copy - mutating it does
  nothing). `PeriodicSchedule.Validate()` and the pure `GetNextOccurrence(now, lastRun)` preview a
  schedule before saving. Call the scheduler from the UI thread; it keeps unsynchronized state.

## Input system

```csharp
builder.ConfigureInputSystem(options =>
{
    var map = new InputActionMap("AppShortcuts");
    map.AddAction("QuickEntry")
       .AddKeyBinding(Key.K, ModifierKeys.Control)
       .AddGlobalBinding(Key.Space, ModifierKeys.Control | ModifierKeys.Alt);
    options.ActionMaps.Add(map);
});

// later, from an IWpfInitializeService registered after ConfigureInputSystem:
inputSystem.FindAction("QuickEntry")!.Performed += (_, _) => ShowQuickEntry();
```

| Binding | Fires | Interaction |
| --- | --- | --- |
| `KeyBinding(key, modifiers, interaction?)` | in-app, while the map's `Scope` (or any app window) has keyboard focus | yes |
| `GlobalKeyBinding(key, modifiers)` | OS-wide via `RegisterHotKey`, even minimized | no - `WM_HOTKEY` has no release |
| `ChordBinding(params (Key, ModifierKeys)[])` | in-app sequence (`Ctrl+K`, `Ctrl+P`), each step within `StepTimeout` (default 1.5 s) | no |

- No double fire to suppress: once `RegisterHotKey` owns a combination, Windows routes every press
  to `WM_HOTKEY`, even while this app is focused. The in-app twin only fires if global
  registration failed.
- Matching is exact (`ModifierKeys.None` never matches with a modifier held). Holding a key does
  not repeat `Performed` (auto-repeat ignored). The keystroke is **not** marked handled, so it
  still reaches the focused control. Alt combinations are unwrapped from `Key.System`.
- `InputActionMap.Scope` gates `KeyBinding` and `ChordBinding` (focus within the element), never
  `GlobalKeyBinding`. `InputActionMap.Enabled` and `IInputSystemService.SetEnabled` switch whole
  maps / the whole feature; disabling the service also unregisters hotkeys and resets interactions
  and chords.
- Chords: bare modifier presses never count as a step; an off-sequence key cancels; the timeout
  re-arms after each step.
- Interactions: `DefaultInteraction` (Started + Performed on press, **Canceled on every release**),
  `HoldInteraction(duration)` (Canceled on release even after it performed),
  `TapInteraction(maxDuration)`, `MultiTapInteraction(tapCount, tapTime, tapDelay)`,
  `PressInteraction(trigger)` (`PressInteractionTrigger.Press`, `Release`, `PressAndRelease`).
  Do not read `Canceled` as "the gesture failed". Interactions keep state - one instance per
  binding.
- Rebinding at runtime: `RemoveBinding`/`ClearBindings` + `AddKeyBinding`/`AddBinding`. Key and
  chord changes apply on the next keystroke; a `GlobalKeyBinding` change needs
  `IInputSystemService.RefreshBindings()` (harmless after any change; also reclaims state).
  Driving it from `IOptionsMonitor`: marshal `OnChange` to the UI thread first
  (`ShortcutRoutingInitializer.cs`).
- Failures are log warnings only: a refused global registration, and several enabled actions
  sharing one key in the same scope (all of them fire).
- `FindAction`/`FindActionMap` are exact, case-sensitive, first match wins.
- `ProcessKeyDown`/`ProcessKeyUp` run the real pipeline - script or unit-test shortcuts without a
  window (`InputSystemFeatureTests.cs`, `InputSystemScopeTests.cs`).
- `Barbatos.Wpf.Input.KeyBinding` collides with `System.Windows.Input.KeyBinding`; alias it.

## Dialogs

`ConfigureDialogs(o => o.CascadeCloseOwnedDialogs = true)` registers `IDialogService` and binds
`Barbatos:Dialogs`. Register each dialog window `AddTransient<TWindow>()`.

```csharp
_dialogService.Show<AboutWindow>();                              // non-modal, double-click safe
_dialogService.ShowDialog<AboutWindow>();                        // modal
_dialogService.Show(dialog, owner: someWindow, key: "customer-42", closeOthers: true);
```

- Dedup key defaults to the window type's full name; a key already open is restored and
  activated instead (`Show` returns `false`, `ShowDialog` returns `null`). The check runs before
  resolving `TWindow`, so a duplicate request constructs nothing. Use an entity id in `key` to
  allow one window per entity.
- Owner is set once before showing: `owner:` if given, else `ActiveWindow` - the most recently
  activated window this service tracks (its dialogs plus `MainWindow`), never the OS foreground
  window. A dialog opened from a dialog is owned by it.
- `closeOthers`, `CloseAll()` and `Close(key)` close gracefully: each dialog's `Closing` can veto;
  `CloseAll`/`Close` return `false` if anything stayed open.
- `CascadeCloseOwnedDialogs` (default `true`): WPF force-closes owned windows ignoring their
  veto (verified); the service closes them first with a respected veto and cancels the owner's
  close if any refuse. Applies to any window that owned a dialog shown through the service,
  `MainWindow` included.
- Only windows shown through the service are tracked; `IsOpen(key)`/`GetOpenDialog(key)` know
  nothing else.

## Keep awake

`ConfigureKeepAwake(o => { o.Enabled = false; o.KeepDisplayOn = false; })`; service
`IKeepAwakeService` (`IsEnabled`, `SetEnabled`, `IsEnabledChanged`).

- `SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED [| ES_DISPLAY_REQUIRED])`. The state
  is per thread, so it is always applied on the UI thread.
- `KeepDisplayOn` is read once; it cannot be changed at runtime. The request is released when the
  host is disposed.

## Run on startup

`ConfigureRunOnStartup(o => ...)`; service `IRunOnStartupService` (`IsEnabled`, `SetEnabled`,
`IsEnabledChanged`). Writes `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

- `RunOnStartupOptions`: `Enabled`, `EntryName` (default: entry-assembly name), `ExecutablePath`
  (default `Environment.ProcessPath`), `Arguments` (appended after the quoted path - e.g. a flag
  your app reads to start hidden in the tray).
- The OS persists this one; `IsEnabled` reads the registry live - but only the `Run` value. The
  Task Manager / Settings "Startup apps" switch is a separate `StartupApproved` entry that keeps
  `Run` in place, so a user who switched the app off there sees `IsEnabled == true` while Windows
  never launches it, and `SetEnabled(true)` cannot undo it. Do not write `StartupApproved` (it is
  undocumented and is the user's OS-level choice); say so in the settings UI instead.
- `Enabled = false` leaves an existing entry untouched, so the user's earlier choice survives.
  Unregister with `SetEnabled(false)`.
- `SetEnabled(true)` is a no-op while an entry with that name exists, even if it points at an old
  executable path (an app that moved or updated into a new folder). Toggle off and on to rewrite it.

## Feature symptoms

| Symptom | Cause | Fix |
| --- | --- | --- |
| Tray menu or icon change does nothing | Icon, menu and tooltip are read once, on the first show | `SetToolTip` for text; own `ITrayIconPlatform` for a dynamic menu |
| Tray icon never appears | `TrayIconOptions.Enabled` defaults to `false` | `Barbatos:TrayIcon:Enabled`, or `SetVisible(true)` |
| Balloon never shows | Icon hidden (no-op), or the user is away / in quiet time / full-screen | Nothing to query; do not rely on it for critical messages |
| `Activated` reports an empty `Title`/`Message` | The toast was shown by an earlier run (or more than 100 toasts ago); Windows returns only its arguments | Route on `Arguments`; treat `Title`/`Message` as display text |
| Periodic job runs as soon as the app starts | No `StartTime` on `Once`/`Hourly`/`Custom`, or neither `TimeOfDay` nor `StartTime` on a calendar schedule | Set `StartTime`/`TimeOfDay` |
| `Once` job runs on every launch | Run history is per process | Persist "already done" yourself (`barbatos-wpf-essentials` Preferences) |
| UI freezes every N minutes | `ExecuteAsync` runs on the UI thread | `await Task.Run(..., cancellationToken)` |
| "Run on startup" is checked but the app does not start with Windows | The user switched it off in Task Manager / Settings > Startup apps (`StartupApproved`), which `IsEnabled` does not read | Tell the user to switch it back on there; do not write that key |
| Schedule description blank after restart | A config override replaces the whole schedule | Restate `Description` in the override |
| `Build()` throws `ArgumentException`/`InvalidOperationException` with periodic services | Duplicate service `Name`, or an invalid schedule | Unique names; `Validate()` |
| Run-on-startup launches an old executable | `SetEnabled(true)` keeps an existing entry | `SetEnabled(false)` then `SetEnabled(true)` |
| Unchecking run-on-startup in config keeps it registered | `Enabled = false` leaves the entry by design | `SetEnabled(false)` from the settings UI |
| Same push shown twice | Server re-delivered without a `ReceiptId`, or history exceeded / set to `0` | Send `ReceiptId`; size `DeduplicationHistorySize` |
| Push notifications never connect | `Enabled` false, or `ServerUrl`/`AppId` unset (error only logged) | Configure `configureSignalR`, add a log provider |
| Push image missing from the toast | `ImageUrl` is only shown by the fallback window | By design |
| Hold action also raises `Canceled` | Every release raises `Canceled`, even after `Performed` | Handle `Performed`; ignore `Canceled` unless you need the release |
| Chord never completes | Step slower than `StepTimeout` (1.5 s), focus outside the map's `Scope`, or a different key in between | Raise `StepTimeout`; check `Scope` |
| Shortcut also types into the focused `TextBox` | The input system never marks key events handled | Choose combinations controls ignore, or scope the map |
| Several actions fire for one key | Duplicate bindings in one scope all fire (warning logged) | Remove the duplicate |
| Two dialogs of one type cannot be open | Default key is the type's full name | Pass a per-entity `key` |
| `CloseAll()` returns `false` | A dialog vetoed in its `Closing` | Expected - unsaved work is kept |

## Adding or changing a Core ConfigureXxx feature

Library change - also load `barbatos-package-authoring`.

1. `Essentials/Xxx/AppHostBuilderExtensions.cs`: a `partial` of `AppHostBuilderExtensions` in
   `namespace Barbatos.Wpf.Hosting`; `AddOptions<XxxOptions>()`, then `Configure(configure)`, then
   `Bind(builder.Configuration.GetSection(XxxOptions.SectionName))` - that order is what makes
   file values win.
2. `XxxOptions` with `public const string SectionName = "Barbatos:Xxx";`.
3. `TryAddSingleton` the platform seam and the service; `TryAddEnumerable` an
   `IWpfInitializeService` whose `Initialize` calls the service's `internal ApplyOptions()`. OS side
   effects happen there, never in a constructor.
4. OS interop behind a public `IXxxPlatform` so the service is testable headless.
5. Runtime toggle `IsEnabled` + `SetEnabled` + `IsEnabledChanged` (the tray uses
   `IsVisible`/`SetVisible`/`IsVisibleChanged`), raising only on an actual change.
6. Release OS state in `Dispose()`; implement `IDisposable` even when `IAsyncDisposable`.
7. Tests with a fake platform; README feature table and notes, API-REFERENCE, a sample settings
   row, a template symbol (`barbatos-docs-sync`, `barbatos-templates`).
