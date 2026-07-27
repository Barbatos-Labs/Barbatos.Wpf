using System.Globalization;
using Barbatos.i18n.DependencyInjection;
using Barbatos.i18n.Json;
using Barbatos.Wpf.AquariusValee.Sample.Views.CustomRules;
using Barbatos.Wpf.AquariusValee.Sample.Views.Login;
using Barbatos.Wpf.AquariusValee.Sample.Views.ProfileEdit;
using Barbatos.Wpf.AquariusValee.Sample.Views.Showcase;
using Barbatos.Wpf.AquariusValee.Sample.Views.Signup;
using Barbatos.Wpf.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.AquariusValee.Sample;

/// <summary>
/// Composes the <see cref="WpfApp"/> host, mirroring .NET MAUI's <c>MauiProgram</c> pattern (and
/// every other sample's own <c>WpfProgram</c> in this repo).
/// </summary>
public static class WpfProgram
{
    public static WpfApp CreateWpfApp()
    {
        var builder = WpfApp.CreateBuilder();

        builder.Services.AddStringLocalizer(locBuilder =>
        {
            locBuilder.FromJson("Locales.en-US.json", new CultureInfo("en-US"));
            locBuilder.FromJson("Locales.vi-VN.json", new CultureInfo("vi-VN"));
        });

        builder.Services.AddSingleton<UsernameAvailabilityService>();

        // Singleton, not transient - MainWindow's TabControl shell builds all four demo
        // ViewModels once, up front, and keeps them alive for its own lifetime (matching
        // Barbatos.Wpf.Aquarius.Sample's own MainViewModel-composes-every-tab convention),
        // rather than constructing a fresh one per navigation the way AquariusRouter.Sample's
        // routed Views do.
        builder.Services.AddSingleton<LoginViewModel>();
        builder.Services.AddSingleton<SignupViewModel>();
        builder.Services.AddSingleton<ProfileEditViewModel>();
        builder.Services.AddSingleton<CustomRulesViewModel>();
        builder.Services.AddSingleton<ShowcaseViewModel>();

        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        return builder.Build();
    }
}
