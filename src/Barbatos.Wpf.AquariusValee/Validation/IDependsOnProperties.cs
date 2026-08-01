// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel.DataAnnotations;

namespace Barbatos.Wpf.AquariusValee.Validation;

/// <summary>
/// Implemented by a <see cref="ValidationAttribute"/> that needs live re-validation whenever one
/// or more sibling model properties change - the declarative counterpart to a hand-wired
/// <see cref="Field{T}.DependsOn{TSource}"/> call.
/// </summary>
/// <remarks>
/// <see cref="Form.FromModel{TModel}"/> scans every property's attributes for this interface,
/// after every field already exists, and calls <see cref="Field{T}.DependsOn{TSource}"/>
/// automatically for each name returned here - application code does not need to wire anything by
/// hand. See <see cref="Rules.PropertyRangeAttribute"/> for a built-in attribute that implements
/// this.
/// </remarks>
public interface IDependsOnProperties
{
    /// <summary>Names of sibling properties on the same model whose changes should re-validate the property this attribute is declared on.</summary>
    IEnumerable<string> DependsOnPropertyNames { get; }
}
