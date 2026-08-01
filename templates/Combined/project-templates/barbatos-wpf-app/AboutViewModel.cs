namespace BarbatosWpfApp;

// Never named by AboutView.xaml - AppRoutes.cs is what pairs the two, via RouteRecord.ViewModel.
// Resolved the same way the View itself is (through Setup.ServiceProvider if one is set, else a
// parameterless constructor), so this could take constructor-injected services exactly like
// MainViewModel does.
public sealed class AboutViewModel
{
    public string Note { get; } =
        "This line comes from AboutViewModel, paired to this View by the route table - the View's own XAML names no ViewModel at all.";
}
