using System.Windows;

namespace Barbatos.Wpf.AquariusRouter.Sample;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    protected override async void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // The WPF/C# counterpart of Vue Router's own "memory history needs the initial navigation
        // pushed explicitly" requirement - there is no URL bar to seed CurrentRoute automatically.
        await Router.Current!.Start("/");
    }
}
