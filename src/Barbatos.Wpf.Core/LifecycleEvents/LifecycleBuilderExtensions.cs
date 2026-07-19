// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Runtime.CompilerServices;

namespace Barbatos.Wpf.LifecycleEvents;

public static class LifecycleBuilderExtensions
{
    public static ILifecycleBuilder AddEvent(this ILifecycleBuilder builder, string eventName, Action action)
    {
        builder.AddEvent<Action>(eventName, action);

        return builder;
    }

    public static ILifecycleBuilder AddEvent<TDelegate>(this ILifecycleBuilder builder, string eventName, TDelegate action)
        where TDelegate : Delegate
    {
        builder.AddEvent(eventName, action);

        return builder;
    }

    internal static TLifecycleBuilder OnEvent<TLifecycleBuilder, TDelegate>(this TLifecycleBuilder builder, TDelegate action, [CallerMemberName] string? eventName = null)
        where TLifecycleBuilder : ILifecycleBuilder
        where TDelegate : Delegate
    {
        builder.AddEvent(eventName ?? typeof(TDelegate).Name, action);

        return builder;
    }
}
