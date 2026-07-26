// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Input;

/// <summary>
/// Options for the InputSystem feature (in-app keyboard shortcuts and OS-level global hotkeys).
/// Can be configured from code via <c>ConfigureInputSystem</c> and/or from configuration files
/// using the <see cref="SectionName"/> section (file values override code values).
/// </summary>
public class InputSystemOptions
{
    /// <summary>
    /// The configuration section the options are bound from.
    /// </summary>
    public const string SectionName = "Barbatos:InputSystem";

    /// <summary>
    /// Whether shortcuts (in-app dispatch and global OS registration alike) are active when the
    /// host is built.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The action maps to register. Action maps can only be added from code (they carry
    /// delegates/event subscriptions, which cannot come from a configuration file).
    /// </summary>
    public IList<InputActionMap> ActionMaps { get; } = new List<InputActionMap>();
}
