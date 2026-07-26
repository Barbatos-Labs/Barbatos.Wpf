# Barbatos.Wpf.AquariusRouter

![Barbatos.Wpf.AquariusRouter logo](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/build/aquariusrouter-logo.svg?raw=true)

### *A Vue Router-style navigation system - for WPF*

**Path matching with params/optional/repeatable segments/custom regex, nested routes and named
outlets via `RouterView`, navigation guards, redirect/alias, and `RouterLink` with live
active-state styling - built directly on [Barbatos.Wpf.Aquarius](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Aquarius/README.md).**

[![NuGet](https://img.shields.io/nuget/v/Barbatos.Wpf.AquariusRouter.svg)](https://www.nuget.org/packages/Barbatos.Wpf.AquariusRouter)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Barbatos.Wpf.AquariusRouter.svg)](https://www.nuget.org/packages/Barbatos.Wpf.AquariusRouter)
[![GitHub stars](https://img.shields.io/github/stars/Barbatos-Labs/Barbatos.Wpf?style=social)](https://github.com/Barbatos-Labs/Barbatos.Wpf/stargazers)
[![License](https://img.shields.io/github/license/Barbatos-Labs/Barbatos.Wpf)](https://github.com/Barbatos-Labs/Barbatos.Wpf/tree/main/LICENSE.md)

---

## 📖 Documentation Menu

* **[Getting Started](#getting-started)**
  * [Introduction](#introduction)
  * [Quick Start](#quick-start)
* **[The Route Table](#the-route-table)**
* **[The Router](#the-router)**
* **[RouterView](#routerview)**
  * [Named Outlets](#named-outlets)
  * [Nested Routes](#nested-routes)
* **[RouterLink](#routerlink)**
* **[Navigation Guards](#navigation-guards)**
  * [The Full Pipeline Order](#the-full-pipeline-order)
* **[Navigation Failures](#navigation-failures)**
* **[Path Matching](#path-matching)**
* **[History](#history)**
* **[Deliberately Out of Scope](#deliberately-out-of-scope)**
* **[Ecosystem](#ecosystem)**
* **[API Reference](#api-reference)**
* **[Community](#community)**

---

## Getting Started

### Introduction

#### What is Barbatos.Wpf.AquariusRouter?

AquariusRouter is a path-based navigation system for WPF: a route table maps path patterns to
Views, a `Router` resolves navigation requests and runs a full navigation-guard pipeline before
committing them, `RouterView` hosts whichever View is currently matched (including nested
outlets), and `RouterLink` turns any button/hyperlink into a navigation trigger with live
active-state tracking. It is the [Vue Router](https://router.vuejs.org/) counterpart to how
[Barbatos.Wpf.Aquarius](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Aquarius/README.md)
already ports Vue 3's Composition API - and it's built directly on top of Aquarius, the same way
`vue-router` itself has a hard dependency on `vue`.

> **Prerequisites**
>
> The rest of this document assumes you've already read
> [Aquarius's own README](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Aquarius/README.md) -
> AquariusRouter reuses `Ref<T>`, `Setup`, and `Directives.Event`/`Command` directly rather than
> reinventing equivalents.
>
> **Familiar with Vue Router?** Names and shapes below are deliberately similar (`RouteRecord`,
> `RouterView`, `RouterLink`, navigation guards) - a head start if you've used it before. Where
> this port genuinely diverges from Vue Router's real behavior, it's called out explicitly with
> the reason why, not left to guesswork.

A WPF desktop app has no browser, no URL bar, and no address to type into - so why a *real*
path matcher (`/users/:id`, custom regex, catch-alls) instead of something simpler like
enum-keyed navigation? Because a WPF app can realistically be registered as a custom URI-scheme
protocol handler (`myapp://users/42/edit`), and a real matcher with params/query/hash lets this
package parse and route an actual incoming protocol-activation URI, not just an in-process
navigation key. `Query`/`Hash`/`FullPath` stay on every resolved location for exactly this reason,
even though there's no address bar to display them in - registering the URI scheme with Windows
itself is an application concern, outside this package's own scope.

### Quick Start

```powershell
dotnet add package Barbatos.Wpf.AquariusRouter
```

Everything in this document lives behind a single XAML namespace:

```xml
<Window ...
        xmlns:aqr="http://schemas.barbatos.co/aquariusrouter/2026/xaml">
```

That one `aqr:` prefix reaches every namespace below (`Barbatos.Wpf.AquariusRouter.Matching`,
`.Routing`, `.Xaml`) - deliberately distinct from Aquarius's own `aq:` prefix, since both are
commonly used together in the same XAML file.

A minimal app:

```csharp
// Composition root, once at startup:
var router = new Router(
[
    new RouteRecord { Path = "/", Name = "home", View = typeof(HomeView) },
    new RouteRecord { Path = "/users/:id", Name = "user", View = typeof(UserView) },
]);
Router.Current = router;
Setup.ServiceProvider = services; // optional - only needed for DI-constructed Views/ViewModels

await router.Start("/");
```

```xml
<!-- MainWindow.xaml -->
<Window ... xmlns:aqr="http://schemas.barbatos.co/aquariusrouter/2026/xaml">
    <DockPanel>
        <StackPanel DockPanel.Dock="Top" Orientation="Horizontal">
            <Button Content="Home" aqr:RouterLink.To="/" />
            <Button Content="Profile" aqr:RouterLink.To="/users/42" />
        </StackPanel>
        <aqr:RouterView />
    </DockPanel>
</Window>
```

See `samples/Barbatos.Wpf.AquariusRouter.Sample` in the repo for a complete, working app
exercising every feature below: param routes, nested routes with a named outlet, a guarded route,
redirect/alias, `RouterLink` active-state styling, and the catch-all 404 view.

---

## The Route Table

`Barbatos.Wpf.AquariusRouter.Routing.RouteRecord` - the WPF/C# counterpart of Vue Router's
`RouteRecordRaw`. A route table is just a list of these, most commonly declared once at startup:

```csharp
new RouteRecord
{
    Path = "/users/:id",
    Name = "user",
    View = typeof(UserView),
    Meta = new Dictionary<string, object?> { ["requiresAuth"] = true },
    Children =
    [
        new RouteRecord { Path = "", Name = "user-overview", View = typeof(UserOverviewView) },
        new RouteRecord { Path = "settings", Name = "user-settings", View = typeof(UserSettingsView) },
    ],
}
```

- **`Path`** - this record's own path contribution. Relative to its parent by default (no leading
  `/` - joined onto the parent's own resolved path); a leading `/` makes it absolute, ignoring
  nesting. An empty string (`""`) is the "index" case - it resolves to exactly the parent's own
  path, the closest thing to a default child.
- **`Name`** - a unique key for navigating without hardcoding a path string:
  `router.Push(new RouteLocationRaw { Name = "user", Params = new() { ["id"] = "42" } })`.
  Registering a second record under an already-used name silently replaces the first.
- **`View`** - the `FrameworkElement`-derived View type rendered into the default outlet. For
  multiple simultaneous outlets, use **`Views`** instead (a `Dictionary<string, Type>` keyed by
  outlet name - see [Named Outlets](#named-outlets)). Neither is required: a record with only
  `Children` and no `View`/`Views` is a pure grouping/layout record (it still participates in
  nested routing, just renders nothing of its own - the same as Vue Router's own
  component-less-parent pattern); a record with only `Redirect` needs neither either.
- **`Children`** - nested child records, joined onto this one's `Path` per the rule above.
- **`Redirect`** - `Func<RouteLocation, RouteLocationRaw>`. When set, navigating to this record
  immediately resolves to a different target instead, *before any navigation guard runs* - only
  the target's own guards ever run, never this record's. Always a delegate (`to => "/somewhere"`),
  collapsing Vue Router's string-or-function union since a static redirect is barely more verbose
  as a lambda.
- **`Alias`** - alternate paths that resolve to this exact same record. Unlike `Redirect`, no
  resolution substitution happens - it's simply reachable under more than one path, and (a
  deliberate simplification from Vue Router - see the type's own XML docs) shares the exact same
  runtime state as the original rather than a cloned, independent record.
- **`Meta`** - an arbitrary data bag, not interpreted by the router itself. Merged root-to-leaf
  into the resolved location's own `Meta` on every single navigation (child keys win over parent
  keys) - read it in a guard (`to.Meta["requiresAuth"]`) without manually walking `to.Matched`.
- **`BeforeEnter`** - a per-route guard, checked only when genuinely entering this record from a
  different one (not on a param/query-only update).

---

## The Router

`Barbatos.Wpf.AquariusRouter.Routing.Router` - the orchestrator. Owns a route table, a history
stack, and the live `CurrentRoute`.

```csharp
var router = new Router(routes, history: null, pathOptions: null);
```

- **`CurrentRoute`** - an Aquarius `Ref<RouteLocation>`, swapped wholesale on every committed
  navigation. Bind to it directly (`{Binding Source={x:Static local:AppRouter.Instance}, Path=CurrentRoute.Value.Path}`)
  or watch it (`Watch.On(router.CurrentRoute, (to, from) => ...)`) the same way you would any other
  Aquarius `Ref<T>` - unlike Vue Router's `useRoute()` (a stable object whose properties are
  getters, needed because Vue's reactivity system requires watching a stable reference), a WPF
  binding already re-evaluates correctly through an ordinary `Ref<T>` replacement, so no
  equivalent indirection exists here.
- **`Push`/`Replace`** - `Task<NavigationFailure?> Push(RouteLocationRaw to)`. Accepts a plain
  string path (implicitly converted) or a full `RouteLocationRaw` (named route + params, or a
  path + query). Runs the complete guard pipeline (see [Navigation Guards](#navigation-guards))
  and returns `null` on success, or why it didn't happen otherwise.
- **`Go`/`Back`/`Forward`** - move through history, running the same guard pipeline first and only
  actually moving on success. Out-of-range is a silent no-op, matching Vue Router's own documented
  behavior.
- **`Start`** - performs the very first navigation. A plain alias for `Push` (unlike Vue Router,
  no special-casing is needed here - see the type's own XML docs for why), but call it explicitly
  once at startup: unlike a browser, there is no ambient URL to seed `CurrentRoute` from
  automatically.
- **`Resolve`** - resolves a `RouteLocationRaw` without navigating.
- **`BeforeEach`/`BeforeResolve`/`AfterEach`** - register global guards/hooks, returning an
  `IDisposable` that unregisters them (the same "subscribe, get a disposable stop handle"
  convention `Watch.On` already uses).
- **`AddRoute`/`RemoveRoute`/`HasRoute`/`GetRoutes`** - register/unregister routes dynamically
  after construction (e.g. for a plugin system), the WPF/C# counterpart of Vue Router's own
  dynamic routing API.
- **`Router.Current`** - a static, settable ambient default, consulted by `RouterView`/`RouterLink`
  when neither sets an explicit `Router` of its own. Set this once at startup for the common
  single-router app (mirroring `Application.Current`'s own shape); a multi-window app that wants
  independent per-window navigation stacks can skip this and set `RouterView.Router`/
  `RouterLink.Router` explicitly per window instead - a real WPF-specific capability with no Vue
  Router equivalent, since a web SPA only ever has one router.

---

## RouterView

`Barbatos.Wpf.AquariusRouter.Xaml.RouterView` - hosts whichever View is currently matched for its
own outlet, at its own position in the route tree.

```xml
<aqr:RouterView />
```

Never assign its `Content` directly - it's entirely managed based on the current route, the same
way Aquarius's own `If` reserves its `Content` for whatever branch is currently showing.

### Named Outlets

```xml
<aqr:RouterView OutletName="sidebar" />
<aqr:RouterView />
```

Multiple simultaneous outlets at the same depth, fed by one route record's `Views` map (default
outlet name is `"default"`, matching `RouteRecord.View`'s own shorthand):

```csharp
new RouteRecord
{
    Path = "/dashboard",
    Name = "dashboard",
    Views = new Dictionary<string, Type> { ["default"] = typeof(DashboardView), ["sidebar"] = typeof(DashboardSidebarView) },
}
```

(Named `OutletName`, not `Name` - WPF's own `FrameworkElement.Name` already means something
unrelated, so reusing it here would silently collide.)

### Nested Routes

A matched View's own XAML just declares another `<aqr:RouterView />` inside itself - it finds its
own depth and effective `Router` automatically, no extra wiring needed:

```xml
<!-- UserView.xaml - matched for /users/:id -->
<DockPanel>
    <TextBlock DockPanel.Dock="Top" Text="{Binding Heading}" />
    <aqr:RouterView /> <!-- renders whichever /users/:id child matched -->
</DockPanel>
```

Depth is resolved by walking up the *visual tree* the first time each `RouterView` loads, not via
an inherited `DependencyProperty` - WPF constructs a whole View subtree eagerly, all at once
(`Activator.CreateInstance`/`InitializeComponent()`), unlike Vue's lazy, top-down renderer, so a
nested `RouterView`'s own constructor runs *before* the outer one even gets the instance back;
walking the tree at `Loaded` (once everything is genuinely connected) sidesteps that entirely. See
`RouterView`'s own XML docs for the full reasoning, including why `Router` itself is centralized
in `Router.Push`'s own pipeline rather than resolved per-`RouterView`.

---

## RouterLink

`Barbatos.Wpf.AquariusRouter.Xaml.RouterLink` - turns any `ButtonBase` or `Hyperlink` into a
navigation trigger, with live active-state tracking.

```xml
<Button Content="About" aqr:RouterLink.To="/about" />
<Hyperlink aqr:RouterLink.To="{aqr:RouteTo Name=user, Params={...}}">Profile</Hyperlink>
```

A set of attached properties on an *existing* clickable element - deliberately not a new templated
control, matching this library family's own established split (attached properties for "add
behavior to an existing element"; a new control is reserved for something that manages child
content/visibility/lifecycle, which a link does not). Click-wiring itself is built on Aquarius's
own `Directives.Event`/`Directives.Command`, not a second hand-rolled event mechanism.

- **`To`** - a `string` path or a `RouteLocationRaw`.
- **`Replace`** - navigates via `Router.Replace` instead of `Router.Push` when `true`.
- **`Router`** - an explicit override, same fallback-to-`Router.Current` shape as `RouterView.Router`.
- **`IsActive`/`IsExactActive`** - live bindable booleans, the native-WPF mapping of Vue Router's
  `linkActiveClass`/`linkExactActiveClass` CSS-class mechanism (consumed via an ordinary
  `DataTrigger`/`Style.Triggers` instead of a class-name string):

  ```xml
  <Button aqr:RouterLink.To="/about">
      <Button.Style>
          <Style TargetType="Button">
              <Style.Triggers>
                  <DataTrigger Binding="{Binding (aqr:RouterLink.IsActive), RelativeSource={RelativeSource Self}}" Value="True">
                      <Setter Property="FontWeight" Value="Bold" />
                  </DataTrigger>
              </Style.Triggers>
          </Style>
      </Button.Style>
  </Button>
  ```

  `IsActive` is true while the current route is this link's target *or any of its descendants*
  (so a link to a parent/index route stays highlighted while any child route is active);
  `IsExactActive` requires an exact match, including every param.

**Not ported**: Vue Router's click-guarding (ignoring a modified click/right-click/`target=_blank`
so the browser's native anchor behavior takes over) - a WPF `Click` event carries no modifier-key
or mouse-button information to guard on in the first place, and there's no "native behavior" to
fall back to. A documented "not applicable," not a gap.

---

## Navigation Guards

A ViewModel opts into exactly the hooks it needs, the same `is`-pattern-checked, one-interface-
per-hook shape Aquarius's own `Lifecycle` hooks already use:

| Interface | Fires on... | Can cancel/redirect? |
| --- | --- | --- |
| `IOnBeforeRouteEnter` | Entering a newly-matched record (new View constructed) | Yes |
| `IOnBeforeRouteUpdate` | The same record staying mounted across a param/query-only change | Yes |
| `IOnBeforeRouteLeave` | Leaving a currently-mounted record entirely | Yes |
| `IOnRouteEnter` | Same timing as `IOnBeforeRouteEnter`, fires immediately before it | No - just data |
| `IOnRouteUpdate` | Same timing as `IOnBeforeRouteUpdate`, fires immediately before it | No - just data |

```csharp
public sealed class DashboardViewModel(AuthState authState) : IOnBeforeRouteEnter
{
    public Task<NavigationGuardResult> OnBeforeRouteEnter(RouteLocation to, RouteLocation from) =>
        Task.FromResult(authState.IsLoggedIn ? NavigationGuardResult.Continue : NavigationGuardResult.Redirect("/login"));
}
```

`NavigationGuardResult` is `Continue`, `Cancel`, or `Redirect(RouteLocationRaw)` - a plain
`bool` implicitly converts too (`true`/`false` for `Continue`/`Cancel`), matching Vue Router's own
guard-return convention. **Unlike Vue Router**, `IOnBeforeRouteEnter` fires with a real
`DataContext`/`this` - `to.Params` is available directly, no `next(vm => ...)` callback-capture
dance needed. Vue Router only needs that dance because it defers creating the entering component
instance until *after* every guard resolves; a WPF View's `DataContext` is already resolved (via
`aq:Setup`) by the time `Router` constructs it and checks this hook, so there's no such gap to
work around.

A ViewModel that just needs its own route params, with nothing to guard/cancel, doesn't need to
implement a guard interface at all - `IOnRouteEnter`/`IOnRouteUpdate` are non-cancelling, always-
invoked companions for exactly that case (the closest analogue of Vue Router's `props: true`,
without a second, parallel prop-binding mechanism):

```csharp
public sealed partial class UserViewModel : ObservableObject, IOnRouteEnter
{
    [ObservableProperty] private string _userId = "";
    public void OnRouteEnter(RouteLocation to, RouteLocation from) => UserId = (string)to.Params["id"]!;
}
```

Global guards run for every navigation:

```csharp
using var unregister = router.BeforeEach((to, from) => Task.FromResult<NavigationGuardResult>(
    to.Meta.TryGetValue("requiresAuth", out var v) && v is true && !authState.IsLoggedIn
        ? NavigationGuardResult.Redirect("/login")
        : true));
```

### The Full Pipeline Order

Matches Vue Router's own documented "Full Navigation Resolution Flow," adjusted only for the
construction-timing difference noted above:

1. Static `RouteRecord.Redirect` on the resolved leaf (if any) - resolved immediately, before
   anything else, looping while the newly-resolved leaf keeps redirecting.
2. Duplicate check (same record/params/query/hash as the current route, and `Force` wasn't set) -
   short-circuits as `NavigationFailureType.Duplicated`.
3. `IOnBeforeRouteLeave` on every record leaving entirely, **deepest-child-first**.
4. Global `BeforeEach` guards, registration order.
5. `IOnRouteUpdate` then `IOnBeforeRouteUpdate` on every reused record, **root-first**.
6. Per-route `BeforeEnter` on genuinely new entering records, root-first.
7. Entering Views are constructed (their own `aq:Setup` resolves `DataContext`), then
   `IOnRouteEnter` then `IOnBeforeRouteEnter` runs on each, root-first.
8. Global `BeforeResolve` guards.
9. Commit: history updated, `CurrentRoute` swapped - every live `RouterView` reacts immediately.
10. Global `AfterEach` hooks, receiving the outcome - fires for **every** outcome except an
    in-progress redirect (which defers to its own eventual recursive call), including a
    cancelled/aborted/duplicated navigation, matching Vue Router's own real behavior (its own docs
    show detecting a failure *inside* `AfterEach` as the canonical idiom, not a try/catch around
    `Push`).

A guard-returned redirect abandons the current navigation and restarts at the new target (with a
30-hop cycle detector, matching Vue Router's own dev-time safety net) - the static-redirect path
in step 1 has no such counter, matching Vue Router's own real asymmetry between the two redirect
sources. A second navigation racing an in-flight one correctly cancels the older one, re-checked
after every single stage above (a guard can itself be slow/async).

---

## Navigation Failures

```csharp
var failure = await router.Push("/dashboard");
if (NavigationFailures.IsNavigationFailure(failure, NavigationFailureType.Aborted))
{
    // a guard returned Cancel
}
```

`Push`/`Replace`/`Go` return `Task<NavigationFailure?>` - `null` on success, otherwise why the
navigation didn't happen. `NavigationFailureType` is `[Flags]` (`Aborted = 4, Cancelled = 8,
Duplicated = 16`, matching Vue Router's own real bit values, not sequential ints), so
`IsNavigationFailure(f, Aborted | Cancelled)` checks more than one type in one call. A guard that
throws a genuine `Exception` is **not** wrapped as one of these - it propagates as-is, the same
real distinction Vue Router itself draws between a guard returning `false`/redirecting and a guard
throwing.

---

## Path Matching

`Barbatos.Wpf.AquariusRouter.Matching` - `PathTokenizer`, `PathParser`, `RouteMatcher`. A real,
independently useful, directly unit-testable public API on its own (no `InternalsVisibleTo`
anywhere in this repo), not just an internal implementation detail of `Router`.

| Syntax | Meaning |
| --- | --- |
| `:id` | A required param. |
| `:id?` | Optional (0 or 1). |
| `:id+` | Repeatable, 1 or more - surfaced as a `string[]`. |
| `:id*` | Repeatable, 0 or more. |
| `:id(\d+)` | A custom regex constraining what matches. |
| `/:pathMatch(.*)*` | The catch-all/404 idiom - always ranked last against any more specific route, however many are registered. |

Overlapping routes are ranked automatically (a static segment always outranks a dynamic one at the
same position; a custom-regex param outranks a plain dynamic one; wildcards/optional/repeatable
rank lower) - the same scoring algorithm Vue Router's own matcher uses, ported faithfully.

---

## History

`Barbatos.Wpf.AquariusRouter.Routing.IRouterHistory` / `MemoryRouterHistory` - the navigation stack
`Router` is built on. `MemoryRouterHistory` (an array of entries plus an integer cursor) is the
only implementation this package ships, since a desktop app has no browser URL/address bar for an
`html5`- or `hash`-mode equivalent to shadow - Vue Router's own docs reach for `createMemoryHistory()`
for exactly this "no real browser environment" case too. It's also simpler than Vue Router's own
history adapters, not just a smaller subset of them: since nothing external can move the cursor
out-of-band the way a real browser back/forward button can, `Router.Go` can run the full guard
pipeline *before* ever touching the cursor, with no "undo a URL change a cancelled guard already
let happen" dance needed.

The abstraction stays pluggable for a custom implementation (e.g. one that persists the stack
across app restarts) - implement `IRouterHistory` directly.

---

## Deliberately Out of Scope

- **HTML5/hash history** - no browser, no address bar, no `popstate`.
- **Scroll behavior** - no DOM/`window.scrollTo` equivalent; no per-view "saved scroll position"
  concept to restore.
- **The experimental Data Loaders package** - not core/stable Vue Router. The guard pipeline
  already gives both *documented, stable* data-fetching patterns for free: "after navigation" is a
  ViewModel starting an async load in `IOnMounted`/`IOnRouteEnter` with a bindable `IsLoading`
  (composes with Aquarius's own `Suspense` for free); "before navigation" is an async
  `IOnBeforeRouteEnter` guard that awaits and only returns `Continue` once ready.
- **Reflection-based automatic param-to-property binding** (Vue Router's `props: true/object/function`) -
  `IOnRouteEnter`/`IOnBeforeRouteEnter` already give a ViewModel direct, explicit access to
  `to.Params`, achieving the same "don't reach into an ambient current-route singleton" decoupling
  goal `props` exists for, with one mechanism instead of two.
- **`RouterView` composing with Aquarius's `Transition`/`TransitionGroup`** - Vue Router's own
  `<RouterView>` explicitly warns against wrapping it directly in `<Transition>`/`<KeepAlive>` (only
  a scoped-slot form works correctly), and `Transition` here is boolean-`Show`-driven, not "animate
  whenever Content changes to something new" - a real semantic gap, not just wiring. Animate inside
  each routed View's own content instead (a `Transition` wrapping that View's internal layout,
  keyed off its own `IOnMounted`).

---

## Ecosystem

Ships as a **single package** - Matching, Routing, and the Xaml layer are all included; there is
nothing else to install beyond Aquarius itself.

### Repository layout

- `src/Barbatos.Wpf.AquariusRouter` - the library.
- `samples/Barbatos.Wpf.AquariusRouter.Sample` - a complete sample application exercising every
  feature area above.
- `tests/Barbatos.Wpf.AquariusRouter.UnitTests` - the unit test suite.

---

## API Reference

Due to the extensive nature of the library's interfaces, classes, and properties, the full API
Reference has been moved to a dedicated document modeled after Microsoft's official .NET
documentation format.

👉 **[Read the Full API Reference](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.AquariusRouter/API-REFERENCE.md)** 👈

---

## Community

See the [root README](https://github.com/Barbatos-Labs/Barbatos.Wpf#community) for maintainers,
support, and license information - shared across every package in this repository.
