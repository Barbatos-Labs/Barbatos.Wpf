// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Core Contributors.
// All Rights Reserved.

using System.Windows;
using Barbatos.Wpf.Hosting;
using Barbatos.Wpf.Hotkeys;
using Barbatos.Wpf.Tray;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Hosting.Sample;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : WpfApplication
{
    /// <summary>
    /// Set when the user explicitly exits (tray menu), so the minimize-to-tray
    /// behavior does not cancel the shutdown.
    /// </summary>
    internal static bool IsExiting { get; private set; }

    /// <inheritdoc />
    protected override WpfApp CreateWpfApp() => WpfProgram.CreateWpfApp();

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Double-clicking the tray icon re-opens the main window.
        if (Services.GetService<ITrayIconService>() is ITrayIconService trayIcon)
            trayIcon.DoubleClicked += (sender, args) => ShowMainWindow();

        // Log hotkey presses into the lifecycle event list.
        if (Services.GetService<IGlobalHotkeyService>() is IGlobalHotkeyService hotkeys)
            hotkeys.HotkeyPressed += (sender, args) =>
                Services.GetRequiredService<MainViewModel>()
                    .LogLifecycleEvent($"Hotkey pressed ({args.Hotkey.Name}: {args.Hotkey.Gesture})");

        // The main window is composed by (and resolved from) the dependency injection container.
        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }

    /// <summary>
    /// Shows and activates the main window. Used by the tray icon and the quick entry hotkey.
    /// </summary>
    internal static void ShowMainWindow()
    {
        if (Current?.MainWindow is not Window window)
            return;

        window.Show();

        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;

        window.Activate();
    }

    /// <summary>
    /// Exits the application, bypassing the minimize-to-tray behavior.
    /// </summary>
    internal static void ExitApplication()
    {
        IsExiting = true;
        Current?.Shutdown();
    }
}
