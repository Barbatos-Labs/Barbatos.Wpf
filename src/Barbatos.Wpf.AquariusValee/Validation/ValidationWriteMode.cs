// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.AquariusValee.Validation;

/// <summary>
/// Governs whether a validation pass is allowed to <i>write</i> visible
/// <see cref="Field{T}.Errors"/>/<see cref="FieldMeta.Validated"/>, separately from whether it
/// computes <see cref="FieldMeta.Valid"/> - the WPF/C# counterpart of vee-validate's internal
/// <c>SchemaValidationMode</c> (<c>'force' | 'validated-only' | 'silent'</c>).
/// </summary>
/// <remarks>
/// This split is what lets a freshly-constructed <see cref="Field{T}"/> report an accurate
/// <see cref="FieldMeta.Valid"/> immediately (so a submit button can already know the form is
/// invalid) without ever flashing a red error before the user has interacted with anything.
/// <see cref="FieldMeta.Valid"/> is updated on <i>every</i> pass regardless of mode; only
/// <see cref="Field{T}.Errors"/> and <see cref="FieldMeta.Validated"/> are gated.
/// </remarks>
internal enum ValidationWriteMode
{
    /// <summary>
    /// Always write <see cref="Field{T}.Errors"/> and set <see cref="FieldMeta.Validated"/> to
    /// <see langword="true"/>. Used by <see cref="Field{T}.HandleBlur"/> (when triggers say so),
    /// a form submit's touch-all-fields step, and any public, explicit
    /// <see cref="Field{T}.ValidateAsync"/> call.
    /// </summary>
    Force,

    /// <summary>
    /// Write <see cref="Field{T}.Errors"/> only if <see cref="FieldMeta.Validated"/> is already
    /// <see langword="true"/>; otherwise behaves like <see cref="Silent"/>. Used by
    /// <see cref="Field{T}.HandleChange"/> and cross-field <see cref="Field{T}.DependsOn{TSource}"/>
    /// triggers, so an untouched field doesn't suddenly show an error purely because a sibling
    /// it depends on changed.
    /// </summary>
    ValidatedOnly,

    /// <summary>
    /// Compute <see cref="FieldMeta.Valid"/> accurately but never write
    /// <see cref="Field{T}.Errors"/> or flip <see cref="FieldMeta.Validated"/>. Used for the
    /// one pass every <see cref="Field{T}"/> runs right after construction.
    /// </summary>
    Silent,
}
