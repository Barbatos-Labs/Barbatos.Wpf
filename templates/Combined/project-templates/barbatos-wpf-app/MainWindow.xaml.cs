using System.Windows;
#if (IncludeAquariusRouter)
using Barbatos.Wpf.AquariusRouter.Routing;
#endif

namespace BarbatosWpfApp;

public partial class MainWindow : Window
{
    public MainWindow(
#if (!AquariusEffective)
        MainViewModel viewModel
#endif
        )
    {
        InitializeComponent();
#if (!AquariusEffective)
        DataContext = viewModel;
#endif
    }
#if (IncludeAquariusRouter)

    protected override async void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Memory-history routers have no URL bar to seed CurrentRoute automatically - the
        // initial navigation has to be pushed explicitly, once, at startup.
        await Router.Current!.Start("/");
    }
#endif
}
