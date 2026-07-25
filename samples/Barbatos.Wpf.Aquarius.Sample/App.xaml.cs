using System.Windows;
using Barbatos.Wpf.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// Interaction logic for App.xaml - uses Barbatos.Wpf.Core's <see cref="WpfApplication"/>/
/// <see cref="WpfAppBuilder"/> hosting model (see <see cref="WpfProgram"/>) so this sample can
/// resolve <see cref="UserStore"/>/<see cref="OrderStore"/> (Barbatos.Wpf.Apsu) through
/// dependency injection, the same way <c>Barbatos.Wpf.Core.Sample</c> does.
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