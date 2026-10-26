// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.LifecycleEvents;

/// <summary>
/// Invocation helpers over <see cref="ILifecycleEventService"/>.
/// </summary>
public static class LifecycleEventServiceExtensions
{
    /// <summary>Invokes every parameterless <see cref="Action"/> registered for <paramref name="eventName"/>, in registration order.</summary>
    public static void InvokeEvents(this ILifecycleEventService lifecycleService, string eventName) =>
        lifecycleService.InvokeEvents<Action>(eventName, action => action?.Invoke());

    /// <summary>
    /// Passes every <typeparamref name="TDelegate"/> registered for <paramref name="eventName"/>
    /// to <paramref name="action"/>, in registration order - <paramref name="action"/> decides
    /// what arguments each delegate is invoked with.
    /// </summary>
    public static void InvokeEvents<TDelegate>(this ILifecycleEventService lifecycleService, string eventName, Action<TDelegate> action)
        where TDelegate : Delegate
    {
        var delegates = lifecycleService.GetEventDelegates<TDelegate>(eventName);

        foreach (var del in delegates)
            action?.Invoke(del);
    }

    internal static void InvokeLifecycleEvents<TDelegate>(this IServiceProvider services, Action<TDelegate> action)
        where TDelegate : Delegate
    {
        if (services == null)
            return;

        var delegates = services.GetLifecycleEventDelegates<TDelegate>();

        foreach (var del in delegates)
            action?.Invoke(del);
    }

    internal static IEnumerable<TDelegate> GetLifecycleEventDelegates<TDelegate>(this IServiceProvider services, string? eventName = null)
        where TDelegate : Delegate
    {
        var lifecycleService = services?.GetService<ILifecycleEventService>();
        if (lifecycleService == null)
            yield break;

        if (eventName == null)
            eventName = typeof(TDelegate).Name;

        foreach (var del in lifecycleService.GetEventDelegates<TDelegate>(eventName))
            yield return del;
    }

    internal static bool ContainsLifecycleEvent<TDelegate>(this IServiceProvider services, string? eventName = null)
        where TDelegate : Delegate
    {
        var lifecycleService = services?.GetService<ILifecycleEventService>();
        if (lifecycleService == null)
            return false;

        if (eventName == null)
            eventName = typeof(TDelegate).Name;

        if (lifecycleService.ContainsEvent(eventName))
            return true;

        return false;
    }
}
