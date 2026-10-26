---
name: barbatos-aquarius-router
description: Add or change navigation with Barbatos.Wpf.AquariusRouter - the RouteRecord table, nested and named-outlet routes, index children, redirects and aliases, navigation guards (IOnBeforeRouteEnter/Update/Leave and global BeforeEach/BeforeResolve/AfterEach), RouterView, RouterLink, RouteTo, modal routes via RouteRecord.Window plus RouterWindows, independent stacks via RouterScope, path matching, history, and in-app search. Use this for routing, deep links, back/forward behavior, moving between screens or pages, "require login before this screen", warning about unsaved changes when leaving, a 404 catch-all, a searchable command palette over screens, or opening a dialog window as part of the route rather than as a one-off command. Also when only the symptom is described - "the edit is silently lost when I click another item in the list", "switching to a different record wipes what I typed", "it should ask before discarding". Vietnamese prompts land here too: điều hướng, chuyển màn hình, chuyển trang, thanh nav, quay lại, chặn khi chưa đăng nhập, cảnh báo chưa lưu, tìm kiếm trong app.
---

# Barbatos.Wpf.AquariusRouter

A Vue Router-style navigation system built directly on Aquarius. `Router` itself never touches
`Window` or `Application`, so the whole pipeline is testable headless.

Full docs: [`src/Barbatos.Wpf.AquariusRouter/README.md`](../../../src/Barbatos.Wpf.AquariusRouter/README.md) ·
[`API-REFERENCE.md`](../../../src/Barbatos.Wpf.AquariusRouter/API-REFERENCE.md).
Reference app: [`samples/Barbatos.Wpf.AquariusRouter.Sample`](../../../samples/Barbatos.Wpf.AquariusRouter.Sample)
— `Routing/AppRoutes.cs` is the canonical route table and demonstrates every guard placement.

```xml
xmlns:aqr="http://schemas.barbatos.co/aquariusrouter/2026/xaml"
```

---

## Wiring

```csharp
builder.Services.AddSingleton<AuthState>();
builder.Services.AddAquarius();
builder.Services.AddAquariusRouter(sp => AppRoutes.Build(sp.GetRequiredService<AuthState>()));

// Routed ViewModels are TRANSIENT: Router constructs a fresh instance on every genuine
// (re-)entry, and a singleton registration silently fights that.
builder.Services.AddTransient<DashboardViewModel>();

var app = builder.Build();
app.Services.UseAquarius();          // Setup.ServiceProvider -> this container
app.Services.UseAquariusRouter();    // publishes Router.Current
```

`routes` is a **factory** so a per-route `BeforeEnter` can close over a DI-resolved service.
`Router` and `IRouterHistory` register with `TryAdd` — register your own first to win.

Register only ViewModels with real constructor dependencies; `Setup`'s `Activator` fallback
covers parameterless ones.

`Router.Start("/")` seeds the first history entry — required once at startup. There is
deliberately no "default top-level route": unlike a browser there is no ambient URL.

Register global guards from an `IWpfInitializeService` (see the sample's
`ConfigureAppNavigationGuards`), not from hand-rolled `GetRequiredService` calls in
`CreateWpfApp`.

---

## The route table

Keep it in one place — `Routing/AppRoutes.cs`, the counterpart of a Vue app's `router/index.ts`.

```csharp
new RouteRecord
{
    Path = "/users/:id",
    Name = "user",
    View = typeof(UserLayoutView),
    Meta = new Dictionary<string, object?> { ["requiresAuth"] = true },
    Children =
    [
        new RouteRecord { Path = "", Name = "user-overview", View = typeof(UserOverviewView) },
        new RouteRecord { Path = "settings", Name = "user-settings", View = typeof(UserSettingsView) },
    ],
}
```

| Member | Notes |
| --- | --- |
| `Path` | Relative to the parent unless it starts with `/`. `""` makes it the **index child** — targeting the parent by path *or by name* descends to it. |
| `Name` | Unique key. Re-registering a name silently replaces the previous record. |
| `View` / `Views` | Default outlet, or a `Dictionary<string, Type>` of named outlets. Setting both throws at registration. A record with only `Children` is a valid layout-only grouping. |
| `ViewModel` / `ViewModels` | Pairs a `DataContext` from the table — for when you own neither the View nor the ViewModel. **Overrides** the View's own `aq:Setup`. Setting both, or naming an outlet with no View, throws at registration. |
| `Children` | Nested records. |
| `Window` | Renders this record into its own `Window` — a **modal route**. See below. |
| `Redirect` | `Func<RouteLocation, RouteLocationRaw>`, always a delegate. Resolves **before any guard runs**; this record's own guards never run. |
| `Alias` | Extra paths resolving to the *same* record and sharing its runtime state. |
| `Meta` | Arbitrary bag, merged root-to-leaf on every resolve (child wins) — read `to.Meta["requiresAuth"]` in a guard without walking `to.Matched`. |
| `BeforeEnter` | Per-route guard, only on a genuinely new entry. |
| `Search` | Opt-in `RouteSearchEntry` list for in-app search. |
| `Instances` | Runtime bookkeeping written only by `Router`. Not configuration. |

