#if (IncludeI18n)
using System.Globalization;
using Barbatos.i18n;
#endif
#if (AquariusEffective)
using Barbatos.Wpf.Aquarius.Reactivity;
#endif
#if (IncludeAquariusRouter)
using Barbatos.Wpf.AquariusRouter.Routing;
using Barbatos.Wpf.AquariusRouter.Searching;
#endif
#if (IncludeDialogs)
using Barbatos.Wpf.Dialogs;
#endif
#if (IncludeRunOnStartup)
using Barbatos.Wpf.Startup;
#endif
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BarbatosWpfApp;

#if (AquariusEffective)
// Constructed through Barbatos.Wpf.Core's DI container - MainWindow.xaml's
// aq:Setup.ViewModel="{x:Type local:MainViewModel}" resolves this through the container that
// UseAquarius points Setup at (see WpfProgram.cs), not Activator.CreateInstance, so a
// constructor dependency on any other registered Core service would just work.
#endif
public sealed partial class MainViewModel : ObservableObject
{
#if (IncludeI18n)
    private readonly ILocalizationCultureManager _cultureManager;
#endif
#if (IncludeDialogs)
    private readonly IDialogService _dialogService;
#endif
#if (IncludeAquariusRouter)
    private readonly Router _router;
    private readonly RouteSearchIndex _searchIndex;
#endif
#if (IncludeRunOnStartup)
    private readonly IRunOnStartupService _runOnStartup;
#endif
#if (IncludeI18n || IncludeDialogs || IncludeAquariusRouter || IncludeRunOnStartup)
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
#if ((IncludeI18n || IncludeDialogs) && IncludeAquariusRouter)
        ,
#endif
#if (IncludeAquariusRouter)
        Router router
#endif
#if ((IncludeI18n || IncludeDialogs || IncludeAquariusRouter) && IncludeRunOnStartup)
        ,
#endif
#if (IncludeRunOnStartup)
        IRunOnStartupService runOnStartup
#endif
        )
    {
#if (IncludeI18n)
        _cultureManager = cultureManager;
#endif
#if (IncludeDialogs)
        _dialogService = dialogService;
#endif
#if (IncludeAquariusRouter)
        _router = router;
        _searchIndex = new RouteSearchIndex(router);
#endif
#if (IncludeRunOnStartup)
        _runOnStartup = runOnStartup;
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
#if (IncludeRunOnStartup)

    // Bound to the "Launch when Windows starts" checkbox in MainWindow.xaml, and the only thing
    // in this app that registers it: WpfProgram.cs leaves RunOnStartupOptions.Enabled false so
    // a first run never adds itself to Windows startup unasked. Read straight from the service
    // rather than cached - the registration is a value under the current user's Run registry
    // key, which outlives this process, so the box shows what is registered right now. It does
    // not see the on/off switch in Task Manager's or Settings' Startup apps list: Windows stores
    // that separately and leaves the Run value in place.
    public bool RunOnStartupEnabled
    {
        get => _runOnStartup.IsEnabled;
        set
        {
            _runOnStartup.SetEnabled(value);
            OnPropertyChanged();
        }
    }
#endif
#if (IncludeAquariusRouter)

    // Typed into the search box in MainWindow.xaml. Re-querying on every keystroke is fine:
    // RouteSearchIndex matches over the in-memory route table, with no I/O anywhere.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SearchResults))]
    [NotifyPropertyChangedFor(nameof(HasSearchResults))]
    private string _searchQuery = "";

    // Empty for a blank query - there is no meaningful "match everything" for an empty box.
    public IReadOnlyList<RouteSearchResult> SearchResults => _searchIndex.Query(SearchQuery);

    public bool HasSearchResults => SearchResults.Count > 0;

    [RelayCommand]
    private async Task GoToSearchResult(RouteSearchResult result)
    {
        SearchQuery = "";
        // RouteSearchResult.To always navigates by the record's Name, never its Path, so a route
        // whose path later changes keeps working without touching its search entries. A
        // searchable route with no Name makes To throw InvalidOperationException - every route
        // with Search entries in AppRoutes.cs needs a Name.
        await _router.Push(result.To);
    }
#endif
}
