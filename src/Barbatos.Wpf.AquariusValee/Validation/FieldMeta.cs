// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.AquariusValee.Validation;

/// <summary>
/// The reactive state tracked for one <see cref="Field{T}"/> - the WPF/C# counterpart of
/// vee-validate's per-field <c>meta</c> object.
/// </summary>
/// <remarks>
/// Every property here is read-only from outside this package - only <see cref="Field{T}"/>
/// itself mutates them, the same "owner mutates, everyone else observes" shape
/// <see cref="Barbatos.Wpf.Aquarius.Reactivity.Ref{T}"/> uses for <c>Value</c>.
/// <para>
/// <b><see cref="Valid"/> defaults to <see langword="true"/></b> - "no errors yet" is not the
/// same as "confirmed valid." Combine it with <see cref="Dirty"/>/<see cref="Touched"/> before
/// showing any "this looks good" UI, the same guidance vee-validate's own docs give.
/// </para>
/// <para>
/// <b><see cref="Validated"/>, not <see cref="Touched"/>, is what gates whether a background
/// validation pass may write visible <see cref="Field{T}.Errors"/></b> - see
/// <see cref="ValidationWriteMode"/>. The two are independent axes: a field validated via
/// <see cref="Field{T}.ValidateAsync"/> alone reports <c>Validated=true, Touched=false</c>; a
/// blurred field with no change-triggered validation reports <c>Touched=true, Validated=false</c>.
/// </para>
/// <para>
/// <b><see cref="Dirty"/> is a live comparison</b>, computed fresh on every read against the
/// field's initial value - not a flag latched the first time the value changes. Typing a value
/// and then retyping the original value un-dirties the field again.
/// </para>
/// </remarks>
public sealed class FieldMeta : ObservableObject
{
    private readonly Func<bool> _computeDirty;
    private bool _touched;
    private bool _validated;
    private bool _pending;
    private bool _valid = true;

    internal FieldMeta(Func<bool> computeDirty) => _computeDirty = computeDirty;

    /// <summary>Whether the bound control has ever lost focus, or a submit attempt force-touched every field.</summary>
    public bool Touched
    {
        get => _touched;
        internal set => SetProperty(ref _touched, value);
    }

    /// <summary>
    /// Whether a real (non-<see cref="ValidationWriteMode.Silent"/>) validation pass has run at
    /// least once for this field - independent of <see cref="Touched"/>. Gates whether a
    /// <see cref="ValidationWriteMode.ValidatedOnly"/> pass (triggered by a value change, or a
    /// cross-field dependency) is allowed to write <see cref="Field{T}.Errors"/>.
    /// </summary>
    public bool Validated
    {
        get => _validated;
        internal set => SetProperty(ref _validated, value);
    }

    /// <summary>Whether an asynchronous rule is currently in flight for this field.</summary>
    /// <remarks>
    /// Raised only by a non-silent pass of a field that has asynchronous rules, and lowered when
    /// that pass ends - with a result, or with an exception from one of its rules. A pass superseded
    /// by a newer one leaves it to the newer pass; <see cref="Field{T}.Reset()"/> and
    /// <see cref="Field{T}.Dispose"/> lower it themselves.
    /// </remarks>
    public bool Pending
    {
        get => _pending;
        internal set => SetProperty(ref _pending, value);
    }

    /// <summary>
    /// Whether the field's current value passes every rule - updated on every validation pass
    /// regardless of <see cref="ValidationWriteMode"/>, so this is always fresh even while
    /// <see cref="Field{T}.Errors"/> itself is still being withheld. Defaults to
    /// <see langword="true"/>.
    /// </summary>
    public bool Valid
    {
        get => _valid;
        internal set => SetProperty(ref _valid, value);
    }

    /// <summary>Whether the field's current value differs from its initial value - a live comparison, computed fresh on every read, never cached.</summary>
    public bool Dirty => _computeDirty();

    /// <summary>Raises <see cref="ObservableObject.PropertyChanged"/> for <see cref="Dirty"/> - called by <see cref="Field{T}"/> whenever its value changes.</summary>
    internal void NotifyDirtyChanged() => OnPropertyChanged(nameof(Dirty));
}
