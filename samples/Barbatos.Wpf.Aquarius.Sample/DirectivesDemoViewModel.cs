using Barbatos.Wpf.Reactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// DataContext for <see cref="DirectivesDemoView"/> - Model/Show/Event(+modifiers)/custom
/// (v-focus)/Class/Style/BuildConfiguration.IsDebug, fully self-contained.
/// </summary>
public sealed partial class DirectivesDemoViewModel : ObservableObject
{
    private readonly Action<string> _log;

    /// <summary>Directives.Class: cycles which named Style is merged onto the status indicator.</summary>
    public Computed<string> StatusClass { get; }

    /// <summary>Directives.Style: recomputed into a fresh dictionary every time <see cref="PreviewFontSize"/> changes.</summary>
    public Computed<IDictionary<string, object>> PreviewStyle { get; }

    public Ref<double> PreviewFontSize { get; } = new(16);

    [ObservableProperty]
    private string _name = "Aquarius";

    [ObservableProperty]
    private bool _showPanel = true;

    [ObservableProperty]
    private int _statusIndex;

    public DirectivesDemoViewModel(Action<string> log)
    {
        _log = log;

        StatusClass = Computed<string>.From(() => StatusIndex switch
        {
            0 => "status-normal",
            1 => "status-warning",
            _ => "status-error",
        }, this);

        PreviewStyle = Computed<IDictionary<string, object>>.From(
            () => new Dictionary<string, object> { ["FontSize"] = PreviewFontSize.Value },
            PreviewFontSize);
    }

    [RelayCommand]
    private void BorderClicked(object? parameter) =>
        _log($"Border clicked - CommandParameter is the raised {parameter?.GetType().Name}");

    [RelayCommand]
    private void EnterPressed() =>
        _log("Enter pressed - matched via Directives.Modifiers=\"enter\"");

    [RelayCommand]
    private void ResetCache() =>
        _log("Cache reset - this button only exists in a Debug build");

    [RelayCommand]
    private void CycleStatus() => StatusIndex = (StatusIndex + 1) % 3;
}
