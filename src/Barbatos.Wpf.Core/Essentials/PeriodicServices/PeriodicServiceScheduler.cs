// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
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
        public Entry(IWpfPeriodicService service, PeriodicSchedule schedule, PeriodicServiceStatus status, IDispatcherTimer timer)
        {
            Service = service;
            Schedule = schedule;
            Status = status;
            Timer = timer;
        }

        public IWpfPeriodicService Service { get; }

        // The scheduler's own working copy, used for GetNextOccurrence - never exposed. Kept
        // separate from Status.Schedule so external code can never mutate its way into desyncing
        // (or corrupting) the armed timer; see UpdateSchedule/AddEntry.
        public PeriodicSchedule Schedule { get; set; }

        public PeriodicServiceStatus Status { get; }

        public IDispatcherTimer Timer { get; }

        public bool IsExecuting { get; set; }

        public bool IsRemoved { get; set; }
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

        var now = DateTimeOffset.Now;
        foreach (var service in services)
            AddEntry(service, now);
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

        IsEnabled = enabled;

        var now = DateTimeOffset.Now;
        foreach (var entry in _entries)
        {
            if (enabled)
            {
                RecomputeSchedule(entry, now);
            }
            else
            {
                // Unconditional, and deliberately not routed through RecomputeSchedule: disabling
                // must be reflected in status immediately, even for an entry that's mid-execution.
                entry.Timer.Stop();
                entry.Status.NextRunTime = null;
            }
        }

        IsEnabledChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateSchedule(string name, PeriodicSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(schedule);

        var entry = Find(name)
            ?? throw new ArgumentException($"No periodic service named '{name}' has been registered.", nameof(name));

        var clone = schedule.Clone();
        clone.Validate();

        entry.Schedule = clone;
        entry.Status.Schedule = clone.Clone();

        RecomputeSchedule(entry, DateTimeOffset.Now);
    }

    public void Register(IWpfPeriodicService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        AddEntry(service, DateTimeOffset.Now);
    }

    public bool Unregister(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var entry = Find(name);
        if (entry is null)
            return false;

        // The timer has very likely already auto-stopped itself (it is never repeating), but an
        // in-flight execution's eventual post-tick reschedule must not resurrect it - IsRemoved
        // guards that in OnTimerTick.
        entry.IsRemoved = true;
        entry.Timer.Stop();
        _entries.Remove(entry);

        return true;
    }

    Entry? Find(string name) =>
        _entries.Find(entry => string.Equals(entry.Status.Name, name, StringComparison.OrdinalIgnoreCase));

    Entry AddEntry(IWpfPeriodicService service, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(service.Name))
            throw new ArgumentException("A periodic service must have a non-empty Name.", nameof(service));

        if (Find(service.Name) is not null)
            throw new ArgumentException($"A periodic service named '{service.Name}' is already registered.", nameof(service));

        // A schedule from configuration overrides the service's own schedule entirely.
        var schedule = (_options.Schedules.TryGetValue(service.Name, out var configured) ? configured : service.Schedule).Clone();
        schedule.Validate();

        var status = new PeriodicServiceStatus(service.Name, schedule.Clone());
        var timer = _serviceProvider.GetRequiredApplicationDispatcher().CreateTimer();
        timer.IsRepeating = false;

        var entry = new Entry(service, schedule, status, timer);
        timer.Tick += (sender, args) => OnTimerTick(entry);

        _entries.Add(entry);

        // Safe to call before the scheduler is ever enabled: RecomputeSchedule only arms the
        // timer when IsEnabled is true, so this just establishes NextRunTime/IsCompleted.
        RecomputeSchedule(entry, now);

        return entry;
    }

    void RecomputeSchedule(Entry entry, DateTimeOffset now)
    {
        // Defers to that execution's own post-tick reschedule (see OnTimerTick) rather than
        // racing it - this also means a schedule change made mid-execution takes effect only
        // once the in-flight run finishes, not immediately.
        if (entry.IsExecuting)
            return;

        entry.Timer.Stop();

        var next = entry.Schedule.GetNextOccurrence(now, entry.Status.LastRunTime);

        entry.Status.IsCompleted = next is null;
        entry.Status.NextRunTime = null;

        if (next is null || !IsEnabled)
            return;

        var delay = next.Value - now;

        entry.Status.NextRunTime = next;
        entry.Timer.Interval = delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
        entry.Timer.Start();
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

        if (entry.IsRemoved || _shutdownTokenSource.IsCancellationRequested)
            return;

        try
        {
            // Guarded rather than left to throw: this runs inside an async void handler, where an
            // uncaught exception would surface as an unhandled dispatcher exception (or crash the
            // app) instead of failing predictably back to a caller, unlike Register/UpdateSchedule/
            // the constructor - which validate synchronously and are meant to throw.
            RecomputeSchedule(entry, DateTimeOffset.Now);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to reschedule the periodic service '{Name}'.", entry.Status.Name);
        }
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
