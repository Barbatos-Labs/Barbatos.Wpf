// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Apsu;
using Barbatos.Wpf.Hosting;
using Barbatos.Wpf.LifecycleEvents;
using Barbatos.Wpf.Mcp;
using Barbatos.Wpf.Tray;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Barbatos.Wpf.Core.Sample;

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
            ["Sample:Greeting"] = "Hello from Barbatos.Wpf.Core — a MAUI-style host for WPF!",
        });
        builder.Configuration.AddJsonFile("appsettings.json", optional: true);
        builder.Configuration.AddJsonFile(SettingsStore.FilePath, optional: true);

        // Core reports a refused global hotkey, a failed periodic run or a push connection error
        // only through ILogger, and registers no logging provider of its own - without one, every
        // such failure is silent. The Debug provider writes them to the debugger's Output window.
        builder.Logging.AddDebug();

        builder.ConfigureLifecycleEvents(events => events.AddWpf(wpf => wpf
            .OnStartup((app, args) => LogLifecycleEvent("Application.OnStartup"))
            .OnActivated((app, args) => LogLifecycleEvent("Application.OnActivated"))
            .OnDeactivated((app, args) => LogLifecycleEvent("Application.OnDeactivated"))
            .OnExit((app, args) => LogLifecycleEvent("Application.OnExit"))
            .OnDispatcherUnhandledException((app, args) =>
            {
                // Barbatos.Wpf.Core's own WpfApplication already subscribes to
                // Application.DispatcherUnhandledException and forwards it here - the idiomatic
                // place to observe it, rather than a second direct subscription in App.xaml.cs.
                // Routed through the same SampleActivityLog a store action's own error goes
                // through (see CounterStore.SimulateError), so both share one policy.
                var activityLog = IWpfPlatformApplication.Current?.Services.GetService<SampleActivityLog>();
                activityLog?.Log($"[Interceptor] DispatcherUnhandledException failed: {args.Exception.Message}");
                LogLifecycleEvent($"Application.OnDispatcherUnhandledException: {args.Exception.Message}");

                // Marked handled so an exception that reaches here (e.g. CounterStore.SimulateError,
                // deliberately uncaught) is logged and then swallowed instead of taking down the
                // whole sample app - a demo button crashing the process it's demonstrating is a
                // poor experience. A real app should weigh this per exception instead of blanket
                // swallowing everything that reaches its own last resort.
                args.Handled = true;
            })
            .OnWindowCreated(window => LogLifecycleEvent($"Window.Created ({window.Title})"))
            .OnWindowLoaded((window, args) => LogLifecycleEvent($"Window.Loaded ({window.Title})"))
            .OnWindowActivated((window, args) => LogLifecycleEvent($"Window.Activated ({window.Title})"))
            .OnWindowDeactivated((window, args) => LogLifecycleEvent($"Window.Deactivated ({window.Title})"))
            .OnWindowStateChanged((window, args) => LogLifecycleEvent($"Window.StateChanged ({window.WindowState})"))
            .OnWindowClosing((window, args) =>
            {
                LogLifecycleEvent($"Window.Closing ({window.Title})");

                // "Keep running in the system tray": closing the main window hides it instead
                // of exiting while the tray icon is visible. Window events fire for every
                // window - the splash screen, AboutWindow, DetailsWindow, the push fallback
                // popups - and cancelling those would leave them hidden but alive (and still
                // tracked as open by IDialogService), so only the main window qualifies.
                // Matched by type rather than against Application.MainWindow: WPF makes the
                // first window created - the splash screen - Application.MainWindow until it
                // closes (Window.cs in dotnet/wpf), so a reference check would cancel the
                // splash's own close whenever the tray icon is already visible at startup.
                if (window is not MainWindow)
                    return;

                var trayIcon = IWpfPlatformApplication.Current?.Services.GetService<ITrayIconService>();
                if (trayIcon?.IsVisible == true && !App.IsExiting)
                {
                    args.Cancel = true;
                    window.Hide();

                    // A hidden window looks identical to a closed app otherwise - this balloon
                    // is what tells the user the app is still running, the same way e.g. Discord
                    // does the moment you close it to the tray.
                    trayIcon.ShowBalloonTip(
                        "Barbatos.Wpf.Core Sample",
                        "Still running in the system tray. Double-click the icon to reopen.");
                    LogLifecycleEvent("TrayIconService.ShowBalloonTip(...) - minimized to tray");
                }
            })
            .OnWindowClosed((window, args) => LogLifecycleEvent($"Window.Closed ({window.Title})"))));

        // Enabled by default (unlike the other optional features below): a second launch of
        // this sample detects the running instance, brings its window to the foreground, and
        // exits immediately instead of opening a second window.
        builder.ConfigureSingleInstance();

        builder.ConfigureDialogs();

        // The optional features from the settings screen. Each one binds its own
        // configuration section and is toggled at runtime by MainViewModel.
        builder.ConfigureRunOnStartup();
        builder.ConfigureKeepAwake();
        // A deliberately varied menu - default (bold) item, grouped separators, and a
        // disabled item - to show off everything TrayMenuItem supports, the same shapes
        // Windows' own tray icons (e.g. Bluetooth) use.
        builder.ConfigureTrayIcon(options =>
        {
            options.MenuItems.Add(new TrayMenuItem("Open", App.ShowMainWindow) { IsDefault = true });
            options.MenuItems.Add(TrayMenuItem.Separator);
            options.MenuItems.Add(new TrayMenuItem("About", App.ShowAboutWindow));
            options.MenuItems.Add(new TrayMenuItem("Check for Updates", () => { }) { IsEnabled = false });
            options.MenuItems.Add(TrayMenuItem.Separator);
            options.MenuItems.Add(new TrayMenuItem("Exit", App.ExitApplication));
        });
        builder.ConfigurePeriodicServices<HeartbeatService>();
        builder.ConfigureNotifications();
        // Disabled by default - no real push server exists yet. ServerUrl/AppId are still set so
        // the sample's "Connect" button has something to actually attempt (and fail gracefully
        // against, proving the app keeps running); use "Simulate incoming notification" instead
        // to see the display/fallback pipeline work without any network involved.
        builder.ConfigurePushNotifications(
            options => options.Enabled = false,
            configureSignalR: options =>
            {
                options.ServerUrl = "https://localhost:5443/hubs/notifications";
                options.AppId = "Barbatos.Wpf.Core.Sample";
            });
        builder.ConfigureEssentials(essentials => essentials
            .AddAppAction("open", "Open", "Show the main window")
            .OnAppAction(action => LogLifecycleEvent($"App action activated ({action.Id})"))
            .UseVersionTracking());

        // Seeds one MCP server that works with zero setup: NuGet's own MCP server, launched via
        // `dotnet dnx` (.NET 10's tool-exec command, the .NET-tool equivalent of `npx`) - no
        // Node.js/npm required, since building this repo already requires the .NET 10 SDK.
        // Verified directly (`dotnet dnx NuGet.Mcp.Server --version 1.4.16 --yes`) rather than
        // assumed - note the separate `--version` flag, not an `@version`-suffixed package id;
        // the latter failed to resolve against a real dnx install even for a version that does
        // exist on NuGet.org. Check https://www.nuget.org/packages/NuGet.Mcp.Server for a newer
        // version before relying on this. The provider is left unconfigured (no API key baked
        // in - BYOK) until the end user enters their own key in the "AI chat" section and clicks
        // Save; Gemini is only the pre-selected default because it has the easiest free-tier key
        // to get for trying the sample.
        builder.ConfigureMcp(
            options => options.Servers.Add(new McpServerDescriptor
            {
                Name = "NuGet",
                TransportKind = McpTransportKind.Stdio,
                Command = "dotnet",
                Arguments = { "dnx", "NuGet.Mcp.Server", "--version", "1.4.16", "--yes" },
            }),
            configureProvider: options =>
            {
                // This sample's own suggested provider catalog - Barbatos.Wpf.Mcp has no fixed
                // enum/list of providers (see AiProviderOptions's remarks for why), so which
                // ones to offer, and their default models/endpoints, is entirely this app's
                // call. MainViewModel.AiProviders reads this same list for its ComboBox.
                options.Providers.Add(new AiProviderDescriptor { Key = "openai", Provider = "openai", Model = "gpt-5.2" });
                // Google's own documented OpenAI-compatible endpoint for the Gemini API.
                options.Providers.Add(new AiProviderDescriptor { Key = "gemini", Provider = "gemini", Model = "gemini-3.5-flash", Endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/" });
                options.Providers.Add(new AiProviderDescriptor { Key = "anthropic", Provider = "anthropic", Model = "claude-opus-4-8" });
                options.Providers.Add(new AiProviderDescriptor { Key = "custom", Provider = "custom" });

                // Gemini is only the pre-selected default because it has the easiest free-tier
                // key to get for trying the sample - the provider is otherwise left unconfigured
                // (no API key baked in - BYOK) until the end user enters their own key in the
                // "AI chat" section and clicks Save.
                var initial = options.Providers.Single(p => p.Key == "gemini");
                options.Provider = initial.Provider;
                options.Model = initial.Model;
                options.Endpoint = initial.Endpoint;
            },
            configureAgent: options =>
            {
                options.Name = "SampleAssistant";

                // Instructions are deliberately NOT set here: this sample rebuilds them on every
                // turn (see MainViewModel.SendAiChatMessage) because they carry the current
                // date/time, which obviously can't be frozen at startup. An app whose
                // instructions are static should just set options.Instructions here instead.

                // Left at its default (AiToolApprovalMode.Always) on purpose - the NuGet MCP
                // server above is third-party code, and demonstrating the approval panel is half
                // the point of this tab. Set AiToolApprovalMode.Never for the older
                // run-tools-immediately behavior that IAiChatService still has.
            });

        // The shared sink SampleActionLoggingInterceptor and the OnDispatcherUnhandledException
        // hook above both report into - DI-constructed (it has no dependencies of its own).
        builder.Services.AddSingleton<SampleActivityLog>();

        // Registered before AddStore<CounterStore>() so every [Action]-marked method on every
        // store gets logged automatically, with zero per-store wiring - see CounterStore.Increment.
        // DI-constructed - no `new` needed, since SampleActionLoggingInterceptor's own constructor
        // needs nothing but the SampleActivityLog registered just above.
        builder.Services.AddSingleton<IStoreActionInterceptor, SampleActionLoggingInterceptor>();
        builder.Services.AddStore<CounterStore>();
        builder.Services.AddSingleton<SettingsStore>();
        builder.Services.AddSingleton<IGreetingService, GreetingService>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainWindow>();
        builder.Services.AddTransient<AboutWindow>();
        builder.Services.AddTransient<DetailsWindow>();

        return builder.Build();
    }

    static void LogLifecycleEvent(string message) =>
        IWpfPlatformApplication.Current?.Services
            .GetRequiredService<MainViewModel>()
            .LogLifecycleEvent(message);
}
