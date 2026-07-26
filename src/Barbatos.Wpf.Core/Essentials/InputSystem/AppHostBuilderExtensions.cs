// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Barbatos.Wpf.Hosting;

public static partial class AppHostBuilderExtensions
{
    /// <summary>
    /// Adds the optional InputSystem feature: in-app keyboard shortcuts (<see cref="KeyBinding"/>),
    /// true OS-level global hotkeys (<see cref="GlobalKeyBinding"/>), and in-app key sequences
    /// (<see cref="ChordBinding"/>, e.g. <c>Ctrl+K</c> then <c>Ctrl+P</c>), unified under one
    /// <see cref="InputAction"/> model. The feature can be configured from code via
    /// <paramref name="configure"/> and/or from the <c>Barbatos:InputSystem</c> configuration
    /// section (configuration values override code values), and toggled at runtime through
    /// <see cref="IInputSystemService"/>.
    /// </summary>
    public static WpfAppBuilder ConfigureInputSystem(this WpfAppBuilder builder, Action<InputSystemOptions>? configure = null)
    {
        var options = builder.Services.AddOptions<InputSystemOptions>();
        if (configure != null)
            options.Configure(configure);
        options.Bind(builder.Configuration.GetSection(InputSystemOptions.SectionName));

        builder.Services.TryAddSingleton<IHotkeyPlatform, Win32HotkeyPlatform>();
        builder.Services.TryAddSingleton<IInputSystemService, InputSystemService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IWpfInitializeService, InputSystemInitializer>());

        return builder;
    }

    class InputSystemInitializer : IWpfInitializeService
    {
        public void Initialize(IServiceProvider services)
        {
            if (services.GetRequiredService<IInputSystemService>() is InputSystemService service)
                service.ApplyOptions();
        }
    }
}
