using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.AquariusRouter.Sample.Views.Users;

public sealed partial class UserOverviewViewModel : ObservableObject, IOnRouteEnter, IOnRouteUpdate
{
    [ObservableProperty]
    private string _userId = "";

    public void OnRouteEnter(RouteLocation to, RouteLocation from) => UserId = (string)to.Params["id"]!;

    public void OnRouteUpdate(RouteLocation to, RouteLocation from) => UserId = (string)to.Params["id"]!;
}
