// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Core Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Power;

/// <summary>
/// Options for the "keep computer awake" feature. Can be configured from code via
/// <c>ConfigureKeepAwake</c> and/or from configuration files using the
/// <see cref="SectionName"/> section (file values override code values).
/// </summary>
public class KeepAwakeOptions
{
    /// <summary>
    /// The configuration section the options are bound from.
    /// </summary>
    public const string SectionName = "Barbatos:KeepAwake";

    /// <summary>
    /// Whether the computer is kept awake as soon as the host is built.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Whether the display is also kept on. Defaults to <see langword="false"/>, which
    /// prevents the computer from idle-sleeping while still allowing the display to turn off.
    /// </summary>
    public bool KeepDisplayOn { get; set; }
}
