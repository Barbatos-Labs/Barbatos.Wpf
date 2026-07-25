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
    /// Shows a balloon notification above the tray icon - e.g. to let the user know the app
    /// is still running after the main window was hidden (see the sample's
    /// <c>OnWindowClosing</c> minimize-to-tray handler), rather than leaving a hidden window
    /// looking indistinguishable from a closed app. A no-op when <see cref="IsVisible"/> is
    /// <see langword="false"/>, since there would be no icon for the balloon to point at.
    /// </summary>
    /// <remarks>
    /// Like Windows' own toast notifications, the OS can silently discard the balloon instead
    /// of raising an error - this method cannot tell you whether the user actually saw it.
    /// Windows itself skips it while the user is locked/away, in "quiet time" (the first hour
    /// after a fresh login), or running a full-screen app/presentation; the user can also
    /// disable an app's balloons entirely from the classic "Notification Area Icons" Control
    /// Panel page (the "Only show notifications" / "Hide icon and notifications" options) -
    /// unlike <see cref="Barbatos.Wpf.Notifications.INotificationService.Availability"/> for
    /// toasts, there is no supported API to query that per-app setting up front.
    /// </remarks>
    void ShowBalloonTip(string title, string text, TrayIconBalloonIcon icon = TrayIconBalloonIcon.None);

    /// <summary>
    /// Occurs when the tray icon is clicked with the primary mouse button.
    /// </summary>
    event EventHandler? Clicked;

    /// <summary>
    /// Occurs when the tray icon is double-clicked.
    /// </summary>
    event EventHandler? DoubleClicked;
}

/// <summary>
/// The icon shown alongside a tray balloon notification, mapped to the corresponding OS
/// icon. See <see cref="ITrayIconService.ShowBalloonTip"/>.
/// </summary>
public enum TrayIconBalloonIcon
{
    /// <summary>No icon.</summary>
    None,

    /// <summary>The informational icon.</summary>
    Info,

    /// <summary>The warning icon.</summary>
    Warning,

    /// <summary>The error icon.</summary>
    Error,
}
