// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Tray;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Hosting.UnitTests;

public class TrayIconFeatureTests
{
    [Fact]
    public void FeatureIsNotRegisteredByDefault()
    {
        var wpfApp = WpfApp.CreateBuilder().Build();

        Assert.Null(wpfApp.Services.GetService<ITrayIconService>());
    }

    [Fact]
    public void HiddenByDefault()
    {
        var platform = new FakeTrayIconPlatform();
        var service = BuildService(platform);

        Assert.False(service.IsVisible);
        Assert.Equal(0, platform.ShowCount);
    }

    [Fact]
    public void SetVisibleShowsAndHidesTheIcon()
    {
        var platform = new FakeTrayIconPlatform();
        var service = BuildService(platform);

        service.SetVisible(true);
        Assert.True(service.IsVisible);
        Assert.Equal(1, platform.ShowCount);

        service.SetVisible(false);
        Assert.False(service.IsVisible);
        Assert.Equal(1, platform.HideCount);
    }

    [Fact]
    public void SetVisibleRaisesIsVisibleChangedOnlyOnChanges()
    {
        var platform = new FakeTrayIconPlatform();
        var service = BuildService(platform);

        var raised = 0;
        service.IsVisibleChanged += (sender, args) => raised++;

        service.SetVisible(true);
        service.SetVisible(true);
        service.SetVisible(false);

        Assert.Equal(2, raised);
    }

    [Fact]
    public void EnabledOptionShowsTheIconDuringBuild()
    {
        var platform = new FakeTrayIconPlatform();
        var service = BuildService(platform, options => options.Enabled = true);

        Assert.True(service.IsVisible);
        Assert.Equal(1, platform.ShowCount);
    }

    [Fact]
    public void OptionsBindFromConfiguration()
    {
        var platform = new FakeTrayIconPlatform();

        var builder = WpfApp.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Barbatos:TrayIcon:Enabled"] = "true",
            ["Barbatos:TrayIcon:ToolTip"] = "From configuration",
        });
        builder.Services.AddSingleton<ITrayIconPlatform>(platform);
        builder.ConfigureTrayIcon(options => options.ToolTip = "From code");
        var wpfApp = builder.Build();

        var service = wpfApp.Services.GetRequiredService<ITrayIconService>();

        Assert.True(service.IsVisible);
        Assert.Equal("From configuration", platform.ShownOptions?.ToolTip);
    }

    [Fact]
    public void MenuItemsFromCodeArePassedToThePlatform()
    {
        var platform = new FakeTrayIconPlatform();
        var service = BuildService(platform, options =>
        {
            options.Enabled = true;
            options.MenuItems.Add(new TrayMenuItem("Open", () => { }));
            options.MenuItems.Add(new TrayMenuItem("Exit", () => { }));
        });

        Assert.NotNull(platform.ShownOptions);
        Assert.Equal(new[] { "Open", "Exit" }, platform.ShownOptions.MenuItems.Select(item => item.Header));
    }

    [Fact]
    public void SetToolTipIsForwardedToThePlatform()
    {
        var platform = new FakeTrayIconPlatform();
        var service = BuildService(platform);

        service.SetToolTip("Hello");

        Assert.Equal("Hello", platform.LastToolTip);
    }

    [Fact]
    public void ClickEventsAreForwarded()
    {
        var platform = new FakeTrayIconPlatform();
        var service = BuildService(platform);

        var clicked = 0;
        var doubleClicked = 0;
        service.Clicked += (sender, args) => clicked++;
        service.DoubleClicked += (sender, args) => doubleClicked++;

        platform.RaiseClicked();
        platform.RaiseDoubleClicked();

        Assert.Equal(1, clicked);
        Assert.Equal(1, doubleClicked);
    }

    static ITrayIconService BuildService(FakeTrayIconPlatform platform, Action<TrayIconOptions>? configure = null)
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<ITrayIconPlatform>(platform);
        builder.ConfigureTrayIcon(configure);
        var wpfApp = builder.Build();

        return wpfApp.Services.GetRequiredService<ITrayIconService>();
    }
}
