// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Barbatos.Wpf.Hosting;

public static partial class AppHostBuilderExtensions
{
    /// <summary>
    /// Adds the optional periodic services feature: every <see cref="IWpfPeriodicService"/>
    /// registered in the service collection is executed on its schedule. Schedules can be
    /// configured from code (the service's <see cref="IWpfPeriodicService.Schedule"/>),
    /// overridden from the <c>Barbatos:PeriodicServices</c> configuration section, and
    /// changed at runtime - or registered/unregistered altogether - through
    /// <see cref="IPeriodicServiceScheduler"/>.
    /// </summary>
    public static WpfAppBuilder ConfigurePeriodicServices(this WpfAppBuilder builder, Action<PeriodicServiceOptions>? configure = null)
    {
        var options = builder.Services.AddOptions<PeriodicServiceOptions>();
        if (configure != null)
            options.Configure(configure);
        options.Bind(builder.Configuration.GetSection(PeriodicServiceOptions.SectionName));

        builder.Services.TryAddSingleton<IPeriodicServiceScheduler, PeriodicServiceScheduler>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IWpfInitializeService, PeriodicServiceInitializer>());

        return builder;
    }

    /// <summary>
    /// Adds the periodic services feature and registers a periodic service implementation.
    /// </summary>
    public static WpfAppBuilder ConfigurePeriodicServices<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this WpfAppBuilder builder, Action<PeriodicServiceOptions>? configure = null)
        where TService : class, IWpfPeriodicService
    {
        builder.Services.AddSingleton<IWpfPeriodicService, TService>();

        return builder.ConfigurePeriodicServices(configure);
    }

    class PeriodicServiceInitializer : IWpfInitializeService
    {
        public void Initialize(IServiceProvider services)
        {
            if (services.GetRequiredService<IPeriodicServiceScheduler>() is PeriodicServiceScheduler scheduler)
                scheduler.ApplyOptions();
        }
    }
}
