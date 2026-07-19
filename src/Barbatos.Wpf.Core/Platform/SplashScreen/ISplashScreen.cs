// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Hosting;

/// <summary>
/// Optionally implemented by a splash screen <see cref="System.Windows.Window"/> returned from
/// <see cref="WpfApplication.CreateSplashScreen"/> to control how long it stays visible.
/// </summary>
/// <remarks>
/// The built-in <see cref="SplashWindow"/> implements this itself, driven by
/// <see cref="SplashScreenOptions.MinimumDisplayDuration"/>. Implement it on your own window
/// too if you override <see cref="WpfApplication.CreateSplashScreen"/> with fully custom UI and
/// still want the minimum-display-duration behavior.
/// </remarks>
public interface ISplashScreen
{
    /// <summary>
    /// The minimum time the splash screen should remain visible from the moment it is shown,
    /// regardless of how quickly the rest of app startup finishes. This is what avoids a
    /// jarring flash when startup happens to be fast - the splash screen then acts like a
    /// deliberate "ad slot" for at least this long. A splash screen window that does not
    /// implement this interface is treated as <see cref="TimeSpan.Zero"/> (closed as soon as
    /// startup finishes, with no minimum).
    /// </summary>
    TimeSpan MinimumDisplayDuration { get; }
}
