// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Tray;

/// <summary>
/// Options for the system tray icon feature. Can be configured from code via
/// <c>ConfigureTrayIcon</c> and/or from configuration files using the
/// <see cref="SectionName"/> section (file values override code values).
/// </summary>
public class TrayIconOptions
{
    /// <summary>
    /// The configuration section the options are bound from.
    /// </summary>
    public const string SectionName = "Barbatos:TrayIcon";

    /// <summary>
    /// Whether the tray icon is shown when the host is built.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The tooltip of the tray icon. Defaults to the application name.
    /// </summary>
    public string? ToolTip { get; set; }

    /// <summary>
    /// The path of an <c>.ico</c> file used for the tray icon. Defaults to the icon
    /// associated with the application executable.
    /// </summary>
    public string? IconPath { get; set; }

    /// <summary>
    /// The context menu items of the tray icon. Menu items can only be added from code.
    /// </summary>
    public IList<TrayMenuItem> MenuItems { get; } = new List<TrayMenuItem>();
}

/// <summary>
/// A context menu item of the tray icon.
/// </summary>
public sealed class TrayMenuItem
{
    public TrayMenuItem(string header, Action action)
    {
        Header = header ?? throw new ArgumentNullException(nameof(header));
        Action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public string Header { get; }

    public Action Action { get; }
}
