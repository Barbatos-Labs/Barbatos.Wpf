// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using Barbatos.Wpf.AquariusRouter.Searching;

namespace Barbatos.Wpf.AquariusRouter.Routing;

/// <summary>
/// One entry in a route table - the WPF/C# counterpart of a Vue Router <c>RouteRecordRaw</c>.
/// </summary>
/// <remarks>
/// <code>
/// new RouteRecord
/// {
///     Path = "/users/:id",
///     Name = "user",
///     View = typeof(UserView),
///     Meta = new Dictionary&lt;string, object?&gt; { ["requiresAuth"] = true },
/// }
/// </code>
/// A deliberate reference type (not a <see langword="record"/>): object identity is what makes a
/// route record "the same route" across navigations - the guard pipeline's leaving/updating/
/// entering diff (see <see cref="Router"/>) compares records by reference, exactly like Vue
/// Router's own <c>isSameRouteRecord</c>, and this record's <see cref="Instances"/> dictionary
/// is mutable, per-object runtime state that has to persist across the record's whole lifetime
/// in the route table - two structurally-identical-but-distinct records must never be treated as
/// interchangeable.
/// <para>
/// <b>Unlike Vue Router</b>, there is no separate "raw" vs. "normalized" record type - one
/// <see cref="RouteRecord"/> plays both roles. Vue's split exists partly for TypeScript
/// structural-typing reasons and partly because a route's <c>meta</c> only gets merged with its
/// ancestors' at resolve time; here, the same merge happens fresh on every
/// <see cref="Matching.RouteMatcher.Resolve"/> call (see <see cref="RouteLocation.Meta"/>), so a
/// second type would add ceremony without adding anything this port actually needs.
/// </para>
/// </remarks>
public sealed class RouteRecord
{
    private IReadOnlyDictionary<string, Type>? _resolvedViews;
    private bool _resolvedViewsComputed;

    /// <summary>
    /// This record's own path contribution. Relative to its parent by default (no leading
    /// <c>/</c> - joined onto the parent's own resolved path); a leading <c>/</c> makes it
    /// absolute, ignoring nesting. An empty string is the "index" case - it resolves to exactly
    /// the parent's own path (see <see cref="Children"/>).
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// A unique name for navigating without hardcoding a path string - see
    /// <see cref="RouteLocationRaw.Name"/>. Registering a second record with an already-used name
    /// silently replaces the first (matching Vue Router's own real behavior).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// The <see cref="FrameworkElement"/>-derived View type rendered into the default outlet
    /// (an unnamed <see cref="Xaml.RouterView"/>) when this record matches. Shorthand for
    /// <see cref="Views"/> with a single <c>"default"</c> entry - set at most one of the two.
    /// Neither is required: a record with no <see cref="View"/>/<see cref="Views"/> (only
    /// <see cref="Children"/>, or only <see cref="Redirect"/>) is a pure grouping/redirect
    /// record, matching Vue Router's own "component-less parent" pattern.
    /// </summary>
    public Type? View { get; init; }

    /// <summary>
    /// Named outlets: each key matches a <see cref="Xaml.RouterView.OutletName"/> elsewhere in the
    /// matched View tree at this record's depth. See <see cref="View"/> for the common
    /// single-outlet shorthand.
    /// </summary>
    public IReadOnlyDictionary<string, Type>? Views { get; init; }

    /// <summary>Nested child records - see <see cref="Path"/> for how a child's path joins its parent's.</summary>
    public IReadOnlyList<RouteRecord>? Children { get; init; }

    /// <summary>
    /// When set, navigating to this record immediately resolves to a different target instead -
    /// before any navigation guard runs, matching Vue Router's own
    /// real ordering (only the target's guards run, never this record's). Always a delegate
    /// (never a bare value) - collapsing Vue Router's string-or-object-or-function union, since
    /// <c>to =&gt; "/target"</c> is barely more verbose than a bare string and C# has no clean
    /// value-or-function duck typing.
    /// </summary>
    public Func<RouteLocation, RouteLocationRaw>? Redirect { get; init; }

    /// <summary>
    /// Alternate paths that resolve to this exact same record (unlike <see cref="Redirect"/>,
    /// no resolution substitution happens - it's simply reachable under more than one path).
    /// Each entry follows the same relative/absolute rules as <see cref="Path"/>.
    /// </summary>
    public IReadOnlyList<string>? Alias { get; init; }

    /// <summary>
    /// An arbitrary data bag - not interpreted by the router itself. Merged root-to-leaf into
    /// <see cref="RouteLocation.Meta"/> on every resolve (child keys win over parent keys),
    /// matching Vue Router's own real, non-cached, per-resolve merge.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Meta { get; init; }

    /// <summary>
    /// A per-route guard, checked only when genuinely entering this record from a different one
    /// (not on a param/query-only update, and - for a nested record - not when moving between
    /// two of its own children). See <see cref="Router"/>'s guard pipeline for the full ordering.
    /// </summary>
    public NavigationGuard? BeforeEnter { get; init; }

    /// <summary>
    /// Opt-in in-app-search entries for this record - unset (the default) means this route is
    /// simply not searchable. A list, not a single entry, so one route can be found under several
    /// different phrasings without a second route just to carry a different title - see
    /// <see cref="RouteSearchIndex"/>.
    /// </summary>
    public IReadOnlyList<RouteSearchEntry>? Search { get; init; }

    /// <summary>
    /// The View instance currently mounted for this record, per outlet name (<c>"default"</c>
    /// for the unnamed outlet). Runtime bookkeeping, not part of the route table's own
    /// configuration - written by <see cref="Router"/> (on construction, during navigation) and
    /// <see cref="Xaml.RouterView"/> (on mount/unmount), read by <see cref="Router"/> to find the
    /// currently-live instance a leaving/updating navigation guard needs to run against. The
    /// direct C#/WPF analogue of Vue Router's own <c>record.instances</c>.
    /// </summary>
    public IDictionary<string, FrameworkElement> Instances { get; } = new Dictionary<string, FrameworkElement>();

    /// <summary>
    /// <see cref="View"/> normalized into <see cref="Views"/>' shape (a single
    /// <c>"default"</c> entry) when only <see cref="View"/> was set - the same normalization
    /// Vue Router applies to its own <c>component</c> shorthand. <see langword="null"/> for a
    /// pure grouping/redirect record with neither set.
    /// </summary>
    /// <remarks>
    /// Computed once and kept: <see cref="View"/>/<see cref="Views"/> are both <c>init</c>-only so
    /// the answer can never go stale, and this is read on genuinely hot paths - every
    /// <see cref="Xaml.RouterView"/>'s own skip-loop on every navigation, plus
    /// <see cref="Router"/>'s construction pass - where rebuilding the shorthand's dictionary each
    /// time would allocate for nothing.
    /// </remarks>
    internal IReadOnlyDictionary<string, Type>? ResolvedViews
    {
        get
        {
            if (!_resolvedViewsComputed)
            {
                _resolvedViews = Views ?? (View is not null ? new Dictionary<string, Type> { ["default"] = View } : null);
                _resolvedViewsComputed = true;
            }

            return _resolvedViews;
        }
    }
}
