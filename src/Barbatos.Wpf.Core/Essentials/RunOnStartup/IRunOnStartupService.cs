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
    /// <remarks>
    /// This reads only the <c>Run</c> registry value this service writes. The on/off switch in Task
    /// Manager's Startup tab and in Settings &gt; Apps &gt; Startup is stored separately, under
    /// <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run</c>, and
    /// leaves the <c>Run</c> value in place: after a user turns the app off there, this still
    /// reads <see langword="true"/> while Windows no longer launches it, and
    /// <see cref="SetEnabled"/>(<see langword="true"/>) does not turn it back on. The service does
    /// not read or write that key on purpose: it is undocumented, and overriding a choice the user
    /// made in the OS's own UI is not this library's call. A settings screen can say so next to
    /// its checkbox.
    /// </remarks>
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
