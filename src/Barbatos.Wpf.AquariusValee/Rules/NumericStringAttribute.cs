// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel.DataAnnotations;

namespace Barbatos.Wpf.AquariusValee.Rules;

/// <summary>
/// Validates that a string is composed of digits only (one or more, no sign) - the WPF/.NET
/// counterpart of vee-validate's <c>numeric</c> rule. <see cref="RangeAttribute"/> assumes an
/// already-numeric-typed property; there is no built-in attribute for "does this raw string
/// look like a number" before it has been parsed/bound to a numeric type. A
/// <see langword="null"/> or empty value is valid. See <see cref="IntegerStringAttribute"/> for
/// the signed counterpart.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class NumericStringAttribute : ValidationAttribute
{
    /// <summary>Creates the attribute.</summary>
    /// <remarks>The default message goes to the base constructor, as the BCL's own attributes pass theirs, so <see cref="ValidationAttribute.ErrorMessage"/> reads <see langword="null"/> until set and <see cref="ValidationAttribute.ErrorMessageResourceName"/> can supply a localized message instead - assigning the default to <c>ErrorMessage</c> made the BCL reject that resource pair with an <see cref="InvalidOperationException"/>.</remarks>
    public NumericStringAttribute()
        : base("The {0} field may only contain digits.")
    {
    }

    /// <inheritdoc/>
    public override bool IsValid(object? value)
    {
        var text = value as string;

        if (string.IsNullOrEmpty(text))
            return true;

        return AsciiSets.ContainsOnly(text, AsciiSets.Digits);
    }
}
