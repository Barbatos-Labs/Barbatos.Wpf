// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Hosting Contributors.
// All Rights Reserved.

using System.Windows.Threading;
using WpfDispatcher = System.Windows.Threading.Dispatcher;
using WpfDispatcherTimer = System.Windows.Threading.DispatcherTimer;

namespace Barbatos.Wpf.Dispatching;

/// <summary>
/// The <see cref="IDispatcher"/> implementation backed by the WPF
/// <see cref="System.Windows.Threading.Dispatcher"/>.
/// </summary>
/// <remarks>
/// This is the WPF counterpart of .NET MAUI's <c>Dispatcher</c>.
/// </remarks>
public class Dispatcher : IDispatcher
{
    readonly WpfDispatcher _dispatcher;

    internal Dispatcher(WpfDispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <summary>
    /// Gets the <see cref="IDispatcher"/> for the current thread.
    /// </summary>
    public static IDispatcher? GetForCurrentThread() =>
        DispatcherProvider.Current.GetForCurrentThread();

    /// <inheritdoc/>
    public bool IsDispatchRequired =>
        !_dispatcher.CheckAccess();

    /// <inheritdoc/>
    public bool Dispatch(Action action)
    {
        _ = action ?? throw new ArgumentNullException(nameof(action));

        if (_dispatcher.HasShutdownStarted || _dispatcher.HasShutdownFinished)
            return false;

        _dispatcher.BeginInvoke(action);
        return true;
    }

    /// <inheritdoc/>
    public bool DispatchDelayed(TimeSpan delay, Action action)
    {
        _ = action ?? throw new ArgumentNullException(nameof(action));

        if (_dispatcher.HasShutdownStarted || _dispatcher.HasShutdownFinished)
            return false;

        var timer = new WpfDispatcherTimer(DispatcherPriority.Normal, _dispatcher)
        {
            Interval = delay
        };
        timer.Tick += OnTimerTick;
        timer.Start();
        return true;

        void OnTimerTick(object? sender, EventArgs args)
        {
            timer.Stop();
            timer.Tick -= OnTimerTick;
            action();
        }
    }

    /// <inheritdoc/>
    public IDispatcherTimer CreateTimer()
    {
        return new DispatcherTimer(new WpfDispatcherTimer(DispatcherPriority.Normal, _dispatcher));
    }
}

/// <summary>
/// The <see cref="IDispatcherTimer"/> implementation backed by the WPF
/// <see cref="System.Windows.Threading.DispatcherTimer"/>.
/// </summary>
public class DispatcherTimer : IDispatcherTimer
{
    readonly WpfDispatcherTimer _timer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DispatcherTimer"/> class.
    /// </summary>
    /// <param name="timer">An instance of <see cref="System.Windows.Threading.DispatcherTimer"/> that will be used for this <see cref="DispatcherTimer"/> instance.</param>
    public DispatcherTimer(WpfDispatcherTimer timer)
    {
        _timer = timer;
    }

    /// <inheritdoc/>
    public TimeSpan Interval
    {
        get => _timer.Interval;
        set => _timer.Interval = value;
    }

    /// <inheritdoc/>
    public bool IsRepeating { get; set; } = true;

    /// <inheritdoc/>
    public bool IsRunning { get; private set; }

    /// <inheritdoc/>
    public event EventHandler? Tick;

    /// <inheritdoc/>
    public void Start()
    {
        if (IsRunning)
            return;

        IsRunning = true;

        _timer.Tick += OnTimerTick;

        _timer.Start();
    }

    /// <inheritdoc/>
    public void Stop()
    {
        if (!IsRunning)
            return;

        IsRunning = false;

        _timer.Tick -= OnTimerTick;

        _timer.Stop();
    }

    void OnTimerTick(object? sender, EventArgs args)
    {
        Tick?.Invoke(this, EventArgs.Empty);

        if (!IsRepeating)
            Stop();
    }
}
