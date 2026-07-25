// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.Dialogs;

/// <summary>
/// The default <see cref="IDialogService"/> implementation. Entirely self-contained: it
/// learns about windows only from the ones it is asked to show (plus
/// <see cref="Application.MainWindow"/>, opportunistically, the first time it is observed),
/// hooking each one's own <see cref="Window.Activated"/>/<see cref="Window.Closed"/> events
/// directly rather than depending on any app-wide lifecycle wiring.
/// </summary>
internal sealed class DialogService : IDialogService
{
    readonly IServiceProvider _services;
    readonly DialogOptions _options;

    // Only windows shown *through* this service (keyed by their dedup key) - what CloseAll()/
    // IsOpen()/GetOpenDialog() operate on.
    readonly Dictionary<string, Window> _openDialogs = new(StringComparer.Ordinal);

    // Every window this service has ever hooked events on, whether shown through it or seen
    // as an owner/MainWindow - guards against double-subscribing the same window.
    readonly HashSet<Window> _trackedWindows = new();

    Window? _activeWindow;

    public DialogService(IServiceProvider services, IOptions<DialogOptions> options)
    {
        _services = services;
        _options = options.Value;
    }

    public Window? ActiveWindow
    {
        get
        {
            if (_activeWindow is { IsLoaded: true })
                return _activeWindow;

            // Opportunistically start tracking MainWindow the first time it's asked for, so
            // that if the app later re-activates it, ActiveWindow reflects that too - without
            // requiring every app to explicitly pass it as an owner up front.
            var mainWindow = Application.Current?.MainWindow;
            if (mainWindow is { IsLoaded: true })
                TrackWindow(mainWindow);

            return mainWindow;
        }
    }

    public bool? ShowDialog<TWindow>(Window? owner = null, string? key = null, bool closeOthers = false) where TWindow : Window
    {
        var resolvedKey = key ?? typeof(TWindow).FullName!;

        if (ActivateIfAlreadyOpen(resolvedKey))
            return null;

        return ShowDialogCore(_services.GetRequiredService<TWindow>(), owner, resolvedKey, closeOthers);
    }

    public bool? ShowDialog(Window dialog, Window? owner = null, string? key = null, bool closeOthers = false)
    {
        _ = dialog ?? throw new ArgumentNullException(nameof(dialog));

        var resolvedKey = key ?? dialog.GetType().FullName!;

        if (ActivateIfAlreadyOpen(resolvedKey))
            return null;

        return ShowDialogCore(dialog, owner, resolvedKey, closeOthers);
    }

    bool? ShowDialogCore(Window dialog, Window? owner, string key, bool closeOthers)
    {
        if (closeOthers)
            CloseAll();

        PrepareToShow(dialog, owner, key);

        // ShowDialog() always ends with the window Closed (either via DialogResult being set,
        // or Close() being called directly), which synchronously runs OnAnyTrackedWindowClosed
        // and untracks it - so there is nothing left to clean up once this returns.
        return dialog.ShowDialog();
    }

    public bool Show<TWindow>(Window? owner = null, string? key = null, bool closeOthers = false) where TWindow : Window
    {
        var resolvedKey = key ?? typeof(TWindow).FullName!;

        if (ActivateIfAlreadyOpen(resolvedKey))
            return false;

        ShowCore(_services.GetRequiredService<TWindow>(), owner, resolvedKey, closeOthers);
        return true;
    }

    public bool Show(Window dialog, Window? owner = null, string? key = null, bool closeOthers = false)
    {
        _ = dialog ?? throw new ArgumentNullException(nameof(dialog));

        var resolvedKey = key ?? dialog.GetType().FullName!;

        if (ActivateIfAlreadyOpen(resolvedKey))
            return false;

        ShowCore(dialog, owner, resolvedKey, closeOthers);
        return true;
    }

    void ShowCore(Window dialog, Window? owner, string key, bool closeOthers)
    {
        if (closeOthers)
            CloseAll();

        PrepareToShow(dialog, owner, key);
        dialog.Show();
    }

    bool ActivateIfAlreadyOpen(string key)
    {
        if (!_openDialogs.TryGetValue(key, out var existing))
            return false;

        ActivateWindow(existing);
        return true;
    }

    void PrepareToShow(Window dialog, Window? owner, string key)
    {
        // Resolved and assigned exactly once, before this (fresh, not-yet-shown) window is
        // ever shown - WPF forbids changing Owner afterwards, and doing it this way (rather
        // than reusing a previously-shown instance) is what avoids the "two dialogs fighting
        // over an owner" class of bug: every dialog gets its own, deliberately-resolved owner
        // exactly once.
        var resolvedOwner = owner ?? ActiveWindow;
        if (resolvedOwner is not null && !ReferenceEquals(resolvedOwner, dialog))
        {
            dialog.Owner = resolvedOwner;
            TrackWindow(resolvedOwner);
        }

        _openDialogs[key] = dialog;
        TrackWindow(dialog);
    }

    void TrackWindow(Window window)
    {
        if (!_trackedWindows.Add(window))
            return;

        window.Closing += OnAnyTrackedWindowClosing;
        window.Activated += OnAnyTrackedWindowActivated;
        window.Closed += OnAnyTrackedWindowClosed;
    }

