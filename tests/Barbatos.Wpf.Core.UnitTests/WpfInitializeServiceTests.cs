// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

public class WpfInitializeServiceTests
{
    [Fact]
    public void InitializeServicesAreInvokedDuringBuild()
    {
        var initService = new TrackingInitializeService();

        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IWpfInitializeService>(initService);
        var wpfApp = builder.Build();

        Assert.Equal(1, initService.InitializeCount);
        Assert.Same(wpfApp.Services, initService.LastServices);
    }

    [Fact]
    public void InitializeServicesAreInvokedInRegistrationOrder()
    {
        var order = new List<string>();

        var builder = WpfApp.CreateBuilder(useDefaults: false);
        builder.Services.AddSingleton<IWpfInitializeService>(new CallbackInitializeService(() => order.Add("first")));
        builder.Services.AddSingleton<IWpfInitializeService>(new CallbackInitializeService(() => order.Add("second")));
        builder.Build();

        Assert.Equal(new[] { "first", "second" }, order);
    }

    [Fact]
    public void ScopedInitializeServicesAreNotInvokedDuringBuild()
    {
        var initService = new TrackingInitializeScopedService();

        var builder = WpfApp.CreateBuilder();
        builder.Services.AddScoped<IWpfInitializeScopedService>(sp => initService);
        builder.Build();

        Assert.Equal(0, initService.InitializeCount);
    }

    [Fact]
    public void ScopedInitializeServicesAreInvokedByInitializeScopedServices()
    {
        var initService = new TrackingInitializeScopedService();

        var builder = WpfApp.CreateBuilder();
        builder.Services.AddScoped<IWpfInitializeScopedService>(sp => initService);
        var wpfApp = builder.Build();

        using var scope = wpfApp.Services.CreateScope();
        scope.ServiceProvider.InitializeScopedServices();

        Assert.Equal(1, initService.InitializeCount);
        Assert.Same(scope.ServiceProvider, initService.LastServices);
    }

    class TrackingInitializeService : IWpfInitializeService
    {
        public int InitializeCount { get; private set; }

        public IServiceProvider? LastServices { get; private set; }

        public void Initialize(IServiceProvider services)
        {
            InitializeCount++;
            LastServices = services;
        }
    }

    class TrackingInitializeScopedService : IWpfInitializeScopedService
    {
        public int InitializeCount { get; private set; }

        public IServiceProvider? LastServices { get; private set; }

        public void Initialize(IServiceProvider services)
        {
            InitializeCount++;
            LastServices = services;
        }
    }

    class CallbackInitializeService : IWpfInitializeService
    {
        readonly Action _callback;

        public CallbackInitializeService(Action callback) => _callback = callback;

        public void Initialize(IServiceProvider services) => _callback();
    }
}
