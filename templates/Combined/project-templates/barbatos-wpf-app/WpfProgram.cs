using Barbatos.Wpf.Aquarius.Composition;
using Barbatos.Wpf.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace BarbatosWpfApp;

/// <summary>
/// Composes the <see cref="WpfApp"/> host, mirroring .NET MAUI's <c>MauiProgram</c> pattern
/// (see Barbatos.Wpf.Core's own README/sample for the full shape this is a minimal slice of).
/// </summary>
public static class WpfProgram
{
    public static WpfApp CreateWpfApp()
    {
        var builder = WpfApp.CreateBuilder();

        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        var app = builder.Build();

        // The actual Aquarius + Core integration: Setup.Enable/Setup.ViewModel (see
        // MainWindow.xaml) resolve a ViewModel through *this* same DI container instead of
        // always Activator.CreateInstance-ing a fresh one - so a ViewModel can take
        // constructor-injected Core services exactly like MainViewModel/MainWindow above do.
        Setup.ServiceProvider = app.Services;

        return app;
    }
}
