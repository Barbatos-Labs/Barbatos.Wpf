#if (IncludeI18n)
using System.Globalization;
using Barbatos.i18n;
#endif
#if (AquariusEffective)
using Barbatos.Wpf.Aquarius.Reactivity;
#endif
#if (IncludeDialogs)
using Barbatos.Wpf.Dialogs;
#endif
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BarbatosWpfApp;

#if (AquariusEffective)
// Constructed through Barbatos.Wpf.Core's DI container - MainWindow.xaml's
// aq:Setup.ViewModel="{x:Type local:MainViewModel}" resolves this via Setup.ServiceProvider
// (see WpfProgram.cs), not Activator.CreateInstance, so a constructor dependency on any
// other registered Core service would just work.
#endif
public sealed partial class MainViewModel : ObservableObject
{
#if (IncludeI18n)
    private readonly ILocalizationCultureManager _cultureManager;
#endif
#if (IncludeDialogs)
    private readonly IDialogService _dialogService;
#endif
#if (IncludeI18n || IncludeDialogs)
    public MainViewModel(
#if (IncludeI18n)
        ILocalizationCultureManager cultureManager
#endif
#if (IncludeI18n && IncludeDialogs)
        ,
#endif
#if (IncludeDialogs)
        IDialogService dialogService
#endif
        )
    {
#if (IncludeI18n)
        _cultureManager = cultureManager;
#endif
#if (IncludeDialogs)
        _dialogService = dialogService;
#endif
    }
#endif

#if (AquariusEffective)
    public Ref<int> Count { get; } = new(0);

#endif
#if (IncludeI18n)
    // Bound as {i18n:StringLocalizer}'s BindArg in MainWindow.xaml. A plain, argument-less
    // {i18n:StringLocalizer Text="..."} only ever evaluates once - Barbatos.i18n has no
    // built-in notification when the current culture changes - so a *live* BindArg (which
    // builds a real MultiBinding under the hood) is what makes the bound text actually
    // refresh: changing this property is what drives that refresh in SwitchLanguage below.
    [ObservableProperty]
    private string _currentCultureName = "en-US";

#endif
#if (AquariusEffective)
    [RelayCommand]
    private void Increment() => Count.Value++;

#endif
#if (IncludeI18n)
    [RelayCommand]
    private void SwitchLanguage(string cultureName)
    {
        _cultureManager.SetCulture(cultureName);
        CurrentCultureName = new CultureInfo(cultureName).Name;
    }
#endif
#if (IncludeDialogs)

    [RelayCommand]
    private void ShowAbout() => _dialogService.Show<AboutWindow>();
#endif
}
