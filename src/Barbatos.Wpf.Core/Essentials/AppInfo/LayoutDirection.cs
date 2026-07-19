// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.ApplicationModel;

/// <summary>
/// Enumerates possible layout directions.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>LayoutDirection</c>.</remarks>
public enum LayoutDirection
{
    /// <summary>The requested layout direction is unknown.</summary>
    Unknown,

    /// <summary>The requested layout direction is left-to-right.</summary>
    LeftToRight,

    /// <summary>The requested layout direction is right-to-left.</summary>
    RightToLeft,
}
