// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel.DataAnnotations;

namespace Barbatos.Wpf.AquariusValee.Rules;

/// <summary>
/// Validates that a value equals one of a fixed set of literals, using vee-validate's own loose,
/// cross-type <c>one_of</c> matching (confirmed against its real source: <c>list.some(item =&gt;
/// item == value)</c>, JS loose equality) - so <c>1</c> (int) and <c>"1"</c> (string) match each
/// other, the same way they would in vee-validate itself. A <see langword="null"/> value is
/// valid - pair with <see cref="RequiredAttribute"/> if that should fail instead.
/// </summary>
/// <remarks>
/// <b>Since .NET 8, prefer the BCL's own <see cref="AllowedValuesAttribute"/> instead</b> for the
/// common case - it is strict-equality-only (confirmed empirically: <c>1</c> and <c>"1"</c> do
/// <i>not</i> match each other there) and needs no separate package. Reach for
/// <see cref="OneOfAttribute"/> specifically when you want vee-validate's own looser, string-form
/// cross-type matching instead of strict equality.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class OneOfAttribute : ValidationAttribute
{
    private readonly object?[] _allowedValues;

    // The string form of each allowed value, rendered once here rather than on every validation
    // pass - the loose cross-type comparison needs it for every candidate, and the allowed set
    // never changes after construction.
    private readonly string?[] _allowedStrings;

    /// <summary>Creates the attribute with the allowed literal values.</summary>
    public OneOfAttribute(params object?[] allowedValues)
    {
        _allowedValues = allowedValues ?? [];
        _allowedStrings = Array.ConvertAll(_allowedValues, allowed => allowed?.ToString());
        ErrorMessage ??= "The {0} field must be one of the allowed values.";
    }

    /// <summary>The allowed literal values, as passed to the constructor.</summary>
    public IReadOnlyList<object?> AllowedValues => _allowedValues;

    /// <inheritdoc/>
    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        var asString = value.ToString();

        for (var i = 0; i < _allowedValues.Length; i++)
        {
            if (Equals(value, _allowedValues[i]) || string.Equals(asString, _allowedStrings[i], StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
