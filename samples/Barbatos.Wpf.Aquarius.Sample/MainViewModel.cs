using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// The sample window's DataContext - a thin composition root. Each feature area gets its
/// own fully self-contained View+ViewModel (<see cref="ReactivityDemoViewModel"/> and
/// friends); this class only owns the one property per demo needed to bind each into its
/// own tab, plus the shared activity log every demo logs into via a small callback passed
/// into its own constructor - it does not hold feature-specific state itself.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    /// <summary>Shared across every demo tab below - each one is handed a callback into this rather than reaching for it directly.</summary>
    public ObservableCollection<string> ActivityLog { get; } = new();

    public ReactivityDemoViewModel ReactivityDemo { get; }
    public LifecycleDemoViewModel LifecycleDemo { get; }
    public DirectivesDemoViewModel DirectivesDemo { get; }
    public TeleportDemoViewModel TeleportDemo { get; }
    public ProvideInjectDemoViewModel ProvideInjectDemo { get; }
    public SuspenseDemoViewModel SuspenseDemo { get; }
    public SlotsDemoViewModel SlotsDemo { get; }
    public ExprDemoViewModel ExprDemo { get; }

    /// <summary>Whether the Lifecycle tab's demo child is currently mounted - lives here (the parent), not on <see cref="LifecycleDemoViewModel"/> itself, the same way an If's Condition lives outside the content it shows.</summary>
    [ObservableProperty]
    private bool _showLifecycleDemo;

    public MainViewModel()
    {
        ReactivityDemo = new ReactivityDemoViewModel(m => Log("Reactivity", m));
        LifecycleDemo = new LifecycleDemoViewModel(m => Log("Lifecycle", m));
        DirectivesDemo = new DirectivesDemoViewModel(m => Log("Directives", m));
        TeleportDemo = new TeleportDemoViewModel();
        ProvideInjectDemo = new ProvideInjectDemoViewModel();
        SuspenseDemo = new SuspenseDemoViewModel(m => Log("Suspense", m));
        SlotsDemo = new SlotsDemoViewModel();
        ExprDemo = new ExprDemoViewModel();
    }

    private void Log(string area, string message) => ActivityLog.Insert(0, $"[{area}] {message}");

    [RelayCommand]
    private void ToggleLifecycleDemo() => ShowLifecycleDemo = !ShowLifecycleDemo;
}
