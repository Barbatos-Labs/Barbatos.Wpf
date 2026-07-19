// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Tray;

/// <summary>
/// Controls the application's system tray icon.
/// </summary>
public interface ITrayIconService
{
    /// <summary>
    /// Gets whether the tray icon is currently visible.
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Shows or hides the tray icon. Intended to be called from a settings UI.
    /// </summary>
    void SetVisible(bool visible);

    /// <summary>
    /// Occurs when <see cref="IsVisible"/> changes.
    /// </summary>
    event EventHandler? IsVisibleChanged;

    /// <summary>
    /// Updates the tooltip of the tray icon.
    /// </summary>
    void SetToolTip(string toolTip);

    /// <summary>
    /// Occurs when the tray icon is clicked with the primary mouse button.
    /// </summary>
    event EventHandler? Clicked;

    /// <summary>
    /// Occurs when the tray icon is double-clicked.
    /// </summary>
    event EventHandler? DoubleClicked;
}
