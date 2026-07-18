// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Hosting.Sample;

/// <summary>
/// A sample <see cref="IWpfPeriodicService"/> that logs a heartbeat into the lifecycle
/// event list. Its interval defaults to 5 seconds and can be overridden from
/// <c>Barbatos:PeriodicServices:Intervals:Heartbeat</c> or from the settings UI.
/// </summary>
public sealed class HeartbeatService : IWpfPeriodicService
{
    public string Name => "Heartbeat";

    public TimeSpan Interval => TimeSpan.FromSeconds(5);

    public Task ExecuteAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        // Periodic services run on the application dispatcher, so touching the UI is safe.
        services.GetRequiredService<MainViewModel>().LogLifecycleEvent("Periodic: Heartbeat tick");

        return Task.CompletedTask;
    }
}
