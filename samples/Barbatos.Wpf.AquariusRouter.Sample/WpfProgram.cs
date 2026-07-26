using Barbatos.Wpf.Aquarius.Composition;
using Barbatos.Wpf.AquariusRouter.Sample.Views.Dashboard;
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

        // A factory registration, not a plain `new Router(AppRoutes.Records)`: AppRoutes.Build
        // needs the already-DI-resolved AuthState singleton so the /admin route's per-route
        // BeforeEnter guard can close over it (see AppRoutes.cs).
        builder.Services.AddSingleton(sp => new Router(AppRoutes.Build(sp.GetRequiredService<AuthState>())));

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

        // The actual Aquarius + AquariusRouter integration: aq:Setup.Enable resolves every routed
        // View's own ViewModel through this same container, so LoginViewModel/DashboardViewModel
        // above get their constructor-injected AuthState/Router exactly like MainViewModel does -
        // and Router.Push's own entering-View construction (Setup.ServiceProvider-first-else-
        // Activator.CreateInstance) resolves the Views themselves through it too.
        Setup.ServiceProvider = app.Services;

        // The ambient default AquariusRouter.Xaml.RouterView/RouterLink fall back to when neither
        // sets an explicit Router of its own - this is what lets MainWindow.xaml's <aqr:RouterView/>
        // and every aqr:RouterLink.To in its nav bar work with zero further wiring.
        Router.Current = app.Services.GetRequiredService<Router>();

        return app;
    }
}
