// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.SingleInstance;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.Core.UnitTests;

/// <summary>
/// <see cref="SingleInstanceOptions.Enabled"/> defaults to <see langword="true"/>, and
/// enabling it makes <c>Build()</c> acquire a real, session-wide named <see cref="Mutex"/>
/// keyed by <c>AppInfo.AppGuid</c> — which is the *same name* for every test in this process
/// (they all share one entry assembly). If two tests both actually reached the "acquire the
/// mutex" code path with <c>Enabled = true</c> while running in parallel (xUnit's default),
/// whichever one lost the race would call <see cref="Environment.Exit(int)"/> and kill the
/// entire test process. Every test below therefore keeps the *final, bound* value of
/// <c>Enabled</c> at <see langword="false"/> when it calls <c>Build()</c>, and instead proves
/// the "defaults to enabled" and "code vs. configuration" behaviors either on a bare
/// <see cref="SingleInstanceOptions"/> instance or via the bound <see cref="IOptions{TOptions}"/>
/// value, without ever letting <c>ApplyOptions()</c> touch the real mutex.
/// </summary>
public class SingleInstanceFeatureTests
{
    [Fact]
    public void EnabledDefaultsToTrue()
    {
        Assert.True(new SingleInstanceOptions().Enabled);
    }

    [Fact]
    public void ActivateMainWindowDefaultsToTrue()
    {
        Assert.True(new SingleInstanceOptions().ActivateMainWindow);
    }

    [Fact]
    public void FeatureIsNotRegisteredByDefault()
    {
        var wpfApp = WpfApp.CreateBuilder().Build();

        Assert.Null(wpfApp.Services.GetService<ISingleInstanceService>());
    }

    [Fact]
    public void FeatureIsRegisteredWhenConfigured()
    {
        var builder = WpfApp.CreateBuilder();
        builder.ConfigureSingleInstance(options => options.Enabled = false);
        var wpfApp = builder.Build();

        Assert.NotNull(wpfApp.Services.GetService<ISingleInstanceService>());
    }

    [Fact]
    public void IsPrimaryInstanceIsTrueWhenDisabled()
    {
        // With Enabled = false, ApplyOptions() never checks the mutex, so this reports the
        // class's own default rather than anything actually observed about this process.
        var builder = WpfApp.CreateBuilder();
        builder.ConfigureSingleInstance(options => options.Enabled = false);
        var wpfApp = builder.Build();

        var service = wpfApp.Services.GetRequiredService<ISingleInstanceService>();

        Assert.True(service.IsPrimaryInstance);
    }

    [Fact]
    public void OptionsBindFromConfiguration()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Barbatos:SingleInstance:Enabled"] = "false",
            ["Barbatos:SingleInstance:ActivateMainWindow"] = "false",
        });
        // Code enables both; configuration (bound afterwards, so it wins) disables both -
        // this proves the same "file overrides code" rule every other feature follows, while
        // keeping the *final* Enabled value false so Build() stays safe (see the class remarks).
        builder.ConfigureSingleInstance(options =>
        {
            options.Enabled = true;
            options.ActivateMainWindow = true;
        });
        var wpfApp = builder.Build();

        var options = wpfApp.Services.GetRequiredService<IOptions<SingleInstanceOptions>>().Value;

        Assert.False(options.Enabled);
        Assert.False(options.ActivateMainWindow);
    }
}
