// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Core Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Tray;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Barbatos.Wpf.Hosting;

public static partial class AppHostBuilderExtensions
{
    /// <summary>
    /// Adds the optional system tray icon feature. The feature can be configured from code
    /// via <paramref name="configure"/> and/or from the <c>Barbatos:TrayIcon</c>
    /// configuration section (configuration values override code values), and toggled at
    /// runtime through <see cref="ITrayIconService"/>.
    /// </summary>
    public static WpfAppBuilder ConfigureTrayIcon(this WpfAppBuilder builder, Action<TrayIconOptions>? configure = null)
    {
        var options = builder.Services.AddOptions<TrayIconOptions>();
        if (configure != null)
            options.Configure(configure);
        options.Bind(builder.Configuration.GetSection(TrayIconOptions.SectionName));

        builder.Services.TryAddSingleton<ITrayIconPlatform, WinFormsTrayIconPlatform>();
        builder.Services.TryAddSingleton<ITrayIconService, TrayIconService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IWpfInitializeService, TrayIconInitializer>());

        return builder;
    }

    class TrayIconInitializer : IWpfInitializeService
    {
        public void Initialize(IServiceProvider services)
        {
            if (services.GetRequiredService<ITrayIconService>() is TrayIconService service)
                service.ApplyOptions();
        }
    }
}
