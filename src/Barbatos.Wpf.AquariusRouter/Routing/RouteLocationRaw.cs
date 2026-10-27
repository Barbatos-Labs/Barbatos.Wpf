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
/// await router.Push(new RouteLocationRaw { Name = "user", Params = new Dictionary&lt;string, object?&gt; { ["id"] = "42" } });
/// await router.Push(new RouteLocationRaw { Path = "/search", Query = new Dictionary&lt;string, string?&gt; { ["q"] = "hello" } });
/// </code>
/// <see cref="Params"/> and <see cref="Query"/> are read-only dictionary interfaces, so a
/// target-typed <c>new() { ... }</c> does not compile against them - name the concrete
/// <see cref="Dictionary{TKey, TValue}"/>.
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

    /// <summary>
    /// Splits a serialized <c>path[?query][#hash]</c> string - a <see cref="RouteLocation.FullPath"/> -
    /// back into its parts, percent-decoding the query. The exact inverse of how
    /// <see cref="Router.Resolve"/> composes one.
    /// </summary>
    /// <remarks>
    /// Two things need this. <see cref="Router.Go"/>/<see cref="Router.Back"/>/<see cref="Router.Forward"/>
    /// use it internally, because <see cref="IRouterHistory"/> stores each entry as its full path
    /// string - moving back to an entry that carried a query means parsing that string, not
    /// matching it as though the whole thing were a path.
    /// <para>
    /// The other is protocol activation: a WPF app registered as a custom URI-scheme handler is
    /// handed something like <c>myapp://users/42?ref=mail#detail</c> on startup, and this is what
    /// turns it into something <see cref="Router.Push"/> accepts. Take it apart with
    /// <see cref="Uri"/> first, minding two things: <see cref="Uri"/> reads the first segment after
    /// <c>//</c> as the <i>host</i> (<c>users</c> here), so <see cref="Uri.PathAndQuery"/> alone is
    /// only <c>/42?ref=mail</c>; and the fragment is not part of <see cref="Uri.PathAndQuery"/> either:
    /// </para>
    /// <code>
    /// var uri = new Uri(activationArgument);   // myapp://users/42?ref=mail#detail
    /// await router.Push(RouteLocationRaw.Parse($"/{uri.Host}{uri.PathAndQuery}{uri.Fragment}"));
    /// </code>
    /// A scheme whose URIs carry an empty authority instead (<c>myapp:///users/42</c>) has an empty
    /// <see cref="Uri.Host"/> and the whole path in <see cref="Uri.PathAndQuery"/>, so there it is
    /// <c>uri.PathAndQuery + uri.Fragment</c> on its own - pick one form for the scheme and parse
    /// that form.
    /// </remarks>
    /// <param name="fullPath">e.g. <c>"/search?q=hello%20world#results"</c>.</param>
    public static RouteLocationRaw Parse(string fullPath)
    {
        ArgumentNullException.ThrowIfNull(fullPath);

        var rest = fullPath;
        string? hash = null;
        Dictionary<string, string?>? query = null;

        // Hash first: it is appended last, so everything after the first '#' is the fragment.
        // A '#' or '?' *inside* a query value was percent-encoded on the way out, so neither
        // split can be fooled by one.
        var hashIndex = rest.IndexOf('#', StringComparison.Ordinal);
        if (hashIndex >= 0)
        {
            hash = rest[(hashIndex + 1)..];
            rest = rest[..hashIndex];
        }

        var queryIndex = rest.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex >= 0)
        {
            query = ParseQuery(rest[(queryIndex + 1)..]);
            rest = rest[..queryIndex];
        }

        return new RouteLocationRaw { Path = rest, Query = query, Hash = hash };
    }

    private static Dictionary<string, string?> ParseQuery(string query)
    {
        var parsed = new Dictionary<string, string?>();

        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=', StringComparison.Ordinal);

            // No '=' at all is a valueless key, which is exactly how a null-valued Query entry is
            // serialized - round-tripping it back to null rather than "" keeps the two distinct.
            if (separator < 0)
                parsed[Uri.UnescapeDataString(pair)] = null;
            else
                parsed[Uri.UnescapeDataString(pair[..separator])] = Uri.UnescapeDataString(pair[(separator + 1)..]);
        }

        return parsed;
    }
}
