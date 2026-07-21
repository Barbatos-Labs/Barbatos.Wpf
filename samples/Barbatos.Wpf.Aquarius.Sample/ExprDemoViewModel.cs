using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// DataContext for <see cref="ExprDemoView"/> - conditional expressions, and If/Else/
/// Else-if, fully self-contained: its own <see cref="Count"/>/<see cref="StatusIndex"/>/
/// <see cref="ShowPanel"/>, not reused from the Reactivity/Directives tabs, so this demo
/// reads standalone instead of requiring a trip to another tab to know what they mean.
/// </summary>
public sealed partial class ExprDemoViewModel : ObservableObject
{
    [ObservableProperty]
    private int _count;

    [ObservableProperty]
    private int _statusIndex;

    [ObservableProperty]
    private bool _showPanel = true;

    [RelayCommand]
    private void Increment() => Count++;

    [RelayCommand]
    private void CycleStatus() => StatusIndex = (StatusIndex + 1) % 3;

    [RelayCommand]
    private void TogglePanel() => ShowPanel = !ShowPanel;
}
