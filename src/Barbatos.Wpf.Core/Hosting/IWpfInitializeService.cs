// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Hosting;

/// <summary>
/// Represents a service that is initialized during the application construction.
/// </summary>
/// <remarks>
/// This service is initialized during the <see cref="WpfAppBuilder.Build()"/> method. It is
/// executed once per application using the root service provider.
/// This is the WPF counterpart of .NET MAUI's <c>IMauiInitializeService</c>.
/// </remarks>
public interface IWpfInitializeService
{
    /// <summary>Initializes the service.</summary>
    /// <param name="services">The application's root service provider.</param>
    void Initialize(IServiceProvider services);
}

/// <summary>
/// Represents a service that is initialized during the window construction.
/// </summary>
/// <remarks>
/// This service is initialized during the creation of a window. It is
/// executed once per window using the window-scoped service provider.
/// This is the WPF counterpart of .NET MAUI's <c>IMauiInitializeScopedService</c>.
/// </remarks>
public interface IWpfInitializeScopedService
{
    /// <summary>Initializes the service for one window.</summary>
    /// <param name="services">The window's scoped service provider.</param>
    void Initialize(IServiceProvider services);
}
