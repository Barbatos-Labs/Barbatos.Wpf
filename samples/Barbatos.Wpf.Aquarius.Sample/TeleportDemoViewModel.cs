using Barbatos.Wpf.Reactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// DataContext for <see cref="TeleportDemoView"/> - the toast (escapes nested layout/
/// clipping) and dockable-panel (the same primitive, extended) demos, fully self-contained.
/// </summary>
public sealed partial class TeleportDemoViewModel : ObservableObject
{
    /// <summary>
    /// Dockable panel demo: "MainDock" (docked, the default) or "FloatingDock" (undocked
    /// into a separate dialog Window - see TeleportDemoView.xaml.cs, which owns opening/
    /// closing that Window in reaction to this value).
    /// </summary>
    public Ref<string> DockTarget { get; } = new("MainDock");

    [ObservableProperty]
    private bool _showToast;

    [ObservableProperty]
    private string _dockableNotes = "Type here, then Undock/Redock - or just close the floating dialog with its own [X] and watch it come home on its own.";

    [RelayCommand]
    private void ToggleToast() => ShowToast = !ShowToast;

    [RelayCommand]
    private void Undock() => DockTarget.Value = "FloatingDock";

    [RelayCommand]
    private void Redock() => DockTarget.Value = "MainDock";
}
