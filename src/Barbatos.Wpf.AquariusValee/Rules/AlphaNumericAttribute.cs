// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel.DataAnnotations;

namespace Barbatos.Wpf.AquariusValee.Rules;

/// <summary>
/// Validates that a string contains letters and digits only - the WPF/.NET counterpart of
/// vee-validate's <c>alpha_num</c> rule. See <see cref="AlphaAttribute"/>'s remarks for why this
/// uses <see cref="char.IsLetter(char)"/>/<see cref="char.IsDigit(char)"/> rather than porting
/// vee-validate's per-locale regex tables. A <see langword="null"/> or empty value is valid.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class AlphaNumericAttribute : ValidationAttribute
{
    /// <summary>Creates the attribute.</summary>
    /// <remarks>The default message goes to the base constructor, as the BCL's own attributes pass theirs, so <see cref="ValidationAttribute.ErrorMessage"/> reads <see langword="null"/> until set and <see cref="ValidationAttribute.ErrorMessageResourceName"/> can supply a localized message instead - assigning the default to <c>ErrorMessage</c> made the BCL reject that resource pair with an <see cref="InvalidOperationException"/>.</remarks>
    public AlphaNumericAttribute()
        : base("The {0} field may only contain letters and numbers.")
    {
    }

    /// <summary>When <see langword="true"/>, only plain ASCII letters/digits are accepted instead of any Unicode letter/digit.</summary>
    public bool AsciiOnly { get; set; }

    /// <inheritdoc/>
    public override bool IsValid(object? value) =>
        CharacterClassRule.IsValid(value, AsciiOnly, AsciiSets.LettersOrDigits, char.IsLetterOrDigit);
}
