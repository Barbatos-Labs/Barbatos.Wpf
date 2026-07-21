# Barbatos.Wpf.Aquarius API Reference

This document provides a comprehensive reference for the Barbatos.Wpf.Aquarius library,
modeled after the official .NET API documentation.

## Namespaces

| Namespace | Description |
|-----------|-------------|
| **[`Barbatos.Wpf.Reactivity`](#barbatoswpfreactivity-namespace)** | `Ref<T>`, `Computed<T>`, `Watch`, `NextTick` - thin sugar over CommunityToolkit.Mvvm. |
| **[`Barbatos.Wpf.Composition`](#barbatoswpfcomposition-namespace)** | `Lifecycle` and its nine hook interfaces, plus `Provide`/`Inject`. |
| **[`Barbatos.Wpf.Xaml`](#barbatoswpfxaml-namespace)** | The `Directives` attached-property family, `If`, `Suspense`, custom-directive extensibility (`Directive`), `Comparisons`, `BuildConfiguration`, `Teleport`, `TeleportHost`, and the free-form-named-slots family (`Slot`/`SlotHost`/`SlotContent`/`SlotProvided`). |
| **[`Barbatos.Wpf.Animation`](#barbatoswpfanimation-namespace)** | `Transition`, `TransitionGroup`. |

All four map onto the single `aq:` XAML namespace - see
[Quick Start](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Aquarius/README.md#quick-start)
in the README.

---

## `Barbatos.Wpf.Reactivity` Namespace

### `Ref<T>` Class

```csharp
public class Ref<T> : ObservableObject
```

- **`Ref()`** - initializes to `default(T)`.
- **`Ref(T value)`**
- **`Value`** (`T`, get/set) - raises `PropertyChanged` only when the new value actually
  differs (default equality comparer).

### `Computed<T>` Class

```csharp
public sealed class Computed<T> : ObservableObject, IDisposable
```

- **`static From(Func<T> getter, params INotifyPropertyChanged[] dependencies)`** →
  `Computed<T>` - read-only form.
- **`static From(Func<T> getter, Action<T> setter, params INotifyPropertyChanged[] dependencies)`**
  → `Computed<T>` - writable form; `setter` is expected to mutate one of `dependencies`,
  `Value` then reflects the result through that dependency's own change notification.
- **`Value`** (`T`, get/set) - the cached, most recently computed value. The setter calls
  the writable-form's `setter` if present; otherwise throws `InvalidOperationException`.
- **`Dispose()`** - stops tracking every dependency.

### `Watch` Static Class

- **`On<T>(Ref<T> source, Action<T, T> onChanged, bool immediate = false, bool once = false, bool deep = false, WatchFlush flush = WatchFlush.Sync)`**
  → `IDisposable`
- **`On<T>(Ref<T> source, Action<T, T, Action<Action>> onChanged, bool immediate = false, bool once = false, bool deep = false, WatchFlush flush = WatchFlush.Sync)`**
  → `IDisposable` - the 3rd callback parameter is an `onCleanup` registrar.
- **`Effect(Action effect, params INotifyPropertyChanged[] dependencies)`** → `IDisposable`
- **`Effect(Action effect, bool once, WatchFlush flush, params INotifyPropertyChanged[] dependencies)`**
  → `IDisposable`

### `WatchFlush` Enum

- **`Sync`** (default) - invoke the callback immediately, synchronously.
- **`Post`** - coalesce rapid-fire changes and invoke the callback once via `NextTick`.

### `NextTick` Static Class

- **`Run(Action callback)`** → `DispatcherOperation`
- **`RunAsync(Action? callback = null)`** → `Task`

Both schedule via `Dispatcher.BeginInvoke(DispatcherPriority.Background, ...)`.

---

## `Barbatos.Wpf.Composition` Namespace

### `Lifecycle` Static Class

- **`EnableProperty`** (`DependencyProperty`, `bool`) - `SetEnable`/`GetEnable`. Turns
  lifecycle-hook dispatch on/off for a `FrameworkElement`, checking its `DataContext`
  against every interface below.

### Lifecycle Hook Interfaces

| Interface | Member |
|-----------|--------|
| `IOnBeforeMount` | `void OnBeforeMount()` |
| `IOnMounted` | `void OnMounted()` |
| `IOnBeforeUpdate` | `void OnBeforeUpdate()` |
| `IOnUpdated` | `void OnUpdated()` |
| `IOnBeforeUnmount` | `void OnBeforeUnmount()` |
| `IOnUnmounted` | `void OnUnmounted()` |
| `IOnActivated` | `void OnActivated()` |
| `IOnDeactivated` | `void OnDeactivated()` |
| `IOnErrorCaptured` | `bool OnErrorCaptured(Exception exception)` - return `true` to mark handled. |

See [Lifecycle Hooks](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Aquarius/README.md#lifecycle-hooks)
in the README for exactly what triggers each one.

### `Provide` Static Class

- **`KeyProperty`** (`DependencyProperty`, `object`) - `SetKey`/`GetKey`.
- **`ValueProperty`** (`DependencyProperty`, `object`) - `SetValue`/`GetValue`. Stores
  into `FrameworkElement.Resources[Key]`; throws `InvalidOperationException` if set on a
  non-`FrameworkElement`.

### `Inject` Class

```csharp
[MarkupExtensionReturnType(typeof(object))]
public class Inject : MarkupExtension
```

- **`Inject()`** / **`Inject(object key)`**
- **`Key`** (`object?`) - `[ConstructorArgument("key")]`, supports the positional
  `{aq:Inject SomeKey}` form.
- **`ProvideValue(IServiceProvider serviceProvider)`** → `object?` - delegates to
  `new DynamicResourceExtension(Key).ProvideValue(serviceProvider)`.
- **`static Get<T>(FrameworkElement from, object key, T? fallback = default)`** → `T?` -
  synchronous C#-side equivalent, via `TryFindResource`.

---

## `Barbatos.Wpf.Xaml` Namespace

### `Directives` Static Class

Attached-property family (`public static partial class`, split across several files by
concern).

| Property | Type | Purpose |
|----------|------|---------|
| `Model` | `object` (must be a `Binding`) | v-model - see below. |
| `Show` | `bool` | v-show - `Visibility.Visible`/`Collapsed`. |
| `Event` | `string` | Names the CLR event to hook via reflection. |
| `Command` | `ICommand` | Executed when `Event` fires. |
| `CommandParameter` | `object` | Defaults to the raised `EventArgs`. |
| `Use` | `Directive` | Attaches a custom directive instance. |
| `UseValue` | `object` | Bound value passed to the directive as `DirectiveBinding.Value`/`OldValue`. |
| `Argument` | `string` | Passed as `DirectiveBinding.Argument`. |
| `Modifiers` | `string` (comma-separated) | Parsed into `DirectiveBinding.Modifiers`; also read by `Event`'s modifier handling. |
| `Class` | `string` (space-separated) | v-bind:class string form - merges named `Style` setters. |
| `Style` | `IDictionary<string, object>` | v-bind:style object form - sets DPs by name directly. |

#### `Directives.Model`

Resolves the real two-way-bindable property by element type:

| Element type | Bound property |
|---|---|
| `TextBox` | `TextProperty` |
| `ToggleButton` (`CheckBox`/`RadioButton`) | `IsCheckedProperty` |
| `Selector` (`ComboBox`/`ListBox`) | `SelectedItemProperty` |
| `RangeBase` (`Slider`) | `ValueProperty` |
| `PasswordBox` | throws `InvalidOperationException` (no DP for `Password`) |
| anything else | throws `InvalidOperationException` |

#### `Directives.Event` Modifiers

See the [Directives.Event](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Aquarius/README.md#directivesevent-v-on)
table in the README for the full modifier list (`stop`/`prevent`/`once`/`self`/mouse-
button aliases/key aliases).

### `Directive` Class

```csharp
public abstract class Directive
{
    public virtual void Mounted(FrameworkElement element, DirectiveBinding binding);
    public virtual void Updated(FrameworkElement element, DirectiveBinding binding);
    public virtual void Unmounted(FrameworkElement element, DirectiveBinding binding);
}
```

### `DirectiveBinding` Class

```csharp
public sealed class DirectiveBinding
{
    public object? Value { get; init; }
    public object? OldValue { get; init; }
    public string? Argument { get; init; }
    public IReadOnlySet<string> Modifiers { get; init; }
}
```

### `If` Class

```csharp
[ContentProperty(nameof(Child))]
public class If : ContentControl
```

- **`Child`** (`object?`) - the XAML content property.
- **`Condition`** (`bool`, default `true`) - `true` shows `Child` via `Content`; `false`
  shows `Else` (or `null` if unset), detaching whichever isn't showing from the visual tree.
- **`Else`** (`object?`, default `null`) - shown while `Condition` is `false`; nest another
  `If` here for an else-if chain.

### `Expr` Class

```csharp
[MarkupExtensionReturnType(typeof(object))]
public class Expr : MarkupExtension
```

- **`Expr()`** / **`Expr(string expression)`** (positional, `{aq:Expr 'a > b'}`)
- **`Expression`** (`string?`, `[ConstructorArgument("expression")]`)
- **`ProvideValue(IServiceProvider)`** → `object` - parses `Expression` and returns a
  `MultiBinding` (one child `Binding` per distinct identifier referenced) wired to an
  internal evaluator converter.
- **`static Evaluate(string expression, object? source)`** → `object?` - synchronous,
  non-reactive evaluation against `source` via reflection (identifiers resolve as ordinary
  C# property names, case-sensitive, dotted paths supported); throws
  `InvalidOperationException` for a syntax error, a property that doesn't exist on the
  current type, or a `#`-prefixed (element-referenced) identifier (no visual tree to
  resolve it against here), but propagates an existing-but-null property as `null`
  (ordinary null propagation, not an error).

Grammar: `>` `>=` `<` `<=` `==` `!=` `&&` `||` `!` `+` `-` `*` `/` `? :` (ternary,
right-associative) and parentheses, over number/string/`true`/`false` literals and
identifiers, all arithmetic evaluated as `double`. An identifier prefixed with `#`
(`#ElementName.Path`) resolves via `Binding.ElementName` in the reactive path instead of
`DataContext`. See the [README](README.md#expr---conditional-expressions) for the full
grammar table, enum-comparison rules, element-reference details, and XAML quoting notes.

### `Suspense` Class

```csharp
[ContentProperty(nameof(Child))]
public class Suspense : ContentControl
```

- **`Child`** (`object?`) - the XAML content property; shown while `IsPending` is `false`.
- **`Fallback`** (`object?`) - shown while `IsPending` is `true`.
- **`IsPending`** (`bool`, default `false`)

### `Comparisons` Static Class

- **`Not`** (`IValueConverter`) - negates a `bool`; supports two-way binding.
- **`IsNull`** (`IValueConverter`) - `value is null`.
- **`IsEqualTo`** (`IValueConverter`) - `Equals(value, parameter)`.

Use via `Converter={x:Static aq:Comparisons.Not}` etc.

### `BuildConfiguration` Static Class

- **`IsDebug`** (`bool`) - whether the running application's entry assembly was built Debug
  (JIT optimizations disabled); `false` for Release or if there is no entry assembly (e.g. the
  XAML designer). Use via `{x:Static aq:BuildConfiguration.IsDebug}`, composed with
  `Directives.Show`/`If`/`Suspense`.
- **`static IsAssemblyDebugBuild(Assembly? assembly)`** → `bool` - the same check against a
  specific assembly instead of the entry one.

### `TeleportHost` Static Class

- **`RegisterHostProperty`** (`DependencyProperty`, `string`) - `SetRegisterHost`/
  `GetRegisterHost`, settable on any `Panel`.
- **`static event Action<string>? HostRegistered`**
- **`static event Action<string>? HostUnregistered`** - raised when the currently-registered
  host under a name unregisters (its `Unloaded` fired, or another value replaced its
  `RegisterHostProperty`); lets a `Teleport` rendered there bring its content home instead
  of leaving it orphaned.
- **`static TryGetHost(string name, out Panel panel)`** → `bool`

### `Teleport` Class

```csharp
public class Teleport : ContentControl
```

- **`To`** (`string?`) - the registered `TeleportHost` name to render into.
- **`Disabled`** (`bool`, default `false`) - when `true`, content renders locally instead
  of teleporting.

### `Slot` Class

```csharp
[ContentProperty(nameof(Content))]
public sealed class Slot
```

- **`Name`** (`string`, default `""`) - `""` targets the default slot.
- **`Content`** (`object?`) - the XAML content property.

Plain CLR class, not a `DependencyObject` - never enters the visual tree itself, only
`Content` does once resolved. Used inside a `SlotHost`'s `Items`.

### `SlotHost` Class

```csharp
[ContentProperty(nameof(Items))]
public class SlotHost : Control
```

- **`Items`** (`ObservableCollection<object>`) - the XAML content property; a flat mix of
  `Slot`-wrapped (named) and plain (implicit default) children.
- **`ResolvedSlots`** (`IReadOnlyDictionary<string, object?>`, read-only DP) - recomputed
  from `Items` on every add/remove/replace/clear.
- **`protected GetSlotContent(string name = "")`** → `object?` - the C#-callable
  counterpart of `SlotContent`, for use by a derived component's own code.
- **`protected IsSlotProvided(string name = "")`** → `bool` - the C#-callable counterpart
  of `SlotProvided`.

Base class for a custom control that wants free-form named slots - a component author
derives from this (e.g. `Card : SlotHost`) and writes their own `ControlTemplate` using
`SlotContent`/`SlotProvided`. Two items claiming the same slot name throws
`InvalidOperationException`.

### `SlotContent` Class

```csharp
[MarkupExtensionReturnType(typeof(object))]
public class SlotContent : MarkupExtension
```

- **`SlotContent()`** / **`SlotContent(string name)`** (positional, `{aq:SlotContent header}`)
- **`Name`** (`string`, default `""`, `[ConstructorArgument("name")]`)
- **`Fallback`** (`object?`) - a simple/primitive substitute when the slot was never
  provided at all (a provided-but-empty slot does not fall back).

`ProvideValue` returns a real, deferred `Binding` (`RelativeSource=TemplatedParent`,
`Path=ResolvedSlots`) with a lookup converter - used inside a `SlotHost`-derived control's
own `ControlTemplate` to project a named slot's content back out.

### `SlotProvided` Class

```csharp
[MarkupExtensionReturnType(typeof(bool))]
public class SlotProvided : MarkupExtension
```

- **`SlotProvided()`** / **`SlotProvided(string name)`** (positional, `{aq:SlotProvided header}`)
- **`Name`** (`string`, default `""`, `[ConstructorArgument("name")]`)

Whether `Name` is a present key in the enclosing `SlotHost`'s `ResolvedSlots` - typically
composed with `If.Condition` to skip a wrapper element entirely when a slot wasn't used.
See the [README](README.md#slots) for the full mechanism and a worked `Card` example.

---

## `Barbatos.Wpf.Animation` Namespace

### `Transition` Class

```csharp
[ContentProperty(nameof(Child))]
public class Transition : ContentControl
```

- **`Child`** (`object?`) - the XAML content property.
- **`Show`** (`bool`, default `true`)
- **`Enter`** (`Storyboard?`) - played (cloned) after mounting, on a `false`→`true` toggle.
- **`Leave`** (`Storyboard?`) - played (cloned) before unmounting; content stays mounted
  until it completes. Without `Leave` set, behaves exactly like `If` - immediate,
  unanimated detach.

### `TransitionGroup` Static Class

- **`EnterProperty`** (`DependencyProperty`, `Storyboard`) - `SetEnter`/`GetEnter`,
  settable on any `ItemsControl`. Every newly-generated item container gets a one-time
  `Loaded` hook that plays a clone of `Enter`. No `Leave`/move-animation support - see the
  [Transition / TransitionGroup](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Aquarius/README.md#transition--transitiongroup)
  section in the README for the scope note.
