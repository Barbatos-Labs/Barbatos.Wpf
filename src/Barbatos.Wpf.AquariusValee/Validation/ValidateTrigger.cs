// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.AquariusValee.Validation;

/// <summary>
/// Controls when a <see cref="Field{T}"/> re-validates itself - the WPF/C# counterpart of
/// vee-validate's <c>validateOnBlur</c>/<c>validateOnChange</c>/<c>validateOnInput</c>/
/// <c>validateOnModelUpdate</c> options.
/// </summary>
/// <remarks>
/// vee-validate's 4 independent booleans exist because a DOM <c>&lt;input&gt;</c> genuinely
/// fires two separate native events - <c>input</c> (every keystroke) and <c>change</c>
/// (on commit/blur) - that a WPF <see cref="System.Windows.Controls.TextBox"/> does not have
/// as two distinct native events in the same way. Porting 4 booleans would cargo-cult a
/// distinction WPF doesn't make. Instead there are exactly two triggers:
/// <list type="bullet">
/// <item><see cref="OnChange"/> fires whenever <see cref="Field{T}.Value"/> actually changes
/// (<see cref="Field{T}.HandleChange"/>), regardless of which WPF event caused it - a field
/// doesn't need to know whether the XAML side chose
/// <see cref="System.Windows.Data.UpdateSourceTrigger.PropertyChanged"/> (live, per-keystroke)
/// or <see cref="System.Windows.Data.UpdateSourceTrigger.LostFocus"/> (commit-on-blur) for its
/// binding.</item>
/// <item><see cref="OnBlur"/> fires purely from the <c>Xaml</c> layer's own
/// <see cref="System.Windows.UIElement.LostFocus"/> hook (<see cref="Field{T}.HandleBlur"/>),
/// independent of whether the value even changed.</item>
/// </list>
/// <see cref="Default"/> matches vee-validate's own <c>&lt;Field&gt;</c>-component default
/// behavior: both blur and a value commit validate.
/// </remarks>
[Flags]
public enum ValidateTrigger
{
    /// <summary>Never re-validate automatically - only an explicit <see cref="Field{T}.ValidateAsync"/> call or a form submit does.</summary>
    None = 0,

    /// <summary>Re-validate whenever <see cref="Field{T}.Value"/> changes.</summary>
    OnChange = 1,

    /// <summary>Re-validate when the bound control loses focus.</summary>
    OnBlur = 2,

    /// <summary>The default - both <see cref="OnChange"/> and <see cref="OnBlur"/>.</summary>
    Default = OnChange | OnBlur,
}
