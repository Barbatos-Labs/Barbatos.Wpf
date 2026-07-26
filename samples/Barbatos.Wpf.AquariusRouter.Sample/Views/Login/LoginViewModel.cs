using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.AquariusRouter.Sample.Views.Login;

/// <summary>
/// Reads back whichever <c>redirect</c> query param the guard that sent the user here attached
/// (see <see cref="Routing.LoginRedirect"/>), so <see cref="LogIn"/> returns to the route that was
/// actually requested - a plain top-level <c>/dashboard</c> guard redirect, a nested
/// <c>/settings/...</c> one, or a global-hotkey deep link fired while unfocused (see
/// <see cref="Routing.ShortcutRouting"/>) - instead of always landing on <c>/dashboard</c>
/// regardless of what was originally asked for. Implements both <see cref="IOnRouteEnter"/> and
/// <see cref="IOnRouteUpdate"/> because navigating from one guard's redirect straight to
/// another's (e.g. clicking "Admin" while already sitting on <c>/login</c> from an earlier
/// redirect) only changes this record's query, not which <c>RouteRecord</c> matches - Router
/// treats that as an update, not a fresh leave+enter.
/// </summary>
public sealed partial class LoginViewModel(AuthState authState, Router router) : IOnRouteEnter, IOnRouteUpdate
{
    private string _redirectTarget = "/dashboard";

    public void OnRouteEnter(RouteLocation to, RouteLocation from) =>
        _redirectTarget = to.Query.TryGetValue(LoginRedirect.RedirectQueryKey, out var redirect) && redirect is not null
            ? redirect
            : "/dashboard";

    public void OnRouteUpdate(RouteLocation to, RouteLocation from) => OnRouteEnter(to, from);

    [RelayCommand]
    private async Task LogIn()
    {
        authState.IsLoggedIn = true;
        await router.Push(_redirectTarget);
    }
}
