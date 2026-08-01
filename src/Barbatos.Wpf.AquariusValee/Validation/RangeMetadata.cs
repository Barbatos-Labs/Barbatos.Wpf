// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Barbatos.Wpf.AquariusValee.Validation;

/// <summary>
/// Reads the numeric bounds out of a <see cref="RangeAttribute"/> so they can be surfaced as
/// <see cref="IField.Minimum"/>/<see cref="IField.Maximum"/>.
/// </summary>
/// <remarks>
/// Shared by the two places a field can acquire rules - <see cref="Form.FromModel{TModel}"/>'s
/// reflection over a model property, and <see cref="Field{T}"/>'s own
/// <c>params ValidationAttribute[]</c> constructor - so the two cannot drift apart.
/// </remarks>
internal static class RangeMetadata
{
    /// <summary>
    /// Extracts <paramref name="range"/>'s bounds as doubles, or <see langword="null"/>s when it
    /// carries no usable numeric range.
    /// </summary>
    /// <remarks>
    /// <see cref="RangeAttribute.Minimum"/>/<see cref="RangeAttribute.Maximum"/> are typed
    /// <see cref="object"/> and hold whatever the chosen constructor put there - including
    /// <see cref="string"/>s for the <c>Range(Type, string, string)</c> overload, which is how
    /// <see cref="DateTime"/> and <see cref="decimal"/> ranges are written. Only a genuinely
    /// numeric <see cref="RangeAttribute.OperandType"/> is accepted here; a date range would
    /// otherwise convert to a meaningless tick count and silently clamp a text box to it.
    /// </remarks>
    internal static (double? Minimum, double? Maximum) From(RangeAttribute? range)
    {
        if (range is null || !IsNumeric(range.OperandType))
            return (null, null);

        return (ToDouble(range.Minimum), ToDouble(range.Maximum));
    }

    private static bool IsNumeric(Type type) =>
        Type.GetTypeCode(type) is TypeCode.Byte or TypeCode.SByte
            or TypeCode.Int16 or TypeCode.UInt16
            or TypeCode.Int32 or TypeCode.UInt32
            or TypeCode.Int64 or TypeCode.UInt64
            or TypeCode.Single or TypeCode.Double or TypeCode.Decimal;

    private static double? ToDouble(object? bound)
    {
        if (bound is null)
            return null;

        try
        {
            // InvariantCulture deliberately: the string overload of RangeAttribute is documented
            // to take invariant-formatted literals, and these are compile-time constants in
            // source rather than anything the end user typed.
            var value = Convert.ToDouble(bound, CultureInfo.InvariantCulture);
            return double.IsFinite(value) ? value : null;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return null;
        }
    }
}
