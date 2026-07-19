// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Dispatching;
using Barbatos.Wpf.LifecycleEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;

namespace Barbatos.Wpf.Core.UnitTests;

public class WpfAppBuilderTests
{
    [Fact]
    public void CreateBuilderReturnsNewInstances()
    {
        var builder1 = WpfApp.CreateBuilder();
        var builder2 = WpfApp.CreateBuilder();

        Assert.NotSame(builder1, builder2);
    }

    [Fact]
    public void BuildMakesServiceCollectionReadOnly()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Build();

        Assert.Throws<InvalidOperationException>(() =>
            builder.Services.AddSingleton<IFooService, FooService>());
    }

    [Fact]
    public void PropertiesCanShareState()
    {
        var builder = WpfApp.CreateBuilder();

        builder.Properties["key"] = "value";

        Assert.Equal("value", builder.Properties["key"]);
    }

    [Fact]
    public void EnvironmentIsAvailableAndRegistered()
    {
        var builder = WpfApp.CreateBuilder();

        Assert.NotNull(builder.Environment);

        var wpfApp = builder.Build();

        var environment = wpfApp.Services.GetRequiredService<IHostEnvironment>();

        Assert.Same(builder.Environment, environment);
    }

    [Fact]
    public void MetricsBuilderIsRegistered()
    {
        var builder = WpfApp.CreateBuilder();
        var wpfApp = builder.Build();

        var metrics = wpfApp.Services.GetRequiredService<IMetricsBuilder>();

        Assert.NotNull(metrics);
    }

    [Fact]
    public void IsAnIHostApplicationBuilder()
    {
        var builder = WpfApp.CreateBuilder();
        IHostApplicationBuilder hostApplicationBuilder = builder;

        Assert.Same(builder.Configuration, hostApplicationBuilder.Configuration);
        Assert.Same(builder.Environment, hostApplicationBuilder.Environment);
        Assert.Same(builder.Properties, hostApplicationBuilder.Properties);
        Assert.Same(builder.Services, hostApplicationBuilder.Services);
        Assert.NotNull(hostApplicationBuilder.Metrics);
        Assert.Same(builder.Services, hostApplicationBuilder.Metrics.Services);
    }

    [Fact]
    public void ConfigurationIsRegisteredAsSingleton()
    {
        var builder = WpfApp.CreateBuilder();
        var wpfApp = builder.Build();

        var configuration = wpfApp.Services.GetRequiredService<IConfiguration>();

        Assert.Same(builder.Configuration, configuration);
    }

    [Fact]
    public void DefaultBuilderRegistersLifecycleAndDispatching()
    {
        var builder = WpfApp.CreateBuilder();
        var wpfApp = builder.Build();

        Assert.NotNull(wpfApp.Services.GetService<ILifecycleEventService>());
        Assert.NotNull(wpfApp.Services.GetService<IDispatcherProvider>());
        Assert.NotNull(wpfApp.Services.GetService<IDispatcher>());
    }

    [Fact]
    public void BuilderWithoutDefaultsSkipsLifecycleAndDispatching()
    {
        var builder = WpfApp.CreateBuilder(useDefaults: false);
        var wpfApp = builder.Build();

        Assert.Null(wpfApp.Services.GetService<ILifecycleEventService>());
        Assert.Null(wpfApp.Services.GetService<IDispatcherProvider>());
        Assert.Null(wpfApp.Services.GetService<IDispatcher>());
    }
}
