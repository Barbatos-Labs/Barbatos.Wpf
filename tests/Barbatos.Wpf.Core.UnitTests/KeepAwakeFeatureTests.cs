// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Power;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

public class KeepAwakeFeatureTests
{
    [Fact]
    public void FeatureIsNotRegisteredByDefault()
    {
        var wpfApp = WpfApp.CreateBuilder().Build();

        Assert.Null(wpfApp.Services.GetService<IKeepAwakeService>());
    }

    [Fact]
    public void DisabledByDefault()
    {
        var powerManager = new FakePowerManager();
        var (service, _) = BuildService(powerManager);

        Assert.False(service.IsEnabled);
        Assert.Empty(powerManager.Calls);
    }

    [Fact]
    public void SetEnabledKeepsTheSystemAwakeWithoutTheDisplay()
    {
        var powerManager = new FakePowerManager();
        var (service, _) = BuildService(powerManager);

        service.SetEnabled(true);

        Assert.True(service.IsEnabled);
        Assert.Equal((true, false), Assert.Single(powerManager.Calls));
    }

    [Fact]
    public void KeepDisplayOnOptionIsForwarded()
    {
        var powerManager = new FakePowerManager();
        var (service, _) = BuildService(powerManager, options => options.KeepDisplayOn = true);

        service.SetEnabled(true);

        Assert.Equal((true, true), Assert.Single(powerManager.Calls));
    }

    [Fact]
    public void SetEnabledFalseReleasesTheSleepBlock()
    {
        var powerManager = new FakePowerManager();
        var (service, _) = BuildService(powerManager);

        service.SetEnabled(true);
        service.SetEnabled(false);

        Assert.False(service.IsEnabled);
        Assert.Equal((false, false), powerManager.Calls[^1]);
    }

    [Fact]
    public void SetEnabledRaisesIsEnabledChangedOnlyOnChanges()
    {
        var powerManager = new FakePowerManager();
        var (service, _) = BuildService(powerManager);

        var raised = 0;
        service.IsEnabledChanged += (sender, args) => raised++;

        service.SetEnabled(true);
        service.SetEnabled(true);
        service.SetEnabled(false);

        Assert.Equal(2, raised);
    }

    [Fact]
    public void EnabledOptionAppliesDuringBuild()
    {
        var powerManager = new FakePowerManager();
        var (service, _) = BuildService(powerManager, options => options.Enabled = true);

        Assert.True(service.IsEnabled);
        Assert.Equal((true, false), Assert.Single(powerManager.Calls));
    }

    [Fact]
    public void OptionsBindFromConfiguration()
    {
        var powerManager = new FakePowerManager();

        var builder = WpfApp.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Barbatos:KeepAwake:Enabled"] = "true",
            ["Barbatos:KeepAwake:KeepDisplayOn"] = "true",
        });
        builder.Services.AddSingleton<IPowerManager>(powerManager);
        builder.ConfigureKeepAwake();
        var wpfApp = builder.Build();

        var service = wpfApp.Services.GetRequiredService<IKeepAwakeService>();

        Assert.True(service.IsEnabled);
        Assert.Equal((true, true), Assert.Single(powerManager.Calls));
    }

    [Fact]
    public void DisposingTheAppReleasesTheSleepBlock()
    {
        var powerManager = new FakePowerManager();
        var (service, wpfApp) = BuildService(powerManager);

        service.SetEnabled(true);
        wpfApp.Dispose();

        Assert.Equal((false, false), powerManager.Calls[^1]);
    }

    static (IKeepAwakeService Service, WpfApp App) BuildService(FakePowerManager powerManager, Action<KeepAwakeOptions>? configure = null)
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IPowerManager>(powerManager);
        builder.ConfigureKeepAwake(configure);
        var wpfApp = builder.Build();

        return (wpfApp.Services.GetRequiredService<IKeepAwakeService>(), wpfApp);
    }
}
