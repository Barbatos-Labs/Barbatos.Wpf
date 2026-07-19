// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.ApplicationModel;

/// <summary>
/// Enumerates different themes an operating system or application can show.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>AppTheme</c>.</remarks>
public enum AppTheme
{
    /// <summary>Default, unknown or unspecified theme.</summary>
    Unspecified,

    /// <summary>Light theme.</summary>
    Light,

    /// <summary>Dark theme.</summary>
    Dark,
}
