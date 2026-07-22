using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// DataContext for <see cref="SuspenseDemoView"/> - the plain, manually-triggered
/// <c>IsPending</c> toggle. See <see cref="LifecycleDemoViewModel"/> for the other
/// Suspense example, driven by a real <see cref="Barbatos.Wpf.Aquarius.Composition.IOnMountedAsync"/>
/// hook instead of a button.
/// </summary>
public sealed partial class SuspenseDemoViewModel : ObservableObject
{
    private readonly Action<string> _log;

    [ObservableProperty]
    private bool _isLoadingDashboard;

    public SuspenseDemoViewModel(Action<string> log) => _log = log;

    [RelayCommand]
    private async Task SimulateLoadAsync()
    {
        IsLoadingDashboard = true;
        _log("IsPending -> true");

        await Task.Delay(TimeSpan.FromSeconds(1.5));

        IsLoadingDashboard = false;
        _log("IsPending -> false");
    }
}
