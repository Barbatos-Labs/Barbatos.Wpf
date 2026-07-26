using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.AquariusRouter.Sample.Views.About;

public sealed partial class AboutViewModel : ObservableObject, IOnRouteEnter
{
    [ObservableProperty]
    private string _currentPath = "";

    public void OnRouteEnter(RouteLocation to, RouteLocation from) => CurrentPath = $"Reached via: {to.Path}";
}
