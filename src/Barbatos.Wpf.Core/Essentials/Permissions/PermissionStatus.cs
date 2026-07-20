// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.ApplicationModel;

/// <summary>
/// Possible statuses of a permission.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>PermissionStatus</c>.</remarks>
public enum PermissionStatus
{
    /// <summary>The permission is in an unknown state.</summary>
    Unknown = 0,

    /// <summary>The user denied the permission request.</summary>
    Denied = 1,

    /// <summary>The feature is disabled on the device.</summary>
    Disabled = 2,

    /// <summary>The user granted permission or is automatically granted.</summary>
    Granted = 3,

    /// <summary>In a restricted state.</summary>
    Restricted = 4,

    /// <summary>In a limited state (only iOS on MAUI; never returned on WPF).</summary>
    Limited = 5
}
