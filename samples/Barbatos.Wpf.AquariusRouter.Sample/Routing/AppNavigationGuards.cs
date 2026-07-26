namespace Barbatos.Wpf.AquariusRouter.Sample.Routing;

/// <summary>
/// Every *global* navigation guard/hook this sample registers, in one place - the counterpart to
/// <see cref="AppRoutes"/> holding the route table itself. Kept separate from route-table
/// construction because these apply across the whole table, not to any one record.
/// </summary>
public static class AppNavigationGuards
{
    public static void Register(Router router, AuthState authState, NavigationHistoryLog history, MainViewModel mainViewModel)
    {
        // Global BeforeEach - the canonical Vue Router idiom: a route opts into protection by
        // declaring RouteRecord.Meta["requiresAuth"] = true (see "/preferences" in AppRoutes),
        // instead of needing its own ViewModel to know anything about auth at all. Contrast with
        // DashboardViewModel's own IOnBeforeRouteEnter, which guards itself directly - both are
        // valid, this sample deliberately shows both placements side by side. The same guard also
        // checks a second, independent Meta flag - requiresPremium (see "/settings") - proving one
        // global BeforeEach can enforce more than one concern without needing a second guard.
        router.BeforeEach((to, _) =>
        {
            var requiresAuth = to.Meta.TryGetValue("requiresAuth", out var authValue) && authValue is true;
            if (requiresAuth && !authState.IsLoggedIn)
                return Task.FromResult(NavigationGuardResult.Redirect(LoginRedirect.To(to)));

            // Requires being logged in already (checked above) - a not-yet-identified user can't
            // meaningfully fail a plan check, so this only ever fires for someone genuinely signed
            // in on the free plan. Cancel (stay put) rather than redirect anywhere - same
            // "insufficient privilege" precedent as the /admin route's own IsAdmin check.
            var requiresPremium = to.Meta.TryGetValue("requiresPremium", out var premiumValue) && premiumValue is true;
            if (requiresPremium && !authState.IsPremium)
                return Task.FromResult<NavigationGuardResult>(false);

            return Task.FromResult<NavigationGuardResult>(true);
        });

        // Global BeforeResolve - runs last, only once every other guard has already allowed the
        // navigation. A real app might do a final async data-prefetch here; this simulates one
        // with a short, genuinely awaited delay so it's actually visible (see MainViewModel's
        // IsNavigating, bound to a small indicator in MainWindow's nav bar) - also proves guards
        // can be truly async, not just synchronous Task.FromResult wrappers.
        router.BeforeResolve(async (_, _) =>
        {
            mainViewModel.IsNavigating = true;
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(80));
                return true;
            }
            finally
            {
                mainViewModel.IsNavigating = false;
            }
        });

        // Global AfterEach - a pure observer, fires for every outcome (including a cancelled/
        // aborted/duplicated navigation) except an in-progress redirect. Logging every attempt,
        // not just successful ones, is exactly what makes the Admin route's Cancel-if-not-admin
        // guard (see AppRoutes) visible as an "Aborted" entry in the history panel.
        router.AfterEach(history.Record);
    }
}
