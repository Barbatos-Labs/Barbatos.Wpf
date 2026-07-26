// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.AquariusRouter.Routing;

/// <summary>
/// A navigation target, as passed to <see cref="Router.Push"/>/<see cref="Router.Replace"/>/
/// <see cref="Router.Resolve"/> or a <see cref="RouteRecord.Redirect"/> delegate's return value -
/// the WPF/C# counterpart of Vue Router's own <c>RouteLocationRaw</c>.
/// </summary>
/// <remarks>
/// Set either <see cref="Path"/> (a literal path string) or <see cref="Name"/> (a named route,
/// with <see cref="Params"/> filled in) - not both meaningfully at once. Matching Vue Router's
/// own explicitly-documented, easy-to-miss rule: <b><see cref="Params"/> is ignored whenever
/// <see cref="Path"/> is also set</b> - a path string is used verbatim, never interpolated with
/// params, since there would be no reliable way to know which <c>:segment</c> a given param name
/// was meant to fill without re-parsing the string. Use <see cref="Name"/>+<see cref="Params"/>
/// (or build the full path string yourself) to get real substitution.
/// <code>
/// await router.Push("/users/42");                                            // implicit from string
/// await router.Push(new RouteLocationRaw { Name = "user", Params = new() { ["id"] = "42" } });
/// await router.Push(new RouteLocationRaw { Path = "/search", Query = new() { ["q"] = "hello" } });
/// </code>
/// </remarks>
public sealed record RouteLocationRaw
{
    /// <summary>A literal path to resolve, e.g. <c>"/users/42"</c>. See the type's own remarks for why this ignores <see cref="Params"/>.</summary>
    public string? Path { get; init; }

    /// <summary>A route's <see cref="RouteRecord.Name"/> to resolve by, combined with <see cref="Params"/>.</summary>
    public string? Name { get; init; }

    /// <summary>
    /// Values for the target's named params - a <see cref="string"/>, or an
    /// <see cref="IReadOnlyList{T}"/> of <see cref="string"/> for a repeatable param. Ignored
    /// entirely when <see cref="Path"/> is set (see this type's own remarks).
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Params { get; init; }

    /// <summary>Extra key/value data carried alongside the navigation, not involved in path matching.</summary>
    public IReadOnlyDictionary<string, string?>? Query { get; init; }

    /// <summary>An arbitrary fragment string, kept for protocol/deep-link round-tripping - see <see cref="RouteLocation.Hash"/>.</summary>
    public string? Hash { get; init; }

    /// <summary>When <see langword="true"/>, replaces the current history entry instead of pushing a new one - see <see cref="IRouterHistory.Replace"/>.</summary>
    public bool Replace { get; init; }

    /// <summary>
    /// When <see langword="true"/>, navigates even if the resolved target is identical to the
    /// current route (which would otherwise short-circuit as <see cref="NavigationFailureType.Duplicated"/>).
    /// </summary>
    public bool Force { get; init; }

    /// <summary>Shorthand for <c>new RouteLocationRaw { Path = path }</c>.</summary>
    public static implicit operator RouteLocationRaw(string path) => new() { Path = path };
}
