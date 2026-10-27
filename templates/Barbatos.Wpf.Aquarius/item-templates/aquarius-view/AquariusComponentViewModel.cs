using CommunityToolkit.Mvvm.ComponentModel;

namespace AquariusTemplateNamespace;

// Picked up automatically by AquariusComponentView.xaml's aq:Setup.Enable="True" -
// "AquariusComponentView" -> "AquariusComponentViewModel" by name, the same convention
// this library's own sample app uses throughout. That View also sets aq:Lifecycle.Enable="True",
// so implementing any Lifecycle hook interface here (IOnMounted, IOnMountedAsync, ... from
// Barbatos.Wpf.Aquarius.Composition) is all it takes for the hook to fire. Keep that one
// Lifecycle.Enable per ViewModel: a second one inside the View would fire every hook twice.
public sealed partial class AquariusComponentViewModel : ObservableObject
{
    [ObservableProperty]
    private string _greeting = "Hello from AquariusComponent!";
}
