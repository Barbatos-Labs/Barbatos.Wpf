// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Core Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Barbatos.Wpf.Hotkeys;
using Barbatos.Wpf.Power;
using Barbatos.Wpf.Startup;
using Barbatos.Wpf.Tray;

namespace Barbatos.Wpf.Hosting.Sample;

/// <summary>
/// The view model for <see cref="MainWindow"/>, resolved from the dependency injection
/// container. The settings toggles talk directly to the hosting feature services and are
/// persisted through <see cref="SettingsStore"/>.
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    readonly IRunOnStartupService _runOnStartup;
    readonly IKeepAwakeService _keepAwake;
    readonly ITrayIconService _trayIcon;
    readonly IGlobalHotkeyService _hotkeys;
    readonly SettingsStore _settingsStore;

    string _quickEntryGesture;

    public MainViewModel(
        IGreetingService greetingService,
        IRunOnStartupService runOnStartup,
        IKeepAwakeService keepAwake,
        ITrayIconService trayIcon,
        IGlobalHotkeyService hotkeys,
        SettingsStore settingsStore)
    {
        Greeting = greetingService.GetGreeting();
        EnvironmentDescription = greetingService.GetEnvironmentDescription();

        _runOnStartup = runOnStartup;
        _keepAwake = keepAwake;
        _trayIcon = trayIcon;
        _hotkeys = hotkeys;
        _settingsStore = settingsStore;

        _quickEntryGesture = _hotkeys.Hotkeys
            .FirstOrDefault(hotkey => hotkey.Name == "QuickEntry")?.Gesture.ToString() ?? string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Greeting { get; }

    public string EnvironmentDescription { get; }

    public ObservableCollection<string> LifecycleEvents { get; } = new();

    public void LogLifecycleEvent(string message) =>
        LifecycleEvents.Add($"[{DateTime.Now:HH:mm:ss.fff}] {message}");

    public bool RunOnStartupEnabled
    {
        get => _runOnStartup.IsEnabled;
        set
        {
            // Persisted by the OS (registry), so nothing to save here.
            _runOnStartup.SetEnabled(value);
            OnPropertyChanged();
        }
    }

    public bool TrayIconEnabled
    {
        get => _trayIcon.IsVisible;
        set
        {
            _trayIcon.SetVisible(value);
            PersistSettings();
            OnPropertyChanged();
        }
    }

    public bool KeepAwakeEnabled
    {
        get => _keepAwake.IsEnabled;
        set
        {
            _keepAwake.SetEnabled(value);
            PersistSettings();
            OnPropertyChanged();
        }
    }

    public string QuickEntryGesture
    {
        get => _quickEntryGesture;
        set
        {
            // Invalid input simply reverts to the current gesture.
            if (HotkeyGesture.TryParse(value, out var gesture))
            {
                _hotkeys.UpdateGesture("QuickEntry", gesture);
                _quickEntryGesture = gesture.ToString();
                PersistSettings();
            }

            OnPropertyChanged();
        }
    }

    void PersistSettings() =>
        _settingsStore.Save(_trayIcon.IsVisible, _keepAwake.IsEnabled, _quickEntryGesture);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
