using System.Collections.ObjectModel;
using System.Globalization;
using Barbatos.i18n;
using Barbatos.Wpf.AquariusValee.Sample.Views.CustomRules;
using Barbatos.Wpf.AquariusValee.Sample.Views.Login;
using Barbatos.Wpf.AquariusValee.Sample.Views.ProfileEdit;
using Barbatos.Wpf.AquariusValee.Sample.Views.Showcase;
using Barbatos.Wpf.AquariusValee.Sample.Views.Signup;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.AquariusValee.Sample;

/// <summary>
/// A thin composition root - one property per demo tab, each already-constructed ViewModel
/// bound directly onto its own tab's <c>DataContext</c> in <c>MainWindow.xaml</c>, mirroring
/// every other sample's own <c>MainViewModel</c> in this repo. Also owns the language switcher
/// state (<see cref="SupportedCultures"/>/<see cref="SelectedCulture"/>) - MainWindow reacts to
/// <see cref="SelectedCulture"/> changing by rebuilding each tab's View (see its own remarks).
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly ILocalizationCultureManager _cultureManager;

    public LoginViewModel LoginDemo { get; }
    public SignupViewModel SignupDemo { get; }
    public ProfileEditViewModel ProfileEditDemo { get; }
    public CustomRulesViewModel CustomRulesDemo { get; }
    public ShowcaseViewModel ShowcaseDemo { get; }

    public ObservableCollection<CultureInfo> SupportedCultures { get; }

    [ObservableProperty]
    private CultureInfo _selectedCulture;

    public MainViewModel(
        LoginViewModel loginDemo,
        SignupViewModel signupDemo,
        ProfileEditViewModel profileEditDemo,
        CustomRulesViewModel customRulesDemo,
        ShowcaseViewModel showcaseDemo,
        ILocalizationCultureManager cultureManager)
    {
        LoginDemo = loginDemo;
        SignupDemo = signupDemo;
        ProfileEditDemo = profileEditDemo;
        CustomRulesDemo = customRulesDemo;
        ShowcaseDemo = showcaseDemo;

        _cultureManager = cultureManager;
        SupportedCultures = new ObservableCollection<CultureInfo>(cultureManager.GetSupportedCultures());
        _selectedCulture = cultureManager.GetCulture();
    }

    partial void OnSelectedCultureChanged(CultureInfo value) => _cultureManager.SetCulture(value);
}
