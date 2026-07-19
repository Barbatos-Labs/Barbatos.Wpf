// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Barbatos.Wpf.Hosting;

public static partial class AppHostBuilderExtensions
{
    /// <summary>
    /// Adds the optional "run on startup" feature. The feature can be configured from code
    /// via <paramref name="configure"/> and/or from the <c>Barbatos:RunOnStartup</c>
    /// configuration section (configuration values override code values), and toggled at
    /// runtime through <see cref="IRunOnStartupService"/>.
    /// </summary>
    public static WpfAppBuilder ConfigureRunOnStartup(this WpfAppBuilder builder, Action<RunOnStartupOptions>? configure = null)
    {
        var options = builder.Services.AddOptions<RunOnStartupOptions>();
        if (configure != null)
            options.Configure(configure);
        options.Bind(builder.Configuration.GetSection(RunOnStartupOptions.SectionName));

        builder.Services.TryAddSingleton<IStartupRegistrar, RegistryStartupRegistrar>();
        builder.Services.TryAddSingleton<IRunOnStartupService, RunOnStartupService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IWpfInitializeService, RunOnStartupInitializer>());

        return builder;
    }

    class RunOnStartupInitializer : IWpfInitializeService
    {
        public void Initialize(IServiceProvider services)
        {
            if (services.GetRequiredService<IRunOnStartupService>() is RunOnStartupService service)
                service.ApplyOptions();
        }
    }
}
