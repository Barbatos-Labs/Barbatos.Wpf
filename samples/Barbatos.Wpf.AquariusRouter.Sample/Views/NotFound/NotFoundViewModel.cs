using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.AquariusRouter.Sample.Views.NotFound;

public sealed partial class NotFoundViewModel : ObservableObject, IOnRouteEnter
{
    [ObservableProperty]
    private string _requestedPath = "";

    public void OnRouteEnter(RouteLocation to, RouteLocation from) => RequestedPath = $"Requested: {to.Path}";
}
