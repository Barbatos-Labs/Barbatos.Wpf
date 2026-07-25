// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.SingleInstance;

/// <summary>
/// Options for the single-instance feature. Can be configured from code via
/// <c>ConfigureSingleInstance</c> and/or from configuration files using the
/// <see cref="SectionName"/> section (file values override code values).
/// </summary>
public class SingleInstanceOptions
{
    /// <summary>
    /// The configuration section the options are bound from.
    /// </summary>
    public const string SectionName = "Barbatos:SingleInstance";

    /// <summary>
    /// Whether a second launch attempt is blocked (and redirected to the first instance)
    /// instead of opening a second window. Defaults to <see langword="true"/> — most Windows
    /// desktop apps only ever allow one running instance.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether the primary instance's <c>Application.Current.MainWindow</c> is automatically
    /// restored (if minimized), shown, and brought to the foreground when a second launch is
    /// detected. Defaults to <see langword="true"/>. Set to <see langword="false"/> to only
    /// receive <see cref="ISingleInstanceService.SecondInstanceLaunched"/> and handle it
    /// yourself.
    /// </summary>
    public bool ActivateMainWindow { get; set; } = true;
}
