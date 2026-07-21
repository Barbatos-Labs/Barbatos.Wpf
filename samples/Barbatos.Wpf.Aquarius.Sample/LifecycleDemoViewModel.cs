using Barbatos.Wpf.Composition;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// DataContext for <see cref="LifecycleDemoView"/>. Implements the lifecycle hooks it
/// wants (see <see cref="Lifecycle"/>) - a ViewModel implements only the ones it needs,
/// exactly like a Vue component only imports the hooks it uses. Also implements
/// <see cref="IOnMountedAsync"/> to demonstrate loading data on mount paired with
/// <see cref="Barbatos.Wpf.Xaml.Suspense"/> - see <see cref="LifecycleDemoView"/>.
/// </summary>
public sealed partial class LifecycleDemoViewModel : ObservableObject,
    IOnBeforeMount, IOnMounted, IOnUpdated, IOnUnmounted, IOnMountedAsync
{
    private readonly Action<string> _log;

    [ObservableProperty]
    private int _ticks;

    [ObservableProperty]
    private bool _isLoadingDashboard = true;

    [ObservableProperty]
    private string? _dashboardData;

    public LifecycleDemoViewModel(Action<string> log) => _log = log;

    public void OnBeforeMount() => _log("OnBeforeMount - constructed, not in the visual tree yet");

    public void OnMounted() => _log("OnMounted - now in the visual tree");

    // Fires for a PropertyChanged from *any* property on this ViewModel, not just Ticks
    // (OnMountedAsync's IsLoadingDashboard/DashboardData trigger it too) - hence "a
    // property" here rather than naming Ticks specifically.
    public void OnUpdated() => _log($"OnUpdated - batched after a property changed (Ticks={Ticks})");

    public void OnUnmounted() => _log("OnUnmounted - removed from the visual tree");

    /// <summary>
    /// Fires alongside <see cref="OnMounted"/> on every mount (toggle the demo off/on to
    /// see it fire fresh each time, not just once) - <see cref="IsLoadingDashboard"/> is
    /// exactly what <c>Suspense.IsPending</c> binds to in the View, so the fallback shows
    /// for real while this is actually awaiting, no simulation needed beyond the delay.
    /// </summary>
    public async Task OnMountedAsync()
    {
        _log("OnMountedAsync - started (Suspense showing its Fallback)");
        IsLoadingDashboard = true;
        DashboardData = null;

        await Task.Delay(TimeSpan.FromSeconds(1.2));

        DashboardData = $"Fetched at {DateTime.Now:T}";
        IsLoadingDashboard = false;
        _log("OnMountedAsync - finished (Suspense showing the resolved content)");
    }

    [RelayCommand]
    private void Tick() => Ticks++;

    [RelayCommand]
    private void TickTwice()
    {
        // Two synchronous PropertyChanged events, but Lifecycle coalesces them through
        // NextTick into a single OnUpdated call - the same batching Vue performs for
        // multiple synchronous mutations.
        Ticks++;
        Ticks++;
    }
}
