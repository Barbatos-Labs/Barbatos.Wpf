# Apsu patterns

Worked examples. A section adapted from a file in `samples/` or `tests/` names that file; the
getter-to-XAML ViewModels are illustrations. The rules behind them are in [SKILL.md](SKILL.md).

Contents:

- [Cross-store stores](#cross-store-stores)
- [Guards: argument, state, cooldown](#guards-argument-state-cooldown)
- [A full chain: three global, three local](#a-full-chain-three-global-three-local)
- [One error path for actions and the dispatcher](#one-error-path-for-actions-and-the-dispatcher)
- [Persistence plugin](#persistence-plugin)
- [Exposing a getter to XAML](#exposing-a-getter-to-xaml)
- [Ejectable interceptor](#ejectable-interceptor)
- [Testing a store and an interceptor](#testing-a-store-and-an-interceptor)

## Cross-store stores

From `samples/Barbatos.Wpf.Aquarius.Sample/OrderStore.cs` (trimmed). Both stores are registered
with `AddStore`; registration order between them does not matter.

```csharp
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

        // List every store the callback reads - this one and UserStore.
        _summary = new Getter<string>(
            () => $"{OrderCount} order(s) for {_userStore.UserName} @ {_userStore.CompanyName}",
            this, _userStore);

        // "watch": react to another store without it knowing about this one. An async lambda
        // handler is async void - its exceptions go to the dispatcher, not to a caller.
        _userStore.PropertyChanged += async (_, args) =>
        {
            if (args.PropertyName == nameof(UserStore.SelectedUserId))
                await RefreshOrdersAsync();
        };
    }

    public int OrderCount
    {
        get => _orderCount;
        private set => SetProperty(ref _orderCount, value);
    }

    public string Summary => _summary.Value;

    [Action]
    public virtual async Task CheckoutAsync()
    {
        var response = await _httpClient.PostAsJsonAsync("posts", new { title = "New order", userId = _userStore.SelectedUserId });
        response.EnsureSuccessStatusCode();
        OrderCount++;
        await _userStore.RefreshProfileAsync(); // explicit cross-store call - runs UserStore's chain too
    }

    [Action]
    public virtual async Task RefreshOrdersAsync() { /* GET posts?userId=..., then set OrderCount */ }

    public override void Reset() => OrderCount = 0;
}
```

## Guards: argument, state, cooldown

All three short-circuit synchronously and call `next` otherwise - none awaits before it.

By argument - `samples/Barbatos.Wpf.Aquarius.Sample/UserIdValidationInterceptor.cs`:

```csharp
public Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
{
    if (context.ActionName == nameof(UserStore.SelectUser)
        && context.Arguments.Count > 0
        && context.Arguments[0] is int userId
        && (userId < 1 || userId > 10))
    {
        return Task.FromResult<object?>(null); // SelectUser's body never runs
    }

    return next();
}
```

By store state - `NonEmptyCartGuardInterceptor.cs`:

```csharp
if (context.ActionName == nameof(OrderStore.CheckoutAsync) && context.Store is OrderStore { OrderCount: 0 })
    return Task.FromResult<object?>(null);

return next();
```

By elapsed time, with a constructor value the container cannot supply -
`CheckoutCooldownInterceptor.cs` (constructor takes `TimeSpan cooldown, ApsuActivityLog activityLog`):

```csharp
public Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
{
    if (context.ActionName != nameof(OrderStore.CheckoutAsync))
        return next();

    var now = DateTimeOffset.UtcNow;
    var remaining = _cooldown - (now - _lastCheckoutAt);
    if (remaining > TimeSpan.Zero)
        return Task.FromResult<object?>(null);

    _lastCheckoutAt = now;
    return next();
}
```

## A full chain: three global, three local

From `samples/Barbatos.Wpf.Aquarius.Sample/WpfProgram.cs`:

```csharp
builder.Services.AddSingleton<ApsuActivityLog>(); // the one sink every interceptor reports into

// Global, outermost first.
builder.Services.AddSingleton<IStoreActionInterceptor, ApsuActionLoggingInterceptor>();
builder.Services.AddSingleton<IStoreActionInterceptor>(sp =>
    new ApsuTimingWarningInterceptor(TimeSpan.FromMilliseconds(200), sp.GetRequiredService<ApsuActivityLog>()));
builder.Services.AddSingleton<IStoreActionInterceptor, ApsuAuditTrailInterceptor>();

builder.Services.AddSingleton<IStorePlugin, UserSessionPersistencePlugin>();

// Local, inside every global, in Add order. The guard goes first so a rejected call never
// spends a cooldown token.
builder.Services.AddStore<UserStore>(i => i.Add<UserIdValidationInterceptor>());
builder.Services.AddStore<OrderStore>(i => i
    .Add<NonEmptyCartGuardInterceptor>()
    .Add(sp => new CheckoutCooldownInterceptor(TimeSpan.FromSeconds(3), sp.GetRequiredService<ApsuActivityLog>())));
```

`OrderStore.CheckoutAsync` therefore runs logging -> timing -> audit -> cart guard -> cooldown ->
action. When the cart guard blocks, the three globals still complete normally - `next()` simply
returned `null` to them - so the logging interceptor still writes its "finished" line.

## One error path for actions and the dispatcher

`samples/Barbatos.Wpf.Core.Sample`: `SampleActionLoggingInterceptor` logs a failed action and
rethrows; `CounterStore.SimulateError()` is deliberately uncaught, so the exception continues to
the `OnDispatcherUnhandledException` lifecycle hook in `WpfProgram.cs`, which reports into the same
`SampleActivityLog` and sets `args.Handled = true`. Pattern: observe in the interceptor with
`catch (Exception ex) { Report(...); throw; }`, decide handling once in the dispatcher hook
(`barbatos-wpf-host`).

## Persistence plugin

From `samples/Barbatos.Wpf.Aquarius.Sample/UserSessionPersistencePlugin.cs`. `IPreferences`
(namespace `Barbatos.Wpf.Storage`) is registered by `WpfApp.CreateBuilder()`; see
`barbatos-wpf-essentials` for where it writes.

```csharp
public sealed class UserSessionPersistencePlugin(IPreferences preferences) : IStorePlugin
{
    const string PreferenceKey = "Apsu.Sample.SelectedUserId";
    const int NoSelection = 0;

    public void Apply(StoreBase store)
    {
        if (store is not UserStore userStore)
            return;

        var savedUserId = preferences.Get(PreferenceKey, NoSelection);
        if (savedUserId != NoSelection)
            userStore.SelectUser(savedUserId); // an action: runs UserStore's interceptor chain

        // The $subscribe equivalent: once per property set, or once per Patch.
        userStore.StateChanged += (_, _) => preferences.Set(PreferenceKey, userStore.SelectedUserId ?? NoSelection);
    }
}
```

(The sample spells the constructor out; the primary constructor here is equivalent.) The restore
happens during `UserStore`'s resolution - before `OrderStore` exists to watch it.

## Exposing a getter to XAML

A `Getter<T>` raises no `PropertyChanged`, so bind to a ViewModel property re-raised on
`StateChanged` of **every** store the getter reads. The getter was subscribed in the store's
constructor, before the ViewModel subscribes, so it is already invalidated when this handler reads it.

```csharp
public sealed class OrdersViewModel : ObservableObject
{
    readonly OrderStore _orderStore;

    public OrdersViewModel(OrderStore orderStore, UserStore userStore)
    {
        _orderStore = orderStore;
        orderStore.StateChanged += (_, _) => OnPropertyChanged(nameof(Summary));
        userStore.StateChanged += (_, _) => OnPropertyChanged(nameof(Summary));
    }

    public string Summary => _orderStore.Summary;
}
```

Register such a ViewModel as a singleton, or unsubscribe when it goes away. The Core sample's
`MainViewModel` forwards on `PropertyChanged` instead; that only works because `CounterStore`
never uses `Patch`.

For a short-lived ViewModel, a disposable alternative from Aquarius (`Barbatos.Wpf.Aquarius.Reactivity`)
that reads raw store state rather than the store's getter:

```csharp
Summary = Computed<string>.From(
    () => $"{orderStore.OrderCount} order(s) for {userStore.UserName}",
    orderStore, userStore);
// Summary.Dispose() when the ViewModel goes away; bind to Summary.Value.
```

## Ejectable interceptor

From `StoreActionInterceptorTests.EjectingTakesEffectOnTheNextCallEvenAfterTheInterceptorAlreadyRanOnceBefore`
and the README:

```csharp
builder.Services.AddStoreInterceptor(new LoggingInterceptor(), out var loggingHandle);   // global
// local: builder.Services.AddStoreInterceptor<OrderStore>(interceptor, out var handle);

// Later, e.g. from a settings toggle:
loggingHandle.Eject();   // next call skips it; a call already running keeps it
```

Only instance overloads return a handle, and `StoreInterceptorBuilder<TStore>.Add` has none -
register an ejectable local interceptor with the standalone `AddStoreInterceptor<TStore>` call.

## Testing a store and an interceptor

Store logic needs no container - `StoreBaseTests` news the store up directly:

```csharp
var store = new TestStore();
var raised = 0;
store.StateChanged += (_, _) => raised++;

store.Patch(_ =>
{
    store.Count = 1;
    store.Name = "a";
});

Assert.Equal(1, raised);
```

Interception needs the real registration path - the `BuildStore` helper in
`StoreActionInterceptorTests.cs`:

```csharp
static TStore BuildStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TStore>(IStoreActionInterceptor interceptor) where TStore : StoreBase
{
    var builder = WpfApp.CreateBuilder();
    builder.Services.AddSingleton(interceptor);
    builder.Services.AddStore<TStore>();
    var wpfApp = builder.Build();

    return wpfApp.Services.GetRequiredService<TStore>();
}
```

The store under test is a `public` non-`sealed` nested class with `public virtual` `[Action]`
methods (`InterceptedStore`). Never add a fixture that awaits before `next` - it hangs the test
host instead of failing. Run with `barbatos-wpf-testing`'s per-TFM discipline:

```bash
dotnet test tests/Barbatos.Wpf.Core.UnitTests -f net8.0-windows10.0.17763.0 --filter "FullyQualifiedName~StoreActionInterceptorTests"
```
