# Aquarius composition patterns

Details for the pieces the SKILL only names. Prose:
[`src/Barbatos.Wpf.Aquarius/README.md`](../../src/Barbatos.Wpf.Aquarius/README.md).

---

## Teleport — render somewhere else in the tree

```xml
<!-- once, typically at the window root -->
<Grid aq:TeleportHost.RegisterHost="Overlay" Panel.ZIndex="100" />

<!-- anywhere, however deeply nested -->
<aq:Teleport To="Overlay" Order="10" Disabled="{Binding IsCompact}">
    <views:ToastView />
</aq:Teleport>
```

- A host is **any `Panel`** (`Grid`, `StackPanel`, `DockPanel`, `UniformGrid`, `WrapPanel`,
  `Canvas`). Anything else throws.
- The content keeps object identity across the move — its `DataContext`, bindings, and
  `Lifecycle` hooks keep working. It is the same component, rendered elsewhere.
- A `Teleport` that mounts before its host still finds it once the host registers.
- Host names are app-wide like a DOM `id`; the most recently loaded duplicate wins.
- **`Order` (lower first, default `0`) is what makes multiple teleports deterministic.**
  Without it, children land in load order, which depends on where each `Teleport` is declared
  and whether its tab has been opened. Ties break by declaration order.
- **Layout attached properties go on the content, not the `Teleport`** — the content becomes
  the panel's real child, so `DockPanel.Dock` / `Grid.Row` belong there.
- A host may keep static children of its own; teleported children are ordered among themselves
  and placed after them.
- Changing `Order` at runtime is a real remove-then-insert (`UIElementCollection` has no move),
  so that one control unmounts and remounts. Siblings are untouched.

This is also how dockable panels are built — no separate API.

---

## Transition / TransitionGroup

**No canned animations ship.** `Transition` only orchestrates *when* your own `Storyboard`s run.

```xml
<aq:Transition Show="{Binding IsOpen}"
               Enter="{StaticResource FadeIn}"
               Leave="{StaticResource FadeOut}">
    <TextBlock Text="Now you see me" />
</aq:Transition>
```

- `Show` → `false`: plays `Leave`, detaching only once it completes.
- `Show` → `true`: restores content immediately (so `Loaded`/`IOnMounted` fire), then plays
  `Enter`.
- **`Enter` does not play on first render** — only on a later `false`→`true` toggle.
- Without `Leave`, behaves exactly like `If`.
- Storyboards are cloned per play, so one shared `Enter` resource across several `Transition`s
  does not cross-trigger `Completed`.
- A re-toggle mid-`Leave` stops the in-flight animation cleanly.

```xml
<ListBox ItemsSource="{Binding Items}"
         VirtualizingPanel.IsVirtualizing="False"
         aq:TransitionGroup.Enter="{StaticResource FadeIn}" />
```

`TransitionGroup` is **enter-only**. A virtualizing panel (the `ListBox`/`ListView` default)
may satisfy a new item with a recycled container, which does not re-fire `Loaded` — turn
virtualization off on lists small enough to afford it if every item must animate. Leave and
reflow animations would require owning a custom panel and are deliberately out of scope.

---

## Provide / Inject

```xml
<Grid aq:Provide.Key="ThemeColor" aq:Provide.Value="{Binding AccentBrush}">
    <TextBlock Foreground="{aq:Inject ThemeColor}" />
</Grid>
```

Built on `FrameworkElement.Resources` + `FindResource`, so it inherits WPF's own logical-tree
walk and override-by-nesting for free — a nearer `Provide` correctly shadows a farther one.
`{aq:Inject}` delegates to `DynamicResourceExtension`, so a later `Provide.Value` change is
picked up live. From C#: `Inject.Get<T>(element, key, fallback)`.

Collision-proof a key with an `object` sentinel instead of a string:

```csharp
public static class Keys { public static readonly object ThemeColor = new(); }
```

```xml
<Grid aq:Provide.Key="{x:Static local:Keys.ThemeColor}" ... />
```

Reactivity crosses the boundary for free — provide a `Ref<T>` or `ObservableObject` and
bindings against it stay live.

**Scope discipline:** `Provide`/`Inject` is for values scoped to a *visual subtree*. A value a
ViewModel needs for its whole lifetime belongs in constructor injection through the DI
container instead.

---

## Slots (free-form named content)

Author derives from `SlotHost` and writes a `ControlTemplate`:

