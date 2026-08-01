#if (IncludeI18n)
using System.Globalization;
using Barbatos.i18n;
using Barbatos.i18n.DependencyInjection;
using Barbatos.i18n.Wpf;
#endif
#if (AquariusEffective)
using Barbatos.Wpf.Aquarius.Composition;
#endif
#if (IncludeAquariusRouter)
using Barbatos.Wpf.AquariusRouter.Routing;
#endif
#if (IncludeDialogs)
using Barbatos.Wpf.Dialogs;
#endif
using Barbatos.Wpf.Hosting;
#if (IncludeInputSystem)
using Barbatos.Wpf.Input;
#endif
#if (IncludeMcp)
using Barbatos.Wpf.Mcp;
#endif
#if (IncludeNotifications)
using Barbatos.Wpf.Notifications;
#endif
#if (IncludeKeepAwake)
using Barbatos.Wpf.Power;
#endif
#if (IncludePushNotifications)
using Barbatos.Wpf.PushNotifications;
#endif
#if (IncludeSingleInstance)
using Barbatos.Wpf.SingleInstance;
#endif
#if (IncludeRunOnStartup)
using Barbatos.Wpf.Startup;
#endif
#if (IncludeTrayIcon)
using Barbatos.Wpf.Tray;
#endif
using Microsoft.Extensions.DependencyInjection;
#if (IncludeTrayIcon || IncludeInputSystem)
using System.Windows;
#endif
#if (IncludeInputSystem)
using System.Windows.Input;
#endif

namespace BarbatosWpfApp;

