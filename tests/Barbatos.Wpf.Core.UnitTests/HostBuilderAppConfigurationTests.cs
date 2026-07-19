// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

public class HostBuilderAppConfigurationTests
{
    [Fact]
    public void CanConfigureAppConfiguration()
    {
        var builder = WpfApp.CreateBuilder();
        builder
            .Configuration
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "key 1", "value 1" },
            });
        var wpfApp = builder.Build();

        var configuration = wpfApp.Services.GetRequiredService<IConfiguration>();

        Assert.Equal("value 1", configuration["key 1"]);
    }

    [Fact]
    public void AppConfigurationOverwritesValues()
    {
        var builder = WpfApp.CreateBuilder();
        builder
            .Configuration
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "key 1", "value 1" },
                { "key 2", "value 2" },
            });

        builder
            .Configuration
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "key 1", "value a" },
            });

        var wpfApp = builder.Build();

        var configuration = wpfApp.Services.GetRequiredService<IConfiguration>();

        Assert.Equal("value a", configuration["key 1"]);
        Assert.Equal("value 2", configuration["key 2"]);
    }

    [Fact]
    public void ConfigureServicesCanUseConfig()
    {
        var builder = WpfApp.CreateBuilder();
        builder
            .Configuration
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "key 1", "value 1" },
            });

        Assert.Equal("value 1", builder.Configuration["key 1"]);
    }

    [Fact]
    public void AppConfigurationIsSameInstanceAsBuilderConfiguration()
    {
        var builder = WpfApp.CreateBuilder();
        var wpfApp = builder.Build();

        Assert.Same(builder.Configuration, wpfApp.Configuration);
    }

    [Fact]
    public void DisposingAppDisposesConfigurationProviders()
    {
        var source = new TrackingConfigurationSource();

        var builder = WpfApp.CreateBuilder();
        ((IConfigurationBuilder)builder.Configuration).Add(source);
        var wpfApp = builder.Build();

        Assert.False(source.Provider.IsDisposed);

        wpfApp.Dispose();

        Assert.True(source.Provider.IsDisposed);
    }

    class TrackingConfigurationSource : IConfigurationSource
    {
        public TrackingConfigurationProvider Provider { get; } = new();

        public IConfigurationProvider Build(IConfigurationBuilder builder) => Provider;
    }

    class TrackingConfigurationProvider : ConfigurationProvider, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }
}
