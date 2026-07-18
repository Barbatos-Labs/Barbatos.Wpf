// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Hosting Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Hosting;

/// <summary>
/// Represents the platform (WPF) application together with its configured services.
/// </summary>
/// <remarks>
/// This is the WPF counterpart of .NET MAUI's <c>IPlatformApplication</c>.
/// </remarks>
public interface IWpfPlatformApplication
{
    /// <summary>
    /// Gets or sets the current platform application.
    /// </summary>
    public static IWpfPlatformApplication? Current { get; set; }

    /// <summary>
    /// The application's configured services.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// The WPF <see cref="System.Windows.Application"/> instance.
    /// </summary>
    System.Windows.Application Application { get; }
}
