using System.Collections.ObjectModel;
using System.Windows.Media;
using Barbatos.Wpf.Reactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// The sample window's DataContext, exercising every Aquarius feature area. Mixes
/// Aquarius's <see cref="Ref{T}"/>/<see cref="Computed{T}"/> sugar with plain
/// CommunityToolkit.Mvvm <c>[ObservableProperty]</c>/<c>[RelayCommand]</c> on purpose -
/// Aquarius is sugar over CommunityToolkit.Mvvm, not a replacement for it, so both styles
/// are expected to coexist freely in the same ViewModel.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private static readonly Brush[] ThemePalette =
    [
        Brushes.MediumPurple, Brushes.SeaGreen, Brushes.IndianRed, Brushes.SteelBlue,
    ];

    /// <summary>Reactivity: a plain reactive value (<c>ref(0)</c>).</summary>
    public Ref<int> Count { get; } = new(0);

    /// <summary>Reactivity: derived from <see cref="Count"/> (<c>computed(() =&gt; count.value * 2)</c>).</summary>
    public Computed<int> Doubled { get; }

    /// <summary>Reactivity: a writable computed (<c>computed({ get, set })</c>) - editing it splits back into <see cref="FirstName"/>/<see cref="LastName"/>.</summary>
    public Computed<string> FullName { get; }

    /// <summary>Composition: provided near the window root, read anywhere below via <c>{aq:Inject}</c>.</summary>
    public Ref<Brush> ThemeColor { get; } = new(ThemePalette[0]);

    /// <summary>Shared activity log for the Reactivity/Lifecycle/Directives sections below.</summary>
    public ObservableCollection<string> ActivityLog { get; } = new();

    /// <summary>Slots: fetched once here, styled entirely by the consumer's ItemTemplate - see MainWindow.xaml.</summary>
    public ObservableCollection<Post> Posts { get; } =
    [
        new Post("ada", "Reactivity is just ObservableObject with a nicer name.", 42),
        new Post("grace", "Teleport is basically an Adorner you don't have to write yourself.", 17),
        new Post("linus", "If you think you need KeepAlive, you probably just need a TabControl.", 99),
    ];

    public LifecycleDemoViewModel LifecycleDemo { get; }

    /// <summary>
    /// Dockable panel demo (built on Teleport): "MainDock" (docked, the default) or
    /// "FloatingDock" (undocked into a separate dialog Window - see MainWindow.xaml.cs,
    /// which owns opening/closing that Window in reaction to this value).
    /// </summary>
    public Ref<string> DockTarget { get; } = new("MainDock");

    /// <summary>Directives.Class: cycles which named Style is merged onto the status indicator below.</summary>
    public Computed<string> StatusClass { get; }

    /// <summary>Directives.Style: recomputed into a fresh dictionary every time PreviewFontSize changes.</summary>
    public Computed<IDictionary<string, object>> PreviewStyle { get; }

    public Ref<double> PreviewFontSize { get; } = new(16);

    [ObservableProperty]
    private string _name = "Aquarius";

    [ObservableProperty]
    private string _firstName = "Ada";

    [ObservableProperty]
    private string _lastName = "Lovelace";

    [ObservableProperty]
    private bool _showPanel = true;

    [ObservableProperty]
    private bool _showLifecycleDemo;

    [ObservableProperty]
    private bool _showToast;

    [ObservableProperty]
    private int _statusIndex;

    [ObservableProperty]
    private bool _isLoadingDashboard;

    [ObservableProperty]
    private string _dockableNotes = "Type here, then Undock/Redock - or just close the floating dialog with its own [X] and watch it come home on its own.";

    public MainViewModel()
    {
        Doubled = Computed<int>.From(() => Count.Value * 2, Count);

        FullName = Computed<string>.From(
            () => $"{FirstName} {LastName}",
            fullName =>
            {
                var parts = fullName.Split(' ', 2);
                FirstName = parts.Length > 0 ? parts[0] : "";
                LastName = parts.Length > 1 ? parts[1] : "";
            },
            this);

        StatusClass = Computed<string>.From(() => StatusIndex switch
        {
            0 => "status-normal",
            1 => "status-warning",
            _ => "status-error",
        }, this);

        PreviewStyle = Computed<IDictionary<string, object>>.From(
            () => new Dictionary<string, object> { ["FontSize"] = PreviewFontSize.Value },
            PreviewFontSize);

        Watch.On(Count, (newValue, oldValue) =>
            ActivityLog.Insert(0, $"[Watch] Count changed {oldValue} -> {newValue}"));

        LifecycleDemo = new LifecycleDemoViewModel(message => ActivityLog.Insert(0, $"[Lifecycle] {message}"));
    }

    [RelayCommand]
    private void Increment() => Count.Value++;

    [RelayCommand]
    private void ToggleLifecycleDemo() => ShowLifecycleDemo = !ShowLifecycleDemo;

    [RelayCommand]
    private void ToggleToast() => ShowToast = !ShowToast;

    [RelayCommand]
    private void CycleThemeColor()
    {
        var index = Array.IndexOf(ThemePalette, ThemeColor.Value);
        ThemeColor.Value = ThemePalette[(index + 1) % ThemePalette.Length];
    }

    [RelayCommand]
    private void BorderClicked(object? parameter) =>
        ActivityLog.Insert(0, $"[Directives.Event] Border clicked - CommandParameter is the raised {parameter?.GetType().Name}");

    [RelayCommand]
    private void EnterPressed() =>
        ActivityLog.Insert(0, "[Directives.Event] Enter pressed - matched via Directives.Modifiers=\"enter\"");

    [RelayCommand]
    private void ResetCache() =>
        ActivityLog.Insert(0, "[BuildConfiguration] Cache reset - this button only exists in a Debug build");

    [RelayCommand]
    private void CycleStatus() => StatusIndex = (StatusIndex + 1) % 3;

    [RelayCommand]
    private void Undock() => DockTarget.Value = "FloatingDock";

    [RelayCommand]
    private void Redock() => DockTarget.Value = "MainDock";

    [RelayCommand]
    private async Task SimulateLoadAsync()
    {
        IsLoadingDashboard = true;
        ActivityLog.Insert(0, "[Suspense] IsPending -> true");

        await Task.Delay(TimeSpan.FromSeconds(1.5));

        IsLoadingDashboard = false;
        ActivityLog.Insert(0, "[Suspense] IsPending -> false");
    }
}

/// <summary>A single feed item - the "slot props" the FancyList-style ItemTemplate below receives.</summary>
public sealed record Post(string Username, string Body, int Likes);
