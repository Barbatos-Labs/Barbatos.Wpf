// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

public class HostBuilderConfigureContainerTests
{
    [Fact]
    public void ConfigureContainerThrowsOnNullFactory()
    {
        var builder = WpfApp.CreateBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.ConfigureContainer<IServiceCollection>(null!));
    }

    [Fact]
    public void BuildUsesTheRegisteredServiceProviderFactory()
    {
        var factory = new TrackingServiceProviderFactory();

        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IFooService, FooService>();
        builder.ConfigureContainer(factory);
        var wpfApp = builder.Build();

        Assert.True(factory.CreateBuilderCalled);
        Assert.True(factory.CreateServiceProviderCalled);
        Assert.IsType<FooService>(wpfApp.Services.GetService<IFooService>());
    }

    [Fact]
    public void ConfigureContainerInvokesTheConfigureDelegate()
    {
        var factory = new TrackingServiceProviderFactory();
        var configured = false;

        var builder = WpfApp.CreateBuilder();
        builder.ConfigureContainer(factory, container =>
        {
            configured = true;
            container.AddSingleton<IFooService, FooService>();
        });
        var wpfApp = builder.Build();

        Assert.True(configured);
        Assert.IsType<FooService>(wpfApp.Services.GetService<IFooService>());
    }

    [Fact]
    public void LastConfigureContainerWins()
    {
        var factory1 = new TrackingServiceProviderFactory();
        var factory2 = new TrackingServiceProviderFactory();

        var builder = WpfApp.CreateBuilder();
        builder.ConfigureContainer(factory1);
        builder.ConfigureContainer(factory2);
        builder.Build();

        Assert.False(factory1.CreateBuilderCalled);
        Assert.True(factory2.CreateBuilderCalled);
    }

    class TrackingServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
    {
        public bool CreateBuilderCalled { get; private set; }

        public bool CreateServiceProviderCalled { get; private set; }

        public IServiceCollection CreateBuilder(IServiceCollection services)
        {
            CreateBuilderCalled = true;
            return services;
        }

        public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
        {
            CreateServiceProviderCalled = true;
            return containerBuilder.BuildServiceProvider();
        }
    }
}
