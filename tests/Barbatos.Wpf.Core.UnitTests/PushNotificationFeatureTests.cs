// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Notifications;
using Barbatos.Wpf.PushNotifications;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

public class PushNotificationFeatureTests
{
    [Fact]
    public void FeatureIsNotRegisteredByDefault()
    {
        var wpfApp = WpfApp.CreateBuilder().Build();

        Assert.Null(wpfApp.Services.GetService<IPushNotificationService>());
    }

    [Fact]
    public void FeatureIsRegisteredWhenConfigured()
    {
        var (service, _, _, _, _) = BuildService();

        Assert.NotNull(service);
    }

    [Fact]
    public async Task ConnectAsyncDelegatesToTheRegisteredTransport()
    {
        // Enabled: false - otherwise the feature's own auto-connect-at-startup already calls
        // StartAsync once, before this test's own explicit call.
        var (service, transport, _, _, _) = BuildService(options => options.Enabled = false);

        await service.ConnectAsync();

        Assert.Equal(1, transport.StartCount);
    }

    [Fact]
    public async Task DisconnectAsyncDelegatesToTheRegisteredTransport()
    {
        var (service, transport, _, _, _) = BuildService();

        await service.DisconnectAsync();

        Assert.Equal(1, transport.StopCount);
    }

    [Fact]
    public async Task RealSignalRTransportThrowsWhenServerUrlIsMissing()
    {
        // Uses the REAL SignalRPushNotificationTransport (not faked) - its validation runs
        // synchronously before any network I/O, so this is safe/fast to test directly.
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<INotificationPlatform>(new FakeNotificationPlatform());
        builder.Services.AddSingleton<Barbatos.Wpf.ApplicationModel.IDeviceIdentity>(new FakeDeviceIdentity());
        builder.Services.AddSingleton<Barbatos.Wpf.ApplicationModel.IAppInfo>(new FakeAppInfo());
        builder.Services.AddSingleton<Barbatos.Wpf.Devices.IDeviceInfo>(new FakeDeviceInfo());
        builder.ConfigurePushNotifications(options => options.Enabled = false, configureSignalR: options => options.AppId = "MyApp");
        var wpfApp = builder.Build();

        var transport = wpfApp.Services.GetRequiredService<IPushNotificationTransport>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => transport.StartAsync());
        Assert.Contains(nameof(SignalRPushNotificationOptions.ServerUrl), exception.Message);
    }

    [Fact]
    public async Task RealSignalRTransportThrowsWhenAppIdIsMissing()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<INotificationPlatform>(new FakeNotificationPlatform());
        builder.Services.AddSingleton<Barbatos.Wpf.ApplicationModel.IDeviceIdentity>(new FakeDeviceIdentity());
        builder.Services.AddSingleton<Barbatos.Wpf.ApplicationModel.IAppInfo>(new FakeAppInfo());
        builder.Services.AddSingleton<Barbatos.Wpf.Devices.IDeviceInfo>(new FakeDeviceInfo());
        builder.ConfigurePushNotifications(options => options.Enabled = false, configureSignalR: options => options.ServerUrl = "https://push.example.com/hub");
        var wpfApp = builder.Build();

        var transport = wpfApp.Services.GetRequiredService<IPushNotificationTransport>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => transport.StartAsync());
        Assert.Contains(nameof(SignalRPushNotificationOptions.AppId), exception.Message);
    }

    [Fact]
    public void RawNotificationFromTheTransportIsDeserializedAndDisplayed()
    {
        var (service, transport, notifications, fallback, _) = BuildService();

        PushNotificationReceivedEventArgs? received = null;
        service.NotificationReceived += (_, args) => received = args;

        transport.RaiseNotificationReceived("""{"appId":"A","title":"Title","message":"Body"}""");

        var shown = Assert.Single(notifications.Shown).Content;
        Assert.Equal("Title", shown.Title);
        Assert.Equal("Body", shown.Message);
        Assert.Empty(fallback.Notified);
        Assert.NotNull(received);
        Assert.False(received!.UsedFallback);
    }

    [Fact]
    public void MalformedRawJsonFromTheTransportIsIgnoredWithoutThrowing()
    {
        var (service, transport, notifications, fallback, _) = BuildService();

        var received = false;
        service.NotificationReceived += (_, _) => received = true;

        var exception = Record.Exception(() => transport.RaiseNotificationReceived("not valid json"));

        Assert.Null(exception);
        Assert.False(received);
        Assert.Empty(notifications.Shown);
        Assert.Empty(fallback.Notified);
    }

    [Fact]
    public async Task SimulateNotificationShowsThroughInotificationServiceWhenAvailable()
    {
        var (service, _, notifications, fallback, _) = BuildService();

        PushNotificationReceivedEventArgs? received = null;
        service.NotificationReceived += (_, args) => received = args;

        var notification = new PushNotification { AppId = "A", Title = "Title", Body = "Body" };
        await service.SimulateNotificationAsync(notification);

        var shown = Assert.Single(notifications.Shown).Content;
        Assert.Equal("Title", shown.Title);
        Assert.Equal("Body", shown.Message);
        Assert.Empty(fallback.Notified);

        Assert.NotNull(received);
        Assert.Same(notification, received!.Notification);
        Assert.False(received.UsedFallback);
    }

    [Fact]
    public async Task SimulateNotificationUsesFallbackWhenOsAvailabilityIsBlocked()
    {
        var (service, _, notifications, fallback, _) = BuildService();
        notifications.Availability = NotificationAvailability.DisabledForUser;

        PushNotificationReceivedEventArgs? received = null;
        service.NotificationReceived += (_, args) => received = args;

        var notification = new PushNotification { AppId = "A", Title = "Title", Body = "Body" };
        await service.SimulateNotificationAsync(notification);

        Assert.Empty(notifications.Shown);
        Assert.Single(fallback.Notified);
        Assert.True(received!.UsedFallback);
    }

    [Fact]
    public async Task SimulateNotificationUsesFallbackWhenNotificationsAreDisabled()
    {
        var (service, notificationService, _, notifications, fallback, _) = BuildServiceWithNotificationService();
        notificationService.SetEnabled(false);

        await service.SimulateNotificationAsync(new PushNotification { AppId = "A", Title = "Title", Body = "Body" });

        Assert.Empty(notifications.Shown);
        Assert.Single(fallback.Notified);
    }

    [Fact]
    public async Task SimulateNotificationUsesFallbackWhenShowThrows()
    {
        var (service, _, notifications, fallback, _) = BuildService();
        notifications.ThrowOnShow = true;

        await service.SimulateNotificationAsync(new PushNotification { AppId = "A", Title = "Title", Body = "Body" });

        Assert.Single(fallback.Notified);
    }

    [Fact]
    public async Task ActivatingARealToastWithAUrlActionOpensItViaLauncher()
    {
        var (service, _, notifications, _, launcher) = BuildService();

        var notification = new PushNotification
        {
            AppId = "A",
            Title = "Title",
            Body = "Body",
            Action = new PushNotificationAction { ActionType = PushNotificationActionType.Url, ActionTarget = "https://example.com/" },
        };
        await service.SimulateNotificationAsync(notification);

        var shown = Assert.Single(notifications.Shown).Content;
        notifications.RaiseActivated(shown.Title, shown.Message, shown.Arguments);

        var opened = Assert.Single(launcher.TryOpened);
        Assert.Equal("https://example.com/", opened.OriginalString);
    }

    [Fact]
    public void ActivatingAFallbackWithARouteActionRaisesRouteRequested()
    {
        var (service, _, _, fallback, launcher) = BuildService();

        var notification = new PushNotification
        {
            AppId = "A",
            Title = "Title",
            Body = "Body",
            Action = new PushNotificationAction { ActionType = PushNotificationActionType.Route, ActionTarget = "settings" },
        };

        PushNotificationRouteRequestedEventArgs? routeArgs = null;
        service.RouteRequested += (_, args) => routeArgs = args;

        fallback.RaiseActivated(notification);

        Assert.Equal("settings", routeArgs?.Route);
        Assert.Empty(launcher.TryOpened);
    }

    [Fact]
    public void ActivatingANoneActionDispatchesNothing()
    {
        var (service, _, _, fallback, launcher) = BuildService();

        var notification = new PushNotification
        {
            AppId = "A",
            Title = "Title",
            Body = "Body",
            Action = new PushNotificationAction { ActionType = PushNotificationActionType.None },
        };

        var routeRequested = false;
        service.RouteRequested += (_, _) => routeRequested = true;

        fallback.RaiseActivated(notification);

        Assert.False(routeRequested);
        Assert.Empty(launcher.TryOpened);
    }

    [Fact]
    public void RealTransportImplementsSynchronousDisposableForContainerSafety()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<INotificationPlatform>(new FakeNotificationPlatform());
        builder.ConfigurePushNotifications(options => options.Enabled = false);
        var wpfApp = builder.Build();

        // HubConnection itself only implements IAsyncDisposable; the DI container's synchronous
        // Dispose() throws if a tracked singleton doesn't also implement IDisposable - this
        // checks the real (non-faked) transport registered by ConfigurePushNotifications.
        var transport = wpfApp.Services.GetRequiredService<IPushNotificationTransport>();
        var disposable = Assert.IsAssignableFrom<IDisposable>(transport);

        var exception = Record.Exception(() => disposable.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void HostDisposesCleanlyWhenPushNotificationsIsConfigured()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<INotificationPlatform>(new FakeNotificationPlatform());
        builder.ConfigurePushNotifications(options => options.Enabled = false);
        var wpfApp = builder.Build();

        var exception = Record.Exception(() => wpfApp.Dispose());
        Assert.Null(exception);
    }

    static (IPushNotificationService Service, FakePushNotificationTransport Transport, FakeNotificationPlatform Notifications, FakePushNotificationFallbackPresenter Fallback, FakeLauncher Launcher) BuildService(Action<PushNotificationOptions>? configure = null)
    {
        var (service, _, transport, notifications, fallback, launcher) = BuildServiceWithNotificationService(configure);
        return (service, transport, notifications, fallback, launcher);
    }

    static (IPushNotificationService Service, INotificationService NotificationService, FakePushNotificationTransport Transport, FakeNotificationPlatform Notifications, FakePushNotificationFallbackPresenter Fallback, FakeLauncher Launcher) BuildServiceWithNotificationService(Action<PushNotificationOptions>? configure = null)
    {
        var transport = new FakePushNotificationTransport();
        var notificationPlatform = new FakeNotificationPlatform();
        var fallback = new FakePushNotificationFallbackPresenter();
        var launcher = new FakeLauncher();

        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<INotificationPlatform>(notificationPlatform);
        builder.Services.AddSingleton<IPushNotificationTransport>(transport);
        builder.Services.AddSingleton<IPushNotificationFallbackPresenter>(fallback);
        builder.Services.AddSingleton<Barbatos.Wpf.ApplicationModel.ILauncher>(launcher);
        builder.ConfigurePushNotifications(configure);
        var wpfApp = builder.Build();

        return (
            wpfApp.Services.GetRequiredService<IPushNotificationService>(),
            wpfApp.Services.GetRequiredService<INotificationService>(),
            transport,
            notificationPlatform,
            fallback,
            launcher);
    }
}
