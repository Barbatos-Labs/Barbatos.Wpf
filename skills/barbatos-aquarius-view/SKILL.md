---
name: barbatos-aquarius-view
description: Build or debug a WPF View + ViewModel with Barbatos.Wpf.Aquarius - Ref/Computed/Watch/NextTick reactivity, the aq:Lifecycle mount/unmount hooks, aq:Setup ViewModel and DataContext resolution, the Directives family (Model/Show/Event/Class/Style/Use), If and Expr, Teleport, Transition/TransitionGroup and Storyboard timing, Provide/Inject, Suspense, Slots, Component and KeepAlive. Use this for any XAML carrying the aq: prefix, and just as much when the user describes only the symptom - "DataContext is null", "the ViewModel never gets notified", "this value should update by itself when that one changes", "the popup is clipped and should float above everything", "state is lost when I switch tabs", "I want it to fade in", "run something when the view appears".
---

# Barbatos.Wpf.Aquarius

Composition-style reactivity and XAML directives for plain WPF. **Thin sugar over
CommunityToolkit.Mvvm and WPF's own binding engine — never a replacement for either.**

Full docs: [`src/Barbatos.Wpf.Aquarius/README.md`](../../src/Barbatos.Wpf.Aquarius/README.md) ·
[`API-REFERENCE.md`](../../src/Barbatos.Wpf.Aquarius/API-REFERENCE.md).
Reference app: [`samples/Barbatos.Wpf.Aquarius.Sample`](../../samples/Barbatos.Wpf.Aquarius.Sample)
(one self-contained View + ViewModel pair per tab).

One XAML namespace covers all four CLR namespaces:

```xml
xmlns:aq="http://schemas.barbatos.co/aquarius/2026/xaml"
```

Aquarius does **not** depend on Core. It works in any WPF app, with or without the host.

---

## Do not reinvent what WPF already has

Check this table before adding anything:

| Want | Use |
| --- | --- |
| One-way / two-way data binding | `{Binding}` — no directive needed |
| Repeat a collection | `ItemsControl` + `DataTemplate` |
| Per-item template supplied by the consumer ("scoped slot") | `ItemsControl.ItemTemplate` — the item *is* the slot props |
| One content area | `ContentControl.Content` |
| Error display for `INotifyDataErrorInfo` | WPF's native validation adorner |
| Anything less trivial than `Not`/`IsNull`/`IsEqualTo` | a `Computed<T>` in the ViewModel |

Aquarius only fills genuine gaps.

---

## Reactivity

```csharp
using Barbatos.Wpf.Aquarius.Reactivity;

public Ref<int> Count { get; } = new(0);

public Computed<int> Doubled { get; }
public Computed<string> FullName { get; }

public MyViewModel()
{
    Doubled = Computed<int>.From(() => Count.Value * 2, Count);

    // writable computed: the setter mutates a declared dependency
    FullName = Computed<string>.From(
        () => $"{FirstName} {LastName}",
        value =>
        {
            var parts = value.Split(' ', 2);
            FirstName = parts.Length > 0 ? parts[0] : "";
            LastName = parts.Length > 1 ? parts[1] : "";
        },
        this);

    Watch.On(Count, (newValue, oldValue) => Log($"{oldValue} -> {newValue}"));
}
```

Bind through `.Value`: `{Binding Count.Value}`.

- **Dependencies are explicit.** C# has no proxy-based tracking — list every
  `INotifyPropertyChanged` the getter reads. `this` is a valid dependency when the getter reads
  `[ObservableProperty]` fields on the ViewModel itself.
- `Ref<T>.Value` raises `PropertyChanged` only on a real change (default comparer).
- `Computed<T>` is `IDisposable` — dispose it if the ViewModel outlives its dependencies.
  A read-only `Computed<T>`'s setter throws `InvalidOperationException`.
- `Watch.On(source, cb, immediate:, once:, deep:, flush:)` returns `IDisposable`. The
  three-parameter overload's third argument is an `onCleanup` registrar. `WatchFlush.Post`
  coalesces rapid changes through `NextTick`; `Sync` (default) fires immediately.
