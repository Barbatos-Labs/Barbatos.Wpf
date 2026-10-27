# Aquarius composition patterns

The mechanics behind the pieces [SKILL.md](SKILL.md) only summarizes. Prose docs:
[Teleport](../../../src/Barbatos.Wpf.Aquarius/README.md#teleport),
[Dockable Panels](../../../src/Barbatos.Wpf.Aquarius/README.md#dockable-panels),
[Transition](../../../src/Barbatos.Wpf.Aquarius/README.md#transition--transitiongroup),
[Provide / Inject](../../../src/Barbatos.Wpf.Aquarius/README.md#provide--inject),
[Slots](../../../src/Barbatos.Wpf.Aquarius/README.md#slots),
[KeepAlive](../../../src/Barbatos.Wpf.Aquarius/README.md#keepalive).

Contents:

1. [Teleport](#teleport) - overlays, several Teleports per host, dockable panels
2. [Transition and TransitionGroup](#transition-and-transitiongroup)
3. [Provide and Inject](#provide-and-inject)
4. [Slots](#slots)
5. [Component and KeepAlive](#component-and-keepalive) - the verified keep-alive table, cache
   control, what still unmounts, the hard rules

## Teleport

Sample: `MainWindow.xaml` (the `Overlay` host), `TeleportDemoView.xaml` + `.xaml.cs`,
`DockableToolDialog.xaml`, `ToastView.xaml`. Tests: `TeleportTests.cs`,
`TeleportMultiHostTests.cs`, `LifecycleLeakTests.cs`.

```xml
<!-- once, near the window root (MainWindow.xaml), declared last (or given a high
     Panel.ZIndex) so it paints above the content it overlays -->
<Grid Grid.RowSpan="2" aq:TeleportHost.RegisterHost="Overlay" />

<!-- anywhere, however deeply nested or clipped (TeleportDemoView.xaml) -->
<aq:If Condition="{Binding ShowToast}">
    <aq:Teleport To="Overlay">
        <local:ToastView DataContext="{Binding}" />
    </aq:Teleport>
</aq:If>
```

- **Host**: any `Panel` (`Grid`, `StackPanel`, `DockPanel`, `UniformGrid`, `WrapPanel`, `Canvas`);
  anything else throws `InvalidOperationException`. It registers on `Loaded` and unregisters on
  `Unloaded` in a registry kept per UI thread - names are unique like a DOM `id`, the most recently
  loaded duplicate wins, and a `Teleport` never sees a host on another thread (a window on its own
  thread has its own names). `TeleportHost.TryGetHost(name, out panel)` (calling thread only), and
  the static events `TeleportHost.HostRegistered`/`HostUnregistered` (process-wide, raised on the
  panel's thread - check `CheckAccess()` in a handler), expose it.
- A `Teleport` that loads before its host picks the host up when it registers. `Disabled="True"`
  keeps the content local; unloading the `Teleport` (an enclosing `If` going false) brings the
  content home first.
- **Content is one `UIElement`** (`Content is UIElement`). A data object rendered through a
  `DataTemplate` is never moved - it silently stays local.
- **Object identity survives a move; the lifecycle does not.** Verified sequences for a
  `Lifecycle.Enable`'d content:
  - First settle into an already-registered host (the normal declarative case):
    `OnBeforeCreate, OnCreated, OnBeforeMount, OnMounted, OnActivated, OnBeforeUnmount,
    OnDeactivated, OnUnmounted, OnBeforeCreate, OnCreated, OnBeforeMount, OnMounted, OnActivated`.
  - Retargeting `To` after settling: `OnDeactivated, OnActivated, OnBeforeUnmount, OnDeactivated,
    OnUnmounted, OnBeforeCreate, OnCreated, OnBeforeMount, OnMounted, OnActivated`.

  WPF has no pre-commit step like Vue's virtual DOM, so a re-parent is a real detach + attach. A
  teleported ViewModel's `OnMounted` must be idempotent.
- **`DataContext` does not travel.** The content inherits from its new ancestors: in the sample the
  `Overlay` host sits under `MainWindow`, whose `DataContext` is `MainViewModel`, not the tab's
  ViewModel. Give teleported content a `DataContext` that is right wherever it lands - its own
  `aq:Setup`, an explicit value (the sample writes `DataContext="{Binding}"` on `ToastView`), or a
  host ancestor that carries the right object. `TeleportTests` pins both sides
  (`ContentBoundViaInheritedDataContextKeepsWorkingAcrossWindowsWhenBothSetIt` /
  `...LosesItsValueIfTheNewWindowDoesNotSetDataContext`).

### Several Teleports, one host

```xml
<UniformGrid Rows="1" aq:TeleportHost.RegisterHost="Toolbar" />
<!-- declared anywhere, in any order -->
<aq:Teleport To="Toolbar" Order="10"><views:SaveButton /></aq:Teleport>
<aq:Teleport To="Toolbar" Order="0"><views:SearchBox /></aq:Teleport>
```

- `Order` (lower first, default `0`) is the only thing that decides placement - never load order,
  which depends on declaration position, host timing and which tabs were opened. Ties break by
  creation (declaration) order.
- Layout attached properties (`DockPanel.Dock`, `Grid.Row`) go on the **content**, which becomes the
  panel's real child - not on the `Teleport`.
- A host may keep static children of its own; teleported children stay contiguous after them.
- Changing `Order` at runtime is a `Children` remove-then-insert (`UIElementCollection` has no
  move): that one content unmounts and remounts, its siblings are untouched.

### Dockable panel (no new API)

One `Teleport`, retargeted between a docked host and a floating dialog's host
(`TeleportDemoView.xaml` + `.xaml.cs`, `DockableToolDialog.xaml`):

```xml
<Border BorderBrush="Gray" BorderThickness="1" MinHeight="70" Padding="4">
    <Grid aq:TeleportHost.RegisterHost="MainDock" />
</Border>
<aq:Teleport To="{Binding DockTarget.Value}">
    <Border Background="#E0F0FF" Padding="8">
        <TextBox aq:Directives.Model="{Binding DockableNotes}" />
    </Border>
</aq:Teleport>
```

```csharp
// ViewModel: intent only - it never touches a Window.
public Ref<string> DockTarget { get; } = new("MainDock");
[RelayCommand] private void Undock() => DockTarget.Value = "FloatingDock";
[RelayCommand] private void Redock() => DockTarget.Value = "MainDock";

// View code-behind, wired once the DataContext is the ViewModel (DataContextChanged, not Loaded -
// a TabControl raises Loaded before the tab is selected and again later):
Watch.On(viewModel.DockTarget, (target, _) =>
{
    if (target == "FloatingDock")
    {
        if (_dockableDialog is not null)
            return;

        _dockableDialog = new DockableToolDialog { Owner = Window.GetWindow(this), DataContext = viewModel };
        _dockableDialog.Closed += (_, _) =>
        {
            _dockableDialog = null;
            viewModel.DockTarget.Value = "MainDock";   // keep intent consistent after [X]
        };
        _dockableDialog.Show();
    }
    else
    {
        _dockableDialog?.Close();
    }
});
```

- The `aq:Teleport` element must live somewhere that outlives both states (the main View), never
  inside the dialog - content lives and dies with its `Teleport`.
- The dialog needs `DataContext = viewModel`: the moved `TextBox` binds against the dialog's tree.
- Closing the dialog with its own [X] brings the content home through `HostUnregistered`; `To` is
  left as is, so a host re-registering under that name later re-floats the content automatically.

## Transition and TransitionGroup

Sample: `MainWindow.xaml` (`FadeSlideIn`/`FadeSlideOut`/`LogEnter` storyboards). Tests:
`TransitionTests.cs`, `TransitionGroupTests.cs`.

```xml
<Window.Resources>
    <Storyboard x:Key="FadeSlideIn">
        <DoubleAnimation Storyboard.TargetProperty="Opacity" From="0" To="1" Duration="0:0:0.25" />
    </Storyboard>
    <Storyboard x:Key="FadeSlideOut">
        <DoubleAnimation Storyboard.TargetProperty="Opacity" From="1" To="0" Duration="0:0:0.25" />
    </Storyboard>
</Window.Resources>

<aq:Transition Show="{Binding ShowLifecycleDemo}"
               Enter="{StaticResource FadeSlideIn}"
               Leave="{StaticResource FadeSlideOut}">
    <local:LifecycleDemoView DataContext="{Binding LifecycleDemo}" />
</aq:Transition>
```

- **No canned animations ship**; `Transition` only decides when your `Storyboard`s run. They begin
  on the `Transition` control itself, so animate its own properties (`Opacity` above).
- `Show` -> `false` plays `Leave` and detaches the content only when it completes; without `Leave`
  it behaves exactly like `If`. `Show` -> `true` restores the content immediately (so `Loaded` and
  `IOnMounted` fire), then plays `Enter`.
- **`Enter` does not play on first render** (no Vue `appear`): the initial `Child` is shown
  directly. To animate in on load, start with `Show` false and flip it.
- Storyboards are cloned per play, so one shared resource across many `Transition`s does not
  cross-fire `Completed`.
- `Leave` is begun controllable and kept until the next show. `Show` -> `true` (mid-`Leave` or
  after it finished) starts `Enter`, then removes `Leave`: a re-toggle mid-`Leave` really stops it
  and keeps the content mounted, and whatever only `Leave` animated drops its held end value
  (`FillBehavior.HoldEnd`) - a `Leave` with no `Enter` re-shows its content at full opacity
  (`TransitionTests.cs`). An `Enter` animation with no `From` starts from the value `Leave`
  reached; with an explicit `From` it jumps there first.

```xml
<ListBox ItemsSource="{Binding ActivityLog}"
         VirtualizingPanel.IsVirtualizing="False"
         aq:TransitionGroup.Enter="{StaticResource LogEnter}" />
```

- `TransitionGroup.Enter` is set on an `ItemsControl` (anything else throws) and is **enter-only**;
  leave and reorder (FLIP) animations would need a custom panel and are out of scope.
- It plays on an item container's first `Loaded` only, then unhooks: a container loaded again
  later (the list's tab or `If` coming back) does not replay (`TransitionGroupTests.cs`). A
  virtualizing panel (the `ListBox`/`ListView` default) may reuse a recycled container for a new
  item, which then gets no animation - turn virtualization off on lists small enough to afford it.

## Provide and Inject

Sample: `MainWindow.xaml` (provider on the root `Grid`), `ProvideInjectDemoView.xaml`,
`ProvideInjectDemoViewModel.cs` (`Ref<Brush> ThemeColor`). Tests: `ProvideInjectTests.cs`.

```xml
<Grid aq:Provide.Key="ThemeColor"
      aq:Provide.Value="{Binding ProvideInjectDemo.ThemeColor.Value}">
    <!-- any depth below -->
    <Border Background="{aq:Inject ThemeColor}" />
</Grid>
```

- `Provide` stores `Value` in the element's `Resources` under `Key` (on a `FrameworkElement` only,
  else it throws); changing `Key` moves the entry. Lookup is WPF's own resource walk, so a nearer
  `Provide` shadows a farther one.
- `{aq:Inject ThemeColor}` (positional) or `{aq:Inject Key=ThemeColor}` is a `DynamicResource`
  underneath: live updates, and like any dynamic resource it targets a dependency property only.
  A key is required - `{aq:Inject}` alone throws "Inject requires a Key." when the XAML loads. From C#:
  `Inject.Get<T>(element, key, fallback)` - a one-time `TryFindResource`.
- A string key can collide with an unrelated resource of the same name (including one in
  `App.xaml`). Collision-proof with an object sentinel:

  ```csharp
  public static class Keys { public static readonly object ThemeColor = new(); }
  ```
  ```xml
  <Grid aq:Provide.Key="{x:Static local:Keys.ThemeColor}" ... />
  ```
- Provide a `Ref<T>` or an `ObservableObject` and bindings against it stay reactive.
- Scope discipline: `Provide`/`Inject` is for values scoped to a visual subtree. A value a
  ViewModel needs for its lifetime comes through constructor injection; state shared across
  screens lives in a store (`barbatos-apsu-store`).

## Slots

Sample: `Card.cs` (`public class Card : SlotHost`) and its `ControlTemplate` in
`SlotsDemoView.xaml`. Tests: `SlotHostTests.cs`, `SlotXamlTests.cs`.

```xml
<!-- author: a Style for the SlotHost subclass (SlotsDemoView.xaml, UserControl.Resources) -->
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

<!-- consumer: a flat mix; anything not wrapped in aq:Slot is the default slot -->
<local:Card>
    <aq:Slot Name="header"><TextBlock Text="Here might be a page title" FontWeight="Bold" /></aq:Slot>
    <TextBlock Text="The default slot - no wrapper needed." TextWrapping="Wrap" />
</local:Card>
```

- Derive from `SlotHost` (a `Control`): `SlotContent`/`SlotProvided` are `TemplatedParent` bindings
  to `SlotHost.ResolvedSlots`, so they resolve only inside that control's `ControlTemplate`.
- Two items claiming one slot name - including two unwrapped default items - throw
  `InvalidOperationException`. Each slot holds exactly one object; wrap siblings in a `Panel`.
- A provided-but-empty slot (`<aq:Slot Name="header" />`) counts as provided; only an absent name
  falls back. `{aq:SlotContent header, Fallback='Untitled'}` is for a simple substitute value; use
  `SlotProvided` + `If` when the wrapper element itself should disappear.
- Adding or removing a `Slot` in `Items` at runtime updates the display (a real unmount/remount of
  that content). Mutating an existing `Slot`'s `Name`/`Content` does not - replace the object.
- Scoped data for a single slot: set `DataContext` on the content object. The repeated-list case is
  `ItemsControl.ItemTemplate`, by design. In the author's C#: `protected GetSlotContent(name)` and
  `IsSlotProvided(name)`.

## Component and KeepAlive

Sample: `KeepAliveDemoView.xaml`, `KeepAliveDemoViewModel.cs`, `KeepAliveSectionViewModel.cs`,
`ProfileSectionView.xaml`, `WpfProgram.cs`. Tests: `KeepAliveTests.cs` (native patterns),
`KeepAliveControlTests.cs`, `KeepAliveXamlTests.cs`, `LifecycleLeakTests.cs`.

```xml
<aq:KeepAlive Max="{Binding Max}" Exclude="{Binding Exclude}" MinHeight="120">
    <aq:Component Is="{Binding CurrentView.Value}" />
</aq:KeepAlive>
```

```csharp
public Ref<Type?> CurrentView { get; } = new(typeof(ProfileSectionView));   // ViewModel names a type only

// WpfProgram.cs - transient, never singleton: an evicted View must come back fresh
builder.Services.AddTransient<ProfileSectionView>();
builder.Services.AddTransient<ProfileSectionViewModel>();
```

- `Component.Is` must name a `FrameworkElement` type (else `InvalidOperationException`); the View
  comes from `Setup.Resolve`, so a constructor-injected View works and its own `aq:Setup` resolves
  its `DataContext` as usual.
- **Used alone, `Component` destroys the outgoing View**; it and its ViewModel become collectable
  (`LifecycleLeakTests.SwitchingAComponentReleasesTheOldViewAndItsViewModel`). `KeepAlive` is the
  opt-out: switching then fires only `IOnDeactivated`/`IOnActivated`, so typed text, scroll
  position and loaded results survive. It is lazy (a View never shown is never built) and restores
  keyboard focus inside a View when you return to it.
- The cache is keyed by View **type**: one cached instance per type. Sections that must keep
  separate state need separate View types (the sample's three `*SectionView`s share one
  `SectionBody`).

### What is genuinely keep-alive - verified by running it

| Pattern | Keep-alive? |
| --- | --- |
| `aq:KeepAlive` around `aq:Component` | **Yes**, and lazy |
| Siblings toggled with `Directives.Show`, all mounted up front | Yes, by hand: everything mounts whether visited or not; no cap, no include/exclude |
| Plain `TabControl` | **No** - every tab loads up front, but switching away genuinely unloads; switching back is a fresh mount |
| `Frame`/`Page`, with or without `Page.KeepAlive` | **No** - identical unmount/remount either way; `Page.KeepAlive` governs journal retention |

Do not "simplify" a `KeepAlive` into a `TabControl`.

### Cache control

- `Max`: LRU cap counting the showing View; `0` (default) is unlimited; never evicts the View on
  screen. Eviction is a real unmount - coming back builds from scratch.
- `Include` / `Exclude`: comma-separated `Type.Name` values (no namespace), `Exclude` checked after
  `Include`. Edits apply retroactively to the cache, except the View on screen, which is dropped
  when you switch away.
- Inspection/teardown: `ActiveView`, `CachedTypes` (snapshot, least-recently-activated first, safe
  to iterate while releasing), `IsCacheable(type)`, `TryGetCached(type, out element)`,
  `Release(type)`, `ReleaseAll()`.

### What still unmounts

An evicted View, a filtered-out View, and **every cached View when the `KeepAlive` itself
unloads** - `Loaded`/`Unloaded` broadcast through a subtree regardless of `Visibility`, so a
`KeepAlive` inside a `TabItem` or an `If` gives its cached Views a real unmount/remount whenever
that container comes and goes. The View and ViewModel instances survive (their `DataContext` is
never touched); the hooks simply run again.

### Hard rules

- **`KeepAlive` owns its children's `Visibility`** (local values). Never put `Directives.Show` on a
  cached View - they fight.
- **Adding to `KeepAlive.Children` directly throws** - such a child would have no cache entry and no
  eviction position. Its content is exactly one `aq:Component`.
- **Register the Views it builds (and their ViewModels) transient.** A singleton hands back the
  evicted instance, state and all.
- **`aq:KeepAlive` around `aqr:RouterView` is not supported.** The router owns routed View
  lifetimes so guards run against the mounted instance (`barbatos-aquarius-router`).