    void OnAnyTrackedWindowClosing(object? sender, CancelEventArgs e)
    {
        if (sender is not Window window || !_options.CascadeCloseOwnedDialogs)
            return;

        // WPF itself closes a window's owned windows once *it* closes - but does so
        // unconditionally, ignoring each owned window's own Closing veto (confirmed: an owned
        // window's Closing/e.Cancel=true does not stop it from being force-closed once its
        // owner closes). That is exactly the data-loss risk this option exists to prevent, so
        // this closes every dialog *this* window owns proactively, before WPF's own cascade
        // would run - each still gets a real, respected chance to veto via its own Closing
        // (recursively too, since this same handler is hooked on every tracked window). If any
        // of them refuse to close, this window's own close is cancelled as well, so the whole
        // chain stays open together rather than tearing down partway through.
        foreach (var owned in _openDialogs.Values.Where(w => ReferenceEquals(w.Owner, window)).ToArray())
        {
            owned.Close();

            if (_trackedWindows.Contains(owned))
            {
                e.Cancel = true;
                return;
            }
        }
    }

    void OnAnyTrackedWindowActivated(object? sender, EventArgs e)
    {
        if (sender is Window window)
            _activeWindow = window;
    }

    void OnAnyTrackedWindowClosed(object? sender, EventArgs e)
    {
        if (sender is not Window window)
            return;

        window.Closing -= OnAnyTrackedWindowClosing;
        window.Activated -= OnAnyTrackedWindowActivated;
        window.Closed -= OnAnyTrackedWindowClosed;
        _trackedWindows.Remove(window);

        if (ReferenceEquals(_activeWindow, window))
        {
            _activeWindow = null;

            // Closing an owned window is supposed to hand activation back to its owner, but
            // (like ActivateWindow()'s own Topmost-toggle already works around) Windows'
            // foreground-lock rules can let some other process - e.g. a debugger-attached IDE -
            // steal it instead. If some other tracked window had already picked up activation
            // natively, it would have fired its own Activated and updated _activeWindow away
            // from this one before this handler ever ran - so still finding it here means that
            // never happened, and the owner needs a manual nudge back to the foreground.
            //
            // NOTE: manual testing against a live Rider debug session showed this - and every
            // timing variant tried (delayed, repeated, held-Topmost) - reliably wins the
            // *first* time a dialog closes in a given app run, but can still lose to Rider
            // reclaiming the foreground on later closes. That looks like Rider itself
            // repeatedly reasserting itself during an active debug session rather than a
            // one-shot race this process can out-wait, so it isn't fixable purely from here;
            // this still fixes the original gap (nothing was attempted at all before) and the
            // common case.
            //
            // Deliberately not reusing ActivateWindow() here: its Show() fallback is only safe
            // for reopening a possibly-hidden *tracked* dialog. This Closed can fire reentrantly
            // while the owner is cascading its own close down to this window (WPF itself closes
            // owned windows once their owner closes) - i.e. with the owner's own WmDestroy still
            // on the call stack - and WPF forbids Show()/Close() on a window while it is itself
            // mid-close. Activate()/Topmost/Focus() carry no such restriction.
            if (window.Owner is { IsLoaded: true } owner)
            {
                owner.Topmost = true;
                owner.Activate();
                owner.Topmost = false;
                owner.Focus();
            }
        }

        UntrackDialog(window);
    }

    void UntrackDialog(Window window)
    {
        var entry = _openDialogs.FirstOrDefault(kv => ReferenceEquals(kv.Value, window));
        if (entry.Key is not null)
            _openDialogs.Remove(entry.Key);
    }

    static void ActivateWindow(Window window)
    {
        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;

        if (!window.IsVisible)
            window.Show();

        // Activate() alone can be silently ignored by Windows' foreground-lock rules when
        // another app currently owns the foreground; briefly toggling Topmost is the
        // standard, reliable way around that without SetForegroundWindow P/Invoke.
        window.Topmost = true;
        window.Activate();
        window.Topmost = false;
        window.Focus();
    }

    public bool IsOpen(string key) => _openDialogs.ContainsKey(key);

    public Window? GetOpenDialog(string key) => _openDialogs.TryGetValue(key, out var window) ? window : null;

    public bool CloseAll()
    {
        var allClosed = true;

        // Snapshot: closing a window synchronously untracks it (and possibly cascades to
        // close others), mutating _openDialogs while it would otherwise still be enumerating.
        foreach (var window in _openDialogs.Values.ToArray())
        {
            window.Close();

            // Window.IsLoaded/IsVisible aren't reliable "did it actually close" signals here
            // (WPF only fully settles them once its dispatcher pumps a frame), but
            // OnAnyTrackedWindowClosed - which Window.Close() runs synchronously, veto aside -
            // is exactly what untracks a window from _trackedWindows, so that's the
            // authoritative check: still tracked means Closing cancelled the close.
            if (_trackedWindows.Contains(window))
                allClosed = false;
        }

        return allClosed;
    }

    public bool Close(string key)
    {
        if (!_openDialogs.TryGetValue(key, out var window))
            return true;

        window.Close();
        return !_trackedWindows.Contains(window);
    }
}
