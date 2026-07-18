// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Hotkeys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Barbatos.Wpf.Hosting;

public static partial class AppHostBuilderExtensions
{
    /// <summary>
    /// Adds the optional global hotkeys feature (for example a "quick entry" shortcut).
    /// Hotkeys are registered from code via <paramref name="configureDelegate"/>; their
    /// gestures can be overridden from the <c>Barbatos:GlobalHotkeys</c> configuration
    /// section and changed at runtime through <see cref="IGlobalHotkeyService"/>.
    /// </summary>
    public static WpfAppBuilder ConfigureGlobalHotkeys(this WpfAppBuilder builder, Action<IGlobalHotkeyBuilder>? configureDelegate = null)
    {
        var options = builder.Services.AddOptions<GlobalHotkeyOptions>();
        options.Bind(builder.Configuration.GetSection(GlobalHotkeyOptions.SectionName));

        builder.Services.TryAddSingleton<IHotkeyPlatform, Win32HotkeyPlatform>();
        builder.Services.TryAddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IWpfInitializeService, GlobalHotkeyInitializer>());

        configureDelegate?.Invoke(new GlobalHotkeyBuilder(builder.Services));

        return builder;
    }

    /// <summary>
    /// Adds the optional global hotkeys feature and configures the options from code.
    /// </summary>
    public static WpfAppBuilder ConfigureGlobalHotkeys(this WpfAppBuilder builder, Action<GlobalHotkeyOptions> configureOptions, Action<IGlobalHotkeyBuilder>? configureDelegate = null)
    {
        _ = configureOptions ?? throw new ArgumentNullException(nameof(configureOptions));

        builder.Services.Configure(configureOptions);

        return builder.ConfigureGlobalHotkeys(configureDelegate);
    }

    class GlobalHotkeyBuilder : IGlobalHotkeyBuilder
    {
        readonly IServiceCollection _services;

        public GlobalHotkeyBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public IGlobalHotkeyBuilder Add(string name, string defaultGesture, Action? callback = null)
        {
            _services.AddSingleton(new GlobalHotkeyRegistration(name, defaultGesture, callback));
            return this;
        }
    }

    class GlobalHotkeyInitializer : IWpfInitializeService
    {
        public void Initialize(IServiceProvider services)
        {
            if (services.GetRequiredService<IGlobalHotkeyService>() is GlobalHotkeyService service)
                service.ApplyOptions();
        }
    }
}
