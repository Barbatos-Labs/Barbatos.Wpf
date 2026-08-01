// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Buffers;

namespace Barbatos.Wpf.AquariusValee.Rules;

/// <summary>
/// Pre-built <see cref="SearchValues{T}"/> for the fixed ASCII character sets this namespace's
/// rules accept.
/// </summary>
/// <remarks>
/// <see cref="MemoryExtensions.ContainsAnyExcept{T}(ReadOnlySpan{T}, SearchValues{T})"/> answers
/// "is every character in this set?" in one vectorised pass, rather than a character-at-a-time
/// loop - the BCL picks an ASCII-bitmap or SIMD strategy per set when it is constructed, which is
/// why each set is built once, statically, instead of per validation. The Unicode-aware paths
/// (<c>char.IsLetter</c> and friends, used whenever <c>AsciiOnly</c> is off) cannot be expressed
/// as a finite set and still loop.
/// </remarks>
internal static class AsciiSets
{
    /// <summary><c>0-9</c>.</summary>
    internal static readonly SearchValues<char> Digits = SearchValues.Create("0123456789");

    /// <summary><c>a-z</c>, <c>A-Z</c>.</summary>
    internal static readonly SearchValues<char> Letters =
        SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");

    /// <summary><c>a-z</c>, <c>A-Z</c>, <c>0-9</c>.</summary>
    internal static readonly SearchValues<char> LettersOrDigits =
        SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");

    /// <summary><c>a-z</c>, <c>A-Z</c>, <c>0-9</c>, <c>_</c>, <c>-</c>.</summary>
    internal static readonly SearchValues<char> LettersDigitsDashUnderscore =
        SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-");

    /// <summary><c>a-z</c>, <c>A-Z</c>, and ASCII whitespace.</summary>
    internal static readonly SearchValues<char> LettersOrAsciiWhitespace =
        SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t\n\v\f\r");

    /// <summary>Whether every character of <paramref name="text"/> belongs to <paramref name="allowed"/>.</summary>
    internal static bool ContainsOnly(string text, SearchValues<char> allowed) =>
        !text.AsSpan().ContainsAnyExcept(allowed);
}
