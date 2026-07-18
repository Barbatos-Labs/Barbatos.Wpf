// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Core Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Hosting.UnitTests;

public class NotificationFeatureTests
{
    [Fact]
    public void FeatureIsNotRegisteredByDefault()
    {
        var wpfApp = WpfApp.CreateBuilder().Build();

        Assert.Null(wpfApp.Services.GetService<INotificationService>());
    }

    [Fact]
    public void FeatureIsRegisteredWhenConfigured()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<INotificationPlatform>(new FakeNotificationPlatform());
        builder.ConfigureNotifications();
        var wpfApp = builder.Build();

        Assert.NotNull(wpfApp.Services.GetService<INotificationService>());
    }

    [Fact]
    public void EnabledByDefault()
    {
        var (service, _) = BuildService();

        Assert.True(service.IsEnabled);
    }

    [Fact]
    public void ShowForwardsToThePlatformWhenEnabled()
    {
        var (service, platform) = BuildService();

        service.Show("Title", "Message", NotificationSeverity.Warning);

        var shown = Assert.Single(platform.Shown);
        Assert.Equal("Title", shown.Title);
        Assert.Equal("Message", shown.Message);
        Assert.Equal(NotificationSeverity.Warning, shown.Severity);
    }

    [Fact]
    public void ShowDefaultsToInfoSeverity()
    {
        var (service, platform) = BuildService();

        service.Show("Title", "Message");

        Assert.Equal(NotificationSeverity.Info, Assert.Single(platform.Shown).Severity);
    }

    [Fact]
    public void ShowIsANoOpWhenDisabled()
    {
        var (service, platform) = BuildService();

        service.SetEnabled(false);
        service.Show("Title", "Message");

        Assert.Empty(platform.Shown);
    }

    [Fact]
    public void ShowThrowsForNullArguments()
    {
        var (service, _) = BuildService();

        Assert.Throws<ArgumentNullException>(() => service.Show(null!, "Message"));
        Assert.Throws<ArgumentNullException>(() => service.Show("Title", null!));
    }

    [Fact]
    public void SetEnabledRaisesIsEnabledChangedOnlyOnChanges()
    {
        var (service, _) = BuildService();

        var raised = 0;
        service.IsEnabledChanged += (sender, args) => raised++;

        service.SetEnabled(false);
        service.SetEnabled(false);
        service.SetEnabled(true);

        Assert.Equal(2, raised);
    }

    [Fact]
    public void DisabledOptionAppliesDuringBuild()
    {
        var platform = new FakeNotificationPlatform();

        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<INotificationPlatform>(platform);
        builder.ConfigureNotifications(options => options.Enabled = false);
        var wpfApp = builder.Build();

        var service = wpfApp.Services.GetRequiredService<INotificationService>();

        Assert.False(service.IsEnabled);

        service.Show("Title", "Message");
        Assert.Empty(platform.Shown);
    }

    [Fact]
    public void OptionsBindFromConfiguration()
    {
        var platform = new FakeNotificationPlatform();

        var builder = WpfApp.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Barbatos:Notifications:Enabled"] = "false",
        });
        builder.Services.AddSingleton<INotificationPlatform>(platform);
        builder.ConfigureNotifications(options => options.Enabled = true);
        var wpfApp = builder.Build();

        Assert.False(wpfApp.Services.GetRequiredService<INotificationService>().IsEnabled);
    }

    [Fact]
    public void ActivatedEventsAreForwarded()
    {
        var (service, platform) = BuildService();

        NotificationActivatedEventArgs? received = null;
        service.Activated += (sender, args) => received = args;

        platform.RaiseActivated("Title", "Message");

        Assert.NotNull(received);
        Assert.Equal("Title", received.Title);
        Assert.Equal("Message", received.Message);
    }

    static (INotificationService Service, FakeNotificationPlatform Platform) BuildService(Action<NotificationOptions>? configure = null)
    {
        var platform = new FakeNotificationPlatform();

        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<INotificationPlatform>(platform);
        builder.ConfigureNotifications(configure);
        var wpfApp = builder.Build();

        return (wpfApp.Services.GetRequiredService<INotificationService>(), platform);
    }
}
