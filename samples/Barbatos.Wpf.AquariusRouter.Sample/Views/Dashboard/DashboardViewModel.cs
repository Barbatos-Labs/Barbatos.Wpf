using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.AquariusRouter.Sample.Views.Dashboard;

/// <summary>
/// The sample's guarded-route demo: <see cref="OnBeforeRouteEnter"/> redirects to <c>/login</c>
/// whenever <see cref="AuthState.IsLoggedIn"/> is false, instead of ever letting this View mount.
/// Also exposes <see cref="AuthState.IsAdmin"/> as a toggle, purely so the <c>/admin</c> route's
/// own per-route <c>RouteRecord.BeforeEnter</c> guard (see <see cref="Routing.AppRoutes"/>) has
/// something to actually allow or Cancel against.
/// </summary>
public sealed partial class DashboardViewModel(AuthState authState, Router router) : IOnBeforeRouteEnter
{
    public AuthState AuthState { get; } = authState;

    public Task<NavigationGuardResult> OnBeforeRouteEnter(RouteLocation to, RouteLocation from) =>
        Task.FromResult(AuthState.IsLoggedIn ? NavigationGuardResult.Continue : NavigationGuardResult.Redirect(LoginRedirect.To(to)));

    [RelayCommand]
    private async Task LogOut()
    {
        AuthState.IsLoggedIn = false;
        await router.Push("/");
    }
}
