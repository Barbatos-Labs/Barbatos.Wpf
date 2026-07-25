// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Apsu;
using Barbatos.Wpf.Storage;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// Demonstrates the attached research's "Auto-Sync/Persist" <see cref="IStorePlugin"/> use case -
/// Pinia's own equivalent reads/writes <c>localStorage</c> directly inside the plugin
/// (<c>store.$patch(JSON.parse(localStorage.getItem(...)))</c> then <c>store.$subscribe(...)</c>);
/// this uses Barbatos.Wpf.Core's own <see cref="IPreferences"/> (already available - <c>WpfApp.CreateBuilder()</c>
/// registers it by default) instead of reinventing key-value storage.
/// </summary>
/// <remarks>
/// Deliberately narrow (only persists <see cref="UserStore.SelectedUserId"/>) rather than a
/// generic "persist any store" framework feature, which nothing here asked for - a real generic
/// version would need per-store (de)serialization logic that this single demo key sidesteps.
/// </remarks>
public sealed class UserSessionPersistencePlugin : IStorePlugin
{
    const string PreferenceKey = "Apsu.Sample.SelectedUserId";
    const int NoSelection = 0;

    readonly IPreferences _preferences;

    public UserSessionPersistencePlugin(IPreferences preferences) => _preferences = preferences;

    public void Apply(StoreBase store)
    {
        if (store is not UserStore userStore)
            return;

        var savedUserId = _preferences.Get(PreferenceKey, NoSelection);
        if (savedUserId != NoSelection)
            userStore.SelectUser(savedUserId);

        // The $subscribe-equivalent: fires once per direct property set, or once per Patch(...).
        userStore.StateChanged += (_, _) => _preferences.Set(PreferenceKey, userStore.SelectedUserId ?? NoSelection);
    }
}
