namespace Barbatos.Wpf.AquariusRouter.Sample.Routing;

/// <summary>
/// Builds a redirect-to-login target that remembers where the user was actually trying to go -
/// shared by every guard placement in this sample that can send someone to <c>/login</c>
/// (<c>DashboardViewModel</c>'s own component-level guard, the global meta-driven <c>BeforeEach</c>,
/// and the <c>/admin</c> route's own per-route <c>BeforeEnter</c> - see <see cref="AppRoutes"/> and
/// <see cref="AppNavigationGuards"/>), so <c>LoginViewModel</c> can send the user back to their
/// real destination afterward instead of always landing on <c>/dashboard</c> regardless of what
/// they actually asked for - the same reason a global-hotkey shortcut into a guarded route (see
/// <see cref="ShortcutRouting"/>) fired while the app was still logged out needs this to actually
/// be observable end to end.
/// </summary>
public static class LoginRedirect
{
    public const string RedirectQueryKey = "redirect";

    /// <summary>A <c>/login</c> target carrying <paramref name="intended"/>'s full path as a <c>redirect</c> query param.</summary>
    public static RouteLocationRaw To(RouteLocation intended) =>
        new() { Path = "/login", Query = new Dictionary<string, string?> { [RedirectQueryKey] = intended.FullPath } };
}
