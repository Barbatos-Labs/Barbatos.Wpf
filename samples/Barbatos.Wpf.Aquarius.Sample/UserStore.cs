// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Net.Http;
using System.Net.Http.Json;
using Barbatos.Wpf.Apsu;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// A Barbatos.Wpf.Apsu store modeling the "userStore" from the cross-store scenarios in
/// https://pinia.vuejs.org/core-concepts/ - <see cref="OrderStore"/> depends on this one
/// (constructor injection, resolved once by <see cref="WpfProgram.CreateWpfApp"/>'s container)
/// the same way Pinia's own <c>useOrderStore</c> calls <c>useUserStore()</c> inside itself.
/// Backed by real network calls to https://jsonplaceholder.typicode.com (a free fake REST API)
/// rather than <see cref="Task.Delay(int)"/> simulations, via the shared <see cref="HttpClient"/>
/// singleton <see cref="WpfProgram.CreateWpfApp"/> registers.
/// </summary>
public class UserStore : StoreBase
{
    readonly HttpClient _httpClient;
    int? _selectedUserId;
    string _userName = "";
    string _companyName = "";

    public UserStore(HttpClient httpClient) => _httpClient = httpClient;

    /// <summary>State - a JSONPlaceholder user id (1-10; see <c>GET /users</c>). <see cref="OrderStore"/>
    /// watches this (see its own constructor) and automatically refreshes its orders when it
    /// changes - the "watch another store's state" pattern from the attached research (Cách 1).</summary>
    public int? SelectedUserId
    {
        get => _selectedUserId;
        private set => SetProperty(ref _selectedUserId, value);
    }

    /// <summary>State - fetched by <see cref="RefreshProfileAsync"/>.</summary>
    public string UserName
    {
        get => _userName;
        private set => SetProperty(ref _userName, value);
    }

    /// <summary>State - fetched by <see cref="RefreshProfileAsync"/>, and also by
    /// <see cref="OrderStore.CheckoutAsync"/> calling back into this store directly (Cách 2).</summary>
    public string CompanyName
    {
        get => _companyName;
        private set => SetProperty(ref _companyName, value);
    }

    [Action]
    public virtual void SelectUser(int userId) => SelectedUserId = userId;

    /// <summary>
    /// <c>GET https://jsonplaceholder.typicode.com/users/{id}</c> - a real network round-trip
    /// instead of a fixed <see cref="Task.Delay(int)"/>, so <c>ApsuTimingWarningInterceptor</c>'s
    /// warning threshold in <see cref="WpfProgram.CreateWpfApp"/> is now measuring genuine,
    /// variable latency.
    /// </summary>
    [Action]
    public virtual async Task RefreshProfileAsync()
    {
        if (SelectedUserId is not { } userId)
            return;

        var user = await _httpClient.GetFromJsonAsync<JsonPlaceholderUser>($"users/{userId}");
        UserName = user?.Name ?? "";
        CompanyName = user?.Company?.Name ?? "";
    }

    /// <summary>
    /// Clears all three properties in one <see cref="StoreBase.Patch"/>, so
    /// <see cref="StoreBase.StateChanged"/> fires once rather than once per property - and
    /// <c>UserSessionPersistencePlugin</c>, which writes on every <see cref="StoreBase.StateChanged"/>,
    /// writes the cleared selection once instead of three times.
    /// </summary>
    public override void Reset() => Patch(_ =>
    {
        SelectedUserId = null;
        UserName = "";
        CompanyName = "";
    });

    sealed record JsonPlaceholderUser(int Id, string Name, JsonPlaceholderCompany? Company);

    sealed record JsonPlaceholderCompany(string Name);
}
