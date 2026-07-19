// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.SingleInstance;

/// <summary>
/// Detects whether this process is the only running instance of the application, identified
/// by <see cref="Barbatos.Wpf.ApplicationModel.IAppInfo.AppGuid"/>.
/// </summary>
/// <remarks>
/// This has no .NET MAUI counterpart (mobile platforms are inherently single-instance). By
/// default (see <see cref="SingleInstanceOptions"/>), a second launch attempt is blocked and
/// silently exits after notifying the first instance — matching what most Windows desktop
/// apps do.
/// </remarks>
public interface ISingleInstanceService
{
    /// <summary>
    /// Gets whether this process is the first (primary) instance of the application.
    /// </summary>
    /// <remarks>
    /// A process that is not the primary instance signals the primary instance and then
    /// terminates via <see cref="Environment.Exit(int)"/> during <c>Build()</c> — before any
    /// window is created — so in practice nothing ever observes this property being
    /// <see langword="false"/>. It is still exposed for completeness and for the
    /// <see cref="SingleInstanceOptions.Enabled"/> = <see langword="false"/> case, where no
    /// check is performed and this always reports <see langword="true"/>.
    /// </remarks>
    bool IsPrimaryInstance { get; }

    /// <summary>
    /// Occurs on the primary instance, on the UI thread, when a second launch attempt is
    /// detected. Raised after <see cref="SingleInstanceOptions.ActivateMainWindow"/>'s default
    /// window-activation behavior (if enabled) has already run.
    /// </summary>
    event EventHandler? SecondInstanceLaunched;
}
