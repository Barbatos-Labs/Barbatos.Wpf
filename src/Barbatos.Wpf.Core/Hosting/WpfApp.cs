// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Hosting;

/// <summary>
/// A WPF application with registered services and configuration data.
/// </summary>
/// <remarks>
/// This is the WPF counterpart of .NET MAUI's <c>MauiApp</c>.
/// </remarks>
public sealed class WpfApp : IDisposable, IAsyncDisposable
{
    private readonly IServiceProvider _services;

    internal WpfApp(IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>
    /// The application's configured services.
    /// </summary>
    public IServiceProvider Services => _services;

    /// <summary>
    /// The application's configured <see cref="IConfiguration"/>.
    /// </summary>
    public IConfiguration Configuration => _services.GetRequiredService<IConfiguration>();

    /// <summary>
    /// Initializes a new instance of the <see cref="WpfAppBuilder"/> class with optional defaults.
    /// </summary>
    /// <param name="useDefaults">Whether to create the <see cref="WpfAppBuilder"/> with common defaults.</param>
    /// <returns>The <see cref="WpfAppBuilder"/>.</returns>
    public static WpfAppBuilder CreateBuilder(bool useDefaults = true) => new(useDefaults);

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeConfiguration();

        (_services as IDisposable)?.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        DisposeConfiguration();

        if (_services is IAsyncDisposable asyncDisposable)
        {
            // Fire and forget because this is called from a sync context
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            (_services as IDisposable)?.Dispose();
        }
    }

    private void DisposeConfiguration()
    {
        // Explicitly dispose the Configuration, since it is added as a singleton object that the ServiceProvider
        // won't dispose.
        (Configuration as IDisposable)?.Dispose();
    }
}
