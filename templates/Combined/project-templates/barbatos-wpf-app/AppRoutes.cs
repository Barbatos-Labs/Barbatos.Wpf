using Barbatos.Wpf.AquariusRouter.Routing;

namespace BarbatosWpfApp;

public static class AppRoutes
{
    public static IReadOnlyList<RouteRecord> Build() =>
    [
        new RouteRecord { Path = "/", Name = "home", View = typeof(HomeView) },
        new RouteRecord { Path = "/about", Name = "about", View = typeof(AboutView) },
    ];
}
