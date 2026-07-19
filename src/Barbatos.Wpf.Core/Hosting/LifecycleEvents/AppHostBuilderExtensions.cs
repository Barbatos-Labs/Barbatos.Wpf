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

    public LifecycleEventRegistration(Action<ILifecycleBuilder> registerAction)
    {
        _registerAction = registerAction;
    }

    internal void AddRegistration(ILifecycleBuilder effects)
    {
        _registerAction(effects);
    }
}

public static partial class WpfAppHostBuilderExtensions
{
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
