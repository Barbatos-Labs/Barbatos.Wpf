using Barbatos.Wpf.AquariusRouter.Routing;
using Barbatos.Wpf.AquariusRouter.Searching;

namespace BarbatosWpfApp;

public static class AppRoutes
{
    public static IReadOnlyList<RouteRecord> Build() =>
    [
        new RouteRecord
        {
            Path = "/", Name = "home", View = typeof(HomeView),
            // Search is opt-in: a record without it simply never appears in the search box.
            Search = [new RouteSearchEntry { Title = "Home", Keywords = "start trang chủ" }],
        },
        new RouteRecord
        {
            Path = "/about", Name = "about", View = typeof(AboutView),
            // Pairs the View with its ViewModel from the route table rather than from the View's
            // own XAML - the option to reach for when View and ViewModel ship in libraries you
            // don't own and can't edit. HomeView above takes the other road: its own
            // aq:Setup.Enable, which finds "HomeViewModel" by naming convention.
            ViewModel = typeof(AboutViewModel),
            Search = [new RouteSearchEntry { Title = "About", Keywords = "version credits giới thiệu" }],
        },
#if (IncludeValee)
        new RouteRecord
        {
            Path = "/signup", Name = "signup", View = typeof(SignUpView),
            Search =
            [
                new RouteSearchEntry { Title = "Sign up" },
                // A second entry, not a second route: one feature stays findable under whichever
                // phrasing a user actually types.
                new RouteSearchEntry { Title = "Create an account", Keywords = "register đăng ký" },
            ],
        },
#endif
    ];
}
