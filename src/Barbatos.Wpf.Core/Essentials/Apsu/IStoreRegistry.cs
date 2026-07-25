// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Apsu;

/// <summary>
/// Tracks every store created through <see cref="StoreServiceCollectionExtensions.AddStore{TStore}"/>
/// for cross-cutting introspection. This is not what makes a store a singleton, or what caches
/// it - the dependency injection container already does both, the same way it does for any other
/// <c>services.AddSingleton&lt;T&gt;()</c> registration (this is this library's counterpart to
/// Pinia's <c>pinia._s</c> store cache, just keyed by CLR type instead of by string id). This
/// registry exists purely so something can ask "which stores actually exist right now" - resolved
/// automatically via dependency injection, so there is normally no need to depend on it directly.
/// </summary>
public interface IStoreRegistry
{
    /// <summary>
    /// Every store that has actually been resolved so far. A store type registered with
    /// <see cref="StoreServiceCollectionExtensions.AddStore{TStore}"/> but never injected or
    /// resolved anywhere does not appear here yet - the same laziness Pinia's own
    /// <c>useStore()</c> has (a store nobody asked for is never created).
    /// </summary>
    IReadOnlyList<StoreBase> Stores { get; }
}
