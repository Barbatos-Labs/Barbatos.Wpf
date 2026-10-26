// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.LifecycleEvents;

/// <summary>
/// Provides access to the delegates registered for the lifecycle events.
/// </summary>
public interface ILifecycleEventService
{
    /// <summary>
    /// The delegates registered for <paramref name="eventName"/> that are of type
    /// <typeparamref name="TDelegate"/>, in registration order - empty when there are none.
    /// </summary>
    IEnumerable<TDelegate> GetEventDelegates<TDelegate>(string eventName)
        where TDelegate : Delegate;

    /// <summary>Whether at least one delegate is registered for <paramref name="eventName"/>.</summary>
    bool ContainsEvent(string eventName);
}
