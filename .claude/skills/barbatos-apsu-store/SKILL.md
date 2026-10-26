---
name: barbatos-apsu-store
description: Manage shared application state with Barbatos.Wpf.Apsu (part of Barbatos.Wpf.Core) - StoreBase with batched StateChanged and Patch, Getter<T> cached derived values, [Action]-marked virtual methods wrapped by IStoreActionInterceptor middleware, IStorePlugin, IStoreRegistry, and the AddStore/AddStoreInterceptor registrations including InterceptorHandle.Eject. Use this whenever state has to be shared across several ViewModels or screens rather than owned by one, when a value should be derived from other state and cached, when an action needs logging, timing, retry, rate-limiting, auditing or guarding without editing the action itself, when a store should reset to its initial state, when a store's [Action] is not being intercepted at all, and when an intercepted action mysteriously hangs and never returns. Vietnamese prompts land here too: trạng thái dùng chung, chia sẻ state giữa các màn hình, store, giá trị tính toán từ state khác, ghi log hành động, hành động bị treo không trả về.
---

# Barbatos.Wpf.Apsu — state management

A Pinia-shaped store layer on `CommunityToolkit.Mvvm.ObservableObject`, part of
`Barbatos.Wpf.Core`. Interception is ASP.NET Core-style middleware over Castle DynamicProxy.

Full docs: [`README.md#state-management-barbatoswpfapsu`](../../../src/Barbatos.Wpf.Core/README.md) ·
[`API-REFERENCE.md#barbatoswpfapsu-namespace`](../../../src/Barbatos.Wpf.Core/API-REFERENCE.md).
Reference: [`samples/Barbatos.Wpf.Core.Sample/CounterStore.cs`](../../../samples/Barbatos.Wpf.Core.Sample/CounterStore.cs)
and the six-interceptor chain in
[`samples/Barbatos.Wpf.Aquarius.Sample/WpfProgram.cs`](../../../samples/Barbatos.Wpf.Aquarius.Sample/WpfProgram.cs).

---

## A store

```csharp
public class CounterStore : StoreBase
{
    int _count;
    readonly Getter<string> _doubleCountLabel;

    public CounterStore() => _doubleCountLabel = new Getter<string>(() => $"Double: {Count * 2}", this);

    public int Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }

    public string DoubleCountLabel => _doubleCountLabel.Value;

    [Action]
    public virtual void Increment() => Count++;

    public override void Reset() => Count = 0;
}
```

```csharp
builder.Services.AddStore<CounterStore>();
```

**Four shape requirements — violating any of them fails, sometimes late:**

| Requirement | Failure if broken |
| --- | --- |
| Class is **not `sealed`** | `InvalidOperationException` at first resolution, once an interceptor applies |
| **Exactly one public constructor** | same |
| Action methods are `public virtual` and `[Action]`-marked | silently never intercepted |
| `Reset()` is overridden | `NotSupportedException` naming the store type — deliberate, so a forgotten override fails loudly |

`AddStore<TStore>()` registers a **singleton**, created lazily and cached. A store's constructor
may take other services or other stores. A store with no applicable interceptor gets a plain
`ActivatorUtilities`-created instance — no proxy, no cost.

`StateChanged` fires once per property set outside `Patch`, or **once per `Patch` call** however
many properties it touched (individual `PropertyChanged` still fires per property). A nested
`Patch` lets the outermost call be the one that fires.

---

## Getters

```csharp
_summary = new Getter<string>(
    () => $"{OrderCount} order(s) for {userStore.UserName}",
    this, userStore);          // list EVERY store the callback reads
```

C# has no proxy-based tracking, so dependencies are explicit and invalidation is **coarse** — any
`StateChanged` on any listed dependency recomputes. That is a deliberate simplification, not a
gap to close. `Value` recomputes only if invalidated since the last read.

---

## Interceptors

