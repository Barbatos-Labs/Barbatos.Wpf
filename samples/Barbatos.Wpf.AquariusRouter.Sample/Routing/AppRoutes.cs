using Barbatos.Wpf.AquariusRouter.Sample.Views.About;
using Barbatos.Wpf.AquariusRouter.Sample.Views.Admin;
using Barbatos.Wpf.AquariusRouter.Sample.Views.CrossAssembly;
using Barbatos.Wpf.AquariusRouter.Sample.Views.Dashboard;
using Barbatos.Wpf.AquariusRouter.Sample.Views.Dialog;
using Barbatos.Wpf.AquariusRouter.Sample.Views.Home;
using Barbatos.Wpf.AquariusRouter.Sample.Views.Login;
using Barbatos.Wpf.AquariusRouter.Sample.Views.NotFound;
using Barbatos.Wpf.AquariusRouter.Sample.Views.Preferences;
using Barbatos.Wpf.AquariusRouter.Sample.Views.Settings;
using Barbatos.Wpf.AquariusRouter.Sample.Views.Users;
using Barbatos.Wpf.AquariusRouter.Searching;
using Barbatos.Wpf.Samples.Shared;
using Barbatos.Wpf.Samples.Shared.CrossAssembly;

namespace Barbatos.Wpf.AquariusRouter.Sample.Routing;

