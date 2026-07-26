// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Barbatos.Wpf.AquariusRouter.Sample.Routing;

/// <summary>
/// Wires <see cref="AppNavigationGuards.Register"/> the DI-idiomatic way - constructor-injected,
/// invoked automatically via <see cref="IWpfInitializeService"/> instead of a manual
/// <c>GetRequiredService</c> call in <c>WpfProgram.cs</c>. <see cref="AppNavigationGuards.Register"/>
/// itself is unchanged - it already operates purely on the instances passed in.
/// </summary>
public sealed class AppNavigationGuardsInitializer(Router router, AuthState authState, NavigationHistoryLog history, MainViewModel mainViewModel) : IWpfInitializeService
{
    public void Initialize(IServiceProvider services) =>
        AppNavigationGuards.Register(router, authState, history, mainViewModel);
}

public static class AppNavigationGuardsInitializerExtensions
{
    public static WpfAppBuilder ConfigureAppNavigationGuards(this WpfAppBuilder builder)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IWpfInitializeService, AppNavigationGuardsInitializer>());
        return builder;
    }
}
