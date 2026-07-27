// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel.DataAnnotations;

namespace Barbatos.Wpf.AquariusValee.Rules;

/// <summary>
/// Validates that a string looks like an integer - an optional leading <c>-</c> followed by one
/// or more digits, no decimal point or exponent - the WPF/.NET counterpart of vee-validate's
/// <c>integer</c> rule. See <see cref="NumericStringAttribute"/>'s remarks for why no BCL
/// attribute already covers this. A <see langword="null"/> or empty value is valid.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class IntegerStringAttribute : ValidationAttribute
{
    /// <summary>Creates the attribute.</summary>
    public IntegerStringAttribute() => ErrorMessage ??= "The {0} field must be a whole number.";

    /// <inheritdoc/>
    public override bool IsValid(object? value)
    {
        var text = value as string;

        if (string.IsNullOrEmpty(text))
            return true;

        var start = 0;

        if (text[0] == '-')
        {
            if (text.Length == 1)
                return false;

            start = 1;
        }

        for (var i = start; i < text.Length; i++)
        {
            if (text[i] is < '0' or > '9')
                return false;
        }

        return true;
    }
}