```csharp
public sealed class TimingInterceptor(ActivityLog log) : IStoreActionInterceptor
{
    public async Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await next();
            log.Log($"{context.Store.GetType().Name}.{context.ActionName}() took {sw.ElapsedMilliseconds}ms");
            return result;
        }
        catch (Exception ex)
        {
            log.Log($"failed: {ex.Message}");
            throw;                 // still propagates to the caller
        }
    }
}
```

`StoreActionContext` carries `Store`, `ActionName`, and `Arguments` (declaration order) — enough
to reject an out-of-range argument by simply **not calling `next`**.

### The one rule that will hang your app

> **Call `next` as your first meaningful step, if you call it at all. Never `await` anything
> genuinely asynchronous before it.**

Castle's underlying `IInvocation.Proceed()` is not safe to reach from a continuation that already
crossed a real `await` suspension relative to `InterceptAsync`'s synchronous call — doing so
**hangs the action forever rather than throwing**. Awaiting real async work *after* `next`
returns (flushing a log, persisting) is fully supported.

### Composition order

- **Global** (`services.AddSingleton<IStoreActionInterceptor, T>()`) — wraps every action on every
  store, in registration order, **outside** every local one.
- **Local** (one store type) — register inside `AddStore`:

  ```csharp
  builder.Services.AddStore<OrderStore>(i => i
      .Add<NonEmptyCartGuardInterceptor>()                 // DI-constructed
      .Add(existingInstance)                               // an instance you built
      .Add(sp => new CooldownInterceptor(TimeSpan.FromSeconds(3), sp.GetRequiredService<Log>())));
  ```

  `Add` overloads chain and mix freely; they run in `Add` order, innermost relative to globals.
  Use the factory overload when a constructor needs a value the container cannot invent (a
  threshold, a duration) alongside services it can.

**Order locals by cost:** a guard that rejects a call should run *before* an interceptor that
spends a rate-limit token on it.

`AddStoreInterceptor<TStore, TInterceptor>()` (keyed DI) is the standalone equivalent; prefer the
`AddStore` callback so one call covers a store and every local interceptor it needs.

### Return types

Sync returns and `Task`/`Task<TResult>` are handled. **`ValueTask`/`ValueTask<T>` is not
recognized as asynchronous** — it is treated as synchronous and the unfinished `ValueTask` itself
becomes the chain's result. Use `Task` for any action that must be intercepted.

For a *synchronous* action the proxy blocks on the chain (`GetAwaiter().GetResult()`). That is
safe only if every interceptor in that chain does purely synchronous work — one that genuinely
suspends risks the classic WPF sync-over-async deadlock.

### Removing an interceptor at runtime

```csharp
services.AddStoreInterceptor(interceptor, out var handle);
// later:
handle.Eject();
```

Idempotent. The slot stays but becomes a transparent pass-through; a call already in flight keeps
using it. There is no re-attach — register a new one.

---

## Plugins and registry

```csharp
services.AddSingleton<IStorePlugin, UserSessionPersistencePlugin>();
```

`Apply(StoreBase)` runs once per store created via `AddStore<TStore>()`, in registration order,
right after that store's own constructor. Use it for cross-cutting behavior (persistence,
telemetry) that should not be written per store.

`IStoreRegistry.Stores` lists every store **actually resolved so far** — a registered but never
injected store does not appear. It exists for introspection only; the container, not this
registry, is what makes a store a singleton. It bootstraps on the first `AddStore` call.

---

## Store vs ViewModel vs Provide/Inject

| Need | Use |
| --- | --- |
| State shared across unrelated ViewModels, app-lifetime | an Apsu store |
| State owned by one screen | the ViewModel itself (`[ObservableProperty]`, `Ref<T>`) |
| A value scoped to one visual subtree | Aquarius `Provide`/`Inject` |

An Apsu store composes cleanly with Aquarius: `Getter<T>` for cross-store derivation,
`Computed<T>` in the ViewModel for View-shaped derivation. See the sample's
"Apsu + Aquarius" tab.

**Nothing here persists itself.** Wire persistence through an `IStorePlugin` or the store's own
constructor.
