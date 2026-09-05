// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.AquariusRouter.Matching;

namespace Barbatos.Wpf.AquariusRouter.Routing;

/// <summary>
/// Options for the <see cref="Router"/> built by
/// <see cref="AquariusRouterServiceCollectionExtensions.AddAquariusRouter"/>. Configured from code
/// via that method's <c>configure</c> parameter.
/// </summary>
/// <remarks>
/// Everything here is also settable directly on a hand-constructed <see cref="Router"/> - this is
/// the DI-shaped way to say the same thing, so an app that composes its router through the
/// container declares the whole configuration in one place.
/// </remarks>
public sealed class RouterOptions
{
    /// <summary>The configuration section these options bind from, following this repository's <c>Barbatos:</c> convention.</summary>
    public const string SectionName = "Barbatos:AquariusRouter";

    /// <summary>
    /// Applied uniformly to every route's compiled path matcher - see
    /// <see cref="PathParserOptions"/>. Left <see langword="null"/> keeps the defaults.
    /// </summary>
    public PathParserOptions? PathOptions { get; set; }
}
