// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

namespace Barbatos.Wpf.LifecycleEvents;

/// <summary>
/// Defines the delegates for the WPF application and window lifecycle events.
/// </summary>
/// <remarks>
/// This is the WPF counterpart of .NET MAUI's <c>WindowsLifecycle</c>, mapped onto the
/// events of <see cref="System.Windows.Application"/> and <see cref="System.Windows.Window"/>.
/// </remarks>
public static class WpfLifecycle
{
    // Application events
    /// <summary>Raised from <see cref="Application.OnStartup"/>, after the app host is built and before the base startup logic runs (so before <c>StartupUri</c> is shown).</summary>
    public delegate void OnStartup(Application application, StartupEventArgs args);
    /// <summary>Raised from <see cref="Application.OnExit"/>, before the window scopes and the app host are disposed.</summary>
    public delegate void OnExit(Application application, ExitEventArgs args);
    /// <summary>Raised when the application becomes the foreground application (<see cref="Application.Activated"/>).</summary>
    public delegate void OnActivated(Application application, EventArgs args);
    /// <summary>Raised when the application stops being the foreground application (<see cref="Application.Deactivated"/>).</summary>
    public delegate void OnDeactivated(Application application, EventArgs args);
    /// <summary>Raised when the user logs off or shuts down Windows (<see cref="Application.SessionEnding"/>); setting <c>args.Cancel</c> asks to keep the session.</summary>
    public delegate void OnSessionEnding(Application application, SessionEndingCancelEventArgs args);
    /// <summary>Raised for an exception left unhandled on the UI thread (<see cref="Application.DispatcherUnhandledException"/>); setting <c>args.Handled</c> keeps the app running.</summary>
    public delegate void OnDispatcherUnhandledException(Application application, DispatcherUnhandledExceptionEventArgs args);

    // Window events
    /// <summary>Raised once per window, the first time it is loaded, right after its DI scope is created and its <see cref="Hosting.IWpfInitializeScopedService"/>s have run - not from its constructor.</summary>
    public delegate void OnWindowCreated(Window window);
    /// <summary>Raised every time a window is loaded (<see cref="FrameworkElement.Loaded"/>), after <see cref="OnWindowCreated"/> on the first time.</summary>
    public delegate void OnWindowLoaded(Window window, RoutedEventArgs args);
    /// <summary>Raised when a window becomes the active window (<see cref="Window.Activated"/>).</summary>
    public delegate void OnWindowActivated(Window window, EventArgs args);
    /// <summary>Raised when a window stops being the active window (<see cref="Window.Deactivated"/>).</summary>
    public delegate void OnWindowDeactivated(Window window, EventArgs args);
    /// <summary>Raised when a window is minimized, maximized or restored (<see cref="Window.StateChanged"/>).</summary>
    public delegate void OnWindowStateChanged(Window window, EventArgs args);
    /// <summary>Raised when a window is about to close (<see cref="Window.Closing"/>); setting <c>args.Cancel</c> keeps it open.</summary>
    public delegate void OnWindowClosing(Window window, CancelEventArgs args);
    /// <summary>Raised after a window has closed (<see cref="Window.Closed"/>), before its DI scope is disposed.</summary>
    public delegate void OnWindowClosed(Window window, EventArgs args);
}
