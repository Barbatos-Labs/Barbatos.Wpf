// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel.DataAnnotations;

namespace Barbatos.Wpf.AquariusValee.Rules;

/// <summary>
/// Validates that a string contains letters only - the WPF/.NET counterpart of vee-validate's
/// <c>alpha</c> rule, for which <see cref="System.ComponentModel.DataAnnotations"/> has no
/// built-in equivalent.
/// </summary>
/// <remarks>
/// vee-validate's own <c>alpha</c> ships ~20 hand-picked per-locale Unicode regex tables. This
/// port deliberately does not replicate that table: <see cref="char.IsLetter(char)"/> already
/// recognizes letters across essentially every script .NET itself knows about (Latin, Cyrillic,
/// Vietnamese diacritics, Greek, CJK, etc. all at once) via the BCL's own Unicode category data -
/// broader than vee-validate's curated locale list, not narrower. Set <see cref="AsciiOnly"/> to
/// restrict to plain <c>a-z</c>/<c>A-Z</c> instead. A <see langword="null"/> or empty value is
/// considered valid - pair with <see cref="RequiredAttribute"/> if emptiness itself should fail.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class AlphaAttribute : ValidationAttribute
{
    /// <summary>Creates the attribute.</summary>
    public AlphaAttribute() => ErrorMessage ??= "The {0} field may only contain letters.";

    /// <summary>When <see langword="true"/>, only plain ASCII <c>a-z</c>/<c>A-Z</c> are accepted instead of any Unicode letter.</summary>
    public bool AsciiOnly { get; set; }

    /// <inheritdoc/>
    public override bool IsValid(object? value) =>
        CharacterClassRule.IsValid(value, AsciiOnly, AsciiSets.Letters, char.IsLetter);
}
