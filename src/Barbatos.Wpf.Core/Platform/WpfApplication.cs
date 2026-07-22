// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel;
using System.Diagnostics;
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
    private Window? _splashScreen;
    private Stopwatch? _splashStopwatch;
    private ShutdownMode _shutdownModeBeforeSplash;

    /// <summary>
    /// Creates the <see cref="WpfApp"/> host for this application.
    /// </summary>
    protected abstract WpfApp CreateWpfApp();

    /// <summary>
    /// Override to configure the built-in <see cref="SplashWindow"/> (app name, logo, sponsor
    /// logos, related links, minimum display duration, ...). Returns <see langword="null"/> (the
    /// default) for no splash screen. For full control over the splash screen's UI instead of
    /// the built-in layout, override <see cref="CreateSplashScreen"/> instead.
    /// </summary>
    protected virtual SplashScreenOptions? GetSplashScreenOptions() => null;

    /// <summary>
    /// Override to provide a fully custom splash screen window instead of the built-in
    /// <see cref="SplashWindow"/>. Implement <see cref="ISplashScreen"/> on it to control
    /// <see cref="ISplashScreen.MinimumDisplayDuration"/>; without it, the minimum display
    /// duration is treated as zero. The default implementation creates a
    /// <see cref="SplashWindow"/> from <see cref="GetSplashScreenOptions"/>, or returns
    /// <see langword="null"/> (no splash screen) when that itself returns <see langword="null"/>.
    /// </summary>
    protected virtual Window? CreateSplashScreen()
    {
        var options = GetSplashScreenOptions();
        return options is null ? null : new SplashWindow(options);
    }

    /// <summary>
    /// Waits out any remaining <see cref="ISplashScreen.MinimumDisplayDuration"/> and then
    /// closes the splash screen shown for <see cref="CreateSplashScreen"/> (if any). Call this
    /// from your own <c>OnStartup</c> override, after <c>base.OnStartup(e)</c> and before
    /// showing your main window, when you use a splash screen; it is a no-op otherwise.
    /// </summary>
    /// <remarks>
    /// The clock starts as soon as the splash screen is shown, before <see cref="CreateWpfApp"/>
    /// runs, so slow startup work is only ever waited out - never artificially delayed further -
    /// by the minimum display duration.
    /// </remarks>
    protected async Task CloseSplashScreenAsync()
    {
        if (_splashScreen is not { } splash)
            return;

        var minimumDuration = (splash as ISplashScreen)?.MinimumDisplayDuration ?? TimeSpan.Zero;
        var remaining = minimumDuration - _splashStopwatch!.Elapsed;
        if (remaining > TimeSpan.Zero)
            await Task.Delay(remaining);

        splash.Close();
        _splashScreen = null;
        _splashStopwatch = null;

        // Restores whatever ShutdownMode was in effect before the splash screen was shown (see
        // OnStartup) - now that the splash is closed, it is up to the caller to show a main
        // window next, and ordinary ShutdownMode.OnLastWindowClose semantics should apply again
        // once it does.
        ShutdownMode = _shutdownModeBeforeSplash;
    }

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
        // Shown before CreateWpfApp() (and before anything else) so it actually covers slow
        // startup work - e.g. a slow IWpfInitializeService. Note that purely synchronous work
        // still blocks the UI thread as usual, so the splash screen (and its progress
        // indicator) will not animate during that specific window; move slow work to an async
        // continuation awaited before CloseSplashScreenAsync() if that matters for your app.
        _splashScreen = CreateSplashScreen();
        if (_splashScreen is not null)
        {
            // The default ShutdownMode (OnLastWindowClose) would otherwise shut the whole
            // application down the moment the splash screen - briefly the only open window -
            // is closed in CloseSplashScreenAsync(), before a main window ever gets shown.
            // Forcing OnExplicitShutdown for the splash's lifetime avoids that; restored once
            // CloseSplashScreenAsync() actually closes it.
            _shutdownModeBeforeSplash = ShutdownMode;
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _splashStopwatch = Stopwatch.StartNew();
            _splashScreen.Show();
        }

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
        // Safety net: closes a splash screen that is still open if the app exits (or throws)
        // before its own OnStartup override reaches CloseSplashScreenAsync().
        if (_splashScreen is { } splash)
        {
            splash.Close();
            _splashScreen = null;
            _splashStopwatch = null;
        }

        _appHost?.Services.InvokeLifecycleEvents<WpfLifecycle.OnExit>(del => del(this, e));

        base.OnExit(e);

        foreach (var scope in _windowScopes.Values)
            scope.Dispose();
        _windowScopes.Clear();

        if (IWpfPlatformApplication.Current == this)
            IWpfPlatformApplication.Current = null;

        // _appHost is cleared *before* disposing it, not after: disposing it disposes the
        // service provider, which cascades into disposing every registered singleton -
        // including, for example, ITrayIconPlatform, whose own Dispose() tears down a native
        // window and can synchronously reenter this Application's OnDeactivated (and
        // potentially other) override while that cascade is still running. Every event
        // forwarder below reads the _appHost field directly, so clearing it first turns any
        // such reentrant call into a harmless no-op instead of reaching a service provider
        // that has already started (but not finished) disposing - which throws
        // ObjectDisposedException, same as calling GetService after Dispose has fully
        // returned does.
        var appHost = _appHost;
        _appHost = null;
        appHost?.Dispose();
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
