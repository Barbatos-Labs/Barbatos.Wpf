// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Hosting;

/// <summary>
/// Schedules the registered <see cref="IWpfPeriodicService"/> instances.
/// </summary>
public interface IPeriodicServiceScheduler
{
    /// <summary>
    /// Gets whether the periodic services are currently running.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Starts or stops all periodic services. Intended to be called from a settings UI.
    /// </summary>
    void SetEnabled(bool enabled);

    /// <summary>
    /// Occurs when <see cref="IsEnabled"/> changes.
    /// </summary>
    event EventHandler? IsEnabledChanged;

    /// <summary>
    /// The live status of every registered periodic service.
    /// </summary>
    IReadOnlyList<PeriodicServiceStatus> Services { get; }

    /// <summary>
    /// Changes the schedule of the named service at runtime, rescheduling it immediately (when
    /// the scheduler is enabled and the service is not currently executing).
    /// </summary>
    /// <exception cref="ArgumentException">No service named <paramref name="name"/> is registered.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="schedule"/> is not internally consistent - see <see cref="PeriodicSchedule.Validate"/>.</exception>
    void UpdateSchedule(string name, PeriodicSchedule schedule);

    /// <summary>
    /// Registers a periodic service at runtime, after the host has already been built - unlike
    /// DI registration via <c>ConfigurePeriodicServices</c>, this can be called at any time. If
    /// the scheduler is currently enabled, the new service is armed immediately.
    /// </summary>
    /// <remarks>
    /// The service's executable logic is still ordinary code supplied by the caller (an
    /// <see cref="IWpfPeriodicService"/> implementation) - this does not let a UI create new
    /// behavior out of nothing, only lets already-written code be scheduled without having been
    /// wired up at host-build time (for example, from a plugin, or a factory parameterized by
    /// something the user picked in a settings UI).
    /// </remarks>
    /// <exception cref="ArgumentException">A service with the same <see cref="IWpfPeriodicService.Name"/> is already registered.</exception>
    /// <exception cref="InvalidOperationException">The service's effective schedule is not internally consistent.</exception>
    void Register(IWpfPeriodicService service);

    /// <summary>
    /// Unregisters the named service, stopping it if it is running. An execution already in
    /// flight is not cancelled, but it will not be rescheduled afterwards.
    /// </summary>
    /// <returns><see langword="true"/> if a service was found and removed; otherwise <see langword="false"/>.</returns>
    bool Unregister(string name);

    /// <summary>
    /// Occurs after each execution of any periodic service (also when the execution failed).
    /// </summary>
    event EventHandler<PeriodicServiceExecutedEventArgs>? ServiceExecuted;
}

/// <summary>
/// The live status of a periodic service.
/// </summary>
public sealed class PeriodicServiceStatus
{
    internal PeriodicServiceStatus(string name, PeriodicSchedule schedule)
    {
        Name = name;
        Schedule = schedule;
    }

    public string Name { get; }

    /// <summary>
    /// A snapshot of the service's currently active schedule. This is always an independent copy
    /// - mutating it has no effect on the scheduler; call
    /// <see cref="IPeriodicServiceScheduler.UpdateSchedule"/> to change the active schedule.
    /// </summary>
    public PeriodicSchedule Schedule { get; internal set; }

    /// <summary>
    /// The next time the service is due to run, or <see langword="null"/> when it is not
    /// currently armed - the scheduler is disabled, or the service is <see cref="IsCompleted"/>.
    /// </summary>
    public DateTimeOffset? NextRunTime { get; internal set; }

    /// <summary>
    /// <see langword="true"/> once the schedule has no more occurrences left - a
    /// <see cref="PeriodicFrequency.Once"/> schedule that has run, or a schedule whose
    /// <see cref="PeriodicSchedule.EndTime"/> has passed. The service remains listed in
    /// <see cref="IPeriodicServiceScheduler.Services"/> rather than disappearing.
    /// </summary>
    public bool IsCompleted { get; internal set; }

    public DateTimeOffset? LastRunTime { get; internal set; }

    public int RunCount { get; internal set; }
}

/// <summary>
/// The event data for <see cref="IPeriodicServiceScheduler.ServiceExecuted"/>.
/// </summary>
public sealed class PeriodicServiceExecutedEventArgs : EventArgs
{
    public PeriodicServiceExecutedEventArgs(PeriodicServiceStatus service, Exception? error)
    {
        Service = service;
        Error = error;
    }

    public PeriodicServiceStatus Service { get; }

    /// <summary>
    /// The exception thrown by the execution, or <see langword="null"/> when it succeeded.
    /// </summary>
    public Exception? Error { get; }
}
