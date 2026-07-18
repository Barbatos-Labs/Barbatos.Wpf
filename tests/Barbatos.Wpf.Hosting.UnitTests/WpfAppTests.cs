// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Hosting Contributors.
// All Rights Reserved.

using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Hosting.UnitTests;

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
}
