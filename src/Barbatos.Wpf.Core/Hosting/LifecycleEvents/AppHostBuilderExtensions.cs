// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Barbatos.Wpf.LifecycleEvents;

/// <summary>
/// Represents a registration of a delegate that configures the lifecycle events.
/// </summary>
public class LifecycleEventRegistration
{
    private readonly Action<ILifecycleBuilder> _registerAction;

    /// <summary>Creates a registration that runs <paramref name="registerAction"/> against the lifecycle builder.</summary>
    /// <param name="registerAction">Adds this registration's delegates to the builder.</param>
    public LifecycleEventRegistration(Action<ILifecycleBuilder> registerAction)
    {
        _registerAction = registerAction;
    }

    internal void AddRegistration(ILifecycleBuilder effects)
    {
        _registerAction(effects);
    }
}

/// <summary>
/// Lifecycle-event wiring on <see cref="WpfAppBuilder"/>, mirroring .NET MAUI's
/// <c>ConfigureLifecycleEvents</c>.
/// </summary>
public static partial class WpfAppHostBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="ILifecycleEventService"/> (once) and adds the lifecycle delegates that
    /// <paramref name="configureDelegate"/> declares. Safe to call more than once - every call's
    /// delegates are kept, in call order.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <param name="configureDelegate">Declares lifecycle delegates, typically via <c>AddWpf(wpf =&gt; wpf.OnStartup(...))</c>; <see langword="null"/> only ensures the service is registered.</param>
    /// <returns><paramref name="builder"/>, for chaining.</returns>
    public static WpfAppBuilder ConfigureLifecycleEvents(this WpfAppBuilder builder, Action<ILifecycleBuilder>? configureDelegate)
    {
        builder.Services.TryAddSingleton<ILifecycleEventService>(sp => new LifecycleEventService(sp.GetServices<LifecycleEventRegistration>()));
        if (configureDelegate != null)
        {
            builder.Services.AddSingleton<LifecycleEventRegistration>(new LifecycleEventRegistration(configureDelegate));
        }

        return builder;
    }
}
