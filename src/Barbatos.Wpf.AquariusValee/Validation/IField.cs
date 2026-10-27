// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Barbatos.Wpf.AquariusValee.Validation;

/// <summary>
/// The non-generic view of a <see cref="Field{T}"/> - lets <see cref="Form"/> hold a
/// heterogeneous collection of fields (a <c>string</c> field next to an <c>int</c> field next
/// to a <c>bool</c> field) without reflection, and lets XAML (<see cref="Xaml.Valee"/>) resolve
/// a field by name without knowing its value type at compile time.
/// </summary>
public interface IField : INotifyPropertyChanged, INotifyDataErrorInfo
{
    /// <summary>This field's name, as passed to <see cref="Form"/>'s <c>DefineField</c> or a <see cref="Field{T}"/> constructor.</summary>
    string Name { get; }

    /// <summary>A friendly display name for error messages - mirrors <c>[Display(Name = "...")]</c> when the field was built via <see cref="Form.FromModel{TModel}"/>.</summary>
    string? Label { get; }

    /// <summary>
    /// Short guidance text for how to fill in this field (e.g. an expected format or example) -
    /// mirrors <c>[Display(Prompt = "...")]</c> when built via <see cref="Form.FromModel{TModel}"/>.
    /// Distinct from <see cref="UIHint"/>/<see cref="DataTypeHint"/>, which pick a control/template,
    /// not guidance text - a plain hint for your own templates to render (e.g. as a watermark or
    /// helper line); Valee does not render it itself. <see langword="null"/> if never set.
    /// </summary>
    string? Prompt { get; }

    /// <summary>
    /// Names which template/control should present this field - mirrors <c>[UIHint("...")]</c>
    /// when built via <see cref="Form.FromModel{TModel}"/>. An arbitrary, app-defined string (e.g.
    /// a <see cref="System.Windows.DataTemplate"/> resource key) - see
    /// <see cref="Xaml.FieldTemplateSelector"/>. <see langword="null"/> if never set.
    /// </summary>
    string? UIHint { get; }

    /// <summary>
    /// The semantic kind of data this field holds - mirrors <c>[DataType(...)]</c> when built via
    /// <see cref="Form.FromModel{TModel}"/>. A closed, well-known fallback for
    /// <see cref="Xaml.FieldTemplateSelector"/> to use when <see cref="UIHint"/> is unset (e.g.
    /// <see cref="System.ComponentModel.DataAnnotations.DataType.Password"/>,
    /// <see cref="System.ComponentModel.DataAnnotations.DataType.MultilineText"/>).
    /// <see langword="null"/> if never set.
    /// </summary>
    DataType? DataTypeHint { get; }

    /// <summary>Whether the field should be shown as not editable - <see langword="true"/> for a property marked <c>[Editable(false)]</c> when built via <see cref="Form.FromModel{TModel}"/>. Defaults to <see langword="false"/>; Valee's own attached properties do not enforce this themselves, it is a hint for your own templates/converters to react to.</summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// The lowest value this field should accept - mirrors <c>[Range]</c>'s lower bound when built
    /// via <see cref="Form.FromModel{TModel}"/>, or when a <see cref="RangeAttribute"/> is passed
    /// to <see cref="Field{T}"/>'s constructor. <see langword="null"/> when unbounded below or
    /// non-numeric.
    /// </summary>
    /// <remarks>
    /// Input-layer metadata only - it is <b>not</b> what validates the value. <c>[Range]</c> (or
    /// whatever rule set the bound) remains the verdict; this simply lets
    /// <see cref="Xaml.Valee.NumericProperty"/> know which keystrokes are pointless and where to
    /// clamp on blur, without the bound having to be repeated in XAML.
    /// </remarks>
    double? Minimum { get; }

    /// <summary>The highest value this field should accept - the counterpart of <see cref="Minimum"/>, with the same "metadata, not validation" caveat.</summary>
    double? Maximum { get; }

    /// <summary>This field's reactive validation state.</summary>
    FieldMeta Meta { get; }

    /// <summary>Every current validation error for this field. Empty when valid.</summary>
    IReadOnlyList<string> Errors { get; }

    /// <summary>The first entry in <see cref="Errors"/>, or <see langword="null"/> if there are none.</summary>
    string? ErrorMessage { get; }

    /// <summary>The field's current value, boxed - a non-generic escape hatch for <see cref="Form.GetValues"/> and similar aggregation.</summary>
    object? RawValue { get; }

    /// <summary>Runs every rule against the current value and writes the result (a <see cref="ValidationWriteMode.Force"/> pass).</summary>
    /// <remarks>
    /// An exception thrown by a rule faults the returned task (or cancels it, for a rule's own
    /// <see cref="OperationCanceledException"/>), after <see cref="FieldMeta.Pending"/> has been
    /// lowered and with <see cref="Errors"/> and <see cref="FieldMeta.Valid"/> left as the previous
    /// pass wrote them. A pass superseded by a newer one completes normally instead, whatever its
    /// rule threw - its result would have been discarded anyway.
    /// </remarks>
    Task ValidateAsync();

    /// <summary>Marks the field <see cref="FieldMeta.Touched"/> and, depending on its trigger configuration, validates.</summary>
    void HandleBlur();

    /// <summary>
    /// Sets a manual, server-driven error (e.g. "email already taken") that bypasses rule
    /// execution entirely - pass <see langword="null"/> to clear it. A subsequent value change
    /// that triggers real validation supersedes this.
    /// </summary>
    void SetError(string? message);

    /// <summary>Adds an error without clearing existing ones - used by <see cref="Form"/> to layer a cross-field/model-level message on top of a field's own rule errors.</summary>
    void AppendError(string message);

    /// <summary>Resets the field to its original initial value, clears touched/validated/errors, and re-runs a silent validation pass.</summary>
    void Reset();
}
