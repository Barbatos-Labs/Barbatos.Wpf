// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Aquarius.Reactivity;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.AquariusValee.Validation;

/// <summary>
/// A <see cref="Form"/>'s aggregated reactive state, derived from every one of its fields -
/// the WPF/C# counterpart of vee-validate's form-level <c>meta</c>.
/// </summary>
/// <remarks>
/// <para>
/// Every property is a <b>plain on-demand getter, computed fresh from the form's fields on
/// every read - never cached.</b> This matters: an earlier draft of this design cached these
/// values and only recomputed them on the next dispatcher tick (mirroring
/// <see cref="Barbatos.Wpf.Aquarius.Composition.Lifecycle"/>'s own <c>IOnUpdated</c> batching
/// too literally). That would have made <see cref="Form.HandleSubmit"/>'s synchronous read of
/// <see cref="Valid"/> - immediately after <c>await</c>-ing a full validation pass - able to
/// read a stale, not-yet-recomputed value, potentially calling the success callback on an
/// actually-invalid form. <see cref="NotifyMayHaveChanged"/> batches only the outgoing
/// <see cref="ObservableObject.PropertyChanged"/> <i>notification</i> (via
/// <see cref="NextTick"/>, the same coalescing shape
/// <see cref="Barbatos.Wpf.Aquarius.Composition.Lifecycle"/> uses for <c>IOnUpdated</c>) - it
/// never gates when a value becomes correct, only how often the UI is told to re-read it.
/// </para>
/// <para>
/// Aggregation strategy mirrors vee-validate's own exactly: <see cref="Touched"/>/
/// <see cref="Pending"/> are true if <i>any</i> field is; <see cref="Valid"/> requires
/// <i>every</i> field to be valid; <see cref="Dirty"/> is true if <i>any</i> field is dirty.
/// </para>
/// </remarks>
public sealed class FormMeta : ObservableObject
{
    private readonly Func<bool> _computeTouched;
    private readonly Func<bool> _computeDirty;
    private readonly Func<bool> _computePending;
    private readonly Func<bool> _computeValid;
    private bool _notifyDirty;
    private bool _notifyScheduled;

    internal FormMeta(Func<bool> computeTouched, Func<bool> computeDirty, Func<bool> computePending, Func<bool> computeValid)
    {
        _computeTouched = computeTouched;
        _computeDirty = computeDirty;
        _computePending = computePending;
        _computeValid = computeValid;
    }

    /// <summary>Whether any field has been touched.</summary>
    public bool Touched => _computeTouched();

    /// <summary>Whether any field's current value differs from its initial value.</summary>
    public bool Dirty => _computeDirty();

    /// <summary>Whether any field has an asynchronous rule currently in flight.</summary>
    public bool Pending => _computePending();

    /// <summary>Whether every field is valid and no stray form-level (e.g. cross-field/model-level) error is set.</summary>
    public bool Valid => _computeValid();

    /// <summary>
    /// Schedules a batched <see cref="ObservableObject.PropertyChanged"/> notification for all
    /// four properties above, coalescing rapid-fire field changes (e.g. touching every field at
    /// submit) into a single UI-facing notification pass. Never gates the properties'
    /// <i>values</i> themselves, which are always computed fresh - see this type's own remarks.
    /// </summary>
    internal void NotifyMayHaveChanged()
    {
        _notifyDirty = true;

        if (_notifyScheduled)
            return;

        _notifyScheduled = true;
        NextTick.Run(() =>
        {
            _notifyScheduled = false;

            if (!_notifyDirty)
                return;

            _notifyDirty = false;
            OnPropertyChanged(nameof(Touched));
            OnPropertyChanged(nameof(Dirty));
            OnPropertyChanged(nameof(Pending));
            OnPropertyChanged(nameof(Valid));
        });
    }
}
