// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel.DataAnnotations;

namespace Barbatos.Wpf.AquariusValee.Rules;

/// <summary>
/// Validates that a value equals none of a fixed set of literals, using vee-validate's own loose,
/// cross-type matching - see <see cref="OneOfAttribute"/>'s remarks (this is simply its negation:
/// <c>not_one_of</c> delegates straight to <c>one_of</c> in vee-validate's own real source too).
/// e.g. a list of reserved usernames. A <see langword="null"/> value is valid.
/// </summary>
/// <remarks>
/// <b>Since .NET 8, prefer the BCL's own <see cref="DeniedValuesAttribute"/> instead</b> for the
/// common, strict-equality case - see <see cref="OneOfAttribute"/>'s remarks.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class NotOneOfAttribute : ValidationAttribute
{
    private readonly object?[] _disallowedValues;

    // Rendered once at construction rather than on every validation pass - see
    // <see cref="OneOfAttribute"/>, which mirrors this.
    private readonly string?[] _disallowedStrings;

    /// <summary>Creates the attribute with the disallowed literal values.</summary>
    /// <remarks>The default message goes to the base constructor, as the BCL's own attributes pass theirs, so <see cref="ValidationAttribute.ErrorMessage"/> reads <see langword="null"/> until set and <see cref="ValidationAttribute.ErrorMessageResourceName"/> can supply a localized message instead - assigning the default to <c>ErrorMessage</c> made the BCL reject that resource pair with an <see cref="InvalidOperationException"/>.</remarks>
    public NotOneOfAttribute(params object?[] disallowedValues)
        : base("The {0} field must not be one of the disallowed values.")
    {
        _disallowedValues = disallowedValues ?? [];
        _disallowedStrings = Array.ConvertAll(_disallowedValues, disallowed => disallowed?.ToString());
    }

    /// <summary>The disallowed literal values, as passed to the constructor.</summary>
    public IReadOnlyList<object?> DisallowedValues => _disallowedValues;

    /// <inheritdoc/>
    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        var asString = value.ToString();

        for (var i = 0; i < _disallowedValues.Length; i++)
        {
            if (Equals(value, _disallowedValues[i]) || string.Equals(asString, _disallowedStrings[i], StringComparison.Ordinal))
                return false;
        }

        return true;
    }
}
