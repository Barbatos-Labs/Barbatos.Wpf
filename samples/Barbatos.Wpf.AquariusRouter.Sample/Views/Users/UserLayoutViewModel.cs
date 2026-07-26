using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.AquariusRouter.Sample.Views.Users;

/// <summary>
/// The sample's nested-route demo: hosts a nested <c>&lt;aqr:RouterView /&gt;</c> (see
/// UserLayoutView.xaml) for its own <c>""</c> (index/overview) and <c>"settings"</c> children.
/// Reused - never reconstructed - when only <c>:id</c> changes (e.g. switching between users via
/// the nav bar), so it implements the non-cancelling <see cref="IOnRouteEnter"/>/
/// <see cref="IOnRouteUpdate"/> pair rather than just <see cref="IOnRouteEnter"/> alone.
/// </summary>
public sealed partial class UserLayoutViewModel : ObservableObject, IOnRouteEnter, IOnRouteUpdate
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Heading))]
    [NotifyPropertyChangedFor(nameof(OverviewTarget))]
    [NotifyPropertyChangedFor(nameof(SettingsTarget))]
    private string _userId = "";

    public string Heading => $"User {UserId}";

    public RouteLocationRaw OverviewTarget => new() { Name = "user-overview", Params = new Dictionary<string, object?> { ["id"] = UserId } };

    public RouteLocationRaw SettingsTarget => new() { Name = "user-settings", Params = new Dictionary<string, object?> { ["id"] = UserId } };

    public void OnRouteEnter(RouteLocation to, RouteLocation from) => UserId = (string)to.Params["id"]!;

    public void OnRouteUpdate(RouteLocation to, RouteLocation from) => UserId = (string)to.Params["id"]!;
}
