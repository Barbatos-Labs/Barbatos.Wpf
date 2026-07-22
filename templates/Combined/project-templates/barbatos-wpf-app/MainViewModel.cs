using System.Globalization;
using Barbatos.i18n;
using Barbatos.Wpf.Aquarius.Reactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BarbatosWpfApp;

// Constructed through Barbatos.Wpf.Core's DI container - MainWindow.xaml's
// aq:Setup.Enable="True" resolves this via Setup.ServiceProvider (see WpfProgram.cs), not
// Activator.CreateInstance, so a constructor dependency on any other registered Core
// service would just work.
public sealed partial class MainViewModel : ObservableObject
{
    private readonly ILocalizationCultureManager _cultureManager;

    public MainViewModel(ILocalizationCultureManager cultureManager)
    {
        _cultureManager = cultureManager;
    }

    public Ref<int> Count { get; } = new(0);

    // Bound as {i18n:StringLocalizer}'s BindArg in MainWindow.xaml. A plain, argument-less
    // {i18n:StringLocalizer Text="..."} only ever evaluates once - Barbatos.i18n has no
    // built-in notification when the current culture changes - so a *live* BindArg (which
    // builds a real MultiBinding under the hood) is what makes the bound text actually
    // refresh: changing this property is what drives that refresh in SwitchLanguage below.
    [ObservableProperty]
    private string _currentCultureName = "en-US";

    [RelayCommand]
    private void Increment() => Count.Value++;

    [RelayCommand]
    private void SwitchLanguage(string cultureName)
    {
        _cultureManager.SetCulture(cultureName);
        CurrentCultureName = new CultureInfo(cultureName).Name;
    }
}
