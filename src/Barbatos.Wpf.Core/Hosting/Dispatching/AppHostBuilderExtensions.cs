// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Dispatching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Barbatos.Wpf.Hosting;

public static partial class AppHostBuilderExtensions
{
    /// <summary>
    /// Registers the dispatching services, mirroring .NET MAUI's <c>ConfigureDispatching</c>.
    /// </summary>
    public static WpfAppBuilder ConfigureDispatching(this WpfAppBuilder builder)
    {
        // register the DispatcherProvider as a singleton for the entire app
        builder.Services.TryAddSingleton<IDispatcherProvider>(svc =>
            // the DispatcherProvider might have already been initialized, so ensure that we are grabbing the
            // Current and putting it in the DI container.
            DispatcherProvider.Current);

        // register a fallback dispatcher for the application itself
        builder.Services.TryAddSingleton<ApplicationDispatcher>(svc => new ApplicationDispatcher(GetDispatcher(svc, false)));
        // register the initializer so we can init the dispatcher in the app thread for the app
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IWpfInitializeService, ApplicationDispatcherInitializer>());

        // register the Dispatcher as a scoped service as there may be different dispatchers per window
        builder.Services.TryAddScoped<IDispatcher>(svc => GetDispatcher(svc, true));
        // register the initializer so we can init the dispatcher in the window thread for that window
        builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IWpfInitializeScopedService, DispatcherInitializer>());

        return builder;
    }

    internal static IDispatcher GetRequiredApplicationDispatcher(this IServiceProvider provider) =>
        provider.GetRequiredService<ApplicationDispatcher>().Dispatcher;

    internal static IDispatcher? GetOptionalApplicationDispatcher(this IServiceProvider provider) =>
        provider.GetService<ApplicationDispatcher>()?.Dispatcher;

    static IDispatcher GetDispatcher(IServiceProvider services, bool fallBackToApplicationDispatcher)
    {
        var provider = services.GetRequiredService<IDispatcherProvider>();
        if (DispatcherProvider.SetCurrent(provider))
        {
            services.CreateLogger<Dispatcher>()?.LogWarning("Replaced an existing DispatcherProvider with one from the service provider.");
        }

        var result = Dispatcher.GetForCurrentThread();

        if (fallBackToApplicationDispatcher && result is null)
            result = services.GetRequiredService<ApplicationDispatcher>().Dispatcher;

        return result!;
    }

    class ApplicationDispatcherInitializer : IWpfInitializeService
    {
        public void Initialize(IServiceProvider services)
        {
            _ = services.GetOptionalApplicationDispatcher();
        }
    }

    class DispatcherInitializer : IWpfInitializeScopedService
    {
        public void Initialize(IServiceProvider services)
        {
            _ = services.GetRequiredService<IDispatcher>();
        }
    }
}
