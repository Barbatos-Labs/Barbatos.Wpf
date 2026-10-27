# Barbatos.Wpf.Aquarius

![Barbatos.Wpf.Aquarius logo](https://raw.githubusercontent.com/Barbatos-Labs/Barbatos.Wpf/main/build/aquarius-logo.svg)

### *Reactivity, lifecycle hooks, directives, Teleport, and Transitions - for WPF*

**Reactive state (`Ref<T>`/`Computed<T>`/`Watch`), lifecycle hooks, XAML directives for
two-way binding/visibility/events, custom directives, `Teleport`, `Transition`,
`Provide`/`Inject`, `Suspense`, `KeepAlive` - for plain desktop WPF, built directly on top of
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
  * [Async hooks](#async-hooks)
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
  * [Several Teleports, one host](#several-teleports-one-host)
* **[Dockable Panels](#dockable-panels)**
* **[Transition / TransitionGroup](#transition--transitiongroup)**
* **[Provide / Inject](#provide--inject)**
* **[Suspense](#suspense)**
* **[Slots](#slots)**
* **[KeepAlive](#keepalive)**
  * [Component](#component)
  * [Cache control](#cache-control)
  * [What still unmounts](#what-still-unmounts)
  * [Routed views](#routed-views)
* **[Patterns that are already native](#patterns-that-are-already-native)**
  * [Scoped slots for lists](#scoped-slots-for-lists)
* **[Ecosystem](#ecosystem)**
  * [Repository layout](#repository-layout)
* **[API Reference](#api-reference)**
* **[Community](#community)**

---

## Getting Started

### Introduction

#### What is Barbatos.Wpf.Aquarius?

Aquarius adds composition-style reactive state and a set of XAML directives to plain WPF:
reactive values and computed properties that keep bindings in sync automatically, lifecycle
hooks for a ViewModel's mount/unmount, two-way binding and event-wiring directives that cut
down on XAML boilerplate, transitions/teleport for content that needs to move or animate, and
a loading-state control for async content.

Aquarius is deliberately **not** a rewrite of WPF's binding engine, layout system, or MVVM
story - it's thin sugar on top of what's already there. Reactivity is built directly on
[CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)'s
`ObservableObject`/`[ObservableProperty]`: `Ref<T>`/`Computed<T>` below just wrap it, not a
competing system. Things WPF already does natively aren't reintroduced either - `{Binding}`,
`ItemsControl`+`DataTemplate`, and (nothing - there's no safe WPF equivalent, and none is
needed) already cover them, see [Already native](#already-native-v-bind-v-for-v-html).

> **Prerequisites**
>
> The rest of this document assumes basic familiarity with C#, XAML, and WPF's dependency
> property system.
>
> **Familiar with Vue's Composition API?** Several names and shapes below are deliberately
> similar (`Ref`/`Computed`/`Watch`, lifecycle hooks, directives) - a head start if you've
> used it before, though nothing here requires that background.

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
in the repo) generates a matching `XyzView.xaml` (with `aq:Setup.Enable="True"` and
`aq:Lifecycle.Enable="True"` already set, so a [lifecycle hook](#lifecycle-hooks) the ViewModel
implements fires with no further wiring) / `XyzView.xaml.cs` / `XyzViewModel.cs` trio in one
step - the closest Aquarius equivalent of scaffolding a new Vue single-file component:

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

- **`Ref<T>`** - one reactive value. Built directly on `ObservableObject`, so it already
  works in bindings, `Watch`, and `Computed<T>` like any other observable. Read/write through
  `.Value` explicitly - deliberately no implicit conversion, so it's always clear in code
  when a value is reactive versus plain.
- **`Computed<T>`** - a derived value. Because C# has no reactive-proxy interception,
  `Computed<T>.From(getter, ...dependencies)` takes its dependencies explicitly rather than
  auto-tracking them. It's also **writable**:

  ```csharp
  public Computed<string> FullName { get; }

  FullName = Computed<string>.From(
      () => $"{FirstName} {LastName}",
      value => { var parts = value.Split(' ', 2); FirstName = parts[0]; LastName = parts[1]; },
      this); // the setter mutates a dependency; Value then updates the normal way
  ```

  Assigning to a `Computed<T>` created without a setter throws `InvalidOperationException`.
- **`Watch`** - `Watch.On(source, (newValue, oldValue) => ...)` reacts to a single `Ref<T>`
  changing; `Watch.Effect(effect, ...dependencies)` runs immediately, then again on any
  dependency change. Both return an `IDisposable` "stop" handle. Options:
  - `immediate: true` - also runs once immediately.
  - `once: true` - stops itself after the first triggered run.
  - `deep: true` (`Watch.On` only) - if the watched value also implements
    `INotifyCollectionChanged` (e.g. an `ObservableCollection<T>`), also reacts to
    Add/Remove/Reset, not just wholesale replacement of `.Value` - covering the single most
    common "watch nested changes" need without a full recursive change-tracking system.
  - `flush: WatchFlush.Post` - coalesces rapid-fire changes through `NextTick` into one
    callback (the default here, `WatchFlush.Sync`, is immediate/synchronous instead). Like
    Vue's post flush, that one callback gets the *newest* value and the value from *before the
    burst*: `0 -> 1 -> 2 -> 3` in one go is a single `(3, 0)`. A change made on a worker thread
    still has its callback run on a UI thread - the one that created the watcher, or
    `Application.Current`'s if the watcher was created off a UI thread.
  - The callback can take a 3rd `onCleanup` parameter: calling it registers an action that
    runs right before the *next* invocation (or on `Dispose`) - the standard way to cancel
    stale async work before starting new work:

    ```csharp
    Watch.On<int>(userId, (id, _, onCleanup) =>
    {
        var cts = new CancellationTokenSource();
        onCleanup(cts.Cancel);
        _ = LoadUserAsync(id, cts.Token);
    });
    ```
- **`NextTick`** - `NextTick.Run(callback)` / `await NextTick.RunAsync()` schedule work via
  `Dispatcher.BeginInvoke(DispatcherPriority.Background, ...)`, i.e. after the current batch
  of UI updates has been processed. This is also the primitive
  [Lifecycle](#lifecycle-hooks)'s `IOnUpdated` batching and `Watch`'s `flush: Post` are
  built on. Called on the UI thread it uses that thread's dispatcher; called from a worker
  thread (a pool continuation after an `await`, a `Task.Run` body) it posts to the UI thread -
  `Application.Current`'s dispatcher - rather than to the worker's own, which nothing ever
  pumps: that is where the callback used to sit forever, taking every coalescing caller with
  it. Only an app with no `Application` at all, calling from a worker, still gets the calling
  thread's dispatcher. The exact order is in the
  [API reference](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Aquarius/API-REFERENCE.md#nexttick-static-class).

A C# `Ref<T>` is always a concrete, statically-typed `Ref<T>` - there's no plain-value/ref/
getter ambiguity to normalize away, so there's no `toRef`/`toRefs`/`unref`-style helper here.

---

## Lifecycle Hooks

`Barbatos.Wpf.Aquarius.Composition` - `Lifecycle.Enable` + eleven `IOnXxx` interfaces (plus eight
more `IOnXxxAsync` counterparts - see [below](#async-hooks)).

Each hook is its own tiny interface, so a ViewModel opts into exactly the ones it needs by
implementing only the ones it cares about:

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

| Interface | Fires on... |
| --- | --- |
| `IOnBeforeCreate` | Same first opportunity as `IOnBeforeMount` below, one step before it |
| `IOnCreated` | Immediately after `IOnBeforeCreate` - nothing observable happens between them in this port |
| `IOnBeforeMount` | `Initialized` (or, if `DataContext` wasn't bound yet at that point - the common case when it's set by the parent - as a guaranteed fallback right before `IOnMounted`) |
| `IOnMounted` | `Loaded` |
| `IOnBeforeUpdate` | The first `PropertyChanged` from `DataContext` in a new update batch (synchronous on the UI thread; a change raised on a worker is marshalled there first) |
| `IOnUpdated` | Once per batch, coalesced through `NextTick` |
| `IOnBeforeUnmount` | `Unloaded`, before `IOnUnmounted` |
| `IOnUnmounted` | `Unloaded` |
| `IOnActivated` | Mount (only if actually visible at that point), `IsVisible` flipping back to `true` while still mounted, and (additionally) a `Window` regaining focus - see [KeepAlive](#keepalive) |
| `IOnDeactivated` | Unmount, `IsVisible` flipping to `false` while still mounted, and (additionally) a `Window` losing focus - see [KeepAlive](#keepalive) |
| `IOnErrorCaptured` | An unhandled exception reaches the dispatcher while mounted |

Notes:

- A ViewModel's constructor has necessarily already run by the time it can be observed as
  a `DataContext` at all - there's no WPF equivalent of hooking in earlier than that - so
  `IOnBeforeCreate`/`IOnCreated` fire back to back, right before `IOnBeforeMount`, rather
  than at any earlier, truer "construction" moment.
- Every hook here follows the element's own mount/unmount cycle, not "once per ViewModel
  object": if the same ViewModel instance is remounted (e.g. behind an `<aq:If>` that
  toggles back), `IOnBeforeCreate`/`IOnCreated`/`IOnBeforeMount` all fire again too - none
  of them are "only the first time this object is ever seen."
- WPF raises a single `Unloaded` event covering the whole teardown -
  `IOnBeforeUnmount`/`IOnUnmounted` both fire from it, back to back, rather than at two
  genuinely distinct moments.
- `IOnUpdated`'s batching is real, not just a name: several synchronous property changes
  in a row produce exactly one `IOnUpdated` call.
- `IOnBeforeUpdate`/`IOnUpdated` always run on the element's own (UI) thread. A ViewModel that
  raises `PropertyChanged` from a worker thread - which WPF bindings already tolerate - has the
  notification marshalled there first, so `IOnBeforeUpdate` then fires once the UI thread
  processes it instead of synchronously inside the worker's setter. (Handled on the worker, the
  element could not be read from that thread: the setter threw, and update batching stopped
  for that element until it unmounted.)
- `IOnErrorCaptured` follows WPF's own `Handled` convention: return `true` to mark the
  exception handled (suppresses the app's unhandled-exception behavior), `false` to let it
  keep propagating - the same polarity every other "did you handle this" callback in WPF
  already uses. The exceptions it sees are those of the element's own dispatcher
  (`Dispatcher.UnhandledException` of the thread the element belongs to, while mounted): on the
  main UI thread that is exactly what `Application.DispatcherUnhandledException` reports, and
  every mounted ViewModel implementing the hook sees every one of them - it is not scoped to the
  View's subtree. An element on a second UI thread gets that thread's exceptions, with no
  `Application` needed. (It used to subscribe `Application.Current.DispatcherUnhandledException`
  from whatever thread mounted it, so an element on another UI thread missed its own thread's
  exceptions, and its handler threw a cross-thread `InvalidOperationException` - in place of the
  real exception - whenever the main thread had one.)
- `IOnActivated`/`IOnDeactivated` are what the [`KeepAlive`](#keepalive) control is built
  on - and why it matters that mount only reports activated when the element is genuinely
  visible (content that mounts already-hidden, like a background TabControl tab, must not
  falsely claim to be activated - see that section for why).

`If` ([below](#if-v-if--v-else--v-else-if)) genuinely detaches/reattaches its content from the visual tree,
so a `Lifecycle.Enable`'d child behind an `<aq:If>` really does receive
`IOnUnmounted`/`IOnMounted` calls as the condition toggles, not just a visibility flip.

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
<!-- DashboardView.xaml: Lifecycle sits on the View that *contains* the Suspense -->
<UserControl x:Class="MyApp.DashboardView" aq:Lifecycle.Enable="True" ...>
    <aq:Suspense IsPending="{Binding IsPending}">
        <local:DashboardContent />
        <aq:Suspense.Fallback>
            <TextBlock Text="Loading..." />
        </aq:Suspense.Fallback>
    </aq:Suspense>
</UserControl>
```

No changes to `Suspense` itself were needed for this - `OnMountedAsync` toggling a plain
`bool` property and `Suspense.IsPending` binding to it already compose on their own. Where
`Lifecycle.Enable` goes is what matters: on the View that holds the `Suspense`, as
`samples/Barbatos.Wpf.Aquarius.Sample/LifecycleDemoView.xaml` does - never on an element
inside `Suspense`'s content. That content is out of the tree while `IsPending` is `true`, so a
hook living there never mounts (a spinner forever, since `IsPending` starts `true`) or, if it
started `false`, mounts again on every resolve and reloads in a loop.

A few rules distinguish these from a naive `async void OnMounted()`, which is exactly what
this exists to avoid (an unobservable, uncatchable fire-and-forget with no way to route a
failure anywhere):

- **Fires at the same point as its sync counterpart, in the same call order - but is never
  awaited.** A slow `OnMountedAsync` does not delay `OnActivated`, a later remount, or
  anything else.
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
XyzViewModel(...)`.

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
  var builder = WpfApp.CreateBuilder();
  builder.Services.AddAquarius();          // registers Aquarius's own options

  var app = builder.Build();
  app.Services.UseAquarius();              // ...and wires them up
  ```

  Two calls, deliberately: `AddAquarius` only *declares* registrations, and `UseAquarius` is
  the one-shot wiring step you run once the container exists - the same split as Core's own
  `UseEssentials`. That wiring step is what a XAML-driven library needs and a pure service
  library does not: `aq:Setup.Enable` and [`Component.Is`](#keepalive) both resolve types
  from markup, where there is no constructor to inject into, so they go through an ambient
  provider. `UseAquarius` is what points that at your container.

  `AddAquarius(options => ...)` also sets `ThrowOnUnresolvedViewModels`,
  `ThrowOnUnresolvedExprIdentifiers` and a custom `ViewModelResolver` from one place, instead
  of as loose static assignments after `Build()`. It is optional - `app.Services.UseAquarius()`
  on its own still does the useful half - and so is the whole thing:
  `Setup.ServiceProvider = host.Services` remains a perfectly good one-liner for an app that
  wants nothing else.

  `AddAquarius` reads **no configuration**: Aquarius depends on the DI and Options
  abstractions only, not on `Microsoft.Extensions.Configuration`, so that it installs into an
  app with no configuration system. `AquariusOptions.SectionName` (`"Barbatos:Aquarius"`) is
  the section name the repository's `Barbatos:` convention reserves; to read the two switches
  from `appsettings.json`, bind it yourself, after `AddAquarius` so file values override code
  values (`Bind` comes from `Microsoft.Extensions.Options.ConfigurationExtensions`, which a
  Barbatos.Wpf.Core app already has):

  ```csharp
  builder.Services.AddAquarius(options => options.ThrowOnUnresolvedViewModels = true);
  builder.Services.AddOptions<AquariusOptions>().Bind(builder.Configuration.GetSection(AquariusOptions.SectionName));
  ```

  `ViewModelResolver` is a delegate and can only be set from code.

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
is deliberately not a `DependencyProperty` for security reasons. Any other element type
throws too, rather than silently doing nothing.

The two-way binding is applied as soon as the bound value first resolves - **including to
`null`**, the usual starting point for a `SelectedItem` nothing is selected in yet or a
`string?` not filled in yet. WPF only runs a property-changed callback when the effective
value changes, so while the attached property defaulted to `null`, a binding whose first value
was also `null` never got that far: the control looked bound, but nothing the user typed or
selected reached the ViewModel. The default is now a private "unset" marker, which any first
value differs from (`GetModel` still reports an unset property as `null`).

### Directives.Show (v-show)

```xml
<Border aq:Directives.Show="{Binding IsOpen}">...</Border>
```

`false` maps to `Visibility.Collapsed` (no layout space, state preserved). The element
stays mounted the whole time.

### If (v-if / v-else / v-else-if)

```xml
<aq:If Condition="{Binding IsOpen}">
    <TextBlock Text="Only in the tree while IsOpen is true" />
</aq:If>
```

Unlike `Directives.Show`, `Condition` going `false` genuinely detaches the content from
the visual tree rather than just hiding it. The child is kept in `If.Child` (the XAML content property) so it reattaches unchanged
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
read visibly worse than a flat if/else-if chain past 3-4 branches, since WPF's
content model has no equivalent to "grouped flat siblings" - a `Switch`/`Case` control
would be the natural escape hatch if that becomes painful, deliberately not built here
since it wasn't asked for.

#### Performance: `If` vs `Directives.Show`

The tradeoff: `If` has near-zero cost while its
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
  simply don't re-fire at all. [`KeepAlive`](#keepalive) below is that same trick packaged
  up, for a whole set of switchable sections.

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
remains the right answer - non-trivial logic belongs in code, not a XAML string.

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
no built-in `Command` for (`Border.MouseLeftButtonDown`, `TextBox.TextChanged`, ...), not
just a fixed list. The command runs with
`Directives.CommandParameter` if set, otherwise the raised `EventArgs` itself. The
subscription follows the element's lifecycle: suspended on `Unloaded` and re-hooked on the
next `Loaded`, so an element that is detached and reattached (an `If` branch, a `Teleport`
move, a `TabItem` switched away from and back) keeps working. Only `once`, or changing
`Event`/`Command` themselves, removes it for good.

**Modifiers** (`Directives.Modifiers`, comma-separated), each a no-op unless the raised
`EventArgs` is actually the matching type:

| Modifier | Effect |
| --- | --- |
| `stop`, `prevent` | Sets `RoutedEventArgs.Handled = true`. WPF collapses the DOM's separate stopPropagation/preventDefault into one flag, so these two are honestly the same operation here. |
| `once` | Unhooks for good the first time the event is raised. |
| `self` | Only invokes if `OriginalSource` is the element itself, not a descendant the event bubbled up from. |
| `left` / `right` / `middle` | Only invokes for that `MouseButtonEventArgs.ChangedButton`. |
| `enter` / `tab` / `esc` / `space` / `up` / `down` / `left` / `right` / `delete` | Only invokes for that `KeyEventArgs.Key` (`delete` matches both Delete and Backspace; `left`/`right` mean arrow keys here, resolved by `KeyEventArgs` vs. `MouseButtonEventArgs` - the same modifier name means something different depending on which event type actually fired). |

```xml
<TextBox aq:Directives.Event="KeyDown"
         aq:Directives.Command="{Binding SubmitCommand}"
         aq:Directives.Modifiers="enter" />
```

**`stop`/`prevent` and `once` act before the filters**, on every raise of the event: the
`self`, mouse-button and key modifiers only decide whether the command then runs. So
`Modifiers="enter,stop"` marks *every* key `Handled`, not only Enter, and `"enter,once"`
unhooks on the first key of any kind - even one that was not Enter and never ran the command.
When only a match should be handled or unhook, drop the modifier and check the key inside the
command.

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

A reusable piece of element behavior, attached declaratively via `Directives.Use` instead of
writing a one-off event handler each time. `Directive` exposes
`Mounted`/`Updated`/`Unmounted`, each receiving a `DirectiveBinding` with `Value`/`OldValue`
(from `Directives.UseValue`, if bound), `Argument` (from `Directives.Argument`), and
`Modifiers` (from `Directives.Modifiers`, shared with
[Directives.Event](#directivesevent-v-on) above). There's no compact
`directive:arg.mod1.mod2="value"` markup syntax - each piece is its own sibling attached
property instead. v1 supports one directive per element through `Directives.Use`; composing
several is possible by writing a directive that itself dispatches to others.

### Directives.Class / Directives.Style (a lighter DataTrigger)

`If`/`Directives.Show` are deliberately minimal (structural mount/unmount or visibility
only) - `Directives.Class`/`Directives.Style` exist for the more common case: toggling a
handful of properties based on data, without reaching for a full `DataTrigger`:

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

WPF bindings are plain property paths with no expression language, so even a trivial check
like "is this collection empty" needs either a bound boolean property or a converter.
`Comparisons.Not`, `.IsNull`, `.IsEqualTo` (reads `ConverterParameter` as the comparand)
remove the ceremony of hand-writing a whole `IValueConverter` class for the three most
common trivial cases; for anything less trivial, a `Computed<T>` in the ViewModel is still
the right answer.

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
any other boolean.

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

While mounted and enabled, `Teleport.Content` is detached from it and added to the
`Panel.Children` of whichever `Panel` registered itself via `TeleportHost.RegisterHost`
under `To` - commonly an overlay `Grid` pinned at a window's root, so content declared deep
inside some nested layout (and subject to its clipping/z-order) can still render pinned
above everything else. `Disabled` keeps the content local instead. A `Teleport` that mounts
before its target host does still finds it once the host registers, instead of silently never
teleporting.

The moved element keeps its **object identity** - the same `TextBox` instance, whatever was
typed into it, its own local values - but two things do not travel with it, unlike Vue's
`<Teleport>`:

- **The lifecycle.** Vue decides where a teleported node lives before committing anything to
  the DOM; WPF has no such pre-commit step, so moving a loaded element between panels is a
  genuine detach and reattach. Every move - including the very first settle into a host that was
  already registered - fires a full `IOnBeforeUnmount`/`IOnUnmounted` then
  `IOnBeforeMount`/`IOnMounted` sequence on `Lifecycle.Enable`'d content, so a teleported
  ViewModel's `OnMounted` must be idempotent. `TeleportTests` pins the exact sequences.
- **`DataContext`.** It is inherited from the *new* ancestors, not carried along: content that
  lands in an overlay host under the window root binds against the window's `DataContext`, not
  the one it was declared under. Give teleported content a `DataContext` that is right wherever
  it lands - its own `aq:Setup`, an explicit `DataContext="{Binding}"` at the declaration site
  (as the sample's toast does), or a host whose ancestors carry the right object.

Like a DOM `id`, host names are expected to be unique app-wide; if two hosts register the
same name, the most recently loaded one wins.

### Several Teleports, one host

A host is **any `Panel`**, and any number of `Teleport`s may target the same one - a toolbar
assembled from widgets declared all over the app, an overlay stack, a docking edge. Pick the
panel for the layout you want:

```xml
<UniformGrid Rows="1" aq:TeleportHost.RegisterHost="Toolbar" />

<!-- ...declared anywhere, in any order: -->
<aq:Teleport To="Toolbar" Order="20"><views:SaveButton /></aq:Teleport>
<aq:Teleport To="Toolbar" Order="0"><views:SearchBox /></aq:Teleport>
<aq:Teleport To="Toolbar" Order="10"><views:FilterPicker /></aq:Teleport>
```

`Order` (lower first, default `0`) is what makes the result deterministic. Without it the
children would land in whatever order they happened to *load* in, which depends on where each
`Teleport` is declared, whether its host was already registered, and whether it sits inside a tab
that has been opened yet. Ties break by declaration order, so leaving everything at the default
still gives a stable result rather than an arbitrary one.

Two things worth knowing:

- **Set layout attached properties on the content, not on the `Teleport`.** It is the content
  element that becomes the panel's real child, so `DockPanel.Dock`, `Grid.Row` and friends belong
  on it - and they keep working after the move.
- **A host may have static children of its own.** Teleported children are positioned relative to
  each other only, and placed after whatever the host declares itself, so a `StackPanel` with a
  fixed header or a `DockPanel` with a permanent status bar works as you'd expect.

Changing `Order` at runtime repositions only that one control's content, leaving its siblings
untouched - though repositioning is a real `Children` remove-then-insert (WPF's
`UIElementCollection` has no move operation), so it fires the same full unmount/remount pair every
other `Teleport` move does.

---

## Dockable Panels

This is **not a new API** - it's `Teleport`/`TeleportHost` from the section above, used
across two `Window`s instead of one. The whole thing is also a working demo in
`samples/Barbatos.Wpf.Aquarius.Sample` (the Teleport tab: `TeleportDemoView.xaml` and its
code-behind, plus `DockableToolDialog.xaml`) - what follows is that same example, trimmed down.

**The idea:** one `<aq:Teleport>`, declared once in a View that stays open, is retargeted
between two registered hosts - a "docked" one in that View, and a "floating" one in a
separate dialog `Window`. Because `Teleport` re-parents rather than recreates, the panel's
state (whatever the user typed, scrolled, etc.) survives every move - though, as above, each
move is still a full unmount/remount as far as lifecycle hooks are concerned.

**1. The docked host and the persistent `Teleport`, both in the View's XAML
(`TeleportDemoView.xaml`):**

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
`TeleportDemoView`'s code-behind, reacting to that same `DockTarget` with `Watch.On`. It is
wired once the `DataContext` is actually the ViewModel - on `DataContextChanged`, not `Loaded`,
because a `TabControl` raises `Loaded` for a tab's content before the tab is ever selected,
and again later with no `Unloaded` in between:

```csharp
private DockableToolDialog? _dockableDialog;
private bool _wired;

public TeleportDemoView()
{
    InitializeComponent();
    DataContextChanged += (_, _) => TryWireDockTargetWatch();
    TryWireDockTargetWatch();
}

private void TryWireDockTargetWatch()
{
    if (_wired || DataContext is not TeleportDemoViewModel viewModel)
        return;

    _wired = true;

    Watch.On(viewModel.DockTarget, (target, _) =>
    {
        if (target == "FloatingDock")
        {
            if (_dockableDialog is not null) return;

            // DataContext set explicitly - see the second point below.
            _dockableDialog = new DockableToolDialog { Owner = Window.GetWindow(this), DataContext = viewModel };
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
  the main View, never inside the dialog. Content lives and dies with the `Teleport`
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

No canned animations ship here - `Transition` only orchestrates *when* your own `Storyboard`
resources run; callers supply the actual animations.

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
[Lifecycle](#lifecycle-hooks) synergy `If` documents) and then plays `Enter`. The initial
content is displayed directly rather than animated in - `Enter` only plays on a later
`false`→`true` toggle, not on first render. Storyboards are cloned before each play, so
sharing one `Enter`/`Leave` resource across multiple `Transition`s (the normal case) doesn't
cross-trigger their `Completed` handlers. Both run on the `Transition` control itself, so they
animate its own properties (`Opacity` above).

A finished `Storyboard` keeps holding its end value (`FillBehavior.HoldEnd`, the default), so
`Leave` is begun *controllable* and kept until the next show. When `Show` goes back to `true` -
mid-`Leave` or long after it finished - `Enter` starts first, taking over whatever it animates
(an `Enter` animation with no `From` starts from where `Leave` had got to, so a half-faded panel
fades back in from there), and then `Leave` is removed, so anything only `Leave` animated returns
to its own value. A re-toggle mid-`Leave`
therefore really stops the in-flight animation and keeps the content mounted, and a
`Transition` with a `Leave` but no matching `Enter` re-shows its content at full opacity.
(Begun non-controllable, as an earlier version did, `Leave` ignored the stop, ran to its end,
and re-shown content could stay at `Opacity` 0.) `TransitionTests` pins both cases.

```xml
<ListBox ItemsSource="{Binding Items}" aq:TransitionGroup.Enter="{StaticResource FadeIn}" />
```

`TransitionGroup` covers the **enter** side only: every newly-generated item container in
an `ItemsControl` gets a one-time `Loaded` hook that plays `Enter`. One-time is literal - the
hook unsubscribes on that first `Loaded`, so a container loaded again later (the list's tab or
`If` coming back) holds an item that is not new and does not replay; an earlier version stayed
subscribed and replayed every container each time (`TransitionGroupTests`). Animating items
*leaving* the list, or animating the reflow when the list reorders (each surviving item
sliding to its new position), both require intercepting removal/reflow *before* the panel
applies it - which for a plain `ItemsControl`/`Panel` means owning a custom panel, a
materially bigger and riskier problem than enter (virtualization/container-recycling
interactions especially).
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

A ViewModel intentionally doesn't hold a reference to its View, so this can't walk a
component tree the way some frameworks do - it leans on WPF's own closest analog instead:
`FrameworkElement.Resources` plus `FindResource`, which already walks up the logical tree
and merges resource dictionaries at each level - `Provide.Value` is just stored into
`Resources` under `Provide.Key`, so it composes with WPF's existing lookup/
override-by-nesting semantics for free, and a nearer `Provide` for the same key correctly
overrides a farther one.

`{aq:Inject ThemeColor}` (positional, as above) or `{aq:Inject Key=ThemeColor}` is the
primary way to consume a provided value *in XAML* - a key is required: `{aq:Inject}` with none
fails to load ("Inject requires a Key."). It delegates internally to
`DynamicResourceExtension`, reusing WPF's own deferred, re-evaluate-on-invalidation
resolution (so a later `Provide.Value` change is picked up live) rather than eagerly
walking the tree itself. For the rare case of needing an injected value from C# instead
(e.g. a View handing it to its own `DataContext`), use
`Inject.Get<T>(element, key, fallback)`.

A string key can collide with an unrelated resource sharing the same name. For
collision-proofing, use a dedicated `object` sentinel instead of a string:

```csharp
public static class Keys { public static readonly object ThemeColor = new(); }
```

```xml
<Grid aq:Provide.Key="{x:Static local:Keys.ThemeColor}" ... />
```

Reactivity crosses the boundary for free: if the provided value is a `Ref<T>` or an
`ObservableObject`, nothing extra is needed - bindings against it (or code reading
`.Value`) already see live updates after injection.

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

C# has no hook to detect "this ViewModel is mid-load" automatically, so this is
deliberately explicit rather than automatic: the ViewModel already knows when it's loading
(an `IsLoading`/`Ref<bool>` property it sets around an async call) - `IsPending` just wires
that straight to which content shows. No nested-boundary handling, no automatic dependency
aggregation, no `pending`/`resolve`/`fallback` events - one explicit boolean in, one of two
contents out.

---

## Slots

`Barbatos.Wpf.Aquarius.Xaml` - `Slot`, `SlotHost`, `SlotContent`, `SlotProvided`.

Free-form named content placeholders: slot names chosen at the use site, not pre-declared
as a `DependencyProperty` per name by the component author. (WPF's
`ContentControl.Content`/`ItemsControl.ItemTemplate` already cover the
single-default-content and list-repeated cases respectively - see
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

Consumers write a flat mix of `Slot`-wrapped (named) and plain (implicit default) children:

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
only by the `aq:Slot` wrapper - any element not wrapped in a `Slot` is implicitly part of
the default slot.

**Fallback, and checking "was this provided"** is two mechanisms:
- `SlotProvided` composed with [`If`](#if-v-if--v-else--v-else-if) (shown above) - skips
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
stays out of scope here, deliberately - a slot holds one
already-realized element instance, which cannot be reused N times the way a `DataTemplate`
can; that's precisely why `ItemsControl.ItemTemplate` exists, and is already documented
separately below.

Each slot (default included) holds exactly one content object - the same rule
[`If.Child`](#if-v-if--v-else--v-else-if)/[`Suspense.Child`](#suspense)/[`Teleport`](#teleport)'s
content already have throughout this library. Two items claiming the same slot name
(including two un-wrapped items both implicitly claiming the default slot) throws.

---

## KeepAlive

`Barbatos.Wpf.Aquarius.Xaml` - `Component` + `KeepAlive`.

Vue's own two pieces, ported as-is. `<aq:Component Is="...">` is
[`<component :is>`](https://vuejs.org/guide/essentials/component-basics.html#dynamic-components):
it renders whichever View type you point it at, and destroys the previous one when that changes.
`<aq:KeepAlive>` wraps it to cache them instead:

```xml
<aq:KeepAlive Max="3" Exclude="ReportView">
    <aq:Component Is="{Binding CurrentView.Value}" />
</aq:KeepAlive>
```

Switching `Is` reveals the cached View for that type, building it only the first time. A ViewModel
under [`Lifecycle.Enable`](#lifecycle-hooks) therefore sees only `IOnDeactivated` when you switch
away and `IOnActivated` when you come back - **never an unmount** - so everything it holds (a
scroll position, a half-filled form, a loaded page of results) is still there.

That is what neither a `TabControl` nor `Frame`/`Page` gives you. Both look like they should, and
both were confirmed *by actually running them* to genuinely destroy and recreate content on every
switch:

| Pattern | Actually keep-alive? |
| --- | --- |
| `aq:KeepAlive` around an `aq:Component` | **Yes** - only `IOnActivated`/`IOnDeactivated` ever fire while switching, however many times you go back and forth. Lazy, too: a View you never visit is never built. |
| Several siblings toggled via [`Directives.Show`](#directivesshow-v-show), all present from the start | **Yes**, and it is what `KeepAlive` does internally - but by hand: every section mounts up front whether or not it is ever looked at, and there is no cache cap and no include/exclude. |
| A plain `TabControl` | **No** - every tab's content genuinely loads up front (confirmed via `IsLoaded`), but the moment you switch *away* from a tab, its content is for-real unloaded; switching back is a fresh mount, not a resume. The "never destroys" impression only holds for the initial render, before anything has been clicked. |
| `Frame`/`Page` navigation | **No**, regardless of `Page.KeepAlive` - confirmed identical unmount/remount behavior with that flag either `true` or `false`. It governs journal/state retention for URI-based navigation, not whether content stays mounted. |

See `KeepAliveTests.cs` (the native patterns) and `KeepAliveControlTests.cs` (these controls) in
the test project for the exact hook sequences each one produces.

### Component

`Is` is a bindable `Type`, so a ViewModel drives it with an ordinary `Ref<Type>`:

```csharp
public Ref<Type?> CurrentView { get; } = new(typeof(ProfileView));
```

The View is resolved through [`Setup`](#setup)'s DI-first-else-`Activator` path, so a
constructor-injected View comes out of your own container and its own `aq:Setup.Enable` resolves
its `DataContext` exactly as usual.

**Used on its own, `Component` destroys the outgoing View** - it is dropped outright, so it *and
its ViewModel* become collectable and anything the ViewModel was holding (a subscription, a timer,
a cache) stops being reachable. That is Vue's behavior, and it is precisely what `KeepAlive` exists
to opt out of, which is why the caching is a wrapper rather than a setting.

> **The name.** Deliberately `Component`, matching Vue's own tag, at the cost of colliding with
> `System.ComponentModel.Component` for C# code that imports both namespaces - a qualified name
> resolves that on the rare occasion it comes up. In XAML, where this type is actually written,
> `<aq:Component>` is unambiguous.

### Cache control

| Property | Meaning |
| --- | --- |
| `Max` | How many Views may stay cached, least-recently-activated evicted first. `0` (default) is unlimited. Mirrors Vue's `max`. |
| `Include` | Comma-separated allow-list of View **type names** that may be cached. Unset caches everything. |
| `Exclude` | Comma-separated deny-list, checked after `Include`. |

`Include`/`Exclude` match the View's **type name** - the WPF counterpart of Vue matching a
component's own `name` option. Vue's regex and array forms have no concise XAML literal, so the
comma-separated string is the one form here.

Eviction and exclusion are real teardowns: the dropped View fires `IOnBeforeUnmount`/
`IOnUnmounted`, and switching back to it builds from scratch. Editing `Include`/`Exclude` at
runtime applies retroactively to what is already cached (Vue watches both for the same reason) -
the one exception being the View currently on screen, which is marked instead and dropped the
moment you switch away, since releasing it outright would blank the control.

### What still unmounts

Exactly as in Vue: a View evicted by `Max`, one filtered out by `Include`/`Exclude`, and **every
cached View when the `KeepAlive` control itself is unloaded**. That last one is a WPF fact rather
than a choice - `Loaded`/`Unloaded` are broadcast through a subtree regardless of `Visibility` - so
putting a `KeepAlive` inside a `TabItem` or an [`If`](#if-v-if--v-else--v-else-if) means every
cached View gets a real unmount/remount cycle whenever that container comes and goes. The
*ViewModel instances* survive that (their `DataContext` is never touched), so state is still
preserved; the hooks simply run again.

Two smaller caveats worth knowing:

- **`KeepAlive` owns its children's `Visibility`**, writing it as a local value - so don't also put
  [`Directives.Show`](#directivesshow-v-show) on a cached View; the two will fight over it.
- Adding children to `KeepAlive` directly (rather than through its `Component`) throws. They would
  have no cache entry and no eviction position, and would simply be arranged on top of whatever is
  showing - a silent layout bug worth failing loudly over.

### Routed views

Vue Router has **no per-route keep-alive option**; it composes in the template instead, via
`<router-view v-slot="{ Component }">`. AquariusRouter deliberately does not offer one either, and
`<aq:KeepAlive>` around an `<aqr:RouterView>` is **not** supported - see
[AquariusRouter's "Deliberately Out of Scope"](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.AquariusRouter/README.md#deliberately-out-of-scope)
for why: its `Router` owns routed View lifetimes so that navigation guards can run against the
instance that is actually mounted, which is a constraint Vue's renderer does not have.

---

## Patterns that are already native

One more pattern turned out to already exist natively in WPF - once that was recognized,
no new API was the right answer.

### Scoped slots for lists

| Concept | WPF equivalent (already exists) |
| --- | --- |
| A single default content area | `ContentControl.Content` |
| A per-item template for a repeated list, where the list owner supplies data and the consumer supplies the template | `ItemsControl.ItemTemplate`/`DataTemplate` - the template runs with whatever object is set as its `DataContext`, which *is* the data passed to the template |

For free-form *named* slots (`header`/`footer`/anything a consumer chooses), see the
[Slots](#slots) section above instead - that's real Aquarius API, not just a native WPF
mapping.

A `FancyList` that encapsulates fetching/paging logic but lets the consumer supply the
per-item template - a common pattern this WPF mapping covers directly:

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
