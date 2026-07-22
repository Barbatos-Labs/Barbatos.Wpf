using System.Globalization;
using Barbatos.i18n;
using Barbatos.i18n.DependencyInjection;
using Barbatos.i18n.Wpf;
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

        // Barbatos.i18n - in-memory translations for this starter, so there's no embedded
        // resource file to ship; swap AddLocalization for FromJson/FromCsv/FromIni (separate
        // NuGet packages) once you have real translation files - see Barbatos.i18n's own
        // README for those.
        builder.Services.AddStringLocalizer(localization =>
        {
            localization.AddLocalization(new CultureInfo("en-US"), new Dictionary<LocalizationKey, string?>
            {
                ["Greeting"] = "Hello from Barbatos.Wpf - Aquarius + Core + i18n, wired together! (current language: {0})",
            }!);
            localization.AddLocalization(new CultureInfo("vi-VN"), new Dictionary<LocalizationKey, string?>
            {
                ["Greeting"] = "Xin chào từ Barbatos.Wpf - Aquarius + Core + i18n, kết hợp cùng nhau! (ngôn ngữ hiện tại: {0})",
            }!);
        });

        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        var app = builder.Build();

        // The actual Aquarius + Core integration: Setup.ViewModel (see MainWindow.xaml)
        // resolves a ViewModel through *this* same DI container instead of always
        // Activator.CreateInstance-ing a fresh one - so a ViewModel can take
        // constructor-injected Core services exactly like MainViewModel/MainWindow above do.
        Setup.ServiceProvider = app.Services;

        // Bridges the DI-built localization container into Barbatos.i18n.Wpf's markup
        // extensions - {i18n:StringLocalizer} in MainWindow.xaml can't take constructor
        // injection (the XAML parser only ever calls its parameterless constructor), so this
        // static bridge is how it finds the container registered above - and sets the
        // starting culture (English, regardless of the OS's own UI culture, so this starter
        // always begins in a known state; switch with the buttons in MainWindow).
        app.Services.UseWpfLocalization().SetLocalizationCulture(new CultureInfo("en-US"));

        return app;
    }
}
