using CommunityToolkit.Mvvm.ComponentModel;

namespace AquariusTemplateNamespace;

// Picked up automatically by AquariusComponentView.xaml's aq:Setup.Enable="True" -
// "AquariusComponentView" -> "AquariusComponentViewModel" by name, the same convention
// this library's own sample app uses throughout. Implement whichever Lifecycle hook
// interfaces (IOnMounted, IOnMountedAsync, ...) this ViewModel needs.
public sealed partial class AquariusComponentViewModel : ObservableObject
{
    [ObservableProperty]
    private string _greeting = "Hello from AquariusComponent!";
}
