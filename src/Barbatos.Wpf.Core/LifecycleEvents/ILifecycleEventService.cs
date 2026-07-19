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
    IEnumerable<TDelegate> GetEventDelegates<TDelegate>(string eventName)
        where TDelegate : Delegate;

    bool ContainsEvent(string eventName);
}
