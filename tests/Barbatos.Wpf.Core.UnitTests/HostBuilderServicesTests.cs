// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Barbatos.Wpf.Core.UnitTests;

public class HostBuilderServicesTests
{
    [Fact]
    public void CanGetServices()
    {
        var builder = WpfApp.CreateBuilder();
        var wpfApp = builder.Build();

        Assert.NotNull(wpfApp.Services);
    }

    [Fact]
    public void GetServiceThrowsWhenConstructorParamTypesWereNotRegistered()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddTransient<IFooBarService, FooBarService>();
        var wpfApp = builder.Build();

        Assert.Throws<InvalidOperationException>(() => wpfApp.Services.GetService<IFooBarService>());
    }

    [Fact]
    public void GetServiceThrowsWhenNoPublicConstructors()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddTransient<IFooService, BadFooService>();
        var wpfApp = builder.Build();

        var ex = Assert.Throws<InvalidOperationException>(() => wpfApp.Services.GetService<IFooService>());
        Assert.Contains("suitable constructor", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetServiceHandlesFirstOfMultipleConstructors()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddTransient<IFooService, FooService>();
        builder.Services.AddTransient<IFooBarService, FooDualConstructor>();
        var wpfApp = builder.Build();

        var service = wpfApp.Services.GetService<IFooBarService>();

        var foobar = Assert.IsType<FooDualConstructor>(service);
        Assert.IsType<FooService>(foobar.Foo);
    }

    [Fact]
    public void GetServiceHandlesSecondOfMultipleConstructors()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddTransient<IBarService, BarService>();
        builder.Services.AddTransient<IFooBarService, FooDualConstructor>();
        var wpfApp = builder.Build();

        var service = wpfApp.Services.GetService<IFooBarService>();

        var foobar = Assert.IsType<FooDualConstructor>(service);
        Assert.IsType<BarService>(foobar.Bar);
    }

    [Fact]
    public void GetServiceCanReturnTypesThatHaveConstructorParams()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddTransient<IFooService, FooService>();
        builder.Services.AddTransient<IBarService, BarService>();
        builder.Services.AddTransient<IFooBarService, FooBarService>();
        var wpfApp = builder.Build();

        var service = wpfApp.Services.GetService<IFooBarService>();

        var foobar = Assert.IsType<FooBarService>(service);
        Assert.IsType<FooService>(foobar.Foo);
        Assert.IsType<BarService>(foobar.Bar);
    }

    [Fact]
    public void GetServiceCanReturnTypesThatHaveUnregisteredConstructorParamsButHaveDefaultValues()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddTransient<IFooBarService, FooDefaultValueConstructor>();
        var wpfApp = builder.Build();

        var foo = wpfApp.Services.GetService<IFooBarService>();

        Assert.NotNull(foo);

        var actual = Assert.IsType<FooDefaultValueConstructor>(foo);

        Assert.Null(actual.Bar);
    }

    [Fact]
    public void GetServiceCanReturnTypesThatHaveRegisteredConstructorParamsAndHaveDefaultValues()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddTransient<IBarService, BarService>();
        builder.Services.AddTransient<IFooBarService, FooDefaultValueConstructor>();
        var wpfApp = builder.Build();

        var foo = wpfApp.Services.GetService<IFooBarService>();

        Assert.NotNull(foo);

        var actual = Assert.IsType<FooDefaultValueConstructor>(foo);

        Assert.NotNull(actual.Bar);
        Assert.IsType<BarService>(actual.Bar);
    }

    [Fact]
    public void GetServiceCanReturnTypesThatHaveSystemDefaultValues()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddTransient<IFooBarService, FooDefaultSystemValueConstructor>();
        var wpfApp = builder.Build();

        var foo = wpfApp.Services.GetService<IFooBarService>();

        Assert.NotNull(foo);

        var actual = Assert.IsType<FooDefaultSystemValueConstructor>(foo);

        Assert.Equal("Default Value", actual.Text);
    }

    [Fact]
    public void GetServiceCanReturnEnumerableParams()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddTransient<IFooService, FooService>();
        builder.Services.AddTransient<IFooService, FooService2>();
        builder.Services.AddTransient<IFooBarService, FooEnumerableService>();
        var wpfApp = builder.Build();

        var service = wpfApp.Services.GetService<IFooBarService>();
        var foobar = Assert.IsType<FooEnumerableService>(service);

        var serviceTypes = foobar.Foos
            .Select(s => s.GetType().FullName)
            .ToArray();
        Assert.Contains(typeof(FooService).FullName, serviceTypes);
        Assert.Contains(typeof(FooService2).FullName, serviceTypes);
    }

    [Fact]
    public void WillRetrieveDifferentTransientServices()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddTransient<IFooService, FooService>();
        var wpfApp = builder.Build();

        AssertTransient<IFooService, FooService>(wpfApp.Services);
    }

    [Fact]
    public void WillRetrieveSameSingletonServices()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IFooService, FooService>();
        var wpfApp = builder.Build();

        AssertSingleton<IFooService, FooService>(wpfApp.Services);
    }

    [Fact]
    public void WillRetrieveMixedServices()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IFooService, FooService>();
        builder.Services.AddTransient<IBarService, BarService>();
        var wpfApp = builder.Build();

        AssertSingleton<IFooService, FooService>(wpfApp.Services);
        AssertTransient<IBarService, BarService>(wpfApp.Services);
    }

    [Fact]
    public void WillRetrieveEnumerables()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddTransient<IFooService, FooService>();
        builder.Services.AddTransient<IFooService, FooService2>();
        var wpfApp = builder.Build();

        var fooServices = wpfApp.Services
            .GetServices<IFooService>()
            .ToArray();
        Assert.Equal(2, fooServices.Length);

        var serviceTypes = fooServices
            .Select(s => s.GetType().FullName)
            .ToArray();
        Assert.Contains(typeof(FooService).FullName, serviceTypes);
        Assert.Contains(typeof(FooService2).FullName, serviceTypes);
    }

    [Fact]
    public void CanCreateLogger()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddLogging();
        var wpfApp = builder.Build();

        var factory = wpfApp.Services.GetRequiredService<ILoggerFactory>();

        var logger = factory.CreateLogger<HostBuilderServicesTests>();

        Assert.NotNull(logger);
    }

    static void AssertTransient<TInterface, TConcrete>(IServiceProvider services)
    {
        var service1 = services.GetService<TInterface>();

        Assert.NotNull(service1);
        Assert.IsType<TConcrete>(service1);

        var service2 = services.GetService<TInterface>();

        Assert.NotNull(service2);
        Assert.IsType<TConcrete>(service2);

        Assert.NotEqual((object?)service1, (object?)service2);
    }

    static void AssertSingleton<TInterface, TConcrete>(IServiceProvider services)
    {
        var service1 = services.GetService<TInterface>();

        Assert.NotNull(service1);
        Assert.IsType<TConcrete>(service1);

        var service2 = services.GetService<TInterface>();

        Assert.NotNull(service2);
        Assert.IsType<TConcrete>(service2);

        Assert.Equal((object?)service1, (object?)service2);
    }
}
