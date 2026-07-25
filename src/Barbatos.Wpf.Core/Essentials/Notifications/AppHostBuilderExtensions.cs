// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Barbatos.Wpf.Hosting;

public static partial class AppHostBuilderExtensions
{
    /// <summary>
    /// Adds the optional push notifications feature. The feature can be configured from
    /// code via <paramref name="configure"/> and/or from the <c>Barbatos:Notifications</c>
    /// configuration section (configuration values override code values), and toggled at
    /// runtime through <see cref="INotificationService"/>.
    /// </summary>
    public static WpfAppBuilder ConfigureNotifications(this WpfAppBuilder builder, Action<NotificationOptions>? configure = null)
    {
        var options = builder.Services.AddOptions<NotificationOptions>();
        if (configure != null)
            options.Configure(configure);
        options.Bind(builder.Configuration.GetSection(NotificationOptions.SectionName));

        builder.Services.TryAddSingleton<INotificationPlatform, ToastNotificationPlatform>();
        builder.Services.TryAddSingleton<INotificationService, NotificationService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IWpfInitializeService, NotificationInitializer>());

        return builder;
    }

    class NotificationInitializer : IWpfInitializeService
    {
        public void Initialize(IServiceProvider services)
        {
            if (services.GetRequiredService<INotificationService>() is NotificationService service)
                service.ApplyOptions();
        }
    }
}
