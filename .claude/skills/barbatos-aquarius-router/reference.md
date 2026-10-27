# AquariusRouter reference

Detail behind [SKILL.md](SKILL.md), checked against `src/Barbatos.Wpf.AquariusRouter`. Exact
signatures live in the package
[API-REFERENCE](../../../src/Barbatos.Wpf.AquariusRouter/API-REFERENCE.md).

Contents: [RouteRecord members](#routerecord-members) -
[Path syntax and ranking](#path-syntax-and-ranking) -
[How a target resolves](#how-a-target-resolves) -
[RouteLocation](#routelocation) - [History](#history) -
[Router resolution and RouterLink](#router-resolution-and-routerlink) - [In-app search](#in-app-search) -
[What keeps a View alive](#what-keeps-a-view-alive)

## RouteRecord members

| Member | Type | Notes |
| --- | --- | --- |
| `Path` | `required string` | Relative to the parent unless it starts with `/`; top level must start with `/`. `""` = index child. |
| `Name` | `string?` | Name lookup key; a later record with the same name takes the name over. |
| `View` / `Views` | `Type?` / `IReadOnlyDictionary<string, Type>?` | `View` is `Views["default"]`. Both set -> `InvalidOperationException` at registration. |
| `ViewModel` / `ViewModels` | `Type?` / `IReadOnlyDictionary<string, Type>?` | Assigned as `DataContext` after the View is built, overriding its `aq:Setup`. Both set, or a key with no matching outlet -> `InvalidOperationException`. Outlets left out keep their own `DataContext`. |
| `Children` | `IReadOnlyList<RouteRecord>?` | `init`-only; runtime children go through `Router.AddRoute(parentName, record)`. |
| `Window` | `Type?` | Modal route; needs `RouterWindows.Enable` on the main window. |
| `Redirect` | `Func<RouteLocation, RouteLocationRaw>?` | Always a delegate (`_ => "/target"`); runs before any guard; the redirecting record's own guards never run. Chained hops count toward the 30-hop limit shared with guard redirects (`InvalidOperationException` past it). |
| `Alias` | `IReadOnlyList<string>?` | Extra paths onto the same record object; path-only (never registered by name); children are not re-attached under the alias. |
| `Meta` | `IReadOnlyDictionary<string, object?>?` | Merged root-to-leaf into `RouteLocation.Meta` on every resolve. |
| `BeforeEnter` | `NavigationGuard?` | Only on a genuine entry - not on a param/query update, not when moving between its own children. |
| `Search` | `IReadOnlyList<RouteSearchEntry>?` | Opt-in search entries. |
| `Instances` | `IDictionary<string, FrameworkElement>` | Live View per outlet name. Written only by `Router`; read by `RouterView`. Not configuration. |

A record is matchable if it has a `Name`, a View, or a `Redirect`. A record with none of those is
a pure grouping record: its children resolve through it, its own path does not.

Dynamic routing on `Router`: `AddRoute(record)`, `AddRoute(parentName, record)` (throws
`InvalidOperationException` if no such parent), `RemoveRoute(record)` / `RemoveRoute(name)`
(removes aliases and every descendant, including dynamically added ones), `HasRoute(name)`,
`GetRoutes()`.

## Path syntax and ranking

| Syntax | Meaning |
| --- | --- |
| `:id` | Required param, one segment. |
| `:id?` | Optional. May share a segment with other tokens. |
| `:id+` / `:id*` | Repeatable (1+ / 0+). Keep it alone in its segment - `/:a-:ids+` is a `FormatException` at registration. |
| `:id(\d+)` | Custom regex; an invalid regex is a `FormatException` at registration. |
| `/:pathMatch(.*)*` | Catch-all; always ranks last. |

- Matching is case-insensitive and tolerates one trailing slash by default.
  `PathParserOptions { Sensitive = true }` / `{ Strict = true }` change that for every route,
  via `RouterOptions.PathOptions` or the `Router` constructor's `pathOptions`.
- Ranking (a port of Vue Router's scoring): static segment > custom-regex param > plain param >
  optional > repeatable > wildcard. Overlapping `/users/new` and `/users/:id` resolve correctly
  in any declaration order. An index child outranks its tied parent.
- Param values are `string`, or `IReadOnlyList<string>` for a repeatable param with at least one
  value. `PathParser.Parse` reports an absent optional param as `""`, but **`RouteLocation.Params`
  omits it** (the matcher drops empty optional values) - read with `TryGetValue`.
- Building a path (`Stringify`): a missing required param throws `InvalidOperationException`
  ("Missing required param"); a missing optional one drops its segment; a list for a
  non-repeatable param throws.

## How a target resolves

`RouteLocationRaw` (`Path`, `Name`, `Params`, `Query`, `Hash`, `Replace`, `Force`; implicit from
`string`):

1. **`Name` set** - lookup by name (`InvalidOperationException` "No route named ..." if absent).
   Params are seeded from the current location: the target's required keys, plus optional keys
   its parent declares; explicit `Params` then overlay them. Then descends through index
   children, exactly like resolving the parent's path.
2. **`Path` set** - first route (in ranking order) whose regex matches the literal string;
   `Params` ignored. No match -> `InvalidOperationException` "No route matches path ...".
3. **Neither** - re-resolves the current route with `Params` merged over the current ones.

`Router.Resolve(raw)` returns the `RouteLocation` without navigating and adds `Query`, `Hash`
and `FullPath` (`path?query#hash`, percent-encoded; a `null` query value serializes as a bare
key). `RouteLocationRaw.Parse(fullPath)` is the exact inverse and is what `Go` uses on history
entries - use it for anything stored as a `FullPath`.

## RouteLocation

`Path`, `Name` (leaf's), `Params`, `Query`, `Hash`, `FullPath`, `Matched` (root to leaf), `Meta`
(merged). `RouteLocation.Start` is the sentinel `CurrentRoute` holds before the first navigation:
`Path = "/"`, empty `Matched` - which is why every outlet is empty until `Start` runs.
`IsSameLocation(other)` = same leaf record by reference + equal params, query and hash; it decides
`Duplicated`.

`Router.CurrentRoute` is an Aquarius `Ref<RouteLocation>`, replaced wholesale on each commit:
bind `CurrentRoute.Value.Path`, or `Watch.On(router.CurrentRoute, (to, from) => ...)` and dispose
the handle.

## History

- `IRouterHistory`: `Location`, `State`, `Push`, `Replace`, `Peek(delta)` (null out of range),
  `Go(delta)` (clamped). `Router.Go` peeks, runs the pipeline, and only moves the cursor on
  success - nothing to undo on a cancel.
- `MemoryRouterHistory` is the only implementation: a list of entries plus a cursor, seeded with
  `"/"`; `Push` after going back discards the forward branch.
- `Router` stores each entry's `FullPath` as the location and the `RouteLocation` as `State` (it
  holds `RouteRecord` references, so a persisting history should store the location string only).
- Replace it by registering your `IRouterHistory` before `AddAquariusRouter`, or pass it to
  `new Router(routes, history)`.
- A guard redirect during `Go` abandons the history move and starts a fresh `Push`.

## Router resolution and RouterLink

Which router a control uses, read at `Loaded` (inheritance has settled by then):

| Control | Order |
| --- | --- |
| `RouterView` | `RouterView.Router` -> inherited `RouterScope.Router` -> the nearest ancestor `RouterView`'s router -> `Router.Current` |
| `RouterLink` | `RouterLink.Router` -> inherited `RouterScope.Router` -> `Router.Current` |
| `RouterWindows` | `RouterScope.Router` on the host -> `Router.Current` |

None found: an `InvalidOperationException` is posted to the Dispatcher (a synchronous throw from
inside WPF's `Loaded` broadcast would take the process down instead of being catchable).

- The target is resolved against the current route on load and on every `CurrentRoute` change;
  one that matches nothing leaves the link inactive rather than throwing during load.
- `IsActive`: the target's leaf record appears anywhere in the current `Matched` chain - or, when
  that leaf is an index child, its parent does - and every param the target specifies equals the
  current one.
- `IsExactActive`: `IsActive`, the match is the current leaf, and the params are exactly equal.
- `To` can change while loaded (a binding, a recycled item container); tracking restarts.
- Clicks go through `Directives.Event`/`Directives.Command` and fire-and-forget `Push`/`Replace`;
  failures are dropped, exceptions rethrown on the element's Dispatcher. Modifier-click handling
  is deliberately not ported - WPF `Click` carries no modifier or button information.

## In-app search

`new RouteSearchIndex(router)` - keep one per ViewModel; `Query(text)` re-reads
`router.GetRoutes()` each call, so routes added or removed later show up on the next keystroke.

- Terms split on spaces; every term must appear in the entry's `Title` or `Keywords` (AND).
- Folding is case-insensitive and strips combining marks only on Latin letters (plus `đ`/`Đ`), so
  "doi mat khau" finds "Đổi mật khẩu" while Japanese dakuten, Thai vowel signs and Cyrillic `й`
  stay intact. Arabic harakat and Hebrew niqqud are kept too - put an unvowelled spelling in
  `Keywords`.
- Rank, lower first: 0 title equals the query, 1 title starts with it, 2 every term in the title,
  3 some term only in `Keywords`. Ties keep `GetRoutes()` order, which is the matcher's ranking
  order (declaration order only among equally ranked paths), then each record's `Search` order.
- `TextResolver` (e.g. `key => localizer[key]`) runs on `Title` and `Keywords` on every query, so a
  language switch shows on the next keystroke. Bind `RouteSearchResult.Title` (resolved), not
  `Entry.Title` (the key).
- `RouteSearchResult.To` is `new RouteLocationRaw { Name = Record.Name }` - it throws
  `InvalidOperationException` when the record has no `Name`, and resolving it throws when the
  route needs a param the current location cannot supply.
- Only matchable records are searched, so entries on a nameless grouping record never appear.
- No search-box control ships; the sample's `MainWindow.xaml` + `MainViewModel.cs` build one from a
  `TextBox` and a `ListBox`.

## What keeps a View alive

- `RouteRecord.Instances` holds the live View (and through it the ViewModel) while the record is
  matched. Leaving clears it at commit; an entry that is cancelled, redirected, superseded or
  throws (a guard, data hook or View constructor) removes the Views it built.
  `RouterLeakTests` pins that a left route's View and ViewModel are collectable.
- `RouterView`, `RouterLink` and `RouterWindows` watch `CurrentRoute` only while loaded, and
  re-arm safely on a second `Loaded` (a `TabControl` raises it twice).
- Your code is what leaks: a singleton-registered routed View or ViewModel, a
  `Watch.On(router.CurrentRoute, ...)` never disposed, a subscription started in `IOnRouteEnter`
  on an instance that a later guard discarded, or an independent-stack `Router` outliving its
  dialog.
