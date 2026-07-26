using System.Windows;
using Barbatos.Wpf.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.AquariusRouter.Sample;

/// <summary>
/// Interaction logic for App.xaml - uses Barbatos.Wpf.Core's <see cref="WpfApplication"/>/
/// <see cref="WpfAppBuilder"/> hosting model (see <see cref="WpfProgram"/>) purely for the DI
/// container - AquariusRouter itself has no dependency on Core.
/// </summary>
public partial class App : WpfApplication
{
    /// <inheritdoc />
    protected override WpfApp CreateWpfApp() => WpfProgram.CreateWpfApp();

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }
}
