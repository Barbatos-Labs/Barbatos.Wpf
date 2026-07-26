using System.Windows;
using System.Windows.Input;
using Barbatos.Wpf.Input;

namespace Barbatos.Wpf.AquariusRouter.Sample.Routing;

/// <summary>
/// Bridges <c>Barbatos.Wpf.Core</c>'s InputSystem (real keyboard shortcuts - in-app and true
/// OS-level global hotkeys) to router deep links - the "shortcut router" pattern the AquariusRouter
/// sample set out to demonstrate. Targets the exact same guarded <c>/settings/*</c> destinations
/// this sample's <c>/settings</c> subtree already has multiple <c>RouterLink</c> entry points into
/// (see <see cref="AppRoutes"/>'s own remarks) - so pressing a shortcut is just a fourth way in,
/// not a special case the router needs to know about.
/// </summary>
/// <remarks>
/// Four shortcuts, each demonstrating a different capability:
/// <list type="bullet">
/// <item><description><c>Ctrl+F9</c> → <c>/dashboard</c> - a plain in-app <see cref="Barbatos.Wpf.Input.KeyBinding"/>.
/// Only fires while this window has focus - try it while a different app is focused, nothing
/// happens.</description></item>
/// <item><description><c>Ctrl+F8</c> → <c>/settings/security</c> - the actual dual-binding
/// unification: <b>one</b> action carrying both a <see cref="Barbatos.Wpf.Input.KeyBinding"/> and a
/// <see cref="GlobalKeyBinding"/> for the identical chord, both driving the same
/// <see cref="InputAction.Performed"/> handler. In practice only the global path ever actually
/// fires from a real keystroke - once <c>RegisterHotKey</c> claims a chord, Windows routes it into
/// <c>WM_HOTKEY</c> exclusively, focused or not (confirmed empirically; see
/// <see cref="InputSystemService"/>'s own remarks) - so the <see cref="Barbatos.Wpf.Input.KeyBinding"/>
/// half is really a fallback for the (rare) case the global registration itself fails, e.g.
/// another app already owns that exact chord.</description></item>
/// <item><description>Holding <c>Ctrl+Shift+F8</c> for half a second → <c>/settings/notifications</c> -
/// demonstrates <see cref="HoldInteraction"/> live. In-app only:
/// <see cref="GlobalKeyBinding"/> cannot carry an interaction at all (see its own remarks for the
/// hard OS-level reason why).</description></item>
/// <item><description>Pressing <c>Ctrl+K</c> then, within the default step timeout, <c>Ctrl+P</c> →
/// the "profile" leaf of <c>/settings</c> - demonstrates a <see cref="ChordBinding"/> (VS Code's
/// "leader key" shortcuts are the model). In-app only, same reason as the Hold shortcut above -
/// see <see cref="ChordBinding"/>'s own remarks. This one also demonstrates name-based
/// <see cref="Router.Push(RouteLocationRaw)"/>: <c>settings-profile</c> is declared with an empty
/// <c>Path</c> (see <see cref="AppRoutes"/>), so it has no literal path string of its own to push
/// to in the first place.</description></item>
/// </list>
/// </remarks>
public static class ShortcutRouting
{
    /// <summary>Declares the action map - called from <c>ConfigureInputSystem</c>, before the host is built, so no <see cref="Router"/> reference is available (or needed) yet.</summary>
    public static void ConfigureActions(InputSystemOptions options)
    {
        var shortcuts = new InputActionMap("AppShortcuts");

        shortcuts.AddAction("GoToDashboard")
            .AddKeyBinding(Key.F9, ModifierKeys.Control);

        shortcuts.AddAction("GoToSecuritySettings")
            .AddKeyBinding(Key.F8, ModifierKeys.Control)
            .AddGlobalBinding(Key.F8, ModifierKeys.Control);

        shortcuts.AddAction("GoToNotificationSettings")
            .AddKeyBinding(Key.F8, ModifierKeys.Control | ModifierKeys.Shift, new HoldInteraction(TimeSpan.FromMilliseconds(500)));

        shortcuts.AddAction("GoToProfileSettings")
            .AddChordBinding((Key.K, ModifierKeys.Control), (Key.P, ModifierKeys.Control));

        options.ActionMaps.Add(shortcuts);
    }

    /// <summary>Wires each action's <see cref="InputAction.Performed"/> to a route - called once <see cref="Router"/> is resolvable, after the host is built.</summary>
    public static void Register(IInputSystemService inputSystem, Router router)
    {
        inputSystem.FindAction("GoToDashboard")!.Performed += async (_, _) => await router.Push("/dashboard");

        // The only one of the four reachable while minimized/unfocused (via GlobalKeyBinding) -
        // navigating alone is invisible if the window itself never comes back on screen, so this
        // one also restores+activates it first. The other three only ever fire while the window
        // already has keyboard focus (in-app KeyBinding/ChordBinding only), so it would be a
        // no-op for them.
        inputSystem.FindAction("GoToSecuritySettings")!.Performed += async (_, _) =>
        {
            RestoreAndActivateMainWindow();
            await router.Push("/settings/security");
        };

        inputSystem.FindAction("GoToNotificationSettings")!.Performed += async (_, _) => await router.Push("/settings/notifications");

        // settings-profile is an empty-path child (see AppRoutes) - pushing the literal string
        // "/settings/profile" would not match it at all, so this is name-based instead of every
        // other shortcut/RouterLink in this sample, which all target a real literal path.
        inputSystem.FindAction("GoToProfileSettings")!.Performed += async (_, _) =>
            await router.Push(new RouteLocationRaw { Name = "settings-profile" });
    }

    /// <summary>
    /// Restores (if minimized) and brings the main window to the foreground - the same proven
    /// technique <c>Barbatos.Wpf.Core</c>'s own <c>SingleInstanceService.ActivateWindow</c> uses
    /// for the analogous "a second launch attempt should reactivate the existing window" case.
    /// Deliberately app-level code, not something <c>GlobalKeyBinding</c>/<c>InputSystemService</c>
    /// does automatically - plenty of real global hotkeys (mute, media play/pause, push-to-talk)
    /// should NOT yank focus away from whatever the user was doing, so this has to be an explicit
    /// per-action choice in the app's own <see cref="InputAction.Performed"/> handler, not a
    /// library default.
    /// </summary>
    static void RestoreAndActivateMainWindow()
    {
        if (Application.Current?.MainWindow is not { } window)
            return;

        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;

        if (!window.IsVisible)
            window.Show();

        // Window.Activate() alone can be silently ignored by Windows' foreground-lock rules when
        // another app currently owns the foreground; briefly toggling Topmost is the standard,
        // reliable way around that without SetForegroundWindow P/Invoke.
        window.Topmost = true;
        window.Activate();
        window.Topmost = false;
        window.Focus();
    }
}
