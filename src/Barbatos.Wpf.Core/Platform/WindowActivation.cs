// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;

namespace Barbatos.Wpf.Hosting;

/// <summary>
/// Brings a <see cref="Window"/> to the foreground reliably, for the features that have to
/// surface an existing window on the user's behalf (re-showing a deduplicated dialog,
/// activating the main window when a second instance launches, handing focus back to a
/// dialog's owner).
/// </summary>
internal static class WindowActivation
{
    /// <summary>
    /// Un-minimizes and shows <paramref name="window"/> if needed, then brings it to the
    /// foreground via <see cref="BringToForeground"/>.
    /// </summary>
    /// <remarks>
    /// Only safe for a window that is not itself mid-close: WPF forbids <see cref="Window.Show"/>
    /// on such a window. Use <see cref="BringToForeground"/> alone there.
    /// </remarks>
    internal static void RestoreAndActivate(Window window)
    {
        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;

        if (!window.IsVisible)
            window.Show();

        BringToForeground(window);
    }

    /// <summary>Activates and focuses <paramref name="window"/>.</summary>
    /// <remarks>
    /// <see cref="Window.Activate"/> alone can be silently ignored by Windows' foreground-lock
    /// rules when another app currently owns the foreground; briefly toggling
    /// <see cref="Window.Topmost"/> is the standard, reliable way around that without
    /// <c>SetForegroundWindow</c> P/Invoke.
    /// </remarks>
    internal static void BringToForeground(Window window)
    {
        window.Topmost = true;
        window.Activate();
        window.Topmost = false;
        window.Focus();
    }
}
