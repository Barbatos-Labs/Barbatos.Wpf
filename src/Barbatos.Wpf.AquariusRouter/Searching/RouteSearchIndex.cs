// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.AquariusRouter.Routing;

namespace Barbatos.Wpf.AquariusRouter.Searching;

/// <summary>
/// Queries every <see cref="RouteSearchEntry"/> across a <see cref="Router"/>'s whole route table
/// (including nested children) and ranks the matches - the actual "jump straight to the feature"
/// engine behind an app's own search box. Deliberately just a query function, not a UI control
/// itself (a search results dropdown/popup is exactly the kind of "manages its own visibility and
/// child content" concern <see cref="Xaml.RouterLink"/>'s own remarks describe as belonging to a
/// real templated control, not this library - so building one is left to the app, exactly like
/// <see cref="Xaml.RouterView"/> is the only concern this library actually hosts).
/// </summary>
/// <remarks>
/// <code>
/// var index = new RouteSearchIndex(router);
/// IReadOnlyList&lt;RouteSearchResult&gt; results = index.Query("doi mat khau");
/// if (results.Count > 0)
///     await router.Push(results[0].To);
/// </code>
/// Always re-reads <see cref="Router.GetRoutes"/> on every <see cref="Query"/> call rather than
/// snapshotting the route table once - a route added/removed later via
/// <see cref="Router.AddRoute(RouteRecord)"/>/<see cref="Router.RemoveRoute(RouteRecord)"/> is
/// reflected on the very next query, with no separate invalidation step to remember.
/// <para>
/// <b>Matching</b> splits the query into space-separated terms and requires <em>every</em> term to
/// appear somewhere in an entry's <see cref="RouteSearchEntry.Title"/> or
/// <see cref="RouteSearchEntry.Keywords"/> combined (AND semantics - each extra word narrows the
/// results, the same way most real search boxes behave) - comparison is case-insensitive and,
/// deliberately, Vietnamese-diacritic-insensitive (<c>"chuyen tien"</c>/<c>"dang nhap"</c> match
/// <c>"Chuyển tiền"</c>/<c>"Đăng nhập"</c> just as well as the fully-accented spelling), matching
/// how a large share of real Vietnamese users actually type into a search box. <b>Ranking</b> then
/// prefers whichever entries matched via <see cref="RouteSearchEntry.Title"/> itself over ones that
/// only matched via <see cref="RouteSearchEntry.Keywords"/>, with an exact/prefix title match
/// ranked above a same-tier match; ties preserve registration order.
/// </para>
/// </remarks>
public sealed class RouteSearchIndex(Router router)
{
    /// <summary>
    /// Every <see cref="RouteSearchEntry"/> across the whole route table that matches every term in
    /// <paramref name="query"/>, best match first. Empty (never <see langword="null"/>) for a
    /// <see langword="null"/>/blank/all-whitespace query - there is no meaningful "match everything"
    /// result set for an empty search box, unlike an unfiltered route listing.
    /// </summary>
    public IReadOnlyList<RouteSearchResult> Query(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < terms.Length; i++)
            terms[i] = SearchText.Fold(terms[i]);

        var joined = string.Join(' ', terms);
        var matches = new List<(int Rank, RouteSearchResult Result)>();

        foreach (var record in router.GetRoutes())
        {
            foreach (var entry in record.Search ?? [])
            {
                if (Rank(entry, terms, joined) is { } rank)
                    matches.Add((rank, new RouteSearchResult { Record = record, Entry = entry }));
            }
        }

        // A stable sort, so entries that tie on rank keep route-registration order - List.Sort is
        // introsort and would not preserve it.
        return matches.OrderBy(m => m.Rank).Select(m => m.Result).ToList();
    }

    /// <summary>
    /// <see langword="null"/> if any of <paramref name="terms"/> appears nowhere in
    /// <paramref name="entry"/>'s title+keywords combined; otherwise a rank where <b>lower is
    /// better</b>: 0 = title equals the whole query exactly, 1 = title starts with it, 2 = every
    /// term appears in the title (in any order), 3 = at least one term was only found via
    /// <see cref="RouteSearchEntry.Keywords"/>.
    /// </summary>
    private static int? Rank(RouteSearchEntry entry, string[] terms, string joined)
    {
        var haystack = entry.FoldedHaystack;
        foreach (var term in terms)
        {
            if (!haystack.Contains(term, StringComparison.Ordinal))
                return null;
        }

        var title = entry.FoldedTitle;
        if (title.Equals(joined, StringComparison.Ordinal))
            return 0;
        if (title.StartsWith(joined, StringComparison.Ordinal))
            return 1;

        foreach (var term in terms)
        {
            if (!title.Contains(term, StringComparison.Ordinal))
                return 3;
        }

        return 2;
    }
}
