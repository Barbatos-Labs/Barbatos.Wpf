using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// The sample window's DataContext - a thin composition root, itself resolved from
/// <see cref="WpfProgram.CreateWpfApp"/>'s dependency injection container. Each feature area
/// gets its own fully self-contained View+ViewModel (<see cref="ReactivityDemoViewModel"/> and
/// friends); this class only owns the one property per demo needed to bind each into its own
/// tab, plus the shared activity log every demo logs into via a small callback passed into its
/// own constructor - it does not hold feature-specific state itself. The one exception is
/// <see cref="UserStore"/>/<see cref="OrderStore"/>/<see cref="ApsuActivityLog"/>
/// (Barbatos.Wpf.Apsu, constructor-injected here since they are genuinely DI-resolved/shared)
/// which are just handed straight to <see cref="ApsuInteropDemoViewModel"/> below, the same as
/// every other child. None of the six Apsu interceptors are ever injected anywhere, nor is any
/// <c>InterceptorHandle</c> - interceptors stay entirely a composition-root concern
/// (<see cref="WpfProgram.CreateWpfApp"/>), invisible all the way down through this class and
/// every child ViewModel; only their log output reaches here, via <see cref="ApsuActivityLog"/>.
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
    public KeepAliveDemoViewModel KeepAliveDemo { get; }
    public ApsuInteropDemoViewModel ApsuInteropDemo { get; }

    /// <summary>Whether the Lifecycle tab's demo child is currently mounted - lives here (the parent), not on <see cref="LifecycleDemoViewModel"/> itself, the same way an If's Condition lives outside the content it shows.</summary>
    [ObservableProperty]
    private bool _showLifecycleDemo;

    public MainViewModel(UserStore userStore, OrderStore orderStore, ApsuActivityLog activityLog, KeepAliveActivityLog keepAliveLog)
    {
        ReactivityDemo = new ReactivityDemoViewModel(m => Log("Reactivity", m));
        LifecycleDemo = new LifecycleDemoViewModel(m => Log("Lifecycle", m));
        DirectivesDemo = new DirectivesDemoViewModel(m => Log("Directives", m));
        TeleportDemo = new TeleportDemoViewModel();
        ProvideInjectDemo = new ProvideInjectDemoViewModel();
        SuspenseDemo = new SuspenseDemoViewModel(m => Log("Suspense", m));
        SlotsDemo = new SlotsDemoViewModel();
        ExprDemo = new ExprDemoViewModel();
        KeepAliveDemo = new KeepAliveDemoViewModel(m => Log("KeepAlive", m));

        // The KeepAlive sections are built by the KeepAlive control itself, from DI, so they
        // report through this shared sink rather than a callback handed to them here.
        keepAliveLog.Logged += m => Log("KeepAlive", m);
        ApsuInteropDemo = new ApsuInteropDemoViewModel(userStore, orderStore, activityLog, m => Log("Apsu+Aquarius", m));
    }

    private void Log(string area, string message) => ActivityLog.Insert(0, $"[{area}] {message}");

    [RelayCommand]
    private void ToggleLifecycleDemo() => ShowLifecycleDemo = !ShowLifecycleDemo;
}