- `Watch.Effect(effect, params deps)` for "run this whenever any of these change".
- `NextTick.Run` / `NextTick.RunAsync` schedule at `DispatcherPriority.Background`.

`Ref<T>`/`Computed<T>` and `[ObservableProperty]` coexist freely — use `[ObservableProperty]`
for plain state and `Computed<T>` for derived values.

---

## Setup — resolving a ViewModel from XAML

```xml
<UserControl aq:Setup.Enable="True" ... />              <!-- XyzView -> XyzViewModel -->
<UserControl aq:Setup.ViewModel="{x:Type vm:SomeViewModel}" ... />   <!-- explicit wins -->
```

Wire the container once, at startup:

```csharp
builder.Services.AddAquarius();     // declares AquariusOptions only
var app = builder.Build();
app.Services.UseAquarius();         // points Setup.ServiceProvider at the container
```

- Resolution order: `Setup.ServiceProvider` → `Activator.CreateInstance` (needs a public
  parameterless constructor). `Setup.Resolve(Type)` is the single place this rule lives —
  `Component` and `RouterView` both go through it. Do not add another construction path.
- Resolves at `Initialized`, **not** `Loaded`, so `Lifecycle` always sees the assigned
  `DataContext` regardless of attached-property ordering.
- `AquariusOptions`: `ThrowOnUnresolvedViewModels`, `ThrowOnUnresolvedExprIdentifiers`,
  `ViewModelResolver`. Turn the throw options on during development to catch typos.
