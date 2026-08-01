// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.AquariusRouter.Routing;

namespace Barbatos.Wpf.AquariusRouter.Searching;

/// <summary>
/// One match returned by <see cref="RouteSearchIndex.Query"/> - the <see cref="RouteRecord"/> it
/// came from, which <see cref="RouteSearchEntry"/> matched, and a ready-to-navigate <see cref="To"/>.
/// </summary>
public sealed record RouteSearchResult
{
    /// <summary>The route this result would navigate to.</summary>
    public required RouteRecord Record { get; init; }

    /// <summary>Which of <see cref="Record"/>'s <see cref="RouteRecord.Search"/> entries matched.</summary>
    public required RouteSearchEntry Entry { get; init; }

    /// <summary>
    /// The text to display for this result - <see cref="RouteSearchEntry.Title"/> after
    /// <see cref="RouteSearchIndex.TextResolver"/> has run, so it is the localized string rather
    /// than the resource key in an app that uses one. <b>Bind this, not <c>Entry.Title</c></b>;
    /// with no resolver configured the two are identical anyway.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Ready to pass straight to <see cref="Router.Push"/>/<see cref="Router.Replace"/>. Always
    /// navigates by <see cref="RouteRecord.Name"/> rather than a literal path - the only reliable
    /// way to target an arbitrary, possibly deeply-nested record regardless of where it lives in
    /// the tree (see <see cref="RouteLocationRaw"/>'s own remarks on why a path string can't carry
    /// params reliably either). Throws immediately, rather than silently resolving a wrong/relative
    /// navigation, if <see cref="Record"/> has no <see cref="RouteRecord.Name"/> - give any route
    /// you mark searchable a <see cref="RouteRecord.Name"/> too.
    /// </summary>
    public RouteLocationRaw To => Record.Name is not null
        ? new RouteLocationRaw { Name = Record.Name }
        : throw new InvalidOperationException(
            $"\"{Entry.Title}\" is searchable but its RouteRecord has no Name, so RouteSearchResult.To has no reliable target to navigate to - give the route a Name.");
}
