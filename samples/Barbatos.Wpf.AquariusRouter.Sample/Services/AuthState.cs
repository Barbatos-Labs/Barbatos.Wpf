using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.AquariusRouter.Sample.Services;

/// <summary>
/// A deliberately tiny stand-in for a real auth service - just enough state for this sample's
/// guard-placement demos to have something meaningful to check: <see cref="IsLoggedIn"/> for the
/// Dashboard route's own <c>IOnBeforeRouteEnter</c> (a component-level guard) and the Preferences/
/// Settings routes' global, meta-driven <c>BeforeEach</c> guard; <see cref="IsAdmin"/> for the
/// Admin route's own <c>RouteRecord.BeforeEnter</c> (a per-route guard that lives outside any
/// ViewModel entirely); <see cref="IsPremium"/> for the Settings subtree's <c>Meta["requiresPremium"]</c>
/// (same global <c>BeforeEach</c> as <see cref="IsLoggedIn"/>, one more Meta flag) and for
/// <c>MainWindow.xaml</c>'s nav bar - a link into a premium-only route stays visible once logged
/// in, but disabled until <see cref="IsPremium"/> is set, via <c>Services.PremiumAccessConverter</c>.
/// </summary>
public sealed partial class AuthState : ObservableObject
{
    [ObservableProperty]
    private bool _isLoggedIn;

    [ObservableProperty]
    private bool _isAdmin;

    [ObservableProperty]
    private bool _isPremium;

    /// <summary>
    /// Bound to <c>MainWindow.xaml</c>'s "Log Out" button - the counterpart of the "Log In"
    /// RouterLink, so the nav bar's logged-out state (see <c>LoggedOutVisibilityConverter</c>) is
    /// actually reachable again without restarting the app.
    /// </summary>
    [RelayCommand]
    private void LogOut() => IsLoggedIn = false;
}
