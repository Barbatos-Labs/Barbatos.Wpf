using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.AquariusRouter.Sample.Views.Preferences;

/// <summary>
/// Demonstrates live, persisted shortcut rebinding end to end: picking a preset here writes
/// <see cref="ShortcutSettingsStore"/>'s settings file, which - because it's loaded with
/// <c>reloadOnChange: true</c> (see <c>WpfProgram.cs</c>) - is picked up immediately by
/// <see cref="ShortcutRoutingInitializer"/> with no app restart. Deliberately just one shortcut
/// (<c>GoToDashboard</c>) and a small fixed preset list, not a full key-chord-capture UI - this
/// proves the rebind mechanism, not a general-purpose customization screen.
/// </summary>
public sealed partial class PreferencesViewModel : ObservableObject
{
    static readonly (string Label, Key Key, ModifierKeys Modifiers)[] DashboardPresets =
    [
        ("Ctrl+F9 (default)", Key.F9, ModifierKeys.Control),
        ("Ctrl+F10", Key.F10, ModifierKeys.Control),
        ("Ctrl+F11", Key.F11, ModifierKeys.Control),
    ];

    readonly ShortcutSettingsStore _settingsStore;

    public PreferencesViewModel(AuthState authState, ShortcutSettingsStore settingsStore, IOptionsMonitor<Dictionary<string, KeyBindingOverride>> overrides)
    {
        AuthState = authState;
        _settingsStore = settingsStore;

        var match = overrides.CurrentValue.TryGetValue("GoToDashboard", out var o)
            ? Array.Find(DashboardPresets, p => p.Key == o.Key && p.Modifiers == o.Modifiers)
            : default;

        _selectedDashboardShortcut = match.Label ?? DashboardPresets[0].Label;
    }

    /// <summary>Exposed only for the "Manage security settings →" RouterLink's own premium-plan IsEnabled binding - PreferencesViewModel otherwise knows nothing about auth (see this class's own remarks and AppRoutes' own).</summary>
    public AuthState AuthState { get; }

    public IReadOnlyList<string> DashboardShortcutPresets { get; } = [.. DashboardPresets.Select(p => p.Label)];

    [ObservableProperty]
    private string _selectedDashboardShortcut;

    partial void OnSelectedDashboardShortcutChanged(string value)
    {
        var preset = Array.Find(DashboardPresets, p => p.Label == value);
        _settingsStore.Save("GoToDashboard", preset.Key, preset.Modifiers);
    }
}
