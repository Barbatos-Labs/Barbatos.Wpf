// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Startup;

/// <summary>
/// Controls whether the application automatically starts when the user logs in.
/// The state is persisted by the OS (registry), so it survives application restarts.
/// </summary>
public interface IRunOnStartupService
{
    /// <summary>
    /// Gets whether the application is currently registered to run on startup.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Registers or unregisters the application to run on startup. Intended to be
    /// called from a settings UI.
    /// </summary>
    void SetEnabled(bool enabled);

    /// <summary>
    /// Occurs when <see cref="IsEnabled"/> changes through <see cref="SetEnabled"/>.
    /// </summary>
    event EventHandler? IsEnabledChanged;
}
