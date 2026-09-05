using Barbatos.Wpf.Aquarius.Composition;
using Barbatos.Wpf.AquariusRouter.Sample.Views.Dashboard;
using Barbatos.Wpf.AquariusRouter.Sample.Views.Dialog;
using Barbatos.Wpf.AquariusRouter.Sample.Views.Login;
using Barbatos.Wpf.AquariusRouter.Sample.Views.Preferences;
using Barbatos.Wpf.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.AquariusRouter.Sample;

/// <summary>
/// Composes the <see cref="WpfApp"/> host, mirroring .NET MAUI's <c>MauiProgram</c> pattern (and
/// <c>Barbatos.Wpf.Aquarius.Sample</c>'s own <c>WpfProgram</c>).
/// </summary>
public static class WpfProgram
{
    public static WpfApp CreateWpfApp()
    {
        var builder = WpfApp.CreateBuilder();

        // Loaded WITH reloadOnChange - what makes ShortcutRoutingInitializer's IOptionsMonitor
        // pick up a saved shortcut preference live, no restart. Contrast with
        // Barbatos.Wpf.Core.Sample's own SettingsStore, whose file is deliberately loaded
        // without reloadOnChange (restart-required by design there).
        builder.Configuration.AddJsonFile(ShortcutSettingsStore.FilePath, optional: true, reloadOnChange: true);

        builder.Services.AddSingleton<AuthState>();
        builder.Services.AddSingleton<NavigationHistoryLog>();

        // AddAquarius/AddAquariusRouter are the DI entry points for the two libraries; the routes
        // parameter is a factory rather than a plain collection because AppRoutes.Build needs the
        // already-DI-resolved AuthState singleton, so the /admin route's per-route BeforeEnter
        // guard can close over it (see AppRoutes.cs).
        builder.Services.AddAquarius();
        builder.Services.AddAquariusRouter(sp => AppRoutes.Build(sp.GetRequiredService<AuthState>()));

        // The whole of the Inspector dialog's wiring: one registration, so RouterWindows can
        // resolve the window from this container when a route declaring it is matched (see
        // AppRoutes' /dashboard children). Transient, so each open gets a fresh window - a closed
        // WPF Window cannot be shown again.
        builder.Services.AddTransient<InspectorWindow>();

        // Only ViewModels with a real constructor dependency need a DI registration at all -
        // Setup's own Activator.CreateInstance fallback already covers every parameterless one
        // (HomeViewModel, AboutViewModel, UserLayoutViewModel, ...) with nothing to register here.
        // Transient, not Singleton: Router constructs a fresh instance every time a record is
        // genuinely (re-)entered (see RouteRecord.Instances), and a DI Singleton registration
        // here would silently fight that by always handing back the same old instance instead.
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<PreferencesViewModel>();

        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        // Registers this sample's keyboard shortcuts (in-app + global hotkeys) - see
        // ShortcutRouting for what they do and why. The actions themselves are declared here,
        // before Build(); ConfigureShortcutRouting wires their .Performed handlers and the live
        // key-binding-override reload automatically once the host is built (IWpfInitializeService,
        // same convention ConfigureInputSystem itself already uses) - must come after
        // ConfigureInputSystem, since it looks up actions that method's own initializer declares.
        builder.ConfigureInputSystem(ShortcutRouting.ConfigureActions);
        builder.ConfigureShortcutRouting();

        // Every *global* guard/hook this sample registers - see AppNavigationGuards for why these
        // live apart from the route table itself. Also IWpfInitializeService-based, for the same
        // reason - no manual GetRequiredService/static-call wiring needed here anymore.
        builder.ConfigureAppNavigationGuards();

        var app = builder.Build();

        // The actual Aquarius + AquariusRouter integration, in two calls instead of two static
        // assignments. UseAquarius points Aquarius's markup-driven type resolution at this
        // container, so aq:Setup.Enable resolves every routed View's own ViewModel through it -
        // LoginViewModel/DashboardViewModel above get their constructor-injected AuthState/Router
        // exactly like MainViewModel does - and Router's own entering-View construction goes
        // through the same path. UseAquariusRouter publishes the registered Router as
        // Router.Current, the ambient default RouterView/RouterLink fall back to when neither sets
        // an explicit Router of its own; that is what lets MainWindow.xaml's <aqr:RouterView/> and
        // every aqr:RouterLink.To in its nav bar work with zero further wiring - and, through
        // RouterWindows, the Inspector dialog's outlet and links too.
        app.Services.UseAquarius();
        app.Services.UseAquariusRouter();

        return app;
    }
}
