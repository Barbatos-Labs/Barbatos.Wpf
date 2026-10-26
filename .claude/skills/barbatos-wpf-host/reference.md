# Desktop feature reference

Per-feature behavior that is easy to get wrong. Full prose:
[`src/Barbatos.Wpf.Core/README.md#optional-desktop-features`](../../../src/Barbatos.Wpf.Core/README.md).

---

## Single instance

Keyed by `AppInfo.AppId` via a named `Mutex`, scoped to the current user's session (not
machine-wide). A second launch detects the running instance **during `Build()`, before any
window exists**, signals it, and calls `Environment.Exit(0)`. The first instance's
`SecondInstanceLaunched` fires on the UI thread and — unless `ActivateMainWindow = false` —
restores/shows/foregrounds `Application.Current.MainWindow` automatically.

Consequence: anything you need to happen on a duplicate launch has to be wired *before*
`builder.Build()` returns.

---

## Tray icon

Backed directly by Win32 `Shell_NotifyIcon` through a hidden `HwndSource` — **no
`UseWindowsForms`, no `System.Windows.Forms.NotifyIcon`**. Never add a WinForms reference for
a tray icon.

```csharp
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

`TrayMenuItem`: `Header`, `Action`, `IconPath` (`.ico`), `IsEnabled`, `IsDefault` (bold), and
the static `Separator`. `ShowBalloonTip(title, text, icon)` is a no-op while `IsVisible` is
false. `MenuItems` is code-only — it has no configuration counterpart.

---

## Notifications (toast)

Full adaptive Windows toasts via `ToastContentBuilder`/`ToastNotificationManagerCompat`, working
for plain unpackaged Win32/WPF apps with no Start-menu shortcut or COM/AUMID registration.

- `Show(title, message, severity)` is a **no-op while `IsEnabled` is false** — one settings
  toggle silences every call site.
- `Show(NotificationContent)` for an image, up to five buttons, and a navigation payload.
  `NotificationButton(text, arguments)` raises `Activated`; `NotificationButton(text, launchUri)`
  opens directly without waking the app.
- Windows **silently drops** a toast it isn't allowed to show. `Show(...)` cannot tell you.
  Read `INotificationService.Availability` (a live, uncached `NotificationAvailability`) when
  you need to know, show an in-app fallback, and offer `OpenSystemSettings()` as the fix.
- `NotificationOptions.IconPath` is the persistent per-app logo overlay; per-call visuals belong
  on `NotificationContent`.

---

## Push notifications (client only — no server included)

Two deliberately separate layers:

- `IPushNotificationService` — transport-agnostic. Displays through `INotificationService`,
  falling back to an in-app `IPushNotificationFallbackPresenter` window whenever
  `Availability != Enabled`, `IsEnabled == false`, or the real toast throws.
- `IPushNotificationTransport` — `StartAsync`/`StopAsync`/`NotificationReceived(raw JSON)`.
  The bundled `SignalRPushNotificationTransport` owns every SignalR detail (hub URL, method
  names, handshake) in its own `SignalRPushNotificationOptions`.

Swap the transport by registering it **before** `ConfigurePushNotifications` (the `TryAddSingleton`
inside then skips the default). FCM/WNS do not map onto this shape — they are token-registration
+ OS-channel systems; the seam exists, a ready implementation does not.

Custom payload shape: implement `IPushNotification` and call
`ConfigurePushNotifications<TNotification>()`.

Two behaviors that exist for real-world reliability and must not be removed:

- **At-least-once delivery, deduplicated.** Every notification with a `ReceiptId` is
  acknowledged after display; the last `DeduplicationHistorySize` ids are remembered so a
  re-delivery is shown once — but still re-acknowledged, because the repeat is evidence the
  first ack was lost.
- **The handshake is re-sent after every automatic reconnect.** SignalR issues a new connection
  id, so a server mapping connections to devices would otherwise see an anonymous stranger.

`Action` dispatch: `Url`/`Setting` open via `ILauncher` automatically; `Route` raises
`RouteRequested` because only the app knows its own screens.

---

## Periodic services

```csharp
builder.ConfigurePeriodicServices<HeartbeatService>();
```

```csharp
public sealed class HeartbeatService : IWpfPeriodicService
{
    public string Name => "Heartbeat";

