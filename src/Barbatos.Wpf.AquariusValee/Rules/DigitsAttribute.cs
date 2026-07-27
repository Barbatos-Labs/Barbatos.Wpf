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
    public DigitsAttribute(int length)
    {
        Length = length;
        ErrorMessage ??= "The {0} field must be exactly " + length + " digits.";
    }

    /// <summary>The required exact digit count.</summary>
    public int Length { get; }

    /// <inheritdoc/>
    public override bool IsValid(object? value)
    {
        var text = value as string;

        if (string.IsNullOrEmpty(text))
            return true;

        if (text.Length != Length)
            return false;

        foreach (var c in text)
        {
            if (c is < '0' or > '9')
                return false;
        }

        return true;
    }
}
