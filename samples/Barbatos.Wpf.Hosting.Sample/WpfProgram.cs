// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Hosting;
using Barbatos.Wpf.LifecycleEvents;
using Barbatos.Wpf.Tray;
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

        // Code defaults first, then the shipped appsettings.json, then the settings the
        // user changed from the UI on a previous run (last one wins).
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Sample:Greeting"] = "Hello from Barbatos.Wpf.Hosting — a MAUI-style host for WPF!",
        });
        builder.Configuration.AddJsonFile("appsettings.json", optional: true);
        builder.Configuration.AddJsonFile(SettingsStore.FilePath, optional: true);

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
            .OnWindowClosing((window, args) =>
            {
                LogLifecycleEvent($"Window.Closing ({window.Title})");

                // "Keep running in the system tray": closing the window hides it instead
                // of exiting while the tray icon is visible.
                var trayIcon = IWpfPlatformApplication.Current?.Services.GetService<ITrayIconService>();
                if (trayIcon?.IsVisible == true && !App.IsExiting)
                {
                    args.Cancel = true;
                    window.Hide();
                }
            })
            .OnWindowClosed((window, args) => LogLifecycleEvent($"Window.Closed ({window.Title})"))));

        // The optional features from the settings screen. Each one binds its own
        // configuration section and is toggled at runtime by MainViewModel.
        builder.ConfigureRunOnStartup();
        builder.ConfigureKeepAwake();
        builder.ConfigureTrayIcon(options =>
        {
            options.MenuItems.Add(new TrayMenuItem("Open", App.ShowMainWindow));
            options.MenuItems.Add(new TrayMenuItem("Exit", App.ExitApplication));
        });
        builder.ConfigureGlobalHotkeys(hotkeys => hotkeys
            .Add("QuickEntry", "Control+Alt+8", App.ShowMainWindow));
        builder.ConfigurePeriodicServices<HeartbeatService>();
        builder.ConfigureNotifications();

        builder.Services.AddSingleton<SettingsStore>();
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
