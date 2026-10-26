// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.LifecycleEvents;

/// <summary>
/// Builds up the delegates registered for the lifecycle events.
/// </summary>
public interface ILifecycleBuilder
{
    /// <summary>Adds <paramref name="action"/> to the delegates invoked for <paramref name="eventName"/>.</summary>
    /// <typeparam name="TDelegate">The delegate type the event is invoked with.</typeparam>
    /// <param name="eventName">The event's name - for the built-in WPF events, the name of its <see cref="WpfLifecycle"/> delegate type.</param>
    /// <param name="action">The delegate to invoke.</param>
    void AddEvent<TDelegate>(string eventName, TDelegate action)
        where TDelegate : Delegate;
}
