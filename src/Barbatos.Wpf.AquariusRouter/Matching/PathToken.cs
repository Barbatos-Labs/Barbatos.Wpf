// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.AquariusRouter.Matching;

/// <summary>The kind of a single <see cref="PathToken"/> produced by <see cref="PathTokenizer"/>.</summary>
public enum PathTokenKind
{
    /// <summary>Literal text, matched verbatim (case-insensitively by default).</summary>
    Static,

    /// <summary><c>:name</c> - a named, capturing segment. See <see cref="PathToken.Optional"/>/<see cref="PathToken.Repeatable"/>/<see cref="PathToken.Regex"/>.</summary>
    Param,
}

/// <summary>
/// One token of a tokenized route path - either literal text or a named param, as produced by
/// <see cref="PathTokenizer.Tokenize"/> and consumed by <see cref="PathParser.Create"/>.
/// </summary>
/// <remarks>
/// Vue Router's own tokenizer also declares a third, <c>Group</c> token kind - but its own
/// tokenizer state machine never actually produces one (confirmed by reading the real source,
/// not assumed); it exists in their type declaration but is dead code in practice. Not ported
/// here for that reason.
/// </remarks>
public sealed record PathToken
{
    /// <summary>See <see cref="PathTokenKind"/>.</summary>
    public required PathTokenKind Kind { get; init; }

    /// <summary>The literal text (<see cref="PathTokenKind.Static"/>) or param name (<see cref="PathTokenKind.Param"/>).</summary>
    public required string Value { get; init; }

    /// <summary>
    /// A custom regex supplied as <c>:name(regex)</c> - e.g. <c>:id(\d+)</c>. Empty (never
    /// <see langword="null"/>) when none was given, matching this project's convention of
    /// treating "no custom regex" the same way <see cref="PathParser"/>'s default pattern
    /// substitution does. <see cref="PathTokenKind.Static"/> tokens never set this.
    /// </summary>
    public string Regex { get; init; } = "";

    /// <summary>
    /// <c>:name?</c> or <c>:name*</c> - zero occurrences are allowed.
    /// <see cref="PathTokenKind.Static"/> tokens are never optional.
    /// </summary>
    public bool Optional { get; init; }

    /// <summary>
    /// <c>:name+</c> (one or more) or <c>:name*</c> (zero or more) - matches one or more
    /// <c>/</c>-separated segments, surfaced as a string array instead of a single string.
    /// <see cref="PathTokenKind.Static"/> tokens are never repeatable.
    /// </summary>
    public bool Repeatable { get; init; }

    /// <summary>Creates a <see cref="PathTokenKind.Static"/> token.</summary>
    public static PathToken Static(string value) => new() { Kind = PathTokenKind.Static, Value = value };

    /// <summary>Creates a <see cref="PathTokenKind.Param"/> token.</summary>
    public static PathToken Param(string name, string regex, bool optional, bool repeatable) =>
        new() { Kind = PathTokenKind.Param, Value = name, Regex = regex, Optional = optional, Repeatable = repeatable };
}
