// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.AquariusRouter.Routing;

namespace Barbatos.Wpf.AquariusRouter.Searching;

/// <summary>
/// One searchable entry for a <see cref="RouteRecord"/> - the in-app-search counterpart of a
/// nav-bar <c>RouterLink</c>, but text-driven instead of click-driven: a super-app or banking-app
/// "jump straight to the feature you need" search bar, letting a user skip however many menu
/// levels a deeply-nested route actually lives under. No Vue Router equivalent exists to port -
/// this is new surface for this library.
/// </summary>
/// <remarks>
/// <code>
/// new RouteRecord
/// {
///     Path = "security", Name = "settings-security", View = typeof(SettingsSecurityView),
///     Search =
///     [
///         new RouteSearchEntry { Title = "Security settings" },
///         new RouteSearchEntry { Title = "Change password", Keywords = "security password" },
///     ],
/// }
/// </code>
/// <see cref="RouteRecord.Search"/> is a <em>list</em>, not a single entry, specifically so one
/// route can be found under several unrelated-looking phrasings (synonyms a real user might type)
/// without needing a second route just to carry a different title - see <see cref="RouteSearchIndex"/>
/// for how these get matched/ranked, and <see cref="RouteSearchResult.To"/> for how a match turns
/// back into a navigation.
/// </remarks>
public sealed class RouteSearchEntry
{
    private string? _foldedTitle;
    private string? _foldedHaystack;

    /// <summary>The display text shown for this entry, and the primary text matched against a query - see <see cref="RouteSearchIndex"/>.</summary>
    public required string Title { get; init; }

    /// <summary>
    /// Extra, unshown terms that also match a query - synonyms/abbreviations a user might type
    /// that don't belong in the displayed <see cref="Title"/> itself (e.g. <c>"đổi mật khẩu"</c>
    /// alongside a <see cref="Title"/> of <c>"Security settings"</c>). Free-form, space-separated;
    /// no fixed delimiter syntax is enforced.
    /// </summary>
    public string? Keywords { get; init; }

    /// <summary>
    /// <see cref="Title"/> in <see cref="SearchText.Fold"/>ed form, computed once on first use.
    /// This type is immutable (every property is <c>init</c>-only), so the folded text can never
    /// go stale - and a search box re-queries on every keystroke, so re-folding each entry's text
    /// per character typed would dominate the cost of searching entirely.
    /// </summary>
    internal string FoldedTitle => _foldedTitle ??= SearchText.Fold(Title);

    /// <summary><see cref="FoldedTitle"/> plus <see cref="Keywords"/>, the single string a query's terms are searched in. Cached for the same reason.</summary>
    internal string FoldedHaystack =>
        _foldedHaystack ??= Keywords is null ? FoldedTitle : $"{FoldedTitle} {SearchText.Fold(Keywords)}";
}
