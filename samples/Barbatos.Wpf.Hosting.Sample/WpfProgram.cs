// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Hosting Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Hosting;
using Barbatos.Wpf.LifecycleEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Hosting.Sample;

/// <summary>
/// Composes the <see cref="WpfApp"/> host, mirroring .NET MAUI's <c>MauiProgram</c> pattern.
/// </summary>
public static class WpfProgram
{
    public static WpfApp CreateWpfApp()
    {
        var builder = WpfApp.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Sample:Greeting"] = "Hello from Barbatos.Wpf.Hosting — a MAUI-style host for WPF!",
        });

        builder.ConfigureLifecycleEvents(events => events.AddWpf(wpf => wpf
            .OnStartup((app, args) => LogLifecycleEvent("Application.OnStartup"))
            .OnActivated((app, args) => LogLifecycleEvent("Application.OnActivated"))
            .OnDeactivated((app, args) => LogLifecycleEvent("Application.OnDeactivated"))
            .OnExit((app, args) => LogLifecycleEvent("Application.OnExit"))
            .OnWindowCreated(window => LogLifecycleEvent($"Window.Created ({window.Title})"))
            .OnWindowLoaded((window, args) => LogLifecycleEvent($"Window.Loaded ({window.Title})"))
            .OnWindowActivated((window, args) => LogLifecycleEvent($"Window.Activated ({window.Title})"))
            .OnWindowDeactivated((window, args) => LogLifecycleEvent($"Window.Deactivated ({window.Title})"))
            .OnWindowStateChanged((window, args) => LogLifecycleEvent($"Window.StateChanged ({window.WindowState})"))
            .OnWindowClosing((window, args) => LogLifecycleEvent($"Window.Closing ({window.Title})"))
            .OnWindowClosed((window, args) => LogLifecycleEvent($"Window.Closed ({window.Title})"))));

        builder.Services.AddSingleton<IGreetingService, GreetingService>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        return builder.Build();
    }

    static void LogLifecycleEvent(string message) =>
        IWpfPlatformApplication.Current?.Services
            .GetRequiredService<MainViewModel>()
            .LogLifecycleEvent(message);
}
