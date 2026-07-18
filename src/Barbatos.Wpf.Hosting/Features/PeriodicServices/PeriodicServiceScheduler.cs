// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Core Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Dispatching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.Hosting;

/// <summary>
/// The default <see cref="IPeriodicServiceScheduler"/> implementation, backed by
/// <see cref="IDispatcherTimer"/> timers on the application dispatcher.
/// </summary>
internal sealed class PeriodicServiceScheduler : IPeriodicServiceScheduler, IDisposable
{
    sealed class Entry
    {
        public Entry(IWpfPeriodicService service, PeriodicServiceStatus status)
        {
            Service = service;
            Status = status;
        }

        public IWpfPeriodicService Service { get; }

        public PeriodicServiceStatus Status { get; }

        public IDispatcherTimer? Timer { get; set; }

        public bool IsExecuting { get; set; }
    }

    readonly PeriodicServiceOptions _options;
    readonly IServiceProvider _serviceProvider;
    readonly ILogger<PeriodicServiceScheduler> _logger;
    readonly List<Entry> _entries = new();
    readonly CancellationTokenSource _shutdownTokenSource = new();

    public PeriodicServiceScheduler(
        IOptions<PeriodicServiceOptions> options,
        IEnumerable<IWpfPeriodicService> services,
        IServiceProvider serviceProvider,
        ILogger<PeriodicServiceScheduler> logger)
    {
        _options = options.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;

        foreach (var service in services)
        {
            // An interval from configuration overrides the service's default interval.
            var interval = _options.Intervals.TryGetValue(service.Name, out var configured)
                ? configured
                : service.Interval;

            if (interval <= TimeSpan.Zero)
                throw new InvalidOperationException($"The periodic service '{service.Name}' has a non-positive interval ({interval}).");

            _entries.Add(new Entry(service, new PeriodicServiceStatus(service.Name, interval)));
        }
    }

    public event EventHandler? IsEnabledChanged;

    public event EventHandler<PeriodicServiceExecutedEventArgs>? ServiceExecuted;

    public bool IsEnabled { get; private set; }

    public IReadOnlyList<PeriodicServiceStatus> Services =>
        _entries.Select(entry => entry.Status).ToArray();

    public void SetEnabled(bool enabled)
    {
        if (enabled == IsEnabled)
            return;

        foreach (var entry in _entries)
        {
            if (enabled)
                StartTimer(entry);
            else
                StopTimer(entry);
        }

        IsEnabled = enabled;
        IsEnabledChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateInterval(string name, TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval), interval, "The interval must be positive.");

        var entry = _entries.Find(entry => string.Equals(entry.Status.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"No periodic service named '{name}' has been registered.", nameof(name));

        entry.Status.Interval = interval;

        if (IsEnabled)
        {
            // Restart the timer so the new interval takes effect immediately.
            StopTimer(entry);
            StartTimer(entry);
        }
    }

    void StartTimer(Entry entry)
    {
        var timer = _serviceProvider.GetRequiredApplicationDispatcher().CreateTimer();
        timer.Interval = entry.Status.Interval;
        timer.IsRepeating = true;
        timer.Tick += (sender, args) => OnTimerTick(entry);

        entry.Timer = timer;
        timer.Start();
    }

    static void StopTimer(Entry entry)
    {
        entry.Timer?.Stop();
        entry.Timer = null;
    }

    async void OnTimerTick(Entry entry)
    {
        // Skip a tick while the previous execution is still running.
        if (entry.IsExecuting || _shutdownTokenSource.IsCancellationRequested)
            return;

        entry.IsExecuting = true;
        Exception? error = null;

        try
        {
            await entry.Service.ExecuteAsync(_serviceProvider, _shutdownTokenSource.Token);
        }
        catch (OperationCanceledException) when (_shutdownTokenSource.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            error = exception;
            _logger.LogError(exception, "The periodic service '{Name}' failed.", entry.Status.Name);
        }
        finally
        {
            entry.IsExecuting = false;
        }

        entry.Status.LastRunTime = DateTimeOffset.Now;
        entry.Status.RunCount++;

        ServiceExecuted?.Invoke(this, new PeriodicServiceExecutedEventArgs(entry.Status, error));
    }

    /// <summary>
    /// Applies the configured options during application construction.
    /// </summary>
    internal void ApplyOptions()
    {
        if (_options.Enabled)
            SetEnabled(true);
    }

    public void Dispose()
    {
        SetEnabled(false);

        _shutdownTokenSource.Cancel();
        _shutdownTokenSource.Dispose();
    }
}