    public PeriodicSchedule Schedule => new()
    {
        Frequency = PeriodicFrequency.Custom,
        Interval = TimeSpan.FromSeconds(5),
        Description = "Logs a heartbeat.",
    };

    public Task ExecuteAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
```

`ExecuteAsync` runs on the application dispatcher — touching the UI is safe.

`Schedule` is read **once**, at registration. Change what is active with
`IPeriodicServiceScheduler.UpdateSchedule(name, schedule)`. Register at runtime with
`Register(service)` / `Unregister(name)`; an execution already in flight finishes but is not
rescheduled.

`PeriodicSchedule` splits into calendar-anchored (`Daily`/`Weekly`/`Monthly` — a wall-clock
`TimeOfDay`, a `DaysOfWeek` flags set, a `DayOfMonth` clamped to short months) and fixed-duration
(`Hourly`/`Custom` + `Interval`). `Validate()` is a pure consistency check;
`GetNextOccurrence(now, lastRun)` is a pure function that never reads the clock — use it to
preview a schedule in a settings UI. Missed occurrences catch up to a **single** next
occurrence, never a replayed backlog.

A configuration override replaces the schedule wholesale, so restate `Description` too or it
reverts to blank.

---

## Input system (keyboard shortcuts)

Three binding kinds under one `InputAction`; the same `Started`/`Performed`/`Canceled` events
fire regardless of which one triggered.

| Binding | Scope | Interaction? |
| --- | --- | --- |
| `KeyBinding(key, modifiers, interaction?)` | in-app, needs focus (optionally `InputActionMap.Scope`) | yes |
| `GlobalKeyBinding(key, modifiers)` | OS-level `RegisterHotKey`, fires while minimized | **no** — `WM_HOTKEY` has no "released" message |
| `ChordBinding(params (Key, ModifierKeys)[])` | in-app *sequence* (`Ctrl+K` then `Ctrl+P`) | no — the sequence is the gesture |

```csharp
builder.ConfigureInputSystem(options =>
{
    var map = new InputActionMap("AppShortcuts");
    map.AddAction("QuickEntry")
       .AddKeyBinding(Key.K, ModifierKeys.Control)
       .AddGlobalBinding(Key.Space, ModifierKeys.Control | ModifierKeys.Alt);
    options.ActionMaps.Add(map);
});

// after Build(), typically from an IWpfInitializeService:
inputSystem.FindAction("QuickEntry")!.Performed += (_, _) => ShowQuickEntry();
```

**There is no double-fire to suppress.** Once `RegisterHotKey` claims a combination, Windows
routes every press into `WM_HOTKEY` exclusively — system-wide, including while this app is
focused — so the `KeyBinding` path structurally never sees that keystroke. The in-app binding is
a fallback for when the global registration fails (another app owns the combination).

Matching is always exact: `ModifierKeys.None` never matches with a modifier held, like WPF's own
`KeyGesture`.

Runtime rebinding (a "customize shortcuts" screen): `RemoveBinding`/`ClearBindings` +
`AddBinding`. A `KeyBinding`/`ChordBinding` change takes effect on the very next keystroke;
a `GlobalKeyBinding` change **also needs `IInputSystemService.RefreshBindings()`** to re-register
with the OS. Calling it after any mutation is harmless and reclaims per-binding state.

Interactions: `DefaultInteraction` (Started+Performed on press, Canceled on release),
`HoldInteraction(duration)`, `TapInteraction(maxDuration)`,
`MultiTapInteraction(count, tapTime, tapDelay)`, `PressInteraction(Press|Release|PressAndRelease)`.

`ProcessKeyDown`/`ProcessKeyUp` are public so a shortcut can be scripted or unit-tested with no
window and no keyboard.

---

## Dialogs

`ConfigureDialogs()` registers `IDialogService`, which centralizes owner assignment,
duplicate-open prevention (a double-click cannot open two), and graceful bulk-close of child
windows that lets in-progress work cancel the close.

```csharp
dialogService.Show<AboutWindow>();
```

Register the window type itself as **transient** — a closed WPF `Window` cannot be shown again.

---

## Keep awake / run on startup

`IKeepAwakeService` blocks idle sleep via `SetThreadExecutionState`; `KeepDisplayOn` also keeps
the display on. The block is released when the host is disposed.

`IRunOnStartupService` writes the current user's `Run` registry key — the OS persists this
state, unlike every other toggle here.
