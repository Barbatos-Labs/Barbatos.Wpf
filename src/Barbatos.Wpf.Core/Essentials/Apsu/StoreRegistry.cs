// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Apsu;

/// <summary>
/// The default <see cref="IStoreRegistry"/>: a thread-safe list, appended to by
/// <see cref="StoreServiceCollectionExtensions.AddStore{TStore}"/> as each store is resolved.
/// </summary>
internal sealed class StoreRegistry : IStoreRegistry
{
    readonly object _lock = new();
    readonly List<StoreBase> _stores = new();

    public IReadOnlyList<StoreBase> Stores
    {
        get
        {
            lock (_lock)
                return _stores.ToList();
        }
    }

    internal void Track(StoreBase store)
    {
        lock (_lock)
            _stores.Add(store);
    }
}
