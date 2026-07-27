using System.Globalization;
using System.Windows;
using Barbatos.i18n.Wpf;
using Barbatos.Wpf.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.AquariusValee.Sample;

/// <summary>
/// Interaction logic for App.xaml - uses Barbatos.Wpf.Core's <see cref="WpfApplication"/>/
/// <see cref="WpfAppBuilder"/> hosting model (see <see cref="WpfProgram"/>) purely for the DI
/// container, matching every other sample in this repo - AquariusValee itself has no dependency
/// on Core.
/// </summary>
public partial class App : WpfApplication
{
    private static readonly CultureInfo[] SupportedStartupCultures = [new("en-US"), new("vi-VN")];

    /// <inheritdoc />
    protected override WpfApp CreateWpfApp() => WpfProgram.CreateWpfApp();

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // LocalizationProvider.GetLocalizationSet does an exact CultureInfo match with no
        // fallback - an unsupported OS UI culture would leave every {i18n:StringLocalizer}
        // binding resolving to its raw key text, so pick en-US unless the OS culture is one we
        // actually ship a Locales/*.json file for.
        var startupCulture = SupportedStartupCultures.FirstOrDefault(c => c.Name == CultureInfo.CurrentUICulture.Name)
            ?? SupportedStartupCultures[0];
        Services.UseWpfLocalization().SetLocalizationCulture(startupCulture);

        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }
}
