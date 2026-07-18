// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Core Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Power;

/// <summary>
/// Prevents the computer from idle-sleeping while the application is running,
/// for example so scheduled or background tasks can run.
/// </summary>
public interface IKeepAwakeService
{
    /// <summary>
    /// Gets whether the computer is currently being kept awake by this application.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Enables or disables keeping the computer awake. Intended to be called from a
    /// settings UI. The state is not persisted; persist it via configuration if needed.
    /// </summary>
    void SetEnabled(bool enabled);

    /// <summary>
    /// Occurs when <see cref="IsEnabled"/> changes.
    /// </summary>
    event EventHandler? IsEnabledChanged;
}
