// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.ApplicationModel;
using Barbatos.Wpf.Dispatching;
using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.SingleInstance;

/// <summary>
/// The default <see cref="ISingleInstanceService"/> implementation, backed by a named
/// <see cref="Mutex"/> (identity) and a named <see cref="EventWaitHandle"/> (the "a second
/// instance was launched" wake signal), both derived from <see cref="IAppInfo.AppGuid"/>.
/// </summary>
internal sealed class SingleInstanceService : ISingleInstanceService, IDisposable
{
    readonly SingleInstanceOptions _options;
    readonly IAppInfo _appInfo;
    readonly ApplicationDispatcher _applicationDispatcher;

    Mutex? _mutex;
    EventWaitHandle? _wakeEvent;
    RegisteredWaitHandle? _registeredWait;

    public SingleInstanceService(IOptions<SingleInstanceOptions> options, IAppInfo appInfo, ApplicationDispatcher applicationDispatcher)
    {
        _options = options.Value;
        _appInfo = appInfo;
        _applicationDispatcher = applicationDispatcher;
    }

    public bool IsPrimaryInstance { get; private set; } = true;

    public event EventHandler? SecondInstanceLaunched;

    /// <summary>
    /// Acquires the single-instance lock. When another instance already holds it, this
    /// signals it and terminates the current process immediately via
    /// <see cref="Environment.Exit(int)"/> — before <c>Build()</c> returns, and therefore
    /// before any window is created.
    /// </summary>
    internal void ApplyOptions()
    {
        if (!_options.Enabled)
            return;

        // Session-local (unprefixed) names, not "Global\": this blocks a second instance for
        // the current user's session, not system-wide across every logged-in user.
        var name = "Barbatos.Wpf.SingleInstance." + _appInfo.AppGuid;

        _mutex = new Mutex(initiallyOwned: true, name: name, createdNew: out var createdNew);
        IsPrimaryInstance = createdNew;

        if (IsPrimaryInstance)
        {
            _wakeEvent = new EventWaitHandle(false, EventResetMode.AutoReset, name + ".Wake");
            _registeredWait = ThreadPool.RegisterWaitForSingleObject(
                _wakeEvent, (state, timedOut) => OnWakeSignaled(), null, Timeout.Infinite, executeOnlyOnce: false);
        }
        else
        {
            using var wakeEvent = new EventWaitHandle(false, EventResetMode.AutoReset, name + ".Wake");
            wakeEvent.Set();

            _mutex.Dispose();
            _mutex = null;

            Environment.Exit(0);
        }
    }

    void OnWakeSignaled() =>
        _applicationDispatcher.Dispatcher.Dispatch(RaiseSecondInstanceLaunched);

    void RaiseSecondInstanceLaunched()
    {
        if (_options.ActivateMainWindow && System.Windows.Application.Current?.MainWindow is { } window)
            ActivateWindow(window);

        SecondInstanceLaunched?.Invoke(this, EventArgs.Empty);
    }

    static void ActivateWindow(System.Windows.Window window)
    {
        if (window.WindowState == System.Windows.WindowState.Minimized)
            window.WindowState = System.Windows.WindowState.Normal;

        if (!window.IsVisible)
            window.Show();

        // Window.Activate() alone can be silently ignored by Windows' foreground-lock rules
        // when another app currently owns the foreground; briefly toggling Topmost is the
        // standard, reliable way around that without SetForegroundWindow P/Invoke.
        window.Topmost = true;
        window.Activate();
        window.Topmost = false;
        window.Focus();
    }

    public void Dispose()
    {
        _registeredWait?.Unregister(_wakeEvent);
        _wakeEvent?.Dispose();
        _mutex?.Dispose();
    }
}
