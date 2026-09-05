using Barbatos.Wpf.Aquarius.Reactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// Drives the <c>&lt;aq:KeepAlive&gt;</c> demo: which View is showing, and how big the cache is
/// allowed to get.
/// </summary>
/// <remarks>
/// The ViewModel only ever names a <see cref="Type"/> - it never touches, caches or even knows
/// about the section Views themselves. That is the direct counterpart of Vue's
/// <c>&lt;component :is="activeComponent"&gt;</c>, where the script side likewise just holds
/// which component to show.
/// </remarks>
public sealed partial class KeepAliveDemoViewModel(Action<string> log) : ObservableObject
{
    /// <summary>The View type currently showing, bound straight to <c>Component.Is</c>.</summary>
    public Ref<Type?> CurrentView { get; } = new(typeof(ProfileSectionView));

    /// <summary>
    /// Bound to <c>KeepAlive.Max</c>. Set it to 2 and switch through all three sections to watch
    /// the least-recently-used one get evicted (and lose its state) in the activity log.
    /// </summary>
    [ObservableProperty]
    private int _max;

    /// <summary>
    /// Bound to <c>KeepAlive.Exclude</c> - a comma-separated deny-list of View <b>type names</b>
    /// that must never be cached, matching Vue's <c>exclude</c> matching component names.
    /// </summary>
    [ObservableProperty]
    private string _exclude = "";

    [RelayCommand]
    private void Show(Type viewType)
    {
        CurrentView.Value = viewType;
        log($"Is = {viewType.Name}");
    }
}
