// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Hosting Contributors.
// All Rights Reserved.

using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Hosting;

/// <summary>
/// Extension methods to run the registered initialization services.
/// </summary>
/// <remarks>
/// This mirrors .NET MAUI's <c>MauiContextExtensions.InitializeAppServices</c>/<c>InitializeScopedServices</c>.
/// </remarks>
public static class WpfAppExtensions
{
    /// <summary>
    /// Runs every registered <see cref="IWpfInitializeService"/> using the root service provider.
    /// </summary>
    public static void InitializeAppServices(this WpfApp wpfApp)
    {
        var initServices = wpfApp.Services.GetServices<IWpfInitializeService>();
        if (initServices is null)
            return;

        foreach (var instance in initServices)
            instance.Initialize(wpfApp.Services);
    }

    /// <summary>
    /// Runs every registered <see cref="IWpfInitializeScopedService"/> using the provided
    /// (typically window-scoped) service provider.
    /// </summary>
    public static void InitializeScopedServices(this IServiceProvider scopedServices)
    {
        var initServices = scopedServices.GetServices<IWpfInitializeScopedService>();
        if (initServices is null)
            return;

        foreach (var service in initServices)
            service.Initialize(scopedServices);
    }
}
