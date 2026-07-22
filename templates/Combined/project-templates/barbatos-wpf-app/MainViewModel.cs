using Barbatos.Wpf.Aquarius.Reactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BarbatosWpfApp;

// Constructed through Barbatos.Wpf.Core's DI container - MainWindow.xaml's
// aq:Setup.Enable="True" resolves this via Setup.ServiceProvider (see WpfProgram.cs), not
// Activator.CreateInstance, so a constructor dependency on any other registered Core
// service would just work.
public sealed partial class MainViewModel : ObservableObject
{
    public Ref<int> Count { get; } = new(0);

    [ObservableProperty]
    private string _greeting = "Hello from Barbatos.Wpf - Aquarius + Core, wired together!";

    [RelayCommand]
    private void Increment() => Count.Value++;
}
