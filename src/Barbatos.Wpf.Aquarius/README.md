# Barbatos.Wpf.Aquarius

![Barbatos.Wpf.Aquarius logo](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/build/aquarius-logo.svg?raw=true)

### *Vue.js's reactivity, lifecycle hooks, directives, Teleport, and Transitions - for WPF*

**Bring Vue 3's Composition-API ergonomics (`ref`/`computed`/`watch`, lifecycle hooks,
`v-model`/`v-show`/`v-if`/`v-on`, custom directives, `<Teleport>`, `<Transition>`,
`provide`/`inject`, `<Suspense>`) to plain desktop WPF, built directly on top of
CommunityToolkit.Mvvm.**

[![NuGet](https://img.shields.io/nuget/v/Barbatos.Wpf.Aquarius.svg)](https://www.nuget.org/packages/Barbatos.Wpf.Aquarius)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Barbatos.Wpf.Aquarius.svg)](https://www.nuget.org/packages/Barbatos.Wpf.Aquarius)
[![GitHub stars](https://img.shields.io/github/stars/Barbatos-Labs/Barbatos.Wpf?style=social)](https://github.com/Barbatos-Labs/Barbatos.Wpf/stargazers)
[![License](https://img.shields.io/github/license/Barbatos-Labs/Barbatos.Wpf)](https://github.com/Barbatos-Labs/Barbatos.Wpf/tree/main/LICENSE.md)

---

## 📖 Documentation Menu

* **[Getting Started](#getting-started)**
  * [Introduction](#introduction)
  * [Quick Start](#quick-start)
* **[Reactivity](#reactivity)**
* **[Lifecycle Hooks](#lifecycle-hooks)**
* **[Setup](#setup)**
* **[Directives](#directives)**
  * [Directives.Model (v-model)](#directivesmodel-v-model)
  * [Directives.Show (v-show)](#directivesshow-v-show)
  * [If (v-if / v-else / v-else-if)](#if-v-if--v-else--v-else-if)
  * [Expr - conditional expressions](#expr---conditional-expressions)
  * [Directives.Event (v-on)](#directivesevent-v-on)
  * [Custom directives](#custom-directives)
  * [Directives.Class / Directives.Style (a lighter DataTrigger)](#directivesclass--directivesstyle-a-lighter-datatrigger)
  * [Comparisons](#comparisons)
  * [Build configuration (Debug-only content)](#build-configuration-debug-only-content)
  * [Already native: v-bind, v-for, v-html](#already-native-v-bind-v-for-v-html)
* **[Teleport](#teleport)**
* **[Dockable Panels](#dockable-panels)**
* **[Transition / TransitionGroup](#transition--transitiongroup)**
* **[Provide / Inject](#provide--inject)**
* **[Suspense](#suspense)**
* **[Slots](#slots)**
* **[Patterns that are already native](#patterns-that-are-already-native)**
  * ["KeepAlive"](#keepalive)
  * [Scoped slots for lists](#scoped-slots-for-lists)
* **[Ecosystem](#ecosystem)**
* **[API Reference](#api-reference)**
* **[Community](#community)**

---

## Getting Started

### Introduction

#### What is Barbatos.Wpf.Aquarius?

Aquarius ports Vue 3's Composition API - the parts of it that make Vue pleasant to write
in - onto plain WPF/XAML, using the same shape MAUI-style porting already established in
[Barbatos.Wpf.Core](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Core/README.md):
name things after their Vue counterpart, document the mapping and any honest divergence,
and never reinvent something WPF already does natively.

That last point matters: Aquarius is deliberately **not** a rewrite of WPF's binding
engine, layout system, or MVVM story. Vue's reactivity is already what
[CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)'s
`ObservableObject`/`[ObservableProperty]` gives you - `Ref<T>`/`Computed<T>` below are thin
sugar directly on top of it, not a competing system. Likewise `v-bind`, `v-for`, and
`v-html` aren't ported at all: `{Binding}`, `ItemsControl`+`DataTemplate`, and (nothing -
there's no safe WPF equivalent, and none is needed) already cover them - see
[Already native](#already-native-v-bind-v-for-v-html).

> **Prerequisites**
>
> The rest of this document assumes basic familiarity with C#, XAML, WPF's dependency
> property system, and - loosely - Vue 3's Composition API (so the "here's the Vue
> equivalent" framing throughout actually lands).

### Quick Start

Add the package via NuGet:

```powershell
dotnet add package Barbatos.Wpf.Aquarius
```

Everything in this document lives behind a single XAML namespace:

```xml
<Window ...
        xmlns:aq="http://schemas.barbatos.co/aquarius/2026/xaml">
```

That one `aq:` prefix reaches every namespace below (`Barbatos.Wpf.Aquarius.Reactivity`,
`Barbatos.Wpf.Aquarius.Composition`, `Barbatos.Wpf.Aquarius.Xaml`, `Barbatos.Wpf.Aquarius.Animation`) - the same way
WPF's own `.../presentation` xmlns quietly spans
`System.Windows`, `System.Windows.Controls`, etc.

Aquarius has no dependency on Barbatos.Wpf.Core and works in any WPF app - install it
alongside Core for the hosting/DI story, or on its own.

**Scaffolding a new View + ViewModel**: an installable `dotnet new` item template (see
[`templates/`](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/templates/README.md)
in the repo) generates a matching `XyzView.xaml` (with `aq:Setup.Enable="True"` already
set) / `XyzView.xaml.cs` / `XyzViewModel.cs` trio in one step - the closest Aquarius
equivalent of scaffolding a new Vue single-file component:

```powershell
dotnet new install ./templates/Barbatos.Wpf.Aquarius/item-templates/aquarius-view   # once
dotnet new aq-view -n Dashboard --namespace MyApp.Features.Dashboard
```

See the [root README](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/README.md#templates)
for the full template lineup (including a combined Aquarius + Core + i18n starter project) and
Visual Studio 2022 / Rider-specific steps.

---

## Reactivity

`Barbatos.Wpf.Aquarius.Reactivity` - `Ref<T>`, `Computed<T>`, `Watch`, `NextTick`.

```csharp
public Ref<int> Count { get; } = new(0);
public Computed<int> Doubled { get; }

public MainViewModel()
{
    Doubled = Computed<int>.From(() => Count.Value * 2, Count);

    Watch.On(Count, (newValue, oldValue) => Log($"{oldValue} -> {newValue}"));
}
```

```xml
<TextBlock Text="{Binding Count.Value}" />
<TextBlock Text="{Binding Doubled.Value}" />
```

- **`Ref<T>`** - one reactive value, mirroring `const count = ref(0)`. Built directly on
  `ObservableObject`, so it already works in bindings, `Watch`, and `Computed<T>` like any
  other observable. Read/write through `.Value` explicitly (deliberately no implicit
  conversion) - the same way Vue script code (outside a template) has to use `.value` too.
- **`Computed<T>`** - a derived value, mirroring `computed(() => ...)`. Because C# has no
  reactive-proxy interception, `Computed<T>.From(getter, ...dependencies)` takes its
  dependencies explicitly - the one deliberate divergence from Vue's own API. It's also
  **writable**, mirroring Vue's `computed({ get, set })` form:

  ```csharp
  public Computed<string> FullName { get; }

  FullName = Computed<string>.From(
      () => $"{FirstName} {LastName}",
      value => { var parts = value.Split(' ', 2); FirstName = parts[0]; LastName = parts[1]; },
      this); // the setter mutates a dependency; Value then updates the normal way
  ```

  Assigning to a `Computed<T>` created without a setter throws `InvalidOperationException`,
  mirroring Vue's runtime warning for the same mistake.
- **`Watch`** - `Watch.On(source, (newValue, oldValue) => ...)` mirrors `watch()`;
  `Watch.Effect(effect, ...dependencies)` mirrors `watchEffect()` (runs immediately, then
  again on any dependency change). Both return an `IDisposable` "stop" handle. Options,
  all mirroring Vue's own `watch` options:
  - `immediate: true` - also runs once immediately.
  - `once: true` - stops itself after the first triggered run.
  - `deep: true` (`Watch.On` only) - if the watched value also implements
    `INotifyCollectionChanged` (e.g. an `ObservableCollection<T>`), also reacts to
    Add/Remove/Reset, not just wholesale replacement of `.Value`. A narrower stand-in for
    Vue's recursive `deep` - there's no reactive-proxy equivalent for an arbitrary nested
    object graph - but it covers the single most common real "deep" need.
  - `flush: WatchFlush.Post` - coalesces rapid-fire changes through `NextTick` into one
    callback, mirroring Vue's default `flush: 'post'` batching (the default here,
    `WatchFlush.Sync`, is immediate/synchronous instead, since that was Aquarius's
    original behavior and changing the default would be a silent breaking change).
  - The callback can take a 3rd `onCleanup` parameter (mirrors Vue's `onWatcherCleanup`):
    calling it registers an action that runs right before the *next* invocation (or on
    `Dispose`) - the standard way to cancel stale async work before starting new work:

    ```csharp
    Watch.On<int>(userId, (id, _, onCleanup) =>
    {
        var cts = new CancellationTokenSource();
        onCleanup(cts.Cancel);
        _ = LoadUserAsync(id, cts.Token);
    });
    ```
- **`NextTick`** - `NextTick.Run(callback)` / `await NextTick.RunAsync()` schedule work via
  `Dispatcher.BeginInvoke(DispatcherPriority.Background, ...)`, mirroring Vue's
  microtask-based "after the DOM updates" queue. This is also the primitive
  [Lifecycle](#lifecycle-hooks)'s `IOnUpdated` batching and `Watch`'s `flush: Post` are
  built on.

`toRef`/`toRefs`/`unref`-style normalization helpers are intentionally not ported: Vue
needs them because a value can silently be a plain value, a ref, or a getter; a C#
`Ref<T>` is always a concrete, statically-typed `Ref<T>`, so there's no ambiguity to
normalize away.

---

## Lifecycle Hooks

`Barbatos.Wpf.Aquarius.Composition` - `Lifecycle.Enable` + eleven `IOnXxx` interfaces (plus eight
more `IOnXxxAsync` counterparts - see [below](#async-hooks)).

Vue's Composition API hooks run inside a component's `setup()`; the WPF/MVVM analogue of
`setup()` is the ViewModel. Each hook is its own tiny interface, so a ViewModel opts into
exactly the ones it needs - the same way a Vue component only imports the hooks it uses:

```xml
<UserControl aq:Lifecycle.Enable="True" ... />
```

```csharp
public sealed partial class MyViewModel : ObservableObject, IOnMounted, IOnUnmounted
{
    public void OnMounted() { /* ... */ }
    public void OnUnmounted() { /* ... */ }
}
```

No code-behind needed: the element's `DataContext` is checked against every hook
interface with a plain `is` pattern.

| Interface | Fires on... | Vue equivalent |
| --- | --- | --- |
| `IOnBeforeCreate` | Same first opportunity as `IOnBeforeMount` below, one step before it | `beforeCreate` |
| `IOnCreated` | Immediately after `IOnBeforeCreate` - nothing observable happens between them in this port | `created` |
| `IOnBeforeMount` | `Initialized` (or, if `DataContext` wasn't bound yet at that point - the common case when it's set by the parent - as a guaranteed fallback right before `IOnMounted`) | `onBeforeMount` |
| `IOnMounted` | `Loaded` | `onMounted` |
| `IOnBeforeUpdate` | The first `PropertyChanged` from `DataContext` in a new update batch (synchronous) | `onBeforeUpdate` |
| `IOnUpdated` | Once per batch, coalesced through `NextTick` | `onUpdated` |
| `IOnBeforeUnmount` | `Unloaded`, before `IOnUnmounted` | `onBeforeUnmount` |
| `IOnUnmounted` | `Unloaded` | `onUnmounted` |
| `IOnActivated` | Mount (only if actually visible at that point), `IsVisible` flipping back to `true` while still mounted, and (additionally) a `Window` regaining focus | `onActivated` (`<KeepAlive>`) |
| `IOnDeactivated` | Unmount, `IsVisible` flipping to `false` while still mounted, and (additionally) a `Window` losing focus | `onDeactivated` (`<KeepAlive>`) |
| `IOnErrorCaptured` | An unhandled exception reaches the dispatcher while mounted | `onErrorCaptured` |

Notes:

- A ViewModel's constructor has necessarily already run by the time it can be observed as
  a `DataContext` at all - there's no WPF equivalent of hooking in earlier than that - so
  `IOnBeforeCreate`/`IOnCreated` fire back to back, right before `IOnBeforeMount`, rather
  than at any earlier, truer "construction" moment.
- Every hook here follows the element's own mount/unmount cycle, not "once per ViewModel
  object": if the same ViewModel instance is remounted (e.g. behind an `<aq:If>` that
  toggles back), `IOnBeforeCreate`/`IOnCreated`/`IOnBeforeMount` all fire again too - none
  of them are "only the first time this object is ever seen."
- WPF raises one `Unloaded` event where Vue has two distinct moments -
  `IOnBeforeUnmount`/`IOnUnmounted` both fire from it, back to back.
- `IOnUpdated`'s batching is real, not just a name: several synchronous property changes
  in a row produce exactly one `IOnUpdated` call, the same way Vue batches multiple
  mutations into one asynchronous DOM update.
- `IOnErrorCaptured` follows WPF's own `Handled` convention rather than Vue's: return
  `true` to mark the exception handled (suppresses the app's unhandled-exception
  behavior), `false` to let it keep propagating - the inverse of Vue's `false` = stop
  propagating, chosen so it behaves the way every other "did you handle this" callback in
  WPF already does.
- `IOnActivated`/`IOnDeactivated` are what makes ["KeepAlive"](#keepalive) work below -
  see that section for why there's no separate `KeepAlive` control, and why it matters
  that mount only reports activated when the element is genuinely visible (content that
  mounts already-hidden, like a background TabControl tab, must not falsely claim to be
  activated - see that section for why).

`If` ([below](#if-v-if)) genuinely detaches/reattaches its content from the visual tree,
so a `Lifecycle.Enable`'d child behind an `<aq:If>` really does receive
`IOnUnmounted`/`IOnMounted` calls as the condition toggles - the same way Vue actually
destroys and recreates a `v-if` subtree.

### Async hooks

Every hook above except `IOnBeforeUpdate`/`IOnUpdated` has an `*Async` counterpart -
`IOnBeforeCreateAsync`, `IOnCreatedAsync`, `IOnBeforeMountAsync`, `IOnMountedAsync`,
`IOnBeforeUnmountAsync`, `IOnUnmountedAsync`, `IOnActivatedAsync`, `IOnDeactivatedAsync` -
returning `Task` instead of `void`, purely additive alongside the sync ones (implement
whichever fits; there's no reason to implement both for the same hook). The obvious use
case is loading data on mount:

```csharp
public sealed partial class DashboardViewModel : ObservableObject, IOnMountedAsync
{
    [ObservableProperty] private bool _isPending = true;
    [ObservableProperty] private DashboardData? _data;

    public async Task OnMountedAsync()
    {
        IsPending = true;
        try { Data = await _api.FetchDashboardAsync(); }
        finally { IsPending = false; }
    }
}
```
```xml
<aq:Suspense IsPending="{Binding IsPending}">
    <local:DashboardView />
    <aq:Suspense.Fallback>
        <TextBlock Text="Loading..." />
    </aq:Suspense.Fallback>
</aq:Suspense>
```

No changes to `Suspense` itself were needed for this - `OnMountedAsync` toggling a plain
`bool` property and `Suspense.IsPending` binding to it already compose on their own.

A few rules distinguish these from a naive `async void OnMounted()`, which is exactly what
this exists to avoid (an unobservable, uncatchable fire-and-forget with no way to route a
failure anywhere):

- **Fires at the same point as its sync counterpart, in the same call order - but is never
  awaited.** A slow `OnMountedAsync` does not delay `OnActivated`, a later remount, or
  anything else - Vue itself does not wait for an async lifecycle callback to resolve
  before continuing either.
- **Returns `Task`, not `ValueTask`.** Each hook fires at most once per mount/unmount/etc.,
  never in a tight loop, so there's no allocation worth saving - `ValueTask` would only add
  sharp edges (can't be awaited twice, can't be inspected once it's already been awaited)
  for no benefit here.
- **A fault is never silently dropped.** `Lifecycle` observes the returned `Task` and, if
  it faults, rethrows the exception onto the element's own `Dispatcher` - the same route an
  ordinary unhandled exception already takes to reach `IOnErrorCaptured` above. There is no
  separate async error hook; a failure from any `*Async` hook surfaces through that same,
  already-existing mechanism.
- **No async counterpart for `IOnBeforeUpdate`/`IOnUpdated`** (tied to synchronous,
  single-batch `NextTick` coalescing - an async hook firing partway through wouldn't
  compose with that) **or `IOnErrorCaptured`** (its `bool` return has to decide `Handled`
  synchronously, before the dispatcher's own exception handling moves on - there's no
  "await, then decide" version of that contract).

---

## Setup

`Barbatos.Wpf.Aquarius.Composition.Setup` - resolves and assigns a View's `DataContext` from a
Type, so a View never needs a code-behind constructor line like `DataContext = new
XyzViewModel(...)`. The closest Aquarius counterpart to how a Vue single-file component's
own `<script>`/`<script setup>` block is simply *there*, with no separate wiring step to
remember.

Two ways to opt in, both set on the View's own XAML root:

```xml
<!-- 1. Say the exact type - always wins, works for any naming/assembly layout -->
<UserControl aq:Setup.ViewModel="{x:Type vm:SomeViewModel}" ... />

<!-- 2. Just say "figure it out" - guesses "XyzView" -> "XyzViewModel" by name -->
<UserControl aq:Setup.Enable="True" ... />
```

- **The naming convention** strips a trailing `"View"` from the View's own type name and
  appends `"ViewModel"` - `ReactivityDemoView` to `ReactivityDemoViewModel`, matching this
  library's own sample app throughout. It checks the View's own assembly first, then falls
  back to scanning every currently-loaded assembly for a same-named type, so a ViewModel
  living in a separate assembly from its View is found too (as long as that assembly is
  already loaded by the time this runs - reference the type once anywhere, or use the
  explicit `ViewModel` override, to guarantee it). Results are cached per View type, not
  recomputed per instance. Replace `Setup.Resolver` (a `Func<Type, Type?>`) to use a
  different convention app-wide - a different suffix, a `Views`/`ViewModels` namespace
  swap, whatever this app's own layout calls for.
- **Where the instance comes from**: `Setup.ServiceProvider` first if set - so
  constructor-injected dependencies resolve the same way any other DI-registered service
  would - falling back to `Activator.CreateInstance(Type)`, which requires a public
  parameterless constructor:

  ```csharp
  // Once at startup - works with Barbatos.Wpf.Core's WpfAppBuilder.Services, or any other
  // Microsoft.Extensions.DependencyInjection IServiceProvider:
  Setup.ServiceProvider = host.Services;
  ```

  A ViewModel with required constructor arguments and no DI configured needs either a
  registration, a parameterless constructor, or to skip this feature for that View and keep
  setting `DataContext` by hand.
- **Explicit wins over convention** when both `ViewModel` and `Enable` are set on the same
  element.
- **`Setup.ThrowOnUnresolved`** (default `false`) - when `Enable="True"` but the naming
  convention can't find a match, the default is to silently leave `DataContext` alone, so
  this feature can be adopted incrementally without an existing View that doesn't yet
  follow the convention suddenly failing at runtime. Set this to `true` during app startup
  to fail fast with a clear exception instead - the same opt-into-strict shape as
  [`Expr.ThrowOnUnresolvedIdentifiers`](#expr---conditional-expressions).
- **Resolves at `Initialized`, not `Loaded`** - specifically so [Lifecycle](#lifecycle-hooks)
  hooks (which check at `Initialized` as a best effort, then guaranteed at `Loaded`) always
  see the already-resolved `DataContext` in time, no matter which attached property XAML
  happens to apply first.

---

## Directives

`Barbatos.Wpf.Aquarius.Xaml` - attached properties and small controls for the things WPF doesn't
already do natively.

### Directives.Model (v-model)

```xml
<TextBox aq:Directives.Model="{Binding Name}" />
<!-- instead of: -->
<TextBox Text="{Binding Name, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
```

Must be set with a `Binding` (a plain value throws). Reads the binding back off the
attached property and re-applies it, as `TwoWay`/`UpdateSourceTrigger=PropertyChanged`,
to the right real property for the element type: `TextBox.Text`,
`ToggleButton.IsChecked` (`CheckBox`/`RadioButton`), `Selector.SelectedItem`
(`ComboBox`/`ListBox`), `RangeBase.Value` (`Slider`). `PasswordBox` throws - `Password`
is deliberately not a `DependencyProperty` for security reasons, the same limitation
Vue's own `v-model` has for `<input type="password">`. Any other element type throws too,
matching Vue's dev-time warning for an invalid `v-model` target rather than silently
doing nothing.

### Directives.Show (v-show)

```xml
<Border aq:Directives.Show="{Binding IsOpen}">...</Border>
```

`false` maps to `Visibility.Collapsed` (no layout space, state preserved) - matching the
`display: none` semantics `v-show` relies on. The element stays mounted the whole time.

### If (v-if / v-else / v-else-if)

```xml
<aq:If Condition="{Binding IsOpen}">
    <TextBlock Text="Only in the tree while IsOpen is true" />
</aq:If>
```

Unlike `Directives.Show`, `Condition` going `false` genuinely detaches the content from
the visual tree rather than just hiding it - the same "destroy" Vue performs for `v-if`.
The child is kept in `If.Child` (the XAML content property) so it reattaches unchanged
once `Condition` is `true` again. See the [Lifecycle](#lifecycle-hooks) note above for
what this means for a `Lifecycle.Enable`'d child.

**`v-else`** is `If.Else` - a second content-bearing property, shown while `Condition` is
`false` (defaults to `null`, so every `<aq:If>` that doesn't set it behaves exactly as
before):

```xml
<aq:If Condition="{Binding IsLoggedIn}">
    <TextBlock Text="Welcome back" />
    <aq:If.Else>
        <TextBlock Text="Please sign in" />
    </aq:If.Else>
</aq:If>
```

**`v-else-if`** has no separate control - nest another `<aq:If>` inside the outer one's
`<aq:If.Else>`:

```xml
<aq:If Condition="{aq:Expr 'Type == &quot;A&quot;'}">
    A content
    <aq:If.Else>
        <aq:If Condition="{aq:Expr 'Type == &quot;B&quot;'}">
            B content
            <aq:If.Else>Fallback content</aq:If.Else>
        </aq:If>
    </aq:If.Else>
</aq:If>
```

This is correct "for free": each nested `If` keeps its own content current independent of
whether its branch is currently attached, and jumping straight from branch A to the final
fallback (skipping B) never touches B's content or mounts B's `DataContext` at all - only
the outer `If`'s own `Condition` changing ever attaches/detaches the nested one. It does
read visibly worse than Vue's flat sibling `v-else-if` past 3-4 branches, since WPF's
content model has no equivalent to "grouped flat siblings" - a `Switch`/`Case` control
would be the natural escape hatch if that becomes painful, deliberately not built here
since it wasn't asked for.

#### Performance: `If` vs `Directives.Show`

Same tradeoff Vue itself documents for `v-if` vs `v-show`: `If` has near-zero cost while its
`Condition` stays put, but a real cost each time it actually toggles (a genuine detach +
reattach); `Directives.Show` is the opposite (always mounted, so a toggle is just a
`Visibility` flip). Two things worth knowing before picking one:

- **Many synchronous `Condition` flips that happen before WPF's dispatcher next runs layout
  cost nothing extra.** WPF's own `Loaded`/`Unloaded` are deferred, not synchronous with the
  `Content` assignment - only the net transition by the time layout actually runs fires
  them, however many times `Condition` flipped on the way there. Verified directly: 100
  back-to-back flips in the same callstack, pumped once, produced exactly one mount and one
  unmount - not 100 (`IfControlTests.ManySynchronousConditionFlipsBeforeAPumpCoalesceIntoAtMostOneMountUnmountPair`).
  So a property that recomputes `Condition` several times in a row within the same
  operation is not, by itself, a performance concern.
- **That coalescing is the exception, not the rule - a periodic/"realtime" source gets none
  of it.** It only happens because those flips share one callstack; a `DispatcherTimer` tick,
  a message from a live feed, a sensor reading - anything arriving as its own separate
  dispatcher operation - pays the full cost **every single time**, because
  `DispatcherTimer`'s default priority (`Background`) is lower than the `Loaded`-priority
  connectivity work a `Condition` change queues, so that work always drains before the next
  tick can even fire; there's no window for two ticks to land in the same pass the way 100
  synchronous flips do. Verified directly with a real `DispatcherTimer`: 5 ticks/second
  produced 5 full, uncoalesced mount-or-unmount sequences, not one net transition
  (`IfControlTests.TimerDrivenTogglesFarApartEachProduceTheirOwnFullMountUnmountCycle`). Each
  full cycle is a genuine `Unloaded` then `Loaded`, so every [Lifecycle](#lifecycle-hooks)
  hook the child implements re-runs from scratch every time (measured at roughly 0.7ms per
  cycle for a trivial child with no-op hooks - a visually heavier subtree, or hooks doing
  real work like loading data or subscribing to something, cost more per toggle, not less).
  **This is exactly what "realtime" means here**: if `Condition` is driven by a value that
  updates several times a second - a live feed, a timer, anything with that shape - switch
  that element to `Directives.Show` instead. It only ever flips `Visibility`, so the hooks
  simply don't re-fire at all (see the [`"KeepAlive"`](#keepalive) note above).

In short: `If` for content that changes rarely (a tab's content, a logged-in/logged-out
split, ...), `Directives.Show` for content that toggles often - and "often" specifically
includes anything realtime/periodic, even at a modest few-times-a-second rate, since that
shape never benefits from the same-callstack coalescing above.

### Expr - conditional expressions

```xml
<aq:If Condition="{aq:Expr 'Count > 0'}">
<Border aq:Directives.Show="{aq:Expr 'Status == &quot;Active&quot;'}">
```

A plain WPF `Binding` path has no expression language - `Comparisons` above covers the
simplest single-value cases, but there was no way to write something like `a + b >= c`
directly in XAML at all. `Expr` parses and reactively evaluates a small expression
grammar over bound properties (identifiers resolve as ordinary property paths against the
ambient `DataContext`, dotted paths like `Order.Total` work same as a plain `{Binding}`):

| Category | Operators / forms |
| --- | --- |
| Comparison | `> >= < <= == !=` |
| Logical (short-circuiting) | `&& \|\| !` |
| Arithmetic | `+ - * /` and parentheses, all evaluated as `double` |
| Ternary, right-associative | `condition ? whenTrue : whenFalse` (only the taken branch runs) |
| Literals | numbers (`1`, `2.5`), strings (`"Hello World"`), lowercase `true`/`false`/`null` |

**Object types, not just primitives**: `==`/`!=` work over any object type - once the
numeric-coercion and enum cases below don't apply, two operands compare via
`object.Equals(object, object)` (reference equality unless the type overrides it), so
`SomeOrder == OtherOrder` and null-checks like `SelectedOrder != null` both just work.
`> >= < <=` still only support numbers or two same-concrete-type `IComparable` values -
there's no general ordering for arbitrary objects.

```xml
<Border aq:Directives.Show="{aq:Expr 'SelectedOrder != null'}">
```

**Enum comparison** goes through the string form rather than a bare `EnumType.Member`
literal: `Status == "Active"` compares an enum-typed `Status` against the member name via
`ToString()` (works in either operand order). A bare enum literal would need to
distinguish a type name from an ordinary dotted property path at parse time, for no real
benefit over the string form - deliberately not supported.

**Element-referenced identifiers**: prefix an identifier with `#` to resolve it against a
named element instead of `DataContext`:

```xml
<Slider x:Name="MySlider" Minimum="0" Maximum="100" />
<Border aq:Directives.Show="{aq:Expr '#MySlider.Value > 50'}">
```

`#MySlider.Value` binds via `Binding.ElementName` the same way
`{Binding ElementName=MySlider, Path=Value}` would; a bare `#MySlider` (no dot) binds to
the element itself. Only `ElementName` works this way - `RelativeSource` doesn't: an
`AncestorType` reference needs to resolve a type name through XAML's own type-resolution
service, which would need a materially bigger identifier grammar for a need this rarely
comes up for. `Expr.Evaluate(string, object?)` (below) cannot resolve `#` identifiers at
all - there's no visual tree to search outside a real XAML load - and throws clearly if one
appears.

**Deliberately out of scope**: string concatenation via `+` (both consumers this was built
for - `If.Condition`/`Directives.Show` - are booleans; use `StringFormat` or multiple
`Run`s to build display text instead), `RelativeSource` identifiers (see above), method
calls, and indexers. For anything beyond this grammar, a `Computed<T>` in the ViewModel
remains the right answer, same as Vue's own guidance to move non-trivial template
expressions into a computed property.

**Typos are the real risk of a grammar living inside a XAML string**: no XAML editor can
syntax-highlight, IntelliSense, or rename-refactor an identifier that only this parser
understands, so renaming a ViewModel property silently stops an `Expr` string from
matching it - no compiler error, nothing red-squiggled. By default an unresolved
identifier fails exactly the way a plain `{Binding TypoPath}` already does (evaluates as
`DependencyProperty.UnsetValue`, "fails open" - `If`/`Directives.Show` both default to
showing content on an unresolved binding). Set `Expr.ThrowOnUnresolvedIdentifiers = true`
once during app startup (e.g. wrapped in `#if DEBUG`) to turn that into an immediate,
specific exception instead - naming exactly which identifier didn't resolve. There is no
way to get real IDE syntax highlighting/IntelliSense for the expression text itself
without a custom XAML language-service extension, which is a much bigger, IDE-specific
undertaking outside this library's scope.

**XAML quoting**: since the whole markup extension already sits inside a double-quoted XML
attribute, a string literal inside the expression needs its quotes written as `&quot;` (an
XML entity, decoded before the expression text ever reaches the parser) rather than an
escaped `\"` - XML attribute values have no backslash-escaping mechanism at all, so a
literal `\"` would not protect the attribute boundary and would produce invalid XML.
`Expr`'s own string-literal grammar still supports `\"`/`\\` for a literal quote/backslash
*inside* the compared value itself - relevant when calling `Expr.Evaluate(string, object?)`
directly from C# (a synchronous, non-reactive counterpart to the markup extension, mirroring
`Inject.Get<T>`), where no XML layer is involved.

### Directives.Event (v-on)

```xml
<Border aq:Directives.Event="MouseLeftButtonDown"
        aq:Directives.Command="{Binding BorderClickedCommand}" />
```

Wires up **any** public .NET event by name via reflection - it works for events WPF gives
no built-in `Command` for (`Border.MouseLeftButtonDown`, `TextBox.TextChanged`, ...), the
same way `v-on` isn't limited to a fixed event list in Vue. The command runs with
`Directives.CommandParameter` if set, otherwise the raised `EventArgs` itself.
Automatically unhooked on `Unloaded`.

**Modifiers** (`Directives.Modifiers`, comma-separated), a curated subset of Vue's real
`v-on` modifier list, each a no-op unless the raised `EventArgs` is actually the matching
type:

| Modifier | Effect |
| --- | --- |
| `stop`, `prevent` | Sets `RoutedEventArgs.Handled = true`. WPF collapses the DOM's separate stopPropagation/preventDefault into one flag, so these two are honestly the same operation here. |
| `once` | Unhooks after this single invocation. |
| `self` | Only invokes if `OriginalSource` is the element itself, not a descendant the event bubbled up from. |
| `left` / `right` / `middle` | Only invokes for that `MouseButtonEventArgs.ChangedButton`. |
| `enter` / `tab` / `esc` / `space` / `up` / `down` / `left` / `right` / `delete` | Only invokes for that `KeyEventArgs.Key` (`delete` matches both Delete and Backspace, matching Vue's own alias; `left`/`right` mean arrow keys here, resolved by `KeyEventArgs` vs. `MouseButtonEventArgs` - exactly how Vue itself reuses these same names for both mouse and keyboard). |

```xml
<TextBox aq:Directives.Event="KeyDown"
         aq:Directives.Command="{Binding SubmitCommand}"
         aq:Directives.Modifiers="enter" />
```

`capture` is intentionally not a modifier: WPF already has a more idiomatic native
equivalent, the `Preview{EventName}` tunneling event -
`Directives.Event="PreviewMouseDown"` *is* the capture-phase port. `passive` is skipped
as not applicable (a browser scroll-performance concept with no WPF equivalent).

### Custom directives

```csharp
public sealed class FocusDirective : Directive
{
    public override void Mounted(FrameworkElement element, DirectiveBinding binding) => element.Focus();
}
```

```xml
<Window.Resources>
    <local:FocusDirective x:Key="AutoFocus" />
</Window.Resources>

<TextBox aq:Directives.Use="{StaticResource AutoFocus}" />
```

The Aquarius counterpart of a Vue 3 custom directive object
(`{ mounted(el, binding) { ... } }`) - this is literally the canonical Vue docs example,
`v-focus`. `Directive` exposes `Mounted`/`Updated`/`Unmounted`, each receiving a
`DirectiveBinding` with `Value`/`OldValue` (from `Directives.UseValue`, if bound),
`Argument` (from `Directives.Argument`), and `Modifiers` (from `Directives.Modifiers`,
shared with [Directives.Event](#directivesevent-v-on) above) - mirroring the `binding`
object every Vue custom directive hook receives. Vue's
`v-my-directive:arg.mod1.mod2="value"` has no XAML equivalent syntax, so each piece is
its own sibling attached property instead of special syntax. v1 supports one directive
per element through `Directives.Use`; composing several is possible by writing a
directive that itself dispatches to others.

### Directives.Class / Directives.Style (a lighter DataTrigger)

`If`/`Directives.Show` are deliberately minimal (structural mount/unmount or visibility
only) - reaching for a full `DataTrigger` just to toggle a handful of properties based on
data is exactly what Vue's `:class`/`:style` **object binding** exists to avoid:

```xml
<Border aq:Directives.Class="{Binding ActiveClasses}">...</Border>
<!-- ActiveClasses might be "active bold" or "" -->
```

A space-separated list of "class names," each looked up as a `Style` resource key (via
`FindResource`, so classes can live in `Window.Resources`/`App.Resources`). Every active
token's setters (including any inherited through `Style.BasedOn`) are applied directly via
`SetValue` - not by assigning `FrameworkElement.Style`, since WPF only allows one `Style`
at a time but several simultaneously-active "classes" need to layer the way CSS classes
do. Later tokens win on conflicting properties; properties from a token that's no longer
active are reverted before the new set is applied.

```xml
<Border aq:Directives.Style="{Binding InlineStyles}">...</Border>
<!-- InlineStyles: new Dictionary<string, object> { ["Background"] = Brushes.Red } -->
```

The object form of `:style` - each key is resolved to a `DependencyProperty` by name
against the element's own type (an unresolvable name throws) and set directly.

### Comparisons

```xml
<aq:If Condition="{Binding Items.Count, Converter={x:Static aq:Comparisons.IsNull}}">
```

Vue templates can write a boolean expression inline (`v-if="count > 0"`); WPF bindings
are plain property paths with no expression language. Vue's own guidance is to move
anything beyond a trivial check into a computed property anyway, so requiring a bound
property/converter here isn't a step down from Vue's best practice - it's the same one.
`Comparisons.Not`, `.IsNull`, `.IsEqualTo` (reads `ConverterParameter` as the comparand)
just remove the ceremony of hand-writing a whole `IValueConverter` class for the three
most common trivial cases; for anything less trivial, a `Computed<T>` in the ViewModel is
still the right answer, same as in Vue.

### Build configuration (Debug-only content)

```xml
<Button Content="Reset local cache (debug only)"
        aq:Directives.Show="{x:Static aq:BuildConfiguration.IsDebug}" />
```

XAML has no preprocessor - `#if DEBUG`/`#endif` are a C#-compiler construct that only applies
to `.cs` files (including code-behind), never to `.xaml` markup itself, since XAML is parsed by
the markup compiler instead. The traditional WPF answer is to declare the element in XAML and
toggle it from code-behind wrapped in `#if DEBUG`; `BuildConfiguration.IsDebug` lets the same
check happen directly in markup instead, composed with `Directives.Show`/`If`/`Suspense` like
any other boolean - the XAML-first counterpart of Vue/Vite's `import.meta.env.DEV`.

It deliberately does not read Barbatos.Wpf.Aquarius's own build configuration - this library
can ship as a Release NuGet package while the application consuming it is still built Debug, or
vice versa. Instead it inspects the entry assembly's `DebuggableAttribute`, which the C#
compiler stamps according to the *consuming application's own* configuration (Debug disables
JIT optimizations, Release does not) - a different, stronger signal than `Debugger.IsAttached`,
which only reports whether a debugger happens to be attached right now (a Debug build launched
by double-clicking its `.exe` still counts as Debug here, with no debugger attached at all).
`BuildConfiguration.IsAssemblyDebugBuild(Assembly?)` is also exposed directly, for checking a
specific assembly other than the entry one.

### Already native: v-bind, v-for, v-html

Not ported - each already has a direct, better-established WPF equivalent:

- **`v-bind`** → `{Binding}` (already exists).
- **`v-for`** → `ItemsControl` + `DataTemplate` (already exists - see also
  [Slots](#slots) below for the "scoped slot" angle on item templates).
- **`v-html`** → no safe equivalent, and none is needed - WPF has no raw-HTML-injection
  surface for this to apply to in the first place.

---

## Teleport

`Barbatos.Wpf.Aquarius.Xaml` - `TeleportHost` + `Teleport`.

```xml
<Grid aq:TeleportHost.RegisterHost="Overlay" Panel.ZIndex="100" />
<!-- ...elsewhere, possibly deeply nested... -->
<aq:Teleport To="Overlay" Disabled="{Binding IsCompact}">
    <views:ToastView />
</aq:Teleport>
```

The Aquarius counterpart of Vue's `<Teleport to="#target">`. While mounted and enabled,
`Teleport.Content` is detached from it and added to the `Panel.Children` of whichever
`Panel` registered itself via `TeleportHost.RegisterHost` under `To` - commonly an
overlay `Grid` pinned at a window's root, so content declared deep inside some nested
layout (and subject to its clipping/z-order) can still render pinned above everything
else. `Disabled` (mirroring Vue's `:disabled`) keeps the content local instead. Because
the moved element keeps its object identity, its own bindings/`DataContext`/
`Lifecycle.Enable` hooks keep working after the move - it's still "the same component,"
just rendered elsewhere. A `Teleport` that mounts before its target host does still finds
it once the host registers, instead of silently never teleporting.

Like a DOM `id`, host names are expected to be unique app-wide; if two hosts register the
same name, the most recently loaded one wins.

---

## Dockable Panels

This is **not a new API** - it's `Teleport`/`TeleportHost` from the section above, used
across two `Window`s instead of one. The whole thing is also a working demo in
`samples/Barbatos.Wpf.Aquarius.Sample` (section "9." in `MainWindow.xaml`) - what follows
is that same example, trimmed down.

**The idea:** one `<aq:Teleport>`, declared once in the main window, is retargeted between
two registered hosts - a "docked" one in the main window, and a "floating" one in a
separate dialog `Window`. Because `Teleport` re-parents rather than recreates, the panel's
state (whatever the user typed, scrolled, etc.) survives every move.

**1. The docked host and the persistent `Teleport`, both in the main window's XAML:**

```xml
<!-- The "home slot" - visibly empty while undocked: -->
<Border BorderBrush="Gray" BorderThickness="1" MinHeight="70">
    <Grid aq:TeleportHost.RegisterHost="MainDock" />
</Border>

<!-- Declared once, here, and never inside the dialog - see the note below on why. -->
<aq:Teleport To="{Binding DockTarget.Value}">
    <Border Background="#E0F0FF" Padding="8">
        <TextBox aq:Directives.Model="{Binding DockableNotes}" />
    </Border>
</aq:Teleport>

<Button Content="Undock" Command="{Binding UndockCommand}" />
<Button Content="Redock" Command="{Binding RedockCommand}" />
```

**2. The floating dialog's XAML is just a host - it has no idea what ends up inside it:**

```xml
<Window x:Class="...DockableToolDialog" ...>
    <Grid aq:TeleportHost.RegisterHost="FloatingDock" />
</Window>
```

**3. The ViewModel only ever expresses *intent* - it never touches a `Window` directly:**

```csharp
public Ref<string> DockTarget { get; } = new("MainDock");

[ObservableProperty]
private string _dockableNotes = "...";

[RelayCommand]
private void Undock() => DockTarget.Value = "FloatingDock";

[RelayCommand]
private void Redock() => DockTarget.Value = "MainDock";
```

**4. Opening/closing the actual dialog `Window` is a View concern**, so it belongs in
`MainWindow`'s code-behind, reacting to that same `DockTarget` with `Watch.On`:

```csharp
private DockableToolDialog? _dockableDialog;

public MainWindow()
{
    InitializeComponent();
    var viewModel = new MainViewModel();
    DataContext = viewModel;

    Watch.On(viewModel.DockTarget, (target, _) =>
    {
        if (target == "FloatingDock")
        {
            if (_dockableDialog is not null) return;

            _dockableDialog = new DockableToolDialog { Owner = this };
            _dockableDialog.Closed += (_, _) =>
            {
                _dockableDialog = null;
                // Closed via its own [X], not via Redock - HostUnregistered already
                // brought the panel home; this just keeps DockTarget consistent.
                viewModel.DockTarget.Value = "MainDock";
            };
            _dockableDialog.Show();
        }
        else
        {
            _dockableDialog?.Close();
        }
    });
}
```

Three things matter for this to work correctly - all visible in the wiring above:

- **The `<aq:Teleport>` element itself must live somewhere that outlives both states** -
  the main window, never inside the dialog. Content lives and dies with the `Teleport`
  control that wraps it; declaring that control inside the dialog instead would tear the
  content down along with it rather than "returning" it anywhere.
- **The dialog `Window` needs its own `DataContext` set explicitly - this is the one that
  actually bites.** `Teleport` really does move the exact same element (same `TextBox`
  instance, nothing recreated) - but `DataContext` is *inherited from the visual tree*,
  not carried along with the moved content. The moment that content's new ancestor chain
  roots at a different `Window`, any binding on it (like `Directives.Model` above) now
  resolves against *that* `Window`'s `DataContext` - which is `null` if never set, so the
  binding silently goes blank even though nothing about `Teleport` itself is broken. This
  is why the code-behind above passes `DataContext = viewModel` when constructing
  `DockableToolDialog`. Forgetting that one line is a very easy, very silent mistake -
  `Barbatos.Wpf.Aquarius.UnitTests`' `TeleportTests` has a test for each side of exactly
  this (`ContentBoundViaInheritedDataContextKeepsWorkingAcrossWindowsWhenBothSetIt` /
  `...LosesItsValueIfTheNewWindowDoesNotSetDataContext`) if you want to see it proven
  directly.
- **Closing the floating dialog with its own `[X]` - without clicking Redock first -**
  still brings the panel home automatically. That's `TeleportHost.HostUnregistered`: when
  the dialog's host unregisters, `Teleport` notices it just lost the host it was actually
  rendered into and restores the content locally instead of losing it. `To` itself is left
  unchanged by that safety net, so if a host under the same name registers again later
  (the dialog reopens), the content re-floats there automatically - same as any host that
  was simply slow to load in the first place. The `Closed` handler above resets
  `DockTarget` too, purely so a *second* click on Undock creates a fresh dialog correctly.

---

## Transition / TransitionGroup

`Barbatos.Wpf.Aquarius.Animation` - `Transition`, `TransitionGroup`.

Vue ships zero default animations - `<Transition>` only orchestrates *when* your own CSS
classes/JS hooks run. This port keeps that philosophy: no canned animations ship here,
callers supply their own `Storyboard` resources.

```xml
<aq:Transition Show="{Binding IsOpen}">
    <aq:Transition.Enter>
        <Storyboard><DoubleAnimation Storyboard.TargetProperty="Opacity" From="0" To="1" Duration="0:0:0.2" /></Storyboard>
    </aq:Transition.Enter>
    <aq:Transition.Leave>
        <Storyboard><DoubleAnimation Storyboard.TargetProperty="Opacity" From="1" To="0" Duration="0:0:0.2" /></Storyboard>
    </aq:Transition.Leave>
    <TextBlock Text="Now you see me" />
</aq:Transition>
```

Merges what `If` does (structural mount/unmount) with animation timing: `Show` going
`false` plays `Leave` first (if set) and only detaches the content once it completes;
`Show` going `true` restores the content immediately (so `Loaded` fires, same
[Lifecycle](#lifecycle-hooks) synergy `If` documents) and then plays `Enter`. A
re-toggle mid-`Leave` stops the in-flight animation cleanly rather than letting two
animations fight. Vue does not play the enter animation on a component's very first
render unless the `appear` prop is set; this mirrors that - the initial content is
displayed directly, `Enter` only plays on a later `false`→`true` toggle. Storyboards are
cloned before each play, so sharing one `Enter`/`Leave` resource across multiple
`Transition`s (the normal case) doesn't cross-trigger their `Completed` handlers.

```xml
<ListBox ItemsSource="{Binding Items}" aq:TransitionGroup.Enter="{StaticResource FadeIn}" />
```

`TransitionGroup` ports the **enter** side only: every newly-generated item container in
an `ItemsControl` gets a one-time `Loaded` hook that plays `Enter`. Vue's real
`<TransitionGroup>` also animates items *leaving* the list and, more remarkably, computes
each surviving item's position delta and animates the reflow (FLIP) when the list
reorders - both require intercepting removal/reflow *before* the panel applies it, which
for a plain `ItemsControl`/`Panel` means owning a custom panel, a materially bigger and
riskier problem than enter (virtualization/container-recycling interactions especially).
Left for a dedicated follow-up rather than rushed here.

> **Note:** a *virtualizing* panel (the default for `ListBox`/`ListView`) can satisfy a
> newly added item by reusing a recycled container instead of creating a new one, and a
> reused container doesn't re-fire `Loaded`. Set `VirtualizingPanel.IsVirtualizing="False"`
> on lists small enough for that to be affordable if the enter animation should be
> guaranteed for every new item.

---

## Provide / Inject

`Barbatos.Wpf.Aquarius.Composition` - `Provide`, `Inject`.

```xml
<Grid aq:Provide.Key="ThemeColor" aq:Provide.Value="{Binding AccentBrush}">
    <!-- ...<TextBlock Foreground="{aq:Inject ThemeColor}" /> anywhere inside... -->
</Grid>
```

Vue's `provide`/`inject` run in component `setup()` (i.e. in Aquarius, the ViewModel) and
walk the *component* tree; ViewModels intentionally don't hold a reference to their View,
so a literal port isn't honest here. This leans on WPF's own closest analog instead:
`FrameworkElement.Resources` plus `FindResource`, which already walks up the logical tree
and merges resource dictionaries at each level - `Provide.Value` is just stored into
`Resources` under `Provide.Key`, so it composes with WPF's existing lookup/
override-by-nesting semantics for free, and a nearer `Provide` for the same key correctly
overrides a farther one.

`{aq:Inject Key}` (or the positional shorthand `{aq:Inject}`) is the primary way to
consume a provided value *in XAML* - it delegates internally to
`DynamicResourceExtension`, reusing WPF's own deferred, re-evaluate-on-invalidation
resolution (so a later `Provide.Value` change is picked up live) rather than eagerly
walking the tree itself. For the rare case of needing an injected value from C# instead
(e.g. a View handing it to its own `DataContext`), use
`Inject.Get<T>(element, key, fallback)`.

Like a DOM `id`, a string key can collide with an unrelated resource sharing the same
name. For collision-proofing - Vue's own recommendation is a `Symbol` - use a dedicated
`object` sentinel instead of a string:

```csharp
public static class Keys { public static readonly object ThemeColor = new(); }
```

```xml
<Grid aq:Provide.Key="{x:Static local:Keys.ThemeColor}" ... />
```

Reactivity crosses the boundary for free: if the provided value is a `Ref<T>` or an
`ObservableObject`, nothing extra is needed - bindings against it (or code reading
`.Value`) already see live updates after injection, the same guarantee Vue gives an
injected ref.

`Provide`/`Inject` is for values scoped to a *visual subtree* (e.g. "everything inside
this Menu shares a MenuContext") - a ViewModel that needs a value for its whole lifetime
should still prefer constructor injection through
[Barbatos.Wpf.Core](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Core/README.md)'s
DI container, a different scoping model.

---

## Suspense

`Barbatos.Wpf.Aquarius.Xaml` - `Suspense`.

```xml
<aq:Suspense IsPending="{Binding IsLoading}">
    <local:DashboardView />
    <aq:Suspense.Fallback>
        <TextBlock Text="Loading..." />
    </aq:Suspense.Fallback>
</aq:Suspense>
```

Vue's real `<Suspense>` auto-detects async `setup()`/async components anywhere down the
tree it wraps, aggregating them into one pending/resolved state. C# has no equivalent
hook to detect "this ViewModel is mid-load" automatically, so this is a deliberately
narrower, explicit port: the ViewModel already knows when it's loading (an
`IsLoading`/`Ref<bool>` property it sets around an async call) - `IsPending` just wires
that straight to which content shows. No nested-Suspense boundary handling, no automatic
dependency aggregation, no `pending`/`resolve`/`fallback` events - one explicit boolean
in, one of two contents out.

---

## Slots

`Barbatos.Wpf.Aquarius.Xaml` - `Slot`, `SlotHost`, `SlotContent`, `SlotProvided`.

A real port of Vue's `<slot>`/`<slot name="x">` outlets: free-form slot names chosen at the
use site, not pre-declared as a `DependencyProperty` per name by the component author.
(WPF's `ContentControl.Content`/`ItemsControl.ItemTemplate` already cover Vue's default
slot and list-repeated scoped slots respectively - see
[Patterns that are already native](#patterns-that-are-already-native) below for those; this
section is specifically for the free-form *named* slots case, which had no native WPF
equivalent at all.)

A component author derives from `SlotHost` and writes their own `ControlTemplate`:

```xml
<Style TargetType="{x:Type local:Card}">
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="{x:Type local:Card}">
                <Border BorderBrush="Gray" BorderThickness="1" CornerRadius="4">
                    <StackPanel>
                        <aq:If Condition="{aq:SlotProvided header}">
                            <Border Background="#F0F0F0" Padding="8">
                                <ContentPresenter Content="{aq:SlotContent header}" />
                            </Border>
                        </aq:If>
                        <aq:If Condition="{aq:SlotProvided}">
                            <Border Padding="8">
                                <ContentPresenter Content="{aq:SlotContent}" />
                            </Border>
                        </aq:If>
                        <aq:If Condition="{aq:SlotProvided footer}">
                            <Border Background="#F0F0F0" Padding="8">
                                <ContentPresenter Content="{aq:SlotContent footer}" />
                            </Border>
                        </aq:If>
                    </StackPanel>
                </Border>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

Consumers write a flat mix of `Slot`-wrapped (named) and plain (implicit default) children -
Vue's own docs' 3-slot Card example, translated 1:1:

```xml
<local:Card>
    <aq:Slot Name="header">
        <TextBlock Text="Here might be a page title" FontWeight="Bold" />
    </aq:Slot>

    <TextBlock Text="A paragraph for the main content." TextWrapping="Wrap" />

    <aq:Slot Name="footer">
        <TextBlock Text="Here's some contact info" FontStyle="Italic" />
    </aq:Slot>
</local:Card>
```

Named-slot content and the bare default-slot `TextBlock` sit flatly together, distinguished
only by the `aq:Slot` wrapper - exactly Vue's own rule ("all top-level non-`<template>`
nodes are implicitly the default slot").

**Fallback + "was this provided" (Vue's `$slots.header`)** is two mechanisms:
- `SlotProvided` composed with [`If`](#if-v-if--v-else--v-else-if) (shown above) - the
  faithful mirror of Vue's own `v-if="$slots.header"` Conditional Slots example, skipping
  the wrapper `Border` entirely rather than just showing blank content in its place.
- `SlotContent.Fallback` - a convenience for a simple/primitive substitute value:
  `{aq:SlotContent footer, Fallback='No contact info'}`.

A slot that was provided but left empty (`<aq:Slot Name="header" />`, no content) still
counts as *provided* - only a genuinely absent slot name falls back or fails `SlotProvided`.

**Reactive**: adding or removing a `Slot` from a `SlotHost`'s `Items` at runtime (not just
during initial XAML parse) updates whatever is currently displaying it - mutating an
already-added `Slot`'s own `Name`/`Content` in place does not, though (no property-changed
notification of its own, the same characteristic `ObservableCollection<T>` itself already
has toward its elements) - replace the `Slot` object instead.

**Scoped slots** (the child passing data back through the outlet) work for a single,
non-repeated slot with zero new syntax: a component author already holds a direct reference
to a slot's content object and can set `.DataContext` on it directly - ordinary WPF, picked
up by the consumer's own `{Binding}`s through normal inheritance. The *repeated/list* case
(Vue's actual "FancyList" example) stays out of scope here, deliberately - a slot holds one
already-realized element instance, which cannot be reused N times the way a `DataTemplate`
can; that's precisely why `ItemsControl.ItemTemplate` exists, and is already documented
separately below.

Each slot (default included) holds exactly one content object - the same rule
[`If.Child`](#if-v-if--v-else--v-else-if)/[`Suspense.Child`](#suspense)/[`Teleport`](#teleport)'s
content already have throughout this library. Two items claiming the same slot name
(including two un-wrapped items both implicitly claiming the default slot) throws.

---

## Patterns that are already native

Two more concepts from Vue's docs turned out to already exist in WPF - once that was
recognized, no new API was the right answer.

### "KeepAlive"

Vue's `<KeepAlive>` caches component instances across dynamic-component switching so
inactive state isn't lost, with `onActivated`/`onDeactivated` firing on cache-in/
cache-out, and no unmount/remount at all. Only one native WPF pattern actually delivers
that - the others look plausible but were confirmed, by actually running it, to
genuinely destroy and recreate content instead:

| Pattern | Actually keep-alive? |
| --- | --- |
| Several siblings toggled via [`Directives.Show`](#directivesshow-v-show), all present in the tree from the start | **Yes** - confirmed: only `IOnActivated`/`IOnDeactivated` ever fire, never `IOnUnmounted`/`IOnMounted`, no matter how many times you switch back and forth. |
| A plain `TabControl` | **No** - every tab's content genuinely loads up front (confirmed via `IsLoaded`), but the moment you switch *away* from any tab, its content is for-real unloaded; switching back is a fresh mount, not a resume. The "never destroys" impression only holds for the initial render, before anything has been clicked. |
| `Frame`/`Page` navigation | **No**, regardless of `Page.KeepAlive` - confirmed identical unmount/remount behavior with that flag either `true` or `false`. It governs journal/state retention for URI-based navigation, not whether content stays mounted. |

So: "KeepAlive" in Aquarius is `Directives.Show`-toggled siblings plus `Lifecycle.Enable`'s
`IOnActivated`/`IOnDeactivated` - specifically *not* `TabControl` or `Frame`, despite how
native and tempting those look for a "switch between views" UI. See `KeepAliveTests.cs`
in the test project for the exact hook sequences each pattern produces.

### Scoped slots for lists

| Vue | WPF equivalent (already exists) |
| --- | --- |
| Default slot (nothing named) | `ContentControl.Content` |
| Scoped slots for a repeated list (Vue's own "FancyList" example) | `ItemsControl.ItemTemplate`/`DataTemplate` - the template runs with whatever object is set as its `DataContext`, which *is* the "props passed to the slot" |

For free-form *named* slots (`header`/`footer`/anything a consumer chooses), see the new
[Slots](#slots) section above instead - that's a real port now, not just a native mapping.

Vue's own canonical scoped-slot example, a `FancyList` that encapsulates fetching/paging
logic but lets the consumer supply the per-item template, ports directly:

```xml
<local:FancyList ItemsSource="{Binding Posts}">
    <local:FancyList.ItemTemplate>
        <DataTemplate>
            <!-- DataContext here *is* the scoped "slot props" -->
            <TextBlock Text="{Binding Body}" />
        </DataTemplate>
    </local:FancyList.ItemTemplate>
</local:FancyList>
```

See the sample app for a complete, working version of this.

---

## Ecosystem

Ships as a **single package** - Reactivity, Lifecycle, Directives, Teleport, Transition,
Provide/Inject, and Suspense are all included; there is nothing else to install.

### Repository layout

- `src/Barbatos.Wpf.Aquarius` - the library.
- `samples/Barbatos.Wpf.Aquarius.Sample` - a complete sample application exercising every
  feature area above.
- `tests/Barbatos.Wpf.Aquarius.UnitTests` - the unit test suite.

---

## API Reference

Due to the extensive nature of the library's interfaces, classes, and properties, the
full API Reference has been moved to a dedicated document modeled after Microsoft's
official .NET documentation format.

👉 **[Read the Full API Reference](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Aquarius/API-REFERENCE.md)** 👈

---

## Community

See the [root README](https://github.com/Barbatos-Labs/Barbatos.Wpf#community) for
maintainers, support, and license information - shared across every package in this
repository.
