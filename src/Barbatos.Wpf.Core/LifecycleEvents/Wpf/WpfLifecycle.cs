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
    public delegate void OnStartup(Application application, StartupEventArgs args);
    public delegate void OnExit(Application application, ExitEventArgs args);
    public delegate void OnActivated(Application application, EventArgs args);
    public delegate void OnDeactivated(Application application, EventArgs args);
    public delegate void OnSessionEnding(Application application, SessionEndingCancelEventArgs args);
    public delegate void OnDispatcherUnhandledException(Application application, DispatcherUnhandledExceptionEventArgs args);

    // Window events
    public delegate void OnWindowCreated(Window window);
    public delegate void OnWindowLoaded(Window window, RoutedEventArgs args);
    public delegate void OnWindowActivated(Window window, EventArgs args);
    public delegate void OnWindowDeactivated(Window window, EventArgs args);
    public delegate void OnWindowStateChanged(Window window, EventArgs args);
    public delegate void OnWindowClosing(Window window, CancelEventArgs args);
    public delegate void OnWindowClosed(Window window, EventArgs args);
}