```xml
<ControlTemplate TargetType="{x:Type local:Card}">
    <Border BorderBrush="Gray" BorderThickness="1" CornerRadius="4">
        <StackPanel>
            <aq:If Condition="{aq:SlotProvided header}">
                <Border Background="#F0F0F0" Padding="8">
                    <ContentPresenter Content="{aq:SlotContent header}" />
                </Border>
            </aq:If>
            <aq:If Condition="{aq:SlotProvided}">
                <Border Padding="8"><ContentPresenter Content="{aq:SlotContent}" /></Border>
            </aq:If>
        </StackPanel>
    </Border>
</ControlTemplate>
```

Consumer writes a flat mix; anything not wrapped in `aq:Slot` is the default slot:

```xml
<local:Card>
    <aq:Slot Name="header"><TextBlock Text="Title" FontWeight="Bold" /></aq:Slot>
    <TextBlock Text="Body copy." TextWrapping="Wrap" />
</local:Card>
```

- Two items claiming the same slot name (including two unwrapped defaults) **throws**.
- Each slot holds exactly **one** content object — the same rule as `If.Child`,
  `Suspense.Child`, and `Teleport`'s content.
- A provided-but-empty slot still counts as provided; only a genuinely absent name falls back.
- `SlotContent.Fallback` is for a simple substitute value; use `SlotProvided` + `If` when the
  wrapper element itself should disappear.
- Adding/removing a `Slot` in `Items` at runtime updates the display. Mutating an already-added
  `Slot`'s `Name`/`Content` in place does **not** — replace the `Slot` object.
- Scoped slots for a single slot need no new syntax: the author sets `.DataContext` on the
  content object. The repeated/list case is `ItemsControl.ItemTemplate`, by design.

`SlotHost` also exposes `protected GetSlotContent(name)` / `IsSlotProvided(name)` for a derived
control's own C#.

---

## Component + KeepAlive

```xml
<aq:KeepAlive Max="3" Exclude="ReportView">
    <aq:Component Is="{Binding CurrentView.Value}" />
</aq:KeepAlive>
```

```csharp
public Ref<Type?> CurrentView { get; } = new(typeof(ProfileView));
```

`Component.Is` resolves the View through `Setup.Resolve` (container first), so a
constructor-injected View comes from your own container and its own `aq:Setup.Enable` resolves
its `DataContext` as usual. Register such Views **transient** — a singleton would silently hand
back an evicted instance, defeating the point.

**Used alone, `Component` destroys the outgoing View** (and its ViewModel becomes collectable).
`KeepAlive` is the wrapper that opts out of that: switching then produces only
`IOnDeactivated`/`IOnActivated`, never an unmount, so scroll position, half-filled forms, and
loaded results survive.

### What is genuinely keep-alive — verified by running it

| Pattern | Keep-alive? |
| --- | --- |
| `aq:KeepAlive` around `aq:Component` | **Yes**, and lazy — a View never visited is never built |
| Siblings toggled with `Directives.Show`, all mounted up front | Yes, but by hand: everything mounts whether visited or not, no cap, no include/exclude |
| Plain `TabControl` | **No** — switching away genuinely unloads; switching back is a fresh mount |
| `Frame`/`Page`, with or without `Page.KeepAlive` | **No** — confirmed identical unmount/remount either way |

Do not "optimize" a `KeepAlive` into a `TabControl`.

### Cache control

`Max` (LRU cap, `0` = unlimited, never evicts the visible one), `Include` / `Exclude`
(comma-separated **type names**, `Exclude` checked after `Include`). Runtime edits apply
retroactively to what is already cached — except the View on screen, which is marked and
dropped on the next switch.

Inspection/teardown API: `ActiveView`, `CachedTypes` (snapshot, LRU-first, safe to iterate
while releasing), `IsCacheable`, `TryGetCached`, `Release(type)`, `ReleaseAll()`.

### What still unmounts

An evicted View, a filtered-out View, and **every cached View when the `KeepAlive` control
itself unloads**. That last one is a WPF fact: `Loaded`/`Unloaded` broadcast through a subtree
regardless of `Visibility`, so a `KeepAlive` inside a `TabItem` or an `If` gets a real
unmount/remount cycle whenever that container comes and goes. ViewModel *instances* survive
(their `DataContext` is never touched) — the hooks simply run again.

### Two hard rules

- **`KeepAlive` owns its children's `Visibility`** (written as a local value). Never also put
  `Directives.Show` on a cached View — they will fight.
- **Adding children to `KeepAlive.Children` directly throws.** They would have no cache entry
  and no eviction position.
- **`<aq:KeepAlive>` around `<aqr:RouterView>` is not supported.** Vue Router has no per-route
  keep-alive option either; AquariusRouter's `Router` owns routed View lifetimes so guards run
  against the mounted instance.
