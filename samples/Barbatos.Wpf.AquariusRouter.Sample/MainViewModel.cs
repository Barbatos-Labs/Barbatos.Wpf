using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.AquariusRouter.Sample;

public sealed partial class MainViewModel(AuthState authState, NavigationHistoryLog navigationHistory) : ObservableObject
{
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
}
