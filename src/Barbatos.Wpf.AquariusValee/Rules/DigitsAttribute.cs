// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel.DataAnnotations;

namespace Barbatos.Wpf.AquariusValee.Rules;

/// <summary>
/// Validates that a string is composed of ASCII digits only, of exactly a given length - the
/// WPF/.NET counterpart of vee-validate's <c>digits</c> rule (e.g. a fixed-length PIN or OTP
/// code). <see cref="RangeAttribute"/> assumes an already-numeric-typed property; there is no
/// built-in attribute for "this many digit characters" on raw string input. A
/// <see langword="null"/> or empty value is valid.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class DigitsAttribute : ValidationAttribute
{
    /// <summary>Creates the attribute requiring exactly <paramref name="length"/> ASCII digits.</summary>
    /// <remarks>The default message goes to the base constructor, as the BCL's own attributes pass theirs, so <see cref="ValidationAttribute.ErrorMessage"/> reads <see langword="null"/> until set and <see cref="ValidationAttribute.ErrorMessageResourceName"/> can supply a localized message instead - assigning the default to <c>ErrorMessage</c> made the BCL reject that resource pair with an <see cref="InvalidOperationException"/>.</remarks>
    public DigitsAttribute(int length)
        : base("The {0} field must be exactly " + length + " digits.")
    {
        Length = length;
    }

    /// <summary>The required exact digit count.</summary>
    public int Length { get; }

    /// <inheritdoc/>
    public override bool IsValid(object? value)
    {
        var text = value as string;

        if (string.IsNullOrEmpty(text))
            return true;

        return text.Length == Length && AsciiSets.ContainsOnly(text, AsciiSets.Digits);
    }
}
