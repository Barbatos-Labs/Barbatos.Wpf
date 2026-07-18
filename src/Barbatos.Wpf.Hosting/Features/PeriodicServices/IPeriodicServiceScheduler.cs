// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Core Contributors.
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
    /// Changes the interval of the named service at runtime, rescheduling it when the
    /// scheduler is enabled.
    /// </summary>
    void UpdateInterval(string name, TimeSpan interval);

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
    internal PeriodicServiceStatus(string name, TimeSpan interval)
    {
        Name = name;
        Interval = interval;
    }

    public string Name { get; }

    public TimeSpan Interval { get; internal set; }

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
