// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel.DataAnnotations;

namespace Barbatos.Wpf.AquariusValee.Rules;

/// <summary>
/// Validates that a string contains letters, digits, underscores, and hyphens only - the
/// WPF/.NET counterpart of vee-validate's <c>alpha_dash</c> rule (a common shape for slugs/
/// usernames). See <see cref="AlphaAttribute"/>'s remarks for the Unicode-vs-locale-table note.
/// A <see langword="null"/> or empty value is valid.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class AlphaDashAttribute : ValidationAttribute
{
    /// <summary>Creates the attribute.</summary>
    public AlphaDashAttribute() => ErrorMessage ??= "The {0} field may only contain letters, numbers, dashes, and underscores.";

    /// <summary>When <see langword="true"/>, only plain ASCII letters/digits are accepted instead of any Unicode letter/digit.</summary>
    public bool AsciiOnly { get; set; }

    /// <inheritdoc/>
    public override bool IsValid(object? value)
    {
        var text = value as string;

        if (string.IsNullOrEmpty(text))
            return true;

        if (AsciiOnly)
            return AsciiSets.ContainsOnly(text, AsciiSets.LettersDigitsDashUnderscore);

        foreach (var c in text)
        {
            if (c is not ('_' or '-') && !char.IsLetterOrDigit(c))
                return false;
        }

        return true;
    }
}