**Index child vs redirect on the parent:** an index child keeps the URL at `/settings`; a
redirect changes it to `/settings/security` and resolves before any guard. Without an index
child, `/settings` matches the shell alone and its nested outlet renders nothing.

Catch-all: `new RouteRecord { Path = "/:pathMatch(.*)*", Name = "not-found", View = ... }`.
Registration order does not matter — `PathParser` always ranks a wildcard last.

`RouteRecord` is a reference type on purpose: **object identity is what makes a route "the same
route" across navigations.** Never make it a `record`, and use
`RouteLocation.IsSameLocation(other)` rather than `==`/`Equals` to test "same target".

---

## Pairing a View with its ViewModel

Pick by how much code you are allowed to change:

| Situation | Use |
| --- | --- |
| You own the View, names follow the convention | `aq:Setup.Enable="True"` in the View |
| You own the View, names/namespaces differ | `aq:Setup.ViewModel="{x:Type …}"` |
| You own **neither** (both ship in libraries) | `RouteRecord.ViewModel` in the table |

Two traps:

- `aq:Setup.Enable` matches the **namespace-qualified** name (`Ns.FooView` → `Ns.FooViewModel`).
  A different *assembly* is fine (if already loaded); a different *namespace* or *name* is not.
  Unresolved leaves `DataContext` null and renders empty — set `Setup.ThrowOnUnresolved = true`
  to make that loud.
- A ViewModel paired from the route table still gets everything **the router** dispatches
  (`IOnRouteEnter`/`IOnRouteUpdate`, every guard interface), but **no Aquarius mount hooks** —
  those need `aq:Lifecycle.Enable` in the View's own XAML. If you cannot edit the View, load
  from `IOnRouteEnter` instead.

---

## Guards

```csharp
public sealed class DashboardViewModel(AuthState auth) : IOnBeforeRouteEnter
{
    public Task<NavigationGuardResult> OnBeforeRouteEnter(RouteLocation to, RouteLocation from) =>
        Task.FromResult(auth.IsLoggedIn
            ? NavigationGuardResult.Continue
            : NavigationGuardResult.Redirect("/login"));
}
```

| Interface | Fires | Cancels? |
| --- | --- | --- |
| `IOnBeforeRouteEnter` | entering a newly-matched record | yes |
| `IOnBeforeRouteUpdate` | same record staying mounted across a param/query change | yes |
| `IOnBeforeRouteLeave` | leaving a mounted record entirely | yes |
| `IOnRouteEnter` / `IOnRouteUpdate` | same timing, immediately before the guard | no — data only |

- `NavigationGuardResult` is `Continue` / `Cancel` / `Redirect(raw)`, and a plain `bool`
  converts implicitly. Guards are **always async** — there is no sync overload.
- **Unlike Vue Router**, `IOnBeforeRouteEnter` fires with a real `this`/`DataContext` —
  `to.Params` is available directly, no `next(vm => …)` dance.
- A ViewModel that only needs its params should implement `IOnRouteEnter`, not a guard.
- A route that only needs protection should declare `Meta` and let one global `BeforeEach`
  enforce it — the ViewModel then knows nothing about auth. One `BeforeEach` can enforce
  several independent concerns (the sample checks `requiresAuth` **and** `requiresPremium`).
- **A guard that throws is not a `NavigationFailure`** — it propagates as-is, the same
  distinction Vue Router draws.

Global guards return `IDisposable`: `router.BeforeEach(g)`, `router.BeforeResolve(g)`,
`router.AfterEach(hook)`.

### Pipeline order — memorize this

1. Static `RouteRecord.Redirect` on the resolved leaf (loops while it keeps redirecting).
2. Duplicate check → `NavigationFailureType.Duplicated` unless `Force`.
3. `IOnBeforeRouteLeave`, **deepest-child-first**.
4. Global `BeforeEach`, registration order.
5. `IOnRouteUpdate` then `IOnBeforeRouteUpdate` on reused records, **root-first**.
6. Per-route `BeforeEnter` on new records, root-first.
7. Entering Views constructed (their `aq:Setup` resolves `DataContext`), then `IOnRouteEnter`
   then `IOnBeforeRouteEnter`, root-first.
8. Global `BeforeResolve`.
9. Commit — history updated, `CurrentRoute` swapped, every `RouterView` reacts.
10. Global `AfterEach`, for **every** outcome except an in-progress redirect — including
    cancelled/aborted/duplicated.

