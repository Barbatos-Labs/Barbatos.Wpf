// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.AquariusRouter.Routing;

/// <summary>
/// A fully resolved navigation location - the WPF/C# counterpart of Vue Router's
/// <c>RouteLocationNormalizedLoaded</c>. Produced by <see cref="Matching.RouteMatcher.Resolve"/>,
/// exposed live as <see cref="Router.CurrentRoute"/>.
/// </summary>
/// <remarks>
/// Unlike Vue Router's own three-layer <c>RouteLocationRaw</c>/<c>RouteLocationNormalized</c>/
/// <c>RouteLocationNormalizedLoaded</c> split, there is only one resolved shape here. That split
/// exists in Vue partly for TypeScript structural-typing reasons and partly because a route's
/// components can still be mid-async-load between "matched" and "loaded" - a state that doesn't
/// exist the same way once a matched View is constructed synchronously (see <see cref="Router"/>'s
/// own remarks on this).
/// <para>
/// <b>Do not use <c>==</c>/<see cref="object.Equals(object?)"/> to test "is this the same
/// navigation target"</b> - use <see cref="IsSameLocation"/> instead. This is a
/// <see langword="record"/> purely for its <see langword="init"/>-property/<c>with</c>-expression
/// ergonomics; its auto-generated structural equality falls through to <see cref="Matched"/>
/// (a <see cref="List{T}"/>, which has no value equality of its own) and would silently behave
/// like reference equality there - harmless for <see cref="Barbatos.Wpf.Aquarius.Reactivity.Ref{T}"/>'s own change
/// detection (a fresh <see cref="Matched"/> list is built on every resolve regardless), but wrong
/// for a semantic "did the target actually change" check, which is exactly what
/// <see cref="IsSameLocation"/> does correctly (path/name/params/query/hash only, matching Vue
/// Router's own real <c>isSameRouteLocation</c> comparison).
/// </para>
/// </remarks>
public sealed record RouteLocation
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyParams = new Dictionary<string, object?>();
    private static readonly IReadOnlyDictionary<string, string?> EmptyQuery = new Dictionary<string, string?>();
    private static readonly IReadOnlyDictionary<string, object?> EmptyMeta = new Dictionary<string, object?>();

    /// <summary>
    /// The sentinel location <see cref="Router.CurrentRoute"/> starts at before
    /// <see cref="Router.Start"/>/<see cref="Router.Push"/> ever resolves a real one - an empty
    /// <see cref="Matched"/> list, matching Vue Router's own <c>START_LOCATION_NORMALIZED</c>.
    /// </summary>
    public static RouteLocation Start { get; } = new() { Path = "/", FullPath = "/", Matched = [] };

    /// <summary>The resolved path, e.g. <c>"/users/42"</c> - params already substituted in.</summary>
    public required string Path { get; init; }

    /// <summary>The matched leaf record's <see cref="RouteRecord.Name"/>, if it has one.</summary>
    public string? Name { get; init; }

    /// <summary>
    /// Extracted param values - a <see cref="string"/>, or an <see cref="IReadOnlyList{T}"/> of
    /// <see cref="string"/> for a repeatable param (see <see cref="Matching.PathToken.Repeatable"/>).
    /// </summary>
    public IReadOnlyDictionary<string, object?> Params { get; init; } = EmptyParams;

    /// <summary>Extra key/value data carried alongside the navigation, not involved in path matching.</summary>
    public IReadOnlyDictionary<string, string?> Query { get; init; } = EmptyQuery;

    /// <summary>An arbitrary fragment string. Kept for protocol-activation/deep-link round-tripping (e.g. <c>myapp://search?q=x#results</c>) even though there is no address bar to display it in.</summary>
    public string? Hash { get; init; }

    /// <summary><see cref="Path"/> plus a serialized <see cref="Query"/>/<see cref="Hash"/> - see <see cref="Matching.RouteMatcher"/> for how it's built.</summary>
    public required string FullPath { get; init; }

    /// <summary>
    /// Every matched record from root to leaf - a nested route's own <see cref="Xaml.RouterView"/>
    /// picks its content from this list by depth (see <see cref="Xaml.RouterView"/>'s remarks).
    /// </summary>
    public required IReadOnlyList<RouteRecord> Matched { get; init; }

    /// <summary>
    /// <see cref="RouteRecord.Meta"/> merged root-to-leaf across <see cref="Matched"/> (child
    /// keys win over parent keys) - recomputed fresh on every resolve, never cached on a record,
    /// matching Vue Router's own real per-resolve merge.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Meta { get; init; } = EmptyMeta;

    /// <summary>
    /// Whether <paramref name="other"/> is the same navigation target as this location - the
    /// same leaf <see cref="RouteRecord"/> (by reference), with equal <see cref="Params"/>,
    /// <see cref="Query"/>, and <see cref="Hash"/>. This is what <see cref="Router.Push"/> uses
    /// to short-circuit a no-op navigation as <see cref="NavigationFailureType.Duplicated"/>.
    /// </summary>
    public bool IsSameLocation(RouteLocation? other)
    {
        if (other is null)
            return false;

        var leaf = Matched.Count > 0 ? Matched[^1] : null;
        var otherLeaf = other.Matched.Count > 0 ? other.Matched[^1] : null;
        if (!ReferenceEquals(leaf, otherLeaf))
            return false;

        return ParamsEqual(Params, other.Params) && QueryEqual(Query, other.Query) && Hash == other.Hash;
    }

    /// <summary>Full bidirectional params-dictionary equality - same key count, every value equal (see <see cref="ParamValueEqual"/>). Also used by <see cref="Xaml.RouterLink"/>'s <c>IsExactActive</c> computation.</summary>
    internal static bool ParamsEqual(IReadOnlyDictionary<string, object?> a, IReadOnlyDictionary<string, object?> b)
    {
        if (a.Count != b.Count)
            return false;

        foreach (var (key, value) in a)
        {
            if (!b.TryGetValue(key, out var otherValue) || !ParamValueEqual(value, otherValue))
                return false;
        }

        return true;
    }

    /// <summary>Equality for one param value - list-element-wise for a repeatable param, plain equality otherwise. Also used by <see cref="Xaml.RouterLink"/>.</summary>
    internal static bool ParamValueEqual(object? a, object? b) =>
        a is IReadOnlyList<string> listA && b is IReadOnlyList<string> listB
            ? listA.SequenceEqual(listB)
            : Equals(a, b);

    private static bool QueryEqual(IReadOnlyDictionary<string, string?> a, IReadOnlyDictionary<string, string?> b)
    {
        if (a.Count != b.Count)
            return false;

        foreach (var (key, value) in a)
        {
            if (!b.TryGetValue(key, out var otherValue) || value != otherValue)
                return false;
        }

        return true;
    }
}
