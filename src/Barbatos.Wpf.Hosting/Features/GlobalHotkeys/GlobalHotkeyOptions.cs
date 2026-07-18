// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Hotkeys;

/// <summary>
/// Options for the global hotkeys feature. Can be configured from code via
/// <c>ConfigureGlobalHotkeys</c> and/or from configuration files using the
/// <see cref="SectionName"/> section (file values override code values).
/// </summary>
public class GlobalHotkeyOptions
{
    /// <summary>
    /// The configuration section the options are bound from.
    /// </summary>
    public const string SectionName = "Barbatos:GlobalHotkeys";

    /// <summary>
    /// Whether the registered hotkeys are activated when the host is built.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Overrides the gesture of a named hotkey, for example
    /// <c>"Barbatos:GlobalHotkeys:Gestures:QuickEntry" = "Control+Alt+Space"</c>.
    /// </summary>
    public Dictionary<string, string> Gestures { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
