// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Net.Http;
using System.Net.Http.Json;
using Barbatos.Wpf.Apsu;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// A Barbatos.Wpf.Apsu store modeling the "orderStore" half of the cross-store scenarios in
/// https://pinia.vuejs.org/core-concepts/ - JSONPlaceholder has no "orders" resource, so this
/// treats a user's <c>/posts</c> as a stand-in for their orders (fetch = list them, checkout =
/// create one), while keeping the same shape the attached research's own example has. Depends on
/// <see cref="UserStore"/> via ordinary constructor injection (the DI container resolves and
/// shares the same <see cref="UserStore"/> instance, since it too is registered with
/// <c>AddStore</c>) - directly analogous to Pinia's own <c>const userStore = useUserStore()</c>
/// called from inside another store.
/// </summary>
public class OrderStore : StoreBase
{
    readonly HttpClient _httpClient;
    readonly UserStore _userStore;
    int _orderCount;
    readonly Getter<string> _summary;

    public OrderStore(HttpClient httpClient, UserStore userStore)
    {
        _httpClient = httpClient;
        _userStore = userStore;

        // Combines this store's own OrderCount with UserStore's CompanyName - Pinia's own
        // equivalent ("a getter combining data from two different stores") needs no special
        // syntax because Vue auto-tracks both; here both dependency stores are listed explicitly.
        _summary = new Getter<string>(
            () => $"{OrderCount} order(s) for {_userStore.UserName} @ {_userStore.CompanyName}",
            this, _userStore);

        // Cách 1 (watch): reacts to UserStore changing, without UserStore needing to know
        // OrderStore exists at all - the same relationship Pinia's own watch(() => userStore.x, ...)
        // sets up inside a store's setup(). StoreBase is a plain INotifyPropertyChanged
        // underneath, so an ordinary PropertyChanged subscription is this port's "watch".
        _userStore.PropertyChanged += async (_, args) =>
        {
            if (args.PropertyName == nameof(UserStore.SelectedUserId))
                await RefreshOrdersAsync();
        };

        // ...and, like Vue's watch(..., { immediate: true }), react to the value already there.
        // UserSessionPersistencePlugin restores the last session's SelectedUserId while UserStore
        // is still being resolved - always before this constructor runs, since it needs that
        // UserStore - so the change notification for it was raised before the subscription above
        // existed and will never arrive. Without this, a restored user showed no orders until the
        // selection changed again.
        if (_userStore.SelectedUserId is not null)
            LoadOrdersForRestoredUser();
    }

    // async void because nothing can await a constructor. Unlike the watch handler (a click the
    // user just made), this runs at startup, so being offline must not take the app down: the
    // logging interceptor has already reported the failure by the time it lands here, and
    // "Refresh orders" tries again. RefreshOrdersAsync is an action, so this call runs the
    // interceptor chain too - Castle wires a proxy's interceptors before the store's own
    // constructor runs.
    async void LoadOrdersForRestoredUser()
    {
        try
        {
            await RefreshOrdersAsync();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
        }
    }

    /// <summary>State.</summary>
    public int OrderCount
    {
        get => _orderCount;
        private set => SetProperty(ref _orderCount, value);
    }

    /// <summary>A getter combining this store with <see cref="UserStore"/> - see the constructor.</summary>
    public string Summary => _summary.Value;

    /// <summary><c>GET https://jsonplaceholder.typicode.com/posts?userId={id}</c> - a real
    /// network round-trip instead of a fixed <see cref="Task.Delay(int)"/>.</summary>
    [Action]
    public virtual async Task RefreshOrdersAsync()
    {
        if (_userStore.SelectedUserId is not { } userId)
        {
            OrderCount = 0;
            return;
        }

        var posts = await _httpClient.GetFromJsonAsync<JsonPlaceholderPost[]>($"posts?userId={userId}");
        OrderCount = posts?.Length ?? 0;
    }

    /// <summary>
    /// Cách 2 (explicit cross-store action call): <c>POST https://jsonplaceholder.typicode.com/posts</c>
    /// to place an order (JSONPlaceholder fakes the create - it returns a plausible response but
    /// does not actually persist it), then calls straight into <see cref="UserStore.RefreshProfileAsync"/> -
    /// the same shape as the attached research's <c>checkout()</c> calling <c>userStore.fetchUserWallet()</c>
    /// after creating an order.
    /// </summary>
    [Action]
    public virtual async Task CheckoutAsync()
    {
        var response = await _httpClient.PostAsJsonAsync("posts", new { title = "New order", userId = _userStore.SelectedUserId });
        response.EnsureSuccessStatusCode();

        OrderCount++;
        await _userStore.RefreshProfileAsync();
    }

    public override void Reset() => OrderCount = 0;

    sealed record JsonPlaceholderPost(int Id, int UserId, string Title);
}
