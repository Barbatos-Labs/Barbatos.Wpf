using System.Windows;
using Barbatos.Wpf.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace BarbatosWpfApp;

public partial class App : WpfApplication
{
    protected override WpfApp CreateWpfApp() => WpfProgram.CreateWpfApp();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Resolved through Core's DI container, same as any other MAUI-style WpfApp.
        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }
}