/// <summary>
/// The sample's whole route table, in one place - the WPF/C# counterpart of a Vue app's
/// <c>router/index.ts</c>. Deliberately demonstrates every guard-placement pattern this library
/// offers, each with a distinct, realistic use case - see <see cref="AppNavigationGuards"/> for
/// the *global* guards (registered separately, since they apply across the whole table rather
/// than belonging to any one record).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><b>Component-level guard</b> - <c>/dashboard</c>'s own
/// <c>DashboardViewModel</c> implements <c>IOnBeforeRouteEnter</c> directly.</description></item>
/// <item><description><b>Global, meta-driven guard</b> - <c>/preferences</c> just declares
/// <c>Meta["requiresAuth"] = true</c>; <c>PreferencesViewModel</c> itself knows nothing about
/// auth at all. See <see cref="AppNavigationGuards.Register"/>.</description></item>
/// <item><description><b>Per-route guard</b> - <c>/admin</c>'s <see cref="RouteRecord.BeforeEnter"/>
/// lives on the route record itself, entirely outside <c>AdminViewModel</c>.</description></item>
/// <item><description><b>In-component leave/update guard</b> - <c>UserSettingsViewModel</c>
/// implements both <c>IOnBeforeRouteLeave</c> and <c>IOnBeforeRouteUpdate</c> (unsaved-changes
/// confirmation applies whether you navigate away entirely or just switch to a different user's
/// settings).</description></item>
/// </list>
/// <c>/settings</c> and its children are this sample's "reachable from everywhere, several levels
/// deep" demo: <c>Meta["requiresAuth"] = true</c> declared once on the parent is inherited by
/// every child (<c>Matching.RouteMatcher</c> merges <c>Meta</c> root-to-leaf on every resolve - see
/// its own remarks), the same global <c>BeforeEach</c> from <see cref="AppNavigationGuards.Register"/>
/// protects the whole subtree with zero extra wiring per child. It has three separate entry
/// points converging on it - the main nav bar (lands on the empty-path "profile" child), a link on
/// <c>DashboardView</c> (straight to "notifications"), and a link on <c>PreferencesView</c>
/// (straight to "security") - plus a fourth: <see cref="ShortcutRouting"/> deep-links a keyboard
/// shortcut directly into all three leaves - "security" and "notifications" by their literal
/// path, "profile" by name (it has none - see <see cref="ShortcutRouting"/>'s own remarks) - one
/// of them (Ctrl+F8, into "security") even while this window is unfocused/minimized via a real
/// OS-level global hotkey, proving the whole ancestor chain (shell + leaf) constructs correctly
/// regardless of how the navigation was triggered.
/// <c>/settings</c> also carries a second Meta flag, <c>requiresPremium</c>, the same
/// inherited-via-Meta mechanism applied to a second, independent concern (subscription tier
/// instead of authentication) - the same global <c>BeforeEach</c> checks both. This is what
/// <c>MainWindow.xaml</c>'s nav bar and the two secondary entry points above reflect: every
/// <c>RouterLink</c> is hidden while logged out, and the ones into this subtree specifically stay
/// visible-but-disabled once logged in on the free plan - see <c>Services.LoggedInVisibilityConverter</c>/
/// <c>Services.PremiumAccessConverter</c> and <see cref="AuthState.IsPremium"/>.
/// <c>settings-security</c> also carries two <see cref="RouteSearchEntry"/> entries rather than
/// one - the in-app-search synonym demo: searching either "security" or the fully-Vietnamese
/// "đổi mật khẩu" (or its unaccented spelling, "doi mat khau" - see <see cref="RouteSearchIndex"/>'s
/// own remarks on diacritic-insensitive matching) finds the same route. Try the search box in
/// <c>MainWindow.xaml</c>'s nav bar.
/// <c>/dashboard</c>'s two children are the <b>modal-route</b> demo: routes that render into a
/// <see cref="RouteRecord.Window"/> rather than into the main window's outlet, opened and closed
/// by <c>RouterWindows</c> straight from the current route. Note that the Inspector is reachable
/// from the search box as well as the nav bar, with nothing extra written for either - the point
/// of declaring the window on the record instead of on the link.
/// </remarks>
public static class AppRoutes
{
    public static IReadOnlyList<RouteRecord> Build(AuthState authState) =>
    [
        new RouteRecord { Path = "/", Name = "home", View = typeof(HomeView) },

        new RouteRecord
        {
            Path = "/about", Name = "about", View = typeof(AboutView), Alias = ["/aboutus"],
            Search = [new RouteSearchEntry { Title = "About" }],
        },

        // InfoCardView is defined once in Barbatos.Wpf.Samples.Shared and reused, unmodified,
        // from Barbatos.Wpf.Aquarius.Sample too (see its own MainWindow.xaml's "Shared" tab) -
        // deliberately self-sufficient (aq:Setup.Enable resolves its own ViewModel) rather than
        // relying on a parent to construct/bind a DataContext, which is what makes it safe to
        // drop into either sample's very different composition style. Router only ever
        // constructs the View type itself for a route (see Router.cs's own remarks) - a routed
        // View always needs to resolve its own DataContext this way, never Router's.
        new RouteRecord
        {
            Path = "/shared-demo", Name = "shared-demo", View = typeof(InfoCardView),
            Search = [new RouteSearchEntry { Title = "Shared component demo" }],
        },

        // The "neither file may be edited" demo. AccountSummaryView carries no aq:Setup wiring at
        // all and names no ViewModel; AccountSummaryPresenter is in ANOTHER assembly
        // (Barbatos.Wpf.Samples.Shared), under another namespace, named "...Presenter" rather than
        // "...ViewModel" so Setup's naming convention could never guess it, and it names no View
        // either. Both stand in for types shipped inside libraries under strict change control.
        // RouteRecord.ViewModel pairs them from here - the composition root, the one place the
        // application author actually owns - and everything downstream (route params,
        // IOnRouteEnter, every guard interface) behaves exactly as if the View had wired itself up.
        new RouteRecord
        {
            Path = "/accounts/:id", Name = "account-summary",
            View = typeof(AccountSummaryView),
            ViewModel = typeof(AccountSummaryPresenter),
            Search = [new RouteSearchEntry { Title = "Account summary", Keywords = "cross assembly viewmodel" }],
        },

        // A static redirect - resolved before any guard runs; "/old-home" itself is never
        // rendered and has no guards of its own to run in the first place.
        new RouteRecord { Path = "/old-home", Redirect = _ => "/" },

        new RouteRecord { Path = "/login", Name = "login", View = typeof(LoginView) },

        new RouteRecord
        {
            Path = "/dashboard",
            Name = "dashboard",
            Views = new Dictionary<string, Type>
            {
                ["default"] = typeof(DashboardView),
                ["sidebar"] = typeof(DashboardSidebarView),
            },
            Search = [new RouteSearchEntry { Title = "Dashboard" }],

            // The modal-route demo (see RouterWindows): two children that render into a Window
            // instead of into the main window's outlet. Everything about them is ordinary - a
            // path, a name, a View, in-app search - except the one extra line saying WHERE they
            // render. Because that lives on the record, every way of getting here works with no
            // extra wiring at all: the nav bar's plain RouterLink.To, the search box below it,
            // router.Push from a ViewModel, a keyboard shortcut, a deep link.
            //
            // One router, one history. /dashboard stays matched and keeps showing behind the
            // dialog, Back closes it, and closing it with [X] navigates back to /dashboard. The
            // window goes once that navigation commits (here after the global BeforeResolve's
            // short delay), and stays if a guard refuses it - the route and what is on screen
            // stay in step.
            //
            // Two siblings rather than a parent and a child: they share one Window type, so
            // stepping between them keeps the same window and only swaps its outlet - a dialog
            // with its own internal navigation. Nest them instead and the dialog would need a
            // nested RouterView of its own, exactly like /settings does in the main window.
            Children =
            [
                new RouteRecord
                {
                    Path = "inspector",
                    Name = "inspector",
                    View = typeof(InspectorOverviewView),
                    Window = typeof(InspectorWindow),
                    Search = [new RouteSearchEntry { Title = "Inspector", Keywords = "dialog window modal" }],
                },
                new RouteRecord
                {
                    Path = "inspector/raw",
                    Name = "inspector-raw",
                    View = typeof(InspectorRawView),
                    Window = typeof(InspectorWindow),
                },
            ],
        },

        // Guarded globally, via Meta - PreferencesViewModel has no guard interface of its own at
        // all. See AppNavigationGuards.Register's BeforeEach.
        new RouteRecord
        {
            Path = "/preferences",
            Name = "preferences",
            View = typeof(PreferencesView),
            Meta = new Dictionary<string, object?> { ["requiresAuth"] = true },
            Search = [new RouteSearchEntry { Title = "Preferences" }],
        },

        // Guarded per-route - AdminViewModel has no guard interface of its own either; the check
        // lives entirely on this record.
        new RouteRecord
        {
            Path = "/admin",
            Name = "admin",
            View = typeof(AdminView),
            BeforeEnter = (to, _) =>
            {
                if (!authState.IsLoggedIn)
                    return Task.FromResult(NavigationGuardResult.Redirect(LoginRedirect.To(to)));

                // Logged in but not an admin - Cancel (stay put) rather than redirect anywhere;
                // watch the navigation history panel to see this show up as "Aborted".
                return Task.FromResult<NavigationGuardResult>(authState.IsAdmin);
            },
        },

        new RouteRecord
        {
            Path = "/users/:id",
            Name = "user",
            View = typeof(UserLayoutView),
            Children =
            [
                new RouteRecord { Path = "", Name = "user-overview", View = typeof(UserOverviewView) },
                new RouteRecord { Path = "settings", Name = "user-settings", View = typeof(UserSettingsView) },
            ],
        },

        // Guarded globally via inherited Meta (see this type's own remarks) - reachable from
        // several different RouterLinks AND from a keyboard shortcut alike.
        new RouteRecord
        {
            Path = "/settings",
            Name = "settings",
            View = typeof(SettingsShellView),
            Meta = new Dictionary<string, object?> { ["requiresAuth"] = true, ["requiresPremium"] = true },
            Children =
            [
                new RouteRecord { Path = "", Name = "settings-profile", View = typeof(SettingsProfileView) },
                new RouteRecord
                {
                    Path = "notifications", Name = "settings-notifications", View = typeof(SettingsNotificationsView),
                    Search = [new RouteSearchEntry { Title = "Notification settings", Keywords = "alerts" }],
                },
                // Two entries, deliberately - the in-app-search synonym demo (see this type's own
                // remarks): "security" and the fully-Vietnamese "đổi mật khẩu" (or its unaccented
                // "doi mat khau" spelling) both find this same route.
                new RouteRecord
                {
                    Path = "security", Name = "settings-security", View = typeof(SettingsSecurityView),
                    Search =
                    [
                        new RouteSearchEntry { Title = "Security settings" },
                        new RouteSearchEntry { Title = "Đổi mật khẩu" },
                    ],
                },
            ],
        },

        // Catch-all - kept last for readability, though registration order doesn't actually
        // matter: RouteMatcher always ranks a wildcard/optional-repeatable path last regardless
        // of where it was added (see Matching.PathParser's scoring).
        new RouteRecord { Path = "/:pathMatch(.*)*", Name = "not-found", View = typeof(NotFoundView) },
    ];
}
