// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using Barbatos.Wpf.ApplicationModel;
using Barbatos.Wpf.Hosting;
using Barbatos.Wpf.Tray;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.Sample;

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
    protected override SplashScreenOptions GetSplashScreenOptions() => new()
    {
        Tagline = "Loading your workspace...",
        // Deliberately visible for a bit even though this sample's own startup is near-instant -
        // see "SplashScreen" in the root README.md for why (avoids a flash on fast machines).
        MinimumDisplayDuration = TimeSpan.FromSeconds(2),
        RelatedLinks =
        {
            new SplashScreenLink("Barbatos.Wpf.Core Sample", "The app that's about to open.", "https://example.com"),
            new SplashScreenLink("Another product by this publisher", "RelatedLinks entries work with or without a Url."),
        },
    };

    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Waits out any remaining SplashScreenOptions.MinimumDisplayDuration, then closes the
        // splash screen shown for GetSplashScreenOptions() above.
        await CloseSplashScreenAsync();

        // Double-clicking the tray icon re-opens the main window.
        if (Services.GetService<ITrayIconService>() is ITrayIconService trayIcon)
            trayIcon.DoubleClicked += (sender, args) => ShowMainWindow();

        // The "Open" app action (taskbar Jump List entry) re-opens the main window.
        AppActions.OnAppAction += (sender, args) =>
        {
            if (args.AppAction.Id == "open")
                ShowMainWindow();
        };

        // The main window is composed by (and resolved from) the dependency injection container.
        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }

    /// <summary>
    /// Shows and activates the main window. Used by the tray icon and app actions.
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
