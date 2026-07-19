// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using Barbatos.Wpf.LifecycleEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Hosting;

/// <summary>
/// The base <see cref="Application"/> class for hosted WPF applications. It builds the
/// <see cref="WpfApp"/> host on startup and surfaces the WPF application and window events
/// through the registered <see cref="WpfLifecycle"/> delegates.
/// </summary>
/// <remarks>
/// This is the WPF counterpart of .NET MAUI's <c>MauiWinUIApplication</c>.
/// </remarks>
public abstract class WpfApplication : Application, IWpfPlatformApplication
{
    private readonly Dictionary<Window, IServiceScope> _windowScopes = new();
    private WpfApp? _appHost;

    /// <summary>
    /// Creates the <see cref="WpfApp"/> host for this application.
    /// </summary>
    protected abstract WpfApp CreateWpfApp();

    /// <summary>
    /// Gets the current <see cref="WpfApplication"/> instance.
    /// </summary>
    public static new WpfApplication? Current => Application.Current as WpfApplication;

    /// <summary>
    /// Gets the <see cref="WpfApp"/> host built by <see cref="CreateWpfApp"/>.
    /// </summary>
    public WpfApp AppHost =>
        _appHost ?? throw new InvalidOperationException("The application host has not been created yet. It is only available after startup.");

    /// <summary>
    /// The application's configured services.
    /// </summary>
    public IServiceProvider Services => AppHost.Services;

    /// <summary>
    /// The WPF <see cref="System.Windows.Application"/> instance, i.e. this application.
    /// </summary>
    public Application Application => this;

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        _appHost = CreateWpfApp();
        IWpfPlatformApplication.Current = this;

        // Track every window in the application so the window lifecycle events are surfaced.
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnAnyWindowLoaded), handledEventsToo: true);

        DispatcherUnhandledException += OnAppDispatcherUnhandledException;

        Services.InvokeLifecycleEvents<WpfLifecycle.OnStartup>(del => del(this, e));

        base.OnStartup(e);
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        _appHost?.Services.InvokeLifecycleEvents<WpfLifecycle.OnExit>(del => del(this, e));

        base.OnExit(e);

        foreach (var scope in _windowScopes.Values)
            scope.Dispose();
        _windowScopes.Clear();

        if (IWpfPlatformApplication.Current == this)
            IWpfPlatformApplication.Current = null;

        _appHost?.Dispose();
        _appHost = null;
    }

    /// <inheritdoc />
    protected override void OnActivated(EventArgs e)
    {
        _appHost?.Services.InvokeLifecycleEvents<WpfLifecycle.OnActivated>(del => del(this, e));

        base.OnActivated(e);
    }

    /// <inheritdoc />
    protected override void OnDeactivated(EventArgs e)
    {
        _appHost?.Services.InvokeLifecycleEvents<WpfLifecycle.OnDeactivated>(del => del(this, e));

        base.OnDeactivated(e);
    }

    /// <inheritdoc />
    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        _appHost?.Services.InvokeLifecycleEvents<WpfLifecycle.OnSessionEnding>(del => del(this, e));

        base.OnSessionEnding(e);
    }

    void OnAppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) =>
        _appHost?.Services.InvokeLifecycleEvents<WpfLifecycle.OnDispatcherUnhandledException>(del => del(this, e));

    void OnAnyWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Window window || _appHost is null)
            return;

        if (!_windowScopes.ContainsKey(window))
        {
            // Create a window-scoped service scope and initialize any window-scoped
            // services, for example the window dispatchers.
            var scope = Services.CreateScope();
            _windowScopes[window] = scope;
            scope.ServiceProvider.InitializeScopedServices();

            Services.InvokeLifecycleEvents<WpfLifecycle.OnWindowCreated>(del => del(window));

            window.Activated += OnWindowActivated;
            window.Deactivated += OnWindowDeactivated;
            window.StateChanged += OnWindowStateChanged;
            window.Closing += OnWindowClosing;
            window.Closed += OnWindowClosed;
        }

        Services.InvokeLifecycleEvents<WpfLifecycle.OnWindowLoaded>(del => del(window, e));
    }

    void OnWindowActivated(object? sender, EventArgs e)
    {
        if (sender is Window window)
            _appHost?.Services.InvokeLifecycleEvents<WpfLifecycle.OnWindowActivated>(del => del(window, e));
    }

    void OnWindowDeactivated(object? sender, EventArgs e)
    {
        if (sender is Window window)
            _appHost?.Services.InvokeLifecycleEvents<WpfLifecycle.OnWindowDeactivated>(del => del(window, e));
    }

    void OnWindowStateChanged(object? sender, EventArgs e)
    {
        if (sender is Window window)
            _appHost?.Services.InvokeLifecycleEvents<WpfLifecycle.OnWindowStateChanged>(del => del(window, e));
    }

    void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (sender is Window window)
            _appHost?.Services.InvokeLifecycleEvents<WpfLifecycle.OnWindowClosing>(del => del(window, e));
    }

    void OnWindowClosed(object? sender, EventArgs e)
    {
        if (sender is not Window window)
            return;

        _appHost?.Services.InvokeLifecycleEvents<WpfLifecycle.OnWindowClosed>(del => del(window, e));

        window.Activated -= OnWindowActivated;
        window.Deactivated -= OnWindowDeactivated;
        window.StateChanged -= OnWindowStateChanged;
        window.Closing -= OnWindowClosing;
        window.Closed -= OnWindowClosed;

        if (_windowScopes.Remove(window, out var scope))
            scope.Dispose();
    }
}
