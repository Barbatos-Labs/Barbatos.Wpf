// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Barbatos.Wpf.AquariusValee.Validation;

namespace Barbatos.Wpf.AquariusValee.Rules;

/// <summary>
/// Validates that a value stays between two <b>sibling properties'</b> own current values, rather
/// than the fixed constants <see cref="RangeAttribute"/> requires - e.g. a <c>Price</c> bounded by
/// a user-editable <c>MinPrice</c>/<c>MaxPrice</c>. <see cref="RangeAttribute"/> cannot express
/// this at all, since C# attribute constructor arguments must be compile-time constants - there is
/// no <c>[Range(MinPrice, MaxPrice)]</c> referencing another property.
/// </summary>
/// <remarks>
/// <para>
/// Only meaningful on a property validated via <see cref="Form.FromModel{TModel}"/> - it reflects
/// <see cref="ValidationContext.ObjectInstance"/> to read the two named properties' live values,
/// the same trick <see cref="CompareAttribute"/> itself uses, which requires a real backing model
/// instance. An ad-hoc <c>Field&lt;T&gt;</c> (<see cref="Form.DefineField{T}(string, T, ValidationAttribute[])"/>)
/// has no such instance - see <see cref="Field{T}.AddRule(ValidationAttribute)"/>'s own remarks.
/// </para>
/// <para>
/// Implements <see cref="IDependsOnProperties"/>, so <see cref="Form.FromModel{TModel}"/> also
/// auto-wires live re-validation - changing <c>MinPrice</c> or <c>MaxPrice</c> re-checks
/// <c>Price</c> immediately, even though <c>Price</c> itself was never touched. No manual
/// <see cref="Field{T}.DependsOn{TSource}"/> call is needed.
/// </para>
/// <para>
/// Attributes are plain reflected metadata with no dependency-injection access, so unlike a
/// hand-written ad-hoc rule closure, this cannot resolve a string-localizer key at validation time.
/// <see cref="ValidationAttribute.ErrorMessage"/> is a plain <see cref="string.Format(IFormatProvider?, string, object?[])"/>
/// template instead - <c>{0}</c> is the display name, <c>{1}</c> the min bound's current value,
/// <c>{2}</c> the max bound's current value.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class PropertyRangeAttribute : ValidationAttribute, IDependsOnProperties
{
    private readonly string _minPropertyName;
    private readonly string _maxPropertyName;

    /// <summary>Creates the attribute, naming the two sibling properties (typically via <see langword="nameof"/>) that hold the current lower and upper bound.</summary>
    /// <remarks>The default message goes to the base constructor, as the BCL's own attributes pass theirs, so <see cref="ValidationAttribute.ErrorMessage"/> reads <see langword="null"/> until set and <see cref="ValidationAttribute.ErrorMessageResourceName"/> can supply a localized message instead - assigning the default to <c>ErrorMessage</c> made the BCL reject that resource pair with an <see cref="InvalidOperationException"/>.</remarks>
    public PropertyRangeAttribute(string minPropertyName, string maxPropertyName)
        : base("The {0} field must be between {1} and {2}.")
    {
        ArgumentException.ThrowIfNullOrEmpty(minPropertyName);
        ArgumentException.ThrowIfNullOrEmpty(maxPropertyName);

        _minPropertyName = minPropertyName;
        _maxPropertyName = maxPropertyName;
    }

    /// <inheritdoc/>
    public IEnumerable<string> DependsOnPropertyNames => [_minPropertyName, _maxPropertyName];

    /// <inheritdoc/>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IComparable comparable)
            return ValidationResult.Success;

        var min = ReadSiblingProperty(validationContext, _minPropertyName);
        var max = ReadSiblingProperty(validationContext, _maxPropertyName);

        var tooLow = min is IComparable minComparable && comparable.CompareTo(minComparable) < 0;
        var tooHigh = max is IComparable maxComparable && comparable.CompareTo(maxComparable) > 0;

        if (!tooLow && !tooHigh)
            return ValidationResult.Success;

        var message = string.Format(CultureInfo.CurrentCulture, ErrorMessageString, validationContext.DisplayName, min, max);
        return new ValidationResult(message, [validationContext.MemberName!]);
    }

    private object? ReadSiblingProperty(ValidationContext validationContext, string propertyName)
    {
        var property = validationContext.ObjectType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property is null)
            throw new InvalidOperationException(
                $"[{nameof(PropertyRangeAttribute)}] on '{validationContext.MemberName}' references unknown property '{propertyName}' on '{validationContext.ObjectType.Name}'.");

        return property.GetValue(validationContext.ObjectInstance);
    }
}
