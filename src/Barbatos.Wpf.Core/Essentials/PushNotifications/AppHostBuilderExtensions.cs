// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.PushNotifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.Hosting;

public static partial class AppHostBuilderExtensions
{
    /// <summary>
    /// Adds the optional push notifications client feature, using the default
    /// <see cref="PushNotification"/> payload shape and the default SignalR transport. The
    /// feature can be configured from code via <paramref name="configure"/>/<paramref name="configureSignalR"/>
    /// and/or from the <c>Barbatos:PushNotifications</c>/<c>Barbatos:PushNotifications:SignalR</c>
    /// configuration sections (configuration values override code values). Also ensures
    /// <see cref="Barbatos.Wpf.Notifications.INotificationService"/> is registered, since it's
    /// the primary display path for incoming notifications.
    /// </summary>
    public static WpfAppBuilder ConfigurePushNotifications(
        this WpfAppBuilder builder,
        Action<PushNotificationOptions>? configure = null,
        Action<SignalRPushNotificationOptions>? configureSignalR = null) =>
        builder.ConfigurePushNotifications<PushNotification>(configure, configureSignalR);

    /// <summary>
    /// Adds the optional push notifications client feature using
    /// <typeparamref name="TNotification"/> as the notification payload shape, for push servers
    /// whose payload doesn't match the default <see cref="PushNotification"/>.
    /// <paramref name="configureSignalR"/> only has an effect while the default SignalR
    /// transport is in use - register your own <see cref="IPushNotificationTransport"/> before
    /// calling this to use a different delivery mechanism entirely, in which case configure that
    /// transport's own options separately.
    /// </summary>
    public static WpfAppBuilder ConfigurePushNotifications<TNotification>(
        this WpfAppBuilder builder,
        Action<PushNotificationOptions>? configure = null,
        Action<SignalRPushNotificationOptions>? configureSignalR = null)
        where TNotification : class, IPushNotification
    {
        var options = builder.Services.AddOptions<PushNotificationOptions>();
        if (configure != null)
            options.Configure(configure);
        options.Bind(builder.Configuration.GetSection(PushNotificationOptions.SectionName));

        var signalROptions = builder.Services.AddOptions<SignalRPushNotificationOptions>();
        if (configureSignalR != null)
            signalROptions.Configure(configureSignalR);
        signalROptions.Bind(builder.Configuration.GetSection(SignalRPushNotificationOptions.SectionName));

        // Safe to call even if the app already called this itself: TryAddSingleton/TryAddEnumerable
        // are idempotent, and passing no `configure` delegate here never overrides anything the
        // app configured on its own call. This just guarantees the primary display path exists.
        builder.ConfigureNotifications();

        builder.Services.TryAddSingleton<IPushNotificationTransport, SignalRPushNotificationTransport>();
        builder.Services.TryAddSingleton<IPushNotificationFallbackPresenter, PushNotificationFallbackPresenter>();
        builder.Services.TryAddSingleton<IPushNotificationService, PushNotificationService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IWpfInitializeService, PushNotificationInitializer<TNotification>>());

        return builder;
    }

    class PushNotificationInitializer<TNotification> : IWpfInitializeService where TNotification : class, IPushNotification
    {
        public void Initialize(IServiceProvider services)
        {
            var service = services.GetRequiredService<IPushNotificationService>();
            if (service is PushNotificationService concrete)
                concrete.UseNotificationType<TNotification>();

            if (services.GetRequiredService<IOptions<PushNotificationOptions>>().Value.Enabled)
                _ = ConnectWithLoggingAsync(service, services);
        }

        // Fire-and-forget on purpose: IWpfInitializeService.Initialize() runs synchronously with
        // no surrounding try/catch (an uncaught exception here would crash app startup), and a
        // connection failure must be logged, not fatal.
        static async Task ConnectWithLoggingAsync(IPushNotificationService service, IServiceProvider services)
        {
            try
            {
                await service.ConnectAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                services.GetService<ILoggerFactory>()?.CreateLogger<IPushNotificationService>()
                    .LogError(ex, "The push notification transport could not start at startup.");
            }
        }
    }
}
