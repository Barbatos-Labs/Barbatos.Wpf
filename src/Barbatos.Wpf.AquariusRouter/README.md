# Barbatos.Wpf.AquariusRouter

![Barbatos.Wpf.AquariusRouter logo](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/build/aquariusrouter-logo.svg?raw=true)

### *A Vue Router-style navigation system - for WPF*

**Path matching with params/optional/repeatable segments/custom regex, nested routes and named
outlets via `RouterView`, navigation guards, redirect/alias, per-window router scopes, and
`RouterLink` with live active-state styling - built directly on [Barbatos.Wpf.Aquarius](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Aquarius/README.md).**

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
  * [Default child routes](#default-child-routes)
  * [Pairing a View with its ViewModel](#pairing-a-view-with-its-viewmodel)
* **[The Router](#the-router)**
  * [Registering it with DI](#registering-it-with-di)
* **[RouterView](#routerview)**
  * [Named Outlets](#named-outlets)
  * [Nested Routes](#nested-routes)
* **[Dialog windows](#dialog-windows)**
  * [Modal routes](#modal-routes---a-route-that-renders-into-a-window)
  * [Independent stacks](#independent-stacks---a-window-with-its-own-router)
* **[RouterLink](#routerlink)**
  * [RouteTo](#routeto)
* **[Navigation Guards](#navigation-guards)**
  * [The Full Pipeline Order](#the-full-pipeline-order)
* **[Navigation Failures](#navigation-failures)**
* **[In-App Search](#in-app-search)**
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

`RouteLocationRaw.Parse` is the piece that turns such an incoming string into a navigation target:

```csharp
var uri = new Uri(activationArgument);          // myapp://users/42?ref=mail#detail
await router.Push(RouteLocationRaw.Parse(uri.PathAndQuery + uri.Fragment));
```

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
- **`ViewModel`** / **`ViewModels`** - the ViewModel type to assign as the View's `DataContext`,
  pairing the two from the route table so neither has to know about the other. See
  [Pairing a View with its ViewModel](#pairing-a-view-with-its-viewmodel) - this is the option for
  when the View ships inside a library you cannot edit.
- **`Children`** - nested child records, joined onto this one's `Path` per the rule above.
- **`Window`** - renders this record into a `Window` of its own instead of into the parent's outlet:
  a **modal route**. The parent route stays matched and keeps showing behind it, and `Back` closes
  the dialog. Declared here rather than on a link so that every way of navigating - a `RouterLink`,
  a `Push` from a ViewModel, a shortcut, in-app search - reaches it with no extra wiring. See
  [Dialog windows](#dialog-windows).
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

### Default child routes

Give a parent a child whose `Path` is the empty string. It contributes nothing to the path, so it
resolves to exactly the parent's own - the "index" child, and the parent's default:

```csharp
new RouteRecord
{
    Path = "/settings", Name = "settings", View = typeof(SettingsShellView),
    Children =
    [
        new RouteRecord { Path = "", Name = "settings-profile", View = typeof(SettingsProfileView) },
        new RouteRecord { Path = "security", Name = "settings-security", View = typeof(SettingsSecurityView) },
    ],
}
```

Navigating to `/settings` now renders the shell **and** the profile child inside its
`RouterView` - and so does targeting the parent by name (`{aqr:RouteTo Name=settings}`), which
descends to the index child just as the path form does. A chain of them descends all the way to the
deepest. Without an index child, `/settings` matches the shell alone and its nested outlet renders
nothing, which is usually not what you want.

The alternative is a **redirect** on the parent, when the default should be a real, differently-pathed
child:

```csharp
new RouteRecord { Path = "/settings", Name = "settings", Redirect = _ => "/settings/security", Children = [...] }
```

The difference: an index child keeps the URL at `/settings`, a redirect changes it to
`/settings/security` and resolves before any guard runs (see [`Redirect`](#the-route-table) above).

> There is deliberately no "default route" for the **top level** of a table. The initial location
> is whatever you pass to `Router.Start("/")` - explicit, because unlike a browser there is no
> ambient URL to fall back on - and anything unmatched is handled by a catch-all
> (`/:pathMatch(.*)*`, see [Path Matching](#path-matching)).

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
  independent per-window navigation stacks can skip this and use
  [`RouterScope.Router`](#dialog-windows) per window instead - a real WPF-specific capability with
  no Vue Router equivalent, since a web SPA only ever has one router.

### Registering it with DI

```csharp
var builder = WpfApp.CreateBuilder();
builder.Services.AddSingleton<AuthState>();

// The routes parameter is a factory, so a per-route guard can close over a DI-resolved
// service without anything having to be constructed by hand first:
builder.Services.AddAquariusRouter(
    sp => AppRoutes.Build(sp.GetRequiredService<AuthState>()));

var app = builder.Build();
app.Services.UseAquarius().UseAquariusRouter();
```

`AddAquariusRouter` registers the `Router` singleton, an `IRouterHistory` and `RouterOptions`
(`PathOptions`); `UseAquariusRouter` publishes the resolved router as
`Router.Current` and returns it. Both the `Router` and the history go in with `TryAdd`, so an app
that needs something else - a history that persists across restarts, say - registers it first and
wins. It is optional: `new Router(routes)` plus `Router.Current = router` still works exactly as
before.

### Pairing a View with its ViewModel

There are three ways to give a routed View its `DataContext`. Pick by **how much of the code you
are allowed to change**:

| Your situation | Use |
| --- | --- |
| You own the View, names follow the convention | `aq:Setup.Enable="True"` in the View |
| You own the View, names/namespaces differ | `aq:Setup.ViewModel="{x:Type ...}"` in the View |
| **You own neither** - both ship inside libraries | **`RouteRecord.ViewModel`** in the route table |

The first two are Aquarius's own wiring and live in the View's XAML. The third exists because that
is exactly what a component library denies you: `AccountSummaryView` ships in one library,
`AccountSummaryPresenter` in another, both under change control, and neither may be edited to
mention the other. Declare the pairing where you *do* have authority - the route table:

```csharp
new RouteRecord
{
    Path = "/accounts/:id",
    Name = "account-summary",
    View = typeof(AccountSummaryView),           // from one library, no aq:Setup wiring at all
    ViewModel = typeof(AccountSummaryPresenter), // from another - neither knows the other exists
}
```

That is the whole job. `Router` constructs the View, then assigns the ViewModel as its
`DataContext`, resolving it through `Setup.ServiceProvider` when one is set - so a
constructor-injected ViewModel works normally. Nothing downstream changes: route params,
`IOnRouteEnter`, and every guard interface reach the paired ViewModel exactly as they would one the
View wired up itself.

Use **`ViewModels`** (a `Dictionary<string, Type>` keyed by outlet name) to pair
[named outlets](#named-outlets) individually; outlets you leave out keep whatever their own View
resolved. A route-declared ViewModel is assigned after the View is constructed, so it deliberately
**wins over** that View's own `aq:Setup` - which is the point when the View's built-in wiring names
a ViewModel this particular route doesn't want.

Two mistakes are rejected while the route table is built, rather than surfacing later as a blank
outlet: setting both `ViewModel` and `ViewModels`, and declaring a ViewModel for an outlet that has
no View.

> **One limit to know.** Aquarius's mount hooks (`IOnMounted`, `IOnBeforeUnmount`, …) are opt-in
> *per View*, via `aq:Lifecycle.Enable` in the View's own XAML - and pairing a ViewModel from the
> route table does not change that. A ViewModel paired onto a View that never opted in still gets
> everything **the router itself** dispatches (`IOnRouteEnter`/`IOnRouteUpdate` and every guard
> interface), but no mount hooks, because nothing is dispatching them for that View. If you need
> both and cannot edit the View, do the loading from `IOnRouteEnter` instead.

> **Know the convention's actual rule before relying on it.** `aq:Setup.Enable` matches on the
> *namespace-qualified* name (`Some.Ns.FooView` → `Some.Ns.FooViewModel`), checking the View's own
> assembly first and then every **already-loaded** assembly. So a different assembly on its own is
> fine; a different **namespace** or a different **name** is not. And when it finds nothing the
> default is to leave `DataContext` null rather than throw - the View just renders empty. Set
> `Setup.ThrowOnUnresolved = true` to make that loud.

`samples/Barbatos.Wpf.AquariusRouter.Sample`'s `/accounts/:id` route is a working example of the
third row: the View is compiled into the sample with no wiring of any kind, its
`AccountSummaryPresenter` ViewModel into `Barbatos.Wpf.Samples.Shared` - different assembly,
different namespace, non-conventional name.

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

## Dialog windows

**Yes - a `RouterView` works inside a child `Window`.** Two shapes, and they answer different
questions. Neither one is a command, an event, or a window factory.

### Modal routes - a route that renders into a Window

The dialog *is* a route. Declare the window on the record, as a child of whatever should stay
showing behind it:

```csharp
new RouteRecord
{
    Path = "/dashboard", Name = "dashboard", View = typeof(DashboardView),
    Children =
    [
        new RouteRecord
        {
            Path = "inspector", Name = "inspector",
            View = typeof(InspectorOverviewView),
            Window = typeof(InspectorWindow),   // <- the only extra line
        },
    ],
}
```

Turn it on once, on the app's main window:

```xml
<Window x:Class="...MainWindow" aqr:RouterWindows.Enable="True" ...>
```

And the link is an ordinary link - it says nothing about a window at all:

```xml
<Button Content="Inspector" aqr:RouterLink.To="/dashboard/inspector" />
```

That is the whole point of putting it on the record: **every** way of navigating reaches it. A
`RouterLink`, a `router.Push` in a ViewModel, a keyboard shortcut, in-app search, a deep link on
cold start - none of them has to know a window is involved, and none of them needs a second
implementation. A property on the link could only ever have worked for links.

The dialog itself declares nothing:

```xml
<Window x:Class="...InspectorWindow" ...>
    <DockPanel>
        <StackPanel DockPanel.Dock="Top" Orientation="Horizontal">
            <Button Content="Overview" aqr:RouterLink.To="/dashboard/inspector" />
            <Button Content="Raw data" aqr:RouterLink.To="/dashboard/inspector/raw" />
        </StackPanel>
        <aqr:RouterView />
    </DockPanel>
</Window>
```

**One router, one history**, which is what makes it behave like a route rather than like a popup:

- `/dashboard` stays matched, so the main window keeps showing the dashboard behind the dialog.
- `Back` closes the dialog, because it is an ordinary entry on the ordinary history stack.
- Closing the dialog with its own `[X]` navigates back to the parent route. The symmetry matters:
  without it the route would go on claiming a window that is no longer on screen, and navigating to
  it again would look like a no-op.
- Two sibling records naming the same `Window` type share one window - stepping between them keeps
  its position, size and focus and swaps only the outlet, which is what a dialog with its own
  internal navigation should look like. Nest them instead and the dialog hosts a nested
  `RouterView` of its own, exactly as it would in the main window.

`RouterWindows` is the only piece of the library that opens a window, and it deliberately lives in
the Xaml layer: `Router` itself never touches `Window` or `Application`, so the whole navigation
pipeline stays testable with no `Application` at all. It watches `CurrentRoute` the same way
`RouterView` does - pull-based, `Loaded`-gated, released on `Unloaded` (which also closes every
window it opened).

The window type is resolved through DI (`Setup.Resolve`), so register it - **transient**, since a
closed WPF `Window` cannot be shown again:

```csharp
builder.Services.AddTransient<InspectorWindow>();
```

**How the outlet in a dialog knows what to render.** A dialog is its own visual-tree root, so a
`RouterView` inside one cannot infer its depth by walking ancestors the way every other outlet
does - it would always come out as 0, however deeply nested the route really is. `RouterWindows`
hands the window the record it opened it for, and the outlet renders that record directly: exact,
and immune to the route table being renested later.

### Independent stacks - a window with its own Router

The other question is a dialog that navigates *entirely on its own* - its own route table, its own
history, unrelated to what the main window is showing. That needs a second `Router`, because a
`Router` has exactly one `CurrentRoute`. Declare it once with the inherited `RouterScope.Router`;
every `RouterView` and `RouterLink` inside the window picks it up:

```csharp
// A wrapper type rather than a second Router registration, because a container can only resolve
// one Router by type and the app's own is already that one:
public sealed class InspectorRouter
{
    public InspectorRouter() => Router = new Router([/* this window's own route table */]);

    public Router Router { get; }
}

public InspectorWindow(InspectorRouter inspectorRouter)
{
    InitializeComponent();
    RouterScope.SetRouter(this, inspectorRouter.Router);
}
```

Property inheritance does not cross a `Window` boundary, which is exactly what makes this safe: the
dialog gets its own scope and cannot accidentally inherit - or drive - the main window's. Depth 0
is correct here, because that router's own chain genuinely starts at the window.

Note that a `Router` owns its records' `Instances` (that is how a guard finds the instance to run
against), so two routers must not share one route table - give the second scope a table of its own.

### Choosing

| | Modal route (`RouteRecord.Window`) | Independent stack (`RouterScope.Router`) |
| --- | --- | --- |
| History | one, shared with the app | its own |
| `Back` | closes the dialog | moves within the dialog |
| Parent route | stays matched, still showing behind | unrelated |
| Reachable from | links, `Push`, shortcuts, search, deep links | whatever drives that router |
| Route table | one | one per router |

Reach for the modal route by default - a dialog is usually *part of* where the user already is, and
the modal route is the shape that keeps the route and the screen from ever disagreeing. Reach for
`RouterScope` when the window is genuinely a second application surface.

Resolution order for both `RouterView` and `RouterLink` is: an explicit `Router` on the control
itself, then the nearest inherited `RouterScope.Router`, then (for a nested `RouterView`) whatever
its ancestor `RouterView` resolved, then `Router.Current`. All read at `Loaded`, by which point
property inheritance has settled. Modal `ShowDialog` is not a special case either, and closing a
dialog unmounts its routed View like any other unload.

`samples/Barbatos.Wpf.AquariusRouter.Sample` has the modal route as a working demo (the "Inspector
(dialog)" link in the nav bar - and the same dialog from the search box, with nothing extra written
for it). `RouterModalRouteTests` pins the behaviors above; `RouterDialogWindowTests` pins the
independent-stack ones.

---

## RouterLink

`Barbatos.Wpf.AquariusRouter.Xaml.RouterLink` - turns any `ButtonBase` or `Hyperlink` into a
navigation trigger, with live active-state tracking.

```xml
<Button Content="About" aqr:RouterLink.To="/about" />
<Hyperlink aqr:RouterLink.To="{aqr:RouteTo Name=user, Params='id=42'}">Profile</Hyperlink>
```

A set of attached properties on an *existing* clickable element - deliberately not a new templated
control, matching this library family's own established split (attached properties for "add
behavior to an existing element"; a new control is reserved for something that manages child
content/visibility/lifecycle, which a link does not). Click-wiring itself is built on Aquarius's
own `Directives.Event`/`Directives.Command`, not a second hand-rolled event mechanism.

- **`To`** - a `string` path, or a `RouteLocationRaw` (bind it for a target that changes at
  runtime, or write one inline with the `{aqr:RouteTo ...}` markup extension below).
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

### RouteTo

`Barbatos.Wpf.AquariusRouter.Xaml.RouteTo` - a markup extension that builds a `RouteLocationRaw`
inline, so a link can target a *named* route instead of hardcoding a path. The XAML counterpart of
Vue Router's own `:to="{ name: 'user', params: { id: 42 }}"`:

```xml
<!-- By name, no params - never hardcodes where that route currently lives: -->
<Button aqr:RouterLink.To="{aqr:RouteTo Name=settings-security}" Content="Security" />

<!-- By name, with params (quote the value - '=' and ',' are XAML's own separators): -->
<Button aqr:RouterLink.To="{aqr:RouteTo Name=user, Params='id=42'}" Content="Profile" />

<!-- Several params/query values, and a repeatable param: -->
<Button aqr:RouterLink.To="{aqr:RouteTo Name=user, Params='id=42;tab=billing', Query='ref=nav'}" />
<Button aqr:RouterLink.To="{aqr:RouteTo Name=files, Params='segments=docs,2026,report'}" />
```

`Params`/`Query` are `key=value` pairs separated by `;` (not `,`, which already separates a markup
extension's own arguments); a comma *within* a `Params` value splits it into the string list a
repeatable param (`:segments+`/`:segments*`) expects. `Hash`, `Replace`, and `Force` map straight
onto their `RouteLocationRaw` counterparts. Setting neither `Name` nor `Path` - or both - throws
at parse time rather than resolving something surprising.

**For a target that changes at runtime, bind `To` to a `RouteLocationRaw` property on your
ViewModel instead.** A markup extension is evaluated once when the XAML is parsed, so a `{Binding}`
nested inside one of its properties would be captured as a `Binding` object rather than its value -
`RouteTo` is deliberately for targets that are constant in the markup, which is the overwhelmingly
common case for a nav bar.

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

## In-App Search

`Barbatos.Wpf.AquariusRouter.Searching` - no Vue Router equivalent to port; new surface this
library adds on its own. In a super-app or a banking app, a feature you need can easily be buried
several menu levels deep - a search box that jumps straight to it, skipping the nav structure
entirely, is a real, common pattern in that kind of app. This is the query/ranking engine behind
one; it deliberately does **not** ship a ready-made search box control (see below).

Opt a route into search by giving it one or more `RouteSearchEntry`:

```csharp
new RouteRecord
{
    Path = "security", Name = "settings-security", View = typeof(SettingsSecurityView),
    Search =
    [
        new RouteSearchEntry { Title = "Security settings" },
        new RouteSearchEntry { Title = "Change password", Keywords = "security password" },
    ],
}
```

`Search` is a **list**, not a single entry - so one route can be found under several different
phrasings (synonyms a real user might actually type) without needing a second route just to carry
a different title. A route with no `Search` entries at all is simply never returned - it's
opt-in, not automatic (a raw `/users/:id`-style route wouldn't have a meaningful search title
anyway).

Then query it - typically from a ViewModel backing your own search box:

```csharp
var index = new RouteSearchIndex(router);

IReadOnlyList<RouteSearchResult> results = index.Query("doi mat khau");
if (results.Count > 0)
    await router.Push(results[0].To);
```

- **Matching** splits the query into space-separated terms and requires *every* term to appear
  somewhere in an entry's `Title` or `Keywords` combined (AND semantics - each extra word narrows
  the results, the way most real search boxes behave). Comparison is case-insensitive and,
  deliberately, **Vietnamese-diacritic-insensitive**: `"chuyen tien"`/`"dang nhap"` match
  `"Chuyển tiền"`/`"Đăng nhập"` just as well as the fully-accented spelling (including `đ`/`Đ`,
  which Unicode models as a distinct base letter rather than a combining mark, so it needs its own
  explicit fold) - matching how a large share of real Vietnamese users actually type into a search
  box.
- **Ranking** prefers whichever entries matched via `Title` itself over ones that only matched via
  `Keywords`, with an exact/prefix title match ranked above a same-tier match; ties preserve
  registration order. Always re-reads `Router.GetRoutes()` on every `Query` call rather than
  snapshotting the route table once, so a route added/removed later via `Router.AddRoute`/
  `RemoveRoute` is reflected on the very next query.
- **`RouteSearchResult.To`** is a ready-to-`Push` `RouteLocationRaw`, always resolved by
  `RouteRecord.Name` (the only reliable way to target an arbitrary, possibly deeply-nested record) -
  it throws immediately, rather than silently resolving a wrong/relative navigation, if the matched
  route has no `Name`.

### Searching in more than one language

A route table is built once at startup, so a `Title` baked in then would stay in whatever language
was current at that moment. Declare **resource keys** instead and resolve them per query:

```csharp
var index = new RouteSearchIndex(router) { TextResolver = key => localizer[key] };
```

`TextResolver` runs on `Title` and `Keywords` alike, on every `Query`, so switching language is
picked up by the very next keystroke with nothing to invalidate. Bind
**`RouteSearchResult.Title`** (the resolved text) rather than `Entry.Title` (the key) - with no
resolver configured the two are identical.

**Folding is script-aware**: a combining mark is only stripped when it sits on a Latin letter. That
keeps Vietnamese, French, German, Nordic, Polish and Turkish behaving as intended, while leaving
every other script intact - important because several scripts encode *letters*, not decorations,
as combining marks: Japanese dakuten (`だ` decomposes to `た` + a mark, so blanket stripping turns
"university" into "physique"), Thai vowel signs, and Cyrillic `й`/Ukrainian `ї`. Since query and
title are folded identically, that kind of damage never looks like "nothing found" - it looks like
unrelated results quietly matching, which is harder to notice. The trade-off: Arabic harakat and
Hebrew niqqud are no longer stripped either; put the unvowelled spelling in `Keywords` if a title
carries them.

**Why no ready-made search box control**: a results dropdown/popup genuinely manages its own
visibility and child content - exactly the kind of concern this library family reserves for a real
templated control (see `RouterLink`'s own reasoning for why *it* is deliberately not one). Building
that control means committing to a lot of decisions (positioning, keyboard navigation, default
styling, theming, accessibility) that vary a lot per app. Shipping just the engine keeps that choice
with you; see `samples/Barbatos.Wpf.AquariusRouter.Sample` for one complete, working way to build
the UI on top of it (a plain `TextBox` + results `ListBox` in the nav bar).

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
  `<RouterView>` explicitly warns against wrapping it directly in `<Transition>` (only a
  scoped-slot form works correctly), and `Transition` here is boolean-`Show`-driven, not "animate
  whenever Content changes to something new" - a real semantic gap, not just wiring. Animate inside
  each routed View's own content instead (a `Transition` wrapping that View's internal layout,
  keyed off its own `IOnMounted`).
- **Keep-alive for routed Views** - and note that Vue Router does not have this either: it has no
  per-route keep-alive option, only the template composition
  `<router-view v-slot="{ Component }"><keep-alive><component :is="Component"/></keep-alive></router-view>`.
  The WPF equivalent, `<aq:KeepAlive>` around an `<aqr:RouterView>`, is not supported, and the
  reason is a genuine structural difference rather than missing wiring: **this `Router` owns routed
  View lifetimes**, because `IOnBeforeRouteLeave`/`IOnBeforeRouteUpdate` have to run against the
  instance that is *currently mounted*, which it tracks centrally in `RouteRecord.Instances` (see
  `Router`'s own remarks). Vue's renderer owns component instances instead, so its `<KeepAlive>` can
  simply take over that ownership; here, a wrapper doing the same would be fighting the router for
  it. Aquarius's [`KeepAlive`](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Aquarius/README.md#keepalive)
  is fully supported for the ordinary case it was designed for - a `<aq:Component Is="...">`
  switching between Views inside a single routed View, which covers most of what a "keep this
  screen's state" request actually means.

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
