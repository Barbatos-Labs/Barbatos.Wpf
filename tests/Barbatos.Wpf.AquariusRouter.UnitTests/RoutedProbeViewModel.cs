namespace Barbatos.Wpf.AquariusRouter.UnitTests;

/// <summary>Resolved by naming convention from <see cref="RoutedProbeView"/>'s own <c>aq:Setup.Enable</c>.</summary>
public sealed class RoutedProbeViewModel : IOnBeforeRouteEnter
{
    public string Greeting => "Hello from a real compiled View";

    public List<string> Calls { get; } = [];

    public Task<NavigationGuardResult> OnBeforeRouteEnter(RouteLocation to, RouteLocation from)
    {
        Calls.Add("OnBeforeRouteEnter");
        return Task.FromResult(NavigationGuardResult.Continue);
    }
}
