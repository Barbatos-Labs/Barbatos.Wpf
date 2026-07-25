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
/// A context menu item of the tray icon, or a separator line (see <see cref="Separator"/>).
/// </summary>
public sealed class TrayMenuItem
{
    public TrayMenuItem(string header, Action action)
    {
        Header = header ?? throw new ArgumentNullException(nameof(header));
        Action = action ?? throw new ArgumentNullException(nameof(action));
    }

    TrayMenuItem()
    {
        Header = string.Empty;
        Action = static () => { };
        IsSeparator = true;
    }

    public string Header { get; }

    public Action Action { get; }

    /// <summary>
    /// The path of an <c>.ico</c> file shown next to the item's text. Optional; when unset, or
    /// the file cannot be loaded, the item is just shown without an icon.
    /// </summary>
    public string? IconPath { get; set; }

    /// <summary>
    /// Whether the item can be clicked. Disabled items are still shown, grayed out, and never
    /// invoke <see cref="Action"/> - e.g. to represent a currently-unavailable command instead
    /// of hiding it outright. Defaults to <see langword="true"/>.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether this is the menu's default item, rendered in bold (matching the convention used
    /// by e.g. the Windows Bluetooth tray icon's "Show Bluetooth Devices"). Purely a rendering
    /// hint - it does not change how the item is activated.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Whether this instance is a non-clickable separator line created via
    /// <see cref="Separator"/>, as opposed to a regular item.
    /// </summary>
    public bool IsSeparator { get; }

    /// <summary>
    /// Creates a non-clickable separator line between menu items.
    /// </summary>
    public static TrayMenuItem Separator => new();
}
