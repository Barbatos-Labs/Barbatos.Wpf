using Barbatos.Wpf.Composition;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// DataContext for <see cref="LifecycleDemoView"/>. Implements the lifecycle hooks it
/// wants (see <see cref="Lifecycle"/>) - a ViewModel implements only the ones it needs,
/// exactly like a Vue component only imports the hooks it uses.
/// </summary>
public sealed partial class LifecycleDemoViewModel : ObservableObject,
    IOnBeforeMount, IOnMounted, IOnUpdated, IOnUnmounted
{
    private readonly Action<string> _log;

    [ObservableProperty]
    private int _ticks;

    public LifecycleDemoViewModel(Action<string> log) => _log = log;

    public void OnBeforeMount() => _log("OnBeforeMount - constructed, not in the visual tree yet");

    public void OnMounted() => _log("OnMounted - now in the visual tree");

    public void OnUpdated() => _log($"OnUpdated - batched after Ticks changed (Ticks={Ticks})");

    public void OnUnmounted() => _log("OnUnmounted - removed from the visual tree");

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
