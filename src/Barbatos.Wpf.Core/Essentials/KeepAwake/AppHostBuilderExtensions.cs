// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Power;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Barbatos.Wpf.Hosting;

public static partial class AppHostBuilderExtensions
{
    /// <summary>
    /// Adds the optional "keep computer awake" feature. The feature can be configured from
    /// code via <paramref name="configure"/> and/or from the <c>Barbatos:KeepAwake</c>
    /// configuration section (configuration values override code values), and toggled at
    /// runtime through <see cref="IKeepAwakeService"/>.
    /// </summary>
    public static WpfAppBuilder ConfigureKeepAwake(this WpfAppBuilder builder, Action<KeepAwakeOptions>? configure = null)
    {
        var options = builder.Services.AddOptions<KeepAwakeOptions>();
        if (configure != null)
            options.Configure(configure);
        options.Bind(builder.Configuration.GetSection(KeepAwakeOptions.SectionName));

        builder.Services.TryAddSingleton<IPowerManager, Win32PowerManager>();
        builder.Services.TryAddSingleton<IKeepAwakeService, KeepAwakeService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IWpfInitializeService, KeepAwakeInitializer>());

        return builder;
    }

    class KeepAwakeInitializer : IWpfInitializeService
    {
        public void Initialize(IServiceProvider services)
        {
            if (services.GetRequiredService<IKeepAwakeService>() is KeepAwakeService service)
                service.ApplyOptions();
        }
    }
}
