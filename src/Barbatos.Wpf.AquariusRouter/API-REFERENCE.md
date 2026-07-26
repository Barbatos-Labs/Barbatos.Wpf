# Barbatos.Wpf.AquariusRouter API Reference

A member-by-member reference for every public type in `Barbatos.Wpf.AquariusRouter`. See the
[README](README.md) for a concept-first walkthrough with worked examples - this document is for
looking up a specific member's exact signature/behavior.

## Namespaces

| Namespace | Contents |
| --- | --- |
| [`Barbatos.Wpf.AquariusRouter.Matching`](#namespace-barbatoswpfaquariusroutermatching) | The path-matching engine: tokenizer, compiled path parser/ranker, route matcher. No WPF dependency. |
| [`Barbatos.Wpf.AquariusRouter.Routing`](#namespace-barbatoswpfaquariusrouterrouting) | The route table, resolved locations, the `Router` orchestrator, guards, failures, history. |
| [`Barbatos.Wpf.AquariusRouter.Xaml`](#namespace-barbatoswpfaquariusrouterxaml) | `RouterView` and `RouterLink`. |

All three are mapped onto one XAML namespace, `http://schemas.barbatos.co/aquariusrouter/2026/xaml`
(prefix `aqr`), via `[assembly: XmlnsDefinition]` in `AssemblyInfo.cs`.

---

## Namespace: `Barbatos.Wpf.AquariusRouter.Matching`

### `PathTokenKind` (enum)

| Member | Meaning |
| --- | --- |
| `Static` | Literal text, matched verbatim. |
| `Param` | A named, capturing segment (`:name`). |

### `PathToken` (sealed record)

One token produced by `PathTokenizer.Tokenize`.

| Member | Type | Meaning |
| --- | --- | --- |
| `Kind` | `PathTokenKind` | Static or Param. |
| `Value` | `string` | Literal text (Static) or param name (Param). |
| `Regex` | `string` | Custom regex from `:name(regex)`; `""` if none. Param only. |
| `Optional` | `bool` | `:name?` or `:name*`. |
| `Repeatable` | `bool` | `:name+` or `:name*`. |
| `static Static(string value)` | `PathToken` | Creates a Static token. |
| `static Param(string name, string regex, bool optional, bool repeatable)` | `PathToken` | Creates a Param token. |

### `PathTokenizer` (static class)

`static IReadOnlyList<IReadOnlyList<PathToken>> Tokenize(string path)`

Splits a path into per-`/`-segment token lists. `""` → one empty segment; `"/"` → one segment with
a single empty Static token. Throws `ArgumentException` if `path` doesn't start with `/` (and
isn't empty), `FormatException` for a malformed grammar (unterminated custom regex, or a
repeatable param sharing a segment with another token).

### `PathParserParamKey` (sealed record)

`(string Name, bool Repeatable, bool Optional)` - one param key extracted while compiling a
`PathParser`.

### `PathParserOptions` (sealed record)

| Member | Default | Meaning |
| --- | --- | --- |
| `Sensitive` | `false` | Case-sensitive matching. |
| `Strict` | `false` | Disallows an extra trailing slash from also matching. |

### `PathParser` (sealed class)

Compiles tokenized segments into a matchable regex, a ranking score, and param keys.

| Member | Type | Meaning |
| --- | --- | --- |
| `Regex` | `Regex` | The compiled matcher. |
| `Score` | `IReadOnlyList<double[]>` | One array per segment; higher sorts first. Compared via `Compare`. |
| `Keys` | `IReadOnlyList<PathParserParamKey>` | Every Param token's key, in capture-group order. |
| `static Create(IReadOnlyList<IReadOnlyList<PathToken>> segments, PathParserOptions? options = null)` | `PathParser` | Compiles tokenized segments (see `PathTokenizer.Tokenize`). |
| `Parse(string path)` | `IReadOnlyDictionary<string, object>?` | Matches and extracts params, or `null`. A value is `string`, or `IReadOnlyList<string>` for a repeatable param with at least one occurrence - an empty/absent optional param is always `""`, never `[]`. |
| `Stringify(IReadOnlyDictionary<string, object?> parameters)` | `string` | Builds a path string from params. Throws `InvalidOperationException` for a missing required param or a non-repeatable param given a list. |
| `static Compare(PathParser a, PathParser b)` | `int` | Negative if `a` should rank before `b` (more specific), positive if after, `0` if equal. |

### `RouteMatcher` (sealed class)

Compiles a route table and resolves locations against it. Operates directly on
`Routing.RouteRecord`.

| Member | Signature | Meaning |
| --- | --- | --- |
| Constructor | `RouteMatcher(IEnumerable<RouteRecord>? routes = null, PathParserOptions? options = null)` | Optionally pre-populated via `AddRoute`. |
| `AddRoute` | `void AddRoute(RouteRecord record)` | Registers a top-level route (and its `Children`/`Alias`). |
| `AddRoute` | `void AddRoute(string parentName, RouteRecord record)` | Registers `record` as a child of the route named `parentName` - the dynamic-routing counterpart of nesting under `Children` at authoring time. |
| `RemoveRoute` | `void RemoveRoute(RouteRecord record)` | Unregisters `record` and its children. |
| `RemoveRoute` | `void RemoveRoute(string name)` | Unregisters whichever route is currently named `name`. |
| `HasRoute` | `bool HasRoute(string name)` | |
| `GetRoutes` | `IReadOnlyList<RouteRecord> GetRoutes()` | Every currently-registered, matchable record (deduplicated across aliases). |
| `Resolve` | `RouteLocation Resolve(RouteLocationRaw location, RouteLocation? current = null)` | Resolves by `Name`, by `Path`, or - if neither is set - relative to `current`. Handles path-matching only; `Router.Resolve` layers query/hash/full-path on top. |

---

## Namespace: `Barbatos.Wpf.AquariusRouter.Routing`

### `RouteRecord` (sealed class)

A route-table entry. Reference type deliberately (not a `record`) - object identity is what makes
a route "the same route" across navigations. See [The Route Table](README.md#the-route-table) for
a worked example.

| Member | Type | Meaning |
| --- | --- | --- |
| `Path` | `required string` | This record's own path contribution (relative unless it starts with `/`, or empty for an "index" child). |
| `Name` | `string?` | Unique navigation key. |
| `View` | `Type?` | The default-outlet View. Shorthand for `Views["default"]`. |
| `Views` | `IReadOnlyDictionary<string, Type>?` | Named outlets. |
| `Children` | `IReadOnlyList<RouteRecord>?` | Nested child records. |
| `Redirect` | `Func<RouteLocation, RouteLocationRaw>?` | Resolved before any guard runs - see README. |
| `Alias` | `IReadOnlyList<string>?` | Alternate paths resolving to this same record. |
| `Meta` | `IReadOnlyDictionary<string, object?>?` | Arbitrary data, merged root-to-leaf into `RouteLocation.Meta`. |
| `BeforeEnter` | `NavigationGuard?` | Per-route guard, only for a genuinely new entry. |
| `Instances` | `IDictionary<string, FrameworkElement>` | Runtime bookkeeping: the currently-mounted View per outlet name. Written by `Router`/`RouterView`; not part of route-table configuration. |

### `RouteLocation` (sealed record)

A fully resolved navigation location - what `Router.CurrentRoute` holds.

| Member | Type | Meaning |
| --- | --- | --- |
| `static Start` | `RouteLocation` | Sentinel `Router.CurrentRoute` starts at before the first navigation (`Path="/"`, empty `Matched`). |
| `Path` | `required string` | Resolved path, params substituted in. |
| `Name` | `string?` | The matched leaf record's name. |
| `Params` | `IReadOnlyDictionary<string, object?>` | `string` or `IReadOnlyList<string>` (repeatable) per key. |
| `Query` | `IReadOnlyDictionary<string, string?>` | Extra key/value data, uninvolved in matching. |
| `Hash` | `string?` | Arbitrary fragment, kept for protocol/deep-link round-tripping. |
| `FullPath` | `required string` | `Path` plus serialized `Query`/`Hash`. |
| `Matched` | `required IReadOnlyList<RouteRecord>` | Every matched record, root to leaf. |
| `Meta` | `IReadOnlyDictionary<string, object?>` | `Matched`'s own `Meta` merged root-to-leaf, recomputed every resolve. |
| `IsSameLocation(RouteLocation? other)` | `bool` | Same leaf record (by reference) + equal params/query/hash. Use this, not `==`/`Equals`, to test "same target" - see the type's own remarks. |

### `RouteLocationRaw` (sealed record)

A navigation target, as passed to `Push`/`Replace`/`Resolve`.

| Member | Type | Meaning |
| --- | --- | --- |
| `Path` | `string?` | A literal path. **Ignores `Params` when set** (matches Vue Router's own documented rule). |
| `Name` | `string?` | A route name, combined with `Params`. |
| `Params` | `IReadOnlyDictionary<string, object?>?` | Ignored when `Path` is set. |
| `Query` | `IReadOnlyDictionary<string, string?>?` | |
| `Hash` | `string?` | |
| `Replace` | `bool` | Replace instead of push. |
| `Force` | `bool` | Bypass the duplicate-navigation short-circuit. |
| `implicit operator RouteLocationRaw(string path)` | | Shorthand for `new RouteLocationRaw { Path = path }`. |

### `NavigationGuard` (delegate)

`Task<NavigationGuardResult> NavigationGuard(RouteLocation to, RouteLocation from)` - always async
(see [Navigation Guards](README.md#navigation-guards) for why no sync overload exists).

### `NavigationHook` (delegate)

`void NavigationHook(RouteLocation to, RouteLocation from, NavigationFailure? failure)` - a
non-cancelling observer, see `Router.AfterEach`.

### `NavigationGuardEffect` (enum)

`Continue`, `Cancel`, `Redirect`.

### `NavigationGuardResult` (readonly struct)

| Member | Type | Meaning |
| --- | --- | --- |
| `Effect` | `NavigationGuardEffect` | |
| `RedirectTo` | `RouteLocationRaw?` | Set only when `Effect` is `Redirect`. |
| `static Continue` | `NavigationGuardResult` | |
| `static Cancel` | `NavigationGuardResult` | |
| `static Redirect(RouteLocationRaw to)` | `NavigationGuardResult` | |
| `implicit operator NavigationGuardResult(bool shouldContinue)` | | `true`/`false` → `Continue`/`Cancel`, matching Vue Router's own guard-return convention. |

### `IOnBeforeRouteEnter` / `IOnBeforeRouteUpdate` / `IOnBeforeRouteLeave` (interfaces)

One method each: `Task<NavigationGuardResult> OnBeforeRouteXxx(RouteLocation to, RouteLocation from)`.
See [Navigation Guards](README.md#navigation-guards) for firing semantics and pipeline order.

### `IOnRouteEnter` / `IOnRouteUpdate` (interfaces)

One method each: `void OnRouteXxx(RouteLocation to, RouteLocation from)` - non-cancelling
companions to the guard interfaces above, fired immediately before them.

### `NavigationFailureType` (`[Flags]` enum)

| Member | Value | Meaning |
| --- | --- | --- |
| `Aborted` | `4` | A guard returned/redirected to `Cancel`. |
| `Cancelled` | `8` | A newer navigation superseded this one. |
| `Duplicated` | `16` | Resolved to `IsSameLocation` as the current route, and `Force` wasn't set. |

Values match Vue Router's own real bit flags exactly (not sequential ints), so
`IsNavigationFailure(f, Aborted | Cancelled)` checks more than one type at once.

### `NavigationFailure` (sealed record)

`{ required NavigationFailureType Type, required RouteLocation To, required RouteLocation From }`

### `NavigationFailures` (static class)

`static bool IsNavigationFailure(NavigationFailure? failure, NavigationFailureType? type = null)` -
`true` if `failure` is non-null and (if `type` is given) its `Type` includes at least one requested flag.

### `IRouterHistory` (interface)

| Member | Signature | Meaning |
| --- | --- | --- |
| `Location` | `string { get; }` | Path at the current position. |
| `State` | `object? { get; }` | Opaque data at the current position. |
| `Push` | `void Push(string location, object? state = null)` | Adds an entry, discarding anything ahead of it. |
| `Replace` | `void Replace(string location, object? state = null)` | Swaps the current entry in place. |
| `Peek` | `string? Peek(int delta)` | The location `delta` steps away, without moving - `null` if out of range. |
| `Go` | `void Go(int delta)` | Moves the position, clamped to the valid range. |

### `MemoryRouterHistory` (sealed class)

The only shipped `IRouterHistory` - an in-process array of entries plus an integer cursor. No
browser/URL dependency. `Router.Start` must be called once at app startup to seed the first entry.

### `Router` (sealed class)

See [The Router](README.md#the-router) for a concept-first walkthrough.

| Member | Signature | Meaning |
| --- | --- | --- |
| Constructor | `Router(IEnumerable<RouteRecord> routes, IRouterHistory? history = null, PathParserOptions? pathOptions = null)` | |
| `static Current` | `Router? { get; set; }` | Ambient default for `RouterView`/`RouterLink`. |
| `CurrentRoute` | `Ref<RouteLocation> { get; }` | The live, current location. |
| `Resolve` | `RouteLocation Resolve(RouteLocationRaw location)` | Resolves without navigating. |
| `Push` | `Task<NavigationFailure?> Push(RouteLocationRaw to)` | Navigates, running the full guard pipeline. |
| `Replace` | `Task<NavigationFailure?> Replace(RouteLocationRaw to)` | Like `Push`, replacing the current history entry. |
| `Start` | `Task<NavigationFailure?> Start(RouteLocationRaw initial)` | The very first navigation - alias for `Push`. |
| `Go` | `Task<NavigationFailure?> Go(int delta)` | Moves through history; silent no-op if out of range. |
| `Back` / `Forward` | `Task<NavigationFailure?> Back()` / `Forward()` | `Go(-1)` / `Go(1)`. |
| `BeforeEach` | `IDisposable BeforeEach(NavigationGuard guard)` | Global guard, after leave guards. |
| `BeforeResolve` | `IDisposable BeforeResolve(NavigationGuard guard)` | Global guard, checked last. |
| `AfterEach` | `IDisposable AfterEach(NavigationHook hook)` | Non-cancelling, fires for every outcome except an in-progress redirect. |
| `AddRoute` | `void AddRoute(RouteRecord record)` / `void AddRoute(string parentName, RouteRecord record)` | Pass-through to the underlying `RouteMatcher`. |
| `RemoveRoute` | `void RemoveRoute(RouteRecord record)` / `void RemoveRoute(string name)` | |
| `HasRoute` | `bool HasRoute(string name)` | |
| `GetRoutes` | `IReadOnlyList<RouteRecord> GetRoutes()` | |

---

## Namespace: `Barbatos.Wpf.AquariusRouter.Xaml`

### `RouterView` (sealed class, `ContentControl`)

| Member | Type | Meaning |
| --- | --- | --- |
| `RouterProperty` / `Router` | `DependencyProperty` / `Router?` | Explicit override; falls back to `Router.Current`. |
| `OutletNameProperty` / `OutletName` | `DependencyProperty` / `string` | The named outlet fed from a matched record's `Views` map. Default `"default"`. |

`Content` is entirely managed - never assign it directly. See
[RouterView](README.md#routerview) for the depth-resolution/nesting model.

### `RouterLink` (static class - attached properties)

| Member | Type | Meaning |
| --- | --- | --- |
| `ToProperty` / `Get/SetTo` | `DependencyProperty` / `object?` | A `string` path or a `RouteLocationRaw`. Settable on `ButtonBase` or `Hyperlink` only. |
| `ReplaceProperty` / `Get/SetReplace` | `DependencyProperty` / `bool` | Navigate via `Replace` instead of `Push`. |
| `RouterProperty` / `Get/SetRouter` | `DependencyProperty` / `Router?` | Explicit override; falls back to `Router.Current`. |
| `IsActiveProperty` / `GetIsActive` | `DependencyProperty` (read-only) / `bool` | Current route is this link's target or a descendant of it. |
| `IsExactActiveProperty` / `GetIsExactActive` | `DependencyProperty` (read-only) / `bool` | Current route is exactly this link's target, including every param. |

See [RouterLink](README.md#routerlink) for the active-state algorithm and a `DataTrigger` example.
