// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

public class WpfAppTests
{
    [Fact]
    public void DisposeDisposesTheServiceProvider()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<DisposableService>();
        var wpfApp = builder.Build();

        var service = wpfApp.Services.GetRequiredService<DisposableService>();

        wpfApp.Dispose();

        Assert.True(service.IsDisposed);
    }

    [Fact]
    public async Task DisposeAsyncDisposesTheServiceProvider()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<DisposableService>();
        var wpfApp = builder.Build();

        var service = wpfApp.Services.GetRequiredService<DisposableService>();

        await wpfApp.DisposeAsync();

        Assert.True(service.IsDisposed);
    }

    [Fact]
    public void ServicesResolvedAfterDisposeThrows()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IFooService, FooService>();
        var wpfApp = builder.Build();

        wpfApp.Dispose();

        Assert.Throws<ObjectDisposedException>(() => wpfApp.Services.GetService<IFooService>());
    }

    [Fact]
    public void AServiceThatResolvesFromTheProviderDuringItsOwnDisposeSeesObjectDisposedException()
    {
        // This is the mechanism behind a real, reported shutdown crash: ITrayIconPlatform's
        // Dispose() tears down a native window, which can reenter WPF's Application and, from
        // there, code that resolves ILifecycleEventService off IServiceProvider again - all
        // while still inside this same Dispose() cascade. Confirms the underlying container
        // behavior WpfApplication.OnExit() now has to guard against by clearing its _appHost
        // field *before* calling Dispose() on it, not after (every event forwarder there reads
        // that field directly, so a reentrant call during Dispose() must see it already gone).
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IFooService, FooService>();
        builder.Services.AddSingleton<ReentrantDisposeService>();
        var wpfApp = builder.Build();

        var service = wpfApp.Services.GetRequiredService<ReentrantDisposeService>();

        wpfApp.Dispose();

        Assert.IsType<ObjectDisposedException>(service.CaughtDuringDispose);
    }
}
