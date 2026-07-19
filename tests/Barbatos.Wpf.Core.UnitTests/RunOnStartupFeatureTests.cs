// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

public class RunOnStartupFeatureTests
{
    [Fact]
    public void FeatureIsNotRegisteredByDefault()
    {
        var wpfApp = WpfApp.CreateBuilder().Build();

        Assert.Null(wpfApp.Services.GetService<IRunOnStartupService>());
    }

    [Fact]
    public void FeatureIsRegisteredWhenConfigured()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IStartupRegistrar>(new FakeStartupRegistrar());
        builder.ConfigureRunOnStartup();
        var wpfApp = builder.Build();

        Assert.NotNull(wpfApp.Services.GetService<IRunOnStartupService>());
    }

    [Fact]
    public void DisabledByDefaultLeavesTheRegistrarUntouched()
    {
        var registrar = new FakeStartupRegistrar();
        var service = BuildService(registrar);

        Assert.False(service.IsEnabled);
        Assert.Empty(registrar.Entries);
    }

    [Fact]
    public void SetEnabledRegistersTheExecutable()
    {
        var registrar = new FakeStartupRegistrar();
        var service = BuildService(registrar, options => options.EntryName = "MyApp");

        service.SetEnabled(true);

        Assert.True(service.IsEnabled);
        var command = Assert.Contains("MyApp", registrar.Entries);
        Assert.Contains(Environment.ProcessPath!, command, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("\"", command, StringComparison.Ordinal);
    }

    [Fact]
    public void SetEnabledUsesTheConfiguredExecutableAndArguments()
    {
        var registrar = new FakeStartupRegistrar();
        var service = BuildService(registrar, options =>
        {
            options.EntryName = "MyApp";
            options.ExecutablePath = @"C:\apps\my.exe";
            options.Arguments = "--minimized";
        });

        service.SetEnabled(true);

        Assert.Equal("\"C:\\apps\\my.exe\" --minimized", registrar.Entries["MyApp"]);
    }

    [Fact]
    public void SetEnabledFalseUnregisters()
    {
        var registrar = new FakeStartupRegistrar();
        var service = BuildService(registrar, options => options.EntryName = "MyApp");

        service.SetEnabled(true);
        service.SetEnabled(false);

        Assert.False(service.IsEnabled);
        Assert.Empty(registrar.Entries);
    }

    [Fact]
    public void SetEnabledRaisesIsEnabledChanged()
    {
        var registrar = new FakeStartupRegistrar();
        var service = BuildService(registrar, options => options.EntryName = "MyApp");

        var raised = 0;
        service.IsEnabledChanged += (sender, args) => raised++;

        service.SetEnabled(true);
        service.SetEnabled(true);

        Assert.Equal(1, raised);
    }

    [Fact]
    public void EnabledOptionAppliesDuringBuild()
    {
        var registrar = new FakeStartupRegistrar();

        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IStartupRegistrar>(registrar);
        builder.ConfigureRunOnStartup(options =>
        {
            options.Enabled = true;
            options.EntryName = "MyApp";
        });
        builder.Build();

        Assert.True(registrar.IsRegistered("MyApp"));
    }

    [Fact]
    public void OptionsBindFromConfiguration()
    {
        var registrar = new FakeStartupRegistrar();

        var builder = WpfApp.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Barbatos:RunOnStartup:Enabled"] = "true",
            ["Barbatos:RunOnStartup:EntryName"] = "ConfiguredApp",
        });
        builder.Services.AddSingleton<IStartupRegistrar>(registrar);
        builder.ConfigureRunOnStartup();
        builder.Build();

        Assert.True(registrar.IsRegistered("ConfiguredApp"));
    }

    [Fact]
    public void ConfigurationOverridesCodeValues()
    {
        var registrar = new FakeStartupRegistrar();

        var builder = WpfApp.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Barbatos:RunOnStartup:EntryName"] = "FromConfiguration",
        });
        builder.Services.AddSingleton<IStartupRegistrar>(registrar);
        builder.ConfigureRunOnStartup(options =>
        {
            options.Enabled = true;
            options.EntryName = "FromCode";
        });
        builder.Build();

        Assert.True(registrar.IsRegistered("FromConfiguration"));
        Assert.False(registrar.IsRegistered("FromCode"));
    }

    static IRunOnStartupService BuildService(FakeStartupRegistrar registrar, Action<RunOnStartupOptions>? configure = null)
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IStartupRegistrar>(registrar);
        builder.ConfigureRunOnStartup(configure);
        var wpfApp = builder.Build();

        return wpfApp.Services.GetRequiredService<IRunOnStartupService>();
    }
}
