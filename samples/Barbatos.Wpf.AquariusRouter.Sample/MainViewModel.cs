using Barbatos.Wpf.AquariusRouter.Searching;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.AquariusRouter.Sample;

public sealed partial class MainViewModel(AuthState authState, NavigationHistoryLog navigationHistory, Router router) : ObservableObject
{
    private readonly RouteSearchIndex _searchIndex = new(router);

    public AuthState AuthState { get; } = authState;

    public NavigationHistoryLog NavigationHistory { get; } = navigationHistory;

    /// <summary>
    /// Flipped true/false around the awaited delay inside the global <c>BeforeResolve</c> guard
    /// (see <see cref="Routing.AppNavigationGuards.Register"/>) - bound to a small indicator in
    /// <c>MainWindow.xaml</c>'s nav bar, so that guard's async work is actually visible rather than
    /// happening invisibly fast.
    /// </summary>
    [ObservableProperty]
    private bool _isNavigating;

    /// <summary>
    /// The in-app-search demo (see <see cref="Routing.AppRoutes"/>'s own <c>Search</c> entries) -
    /// a plain <c>TextBox</c> + results <c>ListBox</c> in <c>MainWindow.xaml</c>'s nav bar,
    /// deliberately hand-built rather than a library-provided control - see
    /// <see cref="RouteSearchIndex"/>'s own remarks for why AquariusRouter itself stops at the
    /// query engine.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SearchResults))]
    [NotifyPropertyChangedFor(nameof(HasSearchResults))]
    private string _searchQuery = "";

    public IReadOnlyList<RouteSearchResult> SearchResults => _searchIndex.Query(SearchQuery);

    public bool HasSearchResults => SearchResults.Count > 0;

    [RelayCommand]
    private async Task GoToSearchResult(RouteSearchResult result)
    {
        SearchQuery = "";
        await router.Push(result.To);
    }
}
