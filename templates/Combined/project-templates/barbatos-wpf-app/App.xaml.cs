using System.Windows;
using Barbatos.Wpf.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace BarbatosWpfApp;

public partial class App : WpfApplication
{
    protected override WpfApp CreateWpfApp() => WpfProgram.CreateWpfApp();

#if (IncludeSplashScreen)
    protected override SplashScreenOptions GetSplashScreenOptions() => new()
    {
        Tagline = "Loading your workspace...",
        MinimumDisplayDuration = TimeSpan.FromSeconds(1),
    };

#endif
#if (IncludeSplashScreen)
    protected override async void OnStartup(StartupEventArgs e)
#else
    protected override void OnStartup(StartupEventArgs e)
#endif
    {
        base.OnStartup(e);
#if (IncludeSplashScreen)
        await CloseSplashScreenAsync();

#endif
        // Resolved through Core's DI container, same as any other MAUI-style WpfApp.
        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }
}
