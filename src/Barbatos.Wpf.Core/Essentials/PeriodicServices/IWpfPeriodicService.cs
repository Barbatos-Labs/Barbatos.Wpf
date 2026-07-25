// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Hosting;

/// <summary>
/// Represents a service that is executed periodically while the application is running.
/// </summary>
/// <remarks>
/// This is the recurring counterpart of <see cref="IWpfInitializeService"/>: implementations
/// registered in the service collection (or passed to <see cref="IPeriodicServiceScheduler.Register"/>
/// at any time after the host has started) are picked up by the scheduler that
/// <c>ConfigurePeriodicServices</c> adds, and executed according to <see cref="Schedule"/>.
/// The schedule can be overridden from the <c>Barbatos:PeriodicServices</c> configuration
/// section and changed at runtime through <see cref="IPeriodicServiceScheduler"/>.
/// Execution happens on the application dispatcher (UI) thread; offload long-running work
/// with <c>await Task.Run(...)</c> inside <see cref="ExecuteAsync"/>.
/// </remarks>
public interface IWpfPeriodicService
{
    /// <summary>
    /// The unique name of the service, used to override its schedule from configuration
    /// (<c>Barbatos:PeriodicServices:Schedules:&lt;Name&gt;</c>) or from the UI.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The default schedule the service runs on. Read once, when the service is registered -
    /// later changes to what this getter returns have no effect; call
    /// <see cref="IPeriodicServiceScheduler.UpdateSchedule"/> to change the active schedule.
    /// </summary>
    PeriodicSchedule Schedule { get; }

    /// <summary>
    /// Executes one run of the service.
    /// </summary>
    /// <param name="services">The application's root service provider.</param>
    /// <param name="cancellationToken">Cancelled when the host shuts down.</param>
    Task ExecuteAsync(IServiceProvider services, CancellationToken cancellationToken);
}
