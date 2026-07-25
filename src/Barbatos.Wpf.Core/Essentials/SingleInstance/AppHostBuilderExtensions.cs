// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.SingleInstance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Barbatos.Wpf.Hosting;

public static partial class AppHostBuilderExtensions
{
    /// <summary>
    /// Adds the single-instance feature: by default, a second launch attempt is blocked and
    /// silently exits (before any window is created) after notifying the first instance,
    /// which brings its main window to the foreground. The feature can be configured from
    /// code via <paramref name="configure"/> and/or from the <c>Barbatos:SingleInstance</c>
    /// configuration section (configuration values override code values).
    /// </summary>
    public static WpfAppBuilder ConfigureSingleInstance(this WpfAppBuilder builder, Action<SingleInstanceOptions>? configure = null)
    {
        var options = builder.Services.AddOptions<SingleInstanceOptions>();
        if (configure != null)
            options.Configure(configure);
        options.Bind(builder.Configuration.GetSection(SingleInstanceOptions.SectionName));

        builder.Services.TryAddSingleton<ISingleInstanceService, SingleInstanceService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IWpfInitializeService, SingleInstanceInitializer>());

        return builder;
    }

    class SingleInstanceInitializer : IWpfInitializeService
    {
        public void Initialize(IServiceProvider services)
        {
            if (services.GetRequiredService<ISingleInstanceService>() is SingleInstanceService service)
                service.ApplyOptions();
        }
    }
}