A guard-returned redirect restarts at the new target with a 30-hop cycle detector; the static
redirect in step 1 has none (matching Vue Router's own asymmetry). A newer navigation cancels an
older in-flight one, re-checked after every stage.

### Failures

```csharp
var failure = await router.Push("/dashboard");
if (NavigationFailures.IsNavigationFailure(failure, NavigationFailureType.Aborted))
{ /* a guard returned Cancel */ }
```

`Aborted = 4`, `Cancelled = 8`, `Duplicated = 16` — real bit flags, so
`Aborted | Cancelled` checks both at once.

---

## XAML surface

```xml
<aqr:RouterView />                             <!-- default outlet -->
<aqr:RouterView OutletName="sidebar" />        <!-- named outlet -->

<Button aqr:RouterLink.To="/about" />
<Button aqr:RouterLink.To="{aqr:RouteTo Name=user, Params='id=42'}" />
```

- `RouterView.Content` is **entirely managed** — never assign it.
- `RouterLink.To` accepts a `string` path or a `RouteLocationRaw`, and is settable on
  `ButtonBase` or `Hyperlink` only. `RouterLink.Replace` navigates via `Replace`.
- `RouterLink.IsActive` / `IsExactActive` are read-only bindable bools — the native mapping of
  Vue's `linkActiveClass`, consumed through an ordinary `DataTrigger`:

  ```xml
  <DataTrigger Binding="{Binding (aqr:RouterLink.IsActive), RelativeSource={RelativeSource Self}}" Value="True">
      <Setter Property="FontWeight" Value="Bold" />
  </DataTrigger>
  ```

- `{aqr:RouteTo}` is evaluated **once at parse time**. For a target that changes at runtime,
  bind `RouterLink.To` to a ViewModel `RouteLocationRaw`. `Params`/`Query` are `;`-separated
  `key=value`; a comma inside one value makes it a repeatable param's list.
- Router resolution order for both controls: explicit `Router` → inherited
  `RouterScope.Router` → (nested `RouterView`) its ancestor's router → `Router.Current`.
  All read at `Loaded`.

---

## Dialog windows — two shapes

**Modal route (default choice).** The dialog *is* a route:

```csharp
new RouteRecord
{
    Path = "inspector", Name = "inspector",
    View = typeof(InspectorOverviewView),
    Window = typeof(InspectorWindow),   // the only extra line
}
```

```xml
<Window aqr:RouterWindows.Enable="True" ...>   <!-- once, on the main window -->
<Button aqr:RouterLink.To="/dashboard/inspector" />   <!-- an ordinary link -->
```

Because it lives on the record, **every** navigation path reaches it — link, `Push`, shortcut,
in-app search, deep link — with no second implementation. One router, one history: the parent
stays matched and visible behind, `Back` closes the dialog, and closing with `[X]` navigates back
to the parent so route and screen can never disagree. Two sibling records naming the same
`Window` type share one window and swap only its outlet.

Register the window **transient** — a closed WPF `Window` cannot be shown again.

**Independent stack** (`RouterScope.Router` set on the dialog `Window`) is for a window that is
genuinely a second application surface with its own table and history. Property inheritance does
not cross a `Window` boundary, which is what makes it safe. A `Router` owns its records'
`Instances`, so **two routers must never share one route table.**

| | Modal route | Independent stack |
| --- | --- | --- |
| History | shared with the app | its own |
| `Back` | closes the dialog | moves within the dialog |
| Parent route | stays matched behind | unrelated |

---

## Path matching, history, search

Segments support `:name`, `:name?` (optional), `:name+` (repeatable), `:name*`
(optional+repeatable), and `:name(regex)`. A repeatable param may not share a segment.
`PathParserOptions` (`Sensitive`, `Strict`) applies uniformly via `RouterOptions.PathOptions`.
Param values are `string`, or `IReadOnlyList<string>` for a repeatable param with at least one
occurrence — an absent optional param is `""`, never `[]`.

`RouteLocationRaw.Path` **ignores `Params`** when set (Vue Router's own documented rule) — use
`Name` + `Params` to build a target from parts. `RouteLocationRaw.Parse(fullPath)` splits a
serialized `path[?query][#hash]` back — that is what `Go`/`Back`/`Forward` and protocol
activation use.

`MemoryRouterHistory` is the only shipped `IRouterHistory` — in-process entries plus a cursor,
no URL dependency. Register your own before `AddAquariusRouter` for persistence.

In-app search is the **query engine only**, deliberately not a search-box control:

```csharp
Search = [new RouteSearchEntry { Title = "Security settings", Keywords = "password" }]
```

`new RouteSearchIndex(router).Query(text)` re-reads `router.GetRoutes()` on every call, so it is
never stale after `AddRoute`/`RemoveRoute`. AND semantics across space-separated terms; ranking
`0` exact title, `1` prefix, `2` all terms in title, `3` matched only via keywords. Bind
`RouteSearchResult.Title` (post-`TextResolver`), never `Entry.Title`. `Result.To` **throws** if
the record has no `Name`. Set `TextResolver` to a localizer lookup to treat titles as resource
keys. Accent folding applies to Latin script only — Japanese dakuten, Thai vowel signs and
Cyrillic `й` stay intact; put an unvowelled spelling in `Keywords` for Arabic/Hebrew.

---

## Out of scope — do not add

- Per-route keep-alive / `<aq:KeepAlive>` around `<aqr:RouterView>`. Vue Router has no such
  option either; `Router` owns routed View lifetimes so guards run against the mounted instance.
- A `RouterLink.Target`-style "open in a window" property — that belongs on the record.
- A shipped search-box control.
