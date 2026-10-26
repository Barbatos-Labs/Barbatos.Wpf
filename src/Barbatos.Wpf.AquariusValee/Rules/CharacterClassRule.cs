// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Buffers;

namespace Barbatos.Wpf.AquariusValee.Rules;

/// <summary>
/// The validation every character-class rule (<see cref="AlphaAttribute"/>,
/// <see cref="AlphaNumericAttribute"/>, <see cref="AlphaDashAttribute"/>,
/// <see cref="AlphaSpacesAttribute"/>) shares: a <see langword="null"/> or empty value passes,
/// and otherwise every character must belong to the rule's class - its fixed
/// <see cref="AsciiSets"/> entry when <c>AsciiOnly</c> is on, or its Unicode-aware predicate
/// when it is off.
/// </summary>
/// <remarks>
/// A helper rather than a common base class, so each rule keeps deriving directly from
/// <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/> - an intermediate
/// base type would be new public API for what is purely an implementation detail.
/// </remarks>
internal static class CharacterClassRule
{
    internal static bool IsValid(object? value, bool asciiOnly, SearchValues<char> asciiSet, Func<char, bool> isUnicodeMember)
    {
        var text = value as string;

        if (string.IsNullOrEmpty(text))
            return true;

        if (asciiOnly)
            return AsciiSets.ContainsOnly(text, asciiSet);

        foreach (var c in text)
        {
            if (!isUnicodeMember(c))
                return false;
        }

        return true;
    }
}