- A View whose parent sets `DataContext` explicitly (the sample's tab shell) should **not**
  also use `Setup`. A View meant to be dropped anywhere (`InfoCardView`) should.

---

## Lifecycle hooks

```xml
<UserControl aq:Lifecycle.Enable="True" ... />
```

```csharp
public sealed partial class MyViewModel : ObservableObject, IOnMounted, IOnUnmounted, IOnMountedAsync
{
    public void OnMounted() { }
    public void OnUnmounted() { }
    public async Task OnMountedAsync() { await LoadAsync(); }
}
```

Eleven sync interfaces — `IOnBeforeCreate`, `IOnCreated`, `IOnBeforeMount`, `IOnMounted`,
`IOnBeforeUpdate`, `IOnUpdated`, `IOnBeforeUnmount`, `IOnUnmounted`, `IOnActivated`,
`IOnDeactivated`, `IOnErrorCaptured` — plus eight `*Async` twins (everything except
`IOnBeforeUpdate`/`IOnUpdated`/`IOnErrorCaptured`). Implement only what you need; implementing
both the sync and async form of the same hook is pointless.

Things that bite:

- Hooks follow the **element's** mount cycle, not "once per ViewModel object". Remounting the
  same instance behind an `<aq:If>` fires `IOnBeforeCreate`/`IOnCreated`/`IOnBeforeMount` again.
- WPF raises one `Unloaded`, so `IOnBeforeUnmount`/`IOnUnmounted` fire back to back.
- `IOnUpdated` is genuinely batched through `NextTick` — several synchronous property changes
  produce exactly one call. It fires for *any* property on the ViewModel.
- `IOnErrorCaptured` returns `true` to mark handled, matching WPF's own convention.
- `*Async` hooks are fired but never awaited; a faulted one rethrows through the element's
  `Dispatcher`, which is how it reaches `IOnErrorCaptured`.
- **`Loaded` can fire twice with no `Unloaded` in between** in WPF. When writing lifecycle-ish
  code by hand, guard both edges and always re-hook whatever you unhook.

---

## Directives

| Attached property | Purpose |
| --- | --- |
| `Directives.Model` | Two-way binding sugar. **Must be a `Binding`.** Maps to `TextBox.Text`, `ToggleButton.IsChecked`, `Selector.SelectedItem`, `RangeBase.Value`. `PasswordBox` and anything else **throw**. |
| `Directives.Show` | `Visibility.Visible`/`Collapsed`. Stays mounted. |
| `Directives.Event` + `Command` + `CommandParameter` | Hook any public CLR event by name via reflection. Parameter defaults to the raised `EventArgs`. Auto-unhooked on `Unloaded`. |
| `Directives.Modifiers` | Comma-separated: `stop`/`prevent` (both set `Handled`), `once`, `self`, `left`/`right`/`middle`, `enter`/`tab`/`esc`/`space`/`up`/`down`/`left`/`right`/`delete`. Each is a no-op unless the `EventArgs` type matches. |
| `Directives.Use` + `UseValue` + `Argument` | Attach a custom `Directive` (`Mounted`/`Updated`/`Unmounted`). One per element. |
| `Directives.Class` | Space-separated `Style` resource keys, layered via `SetValue` (WPF allows only one `Style`, CSS classes must stack). Later tokens win; deactivated tokens revert. |
| `Directives.Style` | `IDictionary<string, object>` — DP resolved by name on the element type; an unknown name throws. |

There is no `capture` modifier — WPF's `Preview{EventName}` tunneling event *is* the capture
phase, so write `Directives.Event="PreviewMouseDown"`.

---

## Structural: If, Expr, Suspense

```xml
<aq:If Condition="{Binding IsOpen}">
    <TextBlock Text="Really detached when false" />
    <aq:If.Else>
        <TextBlock Text="else branch" />   <!-- nest another If for else-if -->
    </aq:If.Else>
</aq:If>
```

`If` genuinely detaches from the visual tree (real `IOnUnmounted`), unlike `Directives.Show`.

`Expr` gives XAML the expression language `{Binding}` lacks:

```xml
<aq:If Condition="{aq:Expr 'Count > 0 &amp;&amp; Total &lt;= Budget'}" />
<Border aq:Directives.Show="{aq:Expr 'SelectedOrder != null'}" />
```

Grammar: `> >= < <= == != && || ! + - * /`, parentheses, right-associative `? :`, over number /
string / `true` / `false` / `null` literals and identifiers. Arithmetic is `double`. `==`/`!=`
work over **any** object (via `object.Equals`), so null checks and object comparisons are fine;
`> >= < <=` need numbers or two same-type `IComparable`s. A `#`-prefixed identifier
(`#ElementName.Path`) binds by `ElementName`. `Expr.ThrowOnUnresolvedIdentifiers = true` at
startup turns a typo'd property from a silent `UnsetValue` into a named exception.

`Suspense` swaps `Fallback` for `Child` on `IsPending` — pair it with `IOnMountedAsync` setting
an `IsLoading` flag.

---

## Teleport, Transition, Provide/Inject, Slots, KeepAlive

See [`patterns.md`](patterns.md) for the full mechanics of each, including:

- `TeleportHost.RegisterHost` (Panels only) + `Teleport.To`/`Order`/`Disabled`.
- `Transition.Enter`/`Leave` storyboards and `TransitionGroup.Enter` for item containers.
- `Provide.Key`/`Provide.Value` (writes into `Resources`) and `{aq:Inject Key}` /
  `Inject.Get<T>`.
- `SlotHost`/`Slot`/`{aq:SlotContent name}`/`{aq:SlotProvided name}` for free-form named slots.
- `Component.Is` + `KeepAlive` (`Max`/`Include`/`Exclude`), and why `TabControl` and
  `Frame`/`Page` are **not** keep-alive despite appearances.

Two rules worth carrying without opening that file:

- **`<aq:KeepAlive>` around an `<aqr:RouterView>` is not supported.** The `Router` owns routed
  View lifetimes so guards run against the mounted instance.
- **`KeepAlive` owns its children's `Visibility`** — never also put `Directives.Show` on a
  cached View.

---

## Adding a View + ViewModel pair

Follow the sample's convention: one `XyzView.xaml` + `XyzView.xaml.cs` (nothing but
`InitializeComponent()`) + `XyzViewModel.cs`, fully self-contained, in the same folder.

```bash
dotnet new aq-view -n Dashboard --namespace MyApp.Features.Dashboard
```

The ViewModel is an `ObservableObject` (`partial`, so `[ObservableProperty]`/`[RelayCommand]`
generators work), holds its own `Ref`/`Computed`, and implements only the lifecycle interfaces
it uses. Register it in DI only when it has real constructor dependencies —
`Setup`'s `Activator` fallback already covers parameterless ones.