/// <summary>
/// Composes the <see cref="WpfApp"/> host, mirroring .NET MAUI's <c>MauiProgram</c> pattern
/// (see Barbatos.Wpf.Core's own README/sample for the full shape this is a minimal slice of).
/// </summary>
public static class WpfProgram
{
    public static WpfApp CreateWpfApp()
    {
        var builder = WpfApp.CreateBuilder();

#if (IncludeI18n)
        // Barbatos.i18n - in-memory translations for this starter, so there's no embedded
        // resource file to ship; swap AddLocalization for FromJson/FromCsv/FromIni (separate
        // NuGet packages) once you have real translation files - see Barbatos.i18n's own
        // README for those.
        builder.Services.AddStringLocalizer(localization =>
        {
            localization.AddLocalization(new CultureInfo("en-US"), new Dictionary<LocalizationKey, string?>
            {
                ["Greeting"] = "Hello from Barbatos.Wpf, wired together! (current language: {0})",
            }!);
            localization.AddLocalization(new CultureInfo("vi-VN"), new Dictionary<LocalizationKey, string?>
            {
                ["Greeting"] = "Xin chào từ Barbatos.Wpf, kết hợp cùng nhau! (ngôn ngữ hiện tại: {0})",
            }!);
        });

#endif
#if (IncludeSingleInstance)
        builder.ConfigureSingleInstance();
#endif
#if (IncludeRunOnStartup)
        builder.ConfigureRunOnStartup();
#endif
#if (IncludeKeepAwake)
        builder.ConfigureKeepAwake();
#endif
#if (IncludeTrayIcon)
        builder.ConfigureTrayIcon(options =>
        {
            options.MenuItems.Add(new TrayMenuItem("Open", () =>
            {
                if (IWpfPlatformApplication.Current?.Application.MainWindow is Window window)
                {
                    window.Show();
                    window.Activate();
                }
            }) { IsDefault = true });
            options.MenuItems.Add(TrayMenuItem.Separator);
            options.MenuItems.Add(new TrayMenuItem("Exit", () => IWpfPlatformApplication.Current?.Application.Shutdown()));
        });
#endif
#if (IncludeNotifications)
        builder.ConfigureNotifications();
#endif
#if (IncludePushNotifications)
        // Disabled by default - no real push server exists for a fresh starter.
        builder.ConfigurePushNotifications(
            options =>
            {
                options.Enabled = false;
                // A server that re-sends until acknowledged will occasionally deliver a duplicate
                // (when the acknowledgement is lost to a dropped connection); this is how many
                // receipt ids are remembered so that stays invisible. 0 disables de-duplication.
                options.DeduplicationHistorySize = 256;
            },
            configureSignalR: options =>
            {
                options.ServerUrl = "https://localhost:5443/hubs/notifications";
                options.AppId = "BarbatosWpfApp";
                // Sent with the handshake, so a server can target a subset of devices instead of
                // broadcasting to every one of them.
                options.Tags["channel"] = "stable";
                // Also available: AppKey (+ AppKeyHeaderName) if the server authenticates the
                // app, and AcknowledgeMethodName - set that to null for a server that doesn't
                // track delivery, so nothing is sent back.
            });
#endif
#if (IncludePeriodicServices)
        builder.ConfigurePeriodicServices<HeartbeatService>();
#endif
#if (IncludeInputSystem)
        builder.ConfigureInputSystem(options =>
        {
            var shortcuts = new InputActionMap("AppShortcuts");
            var showWindow = shortcuts.AddAction("ShowWindow");
            showWindow.AddKeyBinding(Key.F9, ModifierKeys.Control);
            showWindow.Performed += (_, _) =>
            {
                if (IWpfPlatformApplication.Current?.Application.MainWindow is Window window)
                {
                    window.Show();
                    window.Activate();
                }
            };
            options.ActionMaps.Add(shortcuts);
        });
#endif
#if (IncludeDialogs)
        builder.ConfigureDialogs();
#endif
#if (IncludeMcp)
        builder.ConfigureMcp(
            options => options.Servers.Add(new McpServerDescriptor
            {
                Name = "NuGet",
                TransportKind = McpTransportKind.Stdio,
                Command = "dotnet",
                Arguments = { "dnx", "NuGet.Mcp.Server", "--version", "1.4.16", "--yes" },
            }),
            configureAgent: agent =>
            {
                // The Microsoft Agent Framework layer, registered alongside the one-shot
                // IAiChatService rather than replacing it: IAiAgentFactory builds an AIAgent that
                // remembers the conversation across turns, and IAiSessionStore can persist that
                // conversation across app restarts. No API key is baked in either way - the end
                // user brings their own (BYOK).
                agent.Name = "BarbatosWpfAppAssistant";
                agent.Instructions = "You are a helpful assistant inside a WPF desktop app.";
                // ToolApproval is deliberately left at its default, AiToolApprovalMode.Always:
                // an agent run pauses and hands the pending MCP tool call back to you, so the app
                // can ask the end user before anything actually runs.
            });
#endif

        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainWindow>();
#if (IncludeDialogs)
        builder.Services.AddTransient<AboutWindow>();
#endif
#if (IncludeValee)
        // Transient, so navigating back to the form starts it blank rather than re-showing
        // whatever the last visit left in it. Resolved by SignUpView's own aq:Setup.Enable.
        builder.Services.AddTransient<SignUpViewModel>();
#endif
#if (IncludeAquariusRouter)
        builder.Services.AddSingleton(_ => new Router(AppRoutes.Build()));
#endif

        var app = builder.Build();

#if (AquariusEffective)
        // The actual Aquarius + Core integration: Setup.ViewModel (see MainWindow.xaml)
        // resolves a ViewModel through *this* same DI container instead of always
        // Activator.CreateInstance-ing a fresh one - so a ViewModel can take
        // constructor-injected Core services exactly like MainViewModel/MainWindow above do.
        Setup.ServiceProvider = app.Services;
#endif
#if (IncludeAquariusRouter)
        Router.Current = app.Services.GetRequiredService<Router>();
#endif
#if (IncludeI18n)
        // Bridges the DI-built localization container into Barbatos.i18n.Wpf's markup
        // extensions - {i18n:StringLocalizer} in MainWindow.xaml can't take constructor
        // injection (the XAML parser only ever calls its parameterless constructor), so this
        // static bridge is how it finds the container registered above - and sets the
        // starting culture (English, regardless of the OS's own UI culture, so this starter
        // always begins in a known state; switch with the buttons in MainWindow).
        app.Services.UseWpfLocalization().SetLocalizationCulture(new CultureInfo("en-US"));
#endif

        return app;
    }
}
