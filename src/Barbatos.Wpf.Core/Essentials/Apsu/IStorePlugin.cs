// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Apsu;

/// <summary>
/// Extends every store with cross-cutting behavior (logging, persistence, undo/redo, ...) -
/// the <c>pinia.use(plugin)</c> counterpart. Register an implementation with
/// <c>services.AddSingleton&lt;IStorePlugin, MyPlugin&gt;()</c>; every registered plugin's
/// <see cref="Apply"/> runs once for every store created via
/// <see cref="StoreServiceCollectionExtensions.AddStore{TStore}"/>, in the same order they were
/// registered. Applied after the store's constructor has already run, matching Pinia's own
/// plugin timing - a plugin can read or write the store's state, or subscribe to
/// <see cref="StoreBase.StateChanged"/>, but (like a Pinia plugin) cannot transparently
/// intercept an action call, since no action-wrapping pipeline exists to hook into.
/// </summary>
public interface IStorePlugin
{
    /// <summary>Applies this plugin to <paramref name="store"/>, once, right after construction.</summary>
    void Apply(StoreBase store);
}
