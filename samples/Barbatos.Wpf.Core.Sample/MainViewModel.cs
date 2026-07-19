// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Barbatos.Wpf.ApplicationModel.Communication;
using Barbatos.Wpf.Devices;
using Barbatos.Wpf.Networking;
using Barbatos.Wpf.Notifications;
using Barbatos.Wpf.Power;
using Barbatos.Wpf.Startup;
using Barbatos.Wpf.Storage;
using Barbatos.Wpf.Tray;

namespace Barbatos.Wpf.Core.Sample;

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
    readonly IPeriodicServiceScheduler _periodicServices;
    readonly INotificationService _notifications;
    readonly IGreetingService _greetingService;
    readonly IPreferences _preferences;
    readonly ISecureStorage _secureStorage;
    readonly IEmail _email;
    readonly SettingsStore _settingsStore;

    string _heartbeatIntervalSeconds;
    string _secureStorageInput = string.Empty;
    string _secureStorageResult = string.Empty;
    string _displayInfoDescription;

    public MainViewModel(
        IGreetingService greetingService,
        IRunOnStartupService runOnStartup,
        IKeepAwakeService keepAwake,
        ITrayIconService trayIcon,
        IPeriodicServiceScheduler periodicServices,
        INotificationService notifications,
        IConnectivity connectivity,
        IPreferences preferences,
        ISecureStorage secureStorage,
        IEmail email,
        SettingsStore settingsStore)
    {
        Greeting = greetingService.GetGreeting();
        EnvironmentDescription = greetingService.GetEnvironmentDescription();
        AppDeviceDescription = greetingService.GetAppDeviceDescription();
        InstallInfoDescription = greetingService.GetInstallInfoDescription();
        PublisherDescription = greetingService.GetPublisherDescription();
        VersionTrackingDescription = greetingService.GetVersionTrackingDescription();
        ConnectivityDescription = greetingService.GetConnectivityDescription();
        // DeviceDisplay.MainDisplayInfo needs an active window, so it is refreshed from
        // RefreshDisplayInfo() once the main window has loaded (see MainWindow.xaml.cs).
        _displayInfoDescription = "(unavailable before the window is shown)";

        _greetingService = greetingService;
        _runOnStartup = runOnStartup;
        _keepAwake = keepAwake;
        _trayIcon = trayIcon;
        _periodicServices = periodicServices;
        _notifications = notifications;
        _preferences = preferences;
        _secureStorage = secureStorage;
        _email = email;
        _settingsStore = settingsStore;

        _notifications.Activated += (sender, args) =>
            LogLifecycleEvent($"Notification activated ({args.Title})");

        // Live-updates whenever the network connection changes.
        connectivity.ConnectivityChanged += (sender, args) =>
        {
            ConnectivityDescription = $"{args.NetworkAccess} via [{string.Join(", ", args.ConnectionProfiles)}]";
            OnPropertyChanged(nameof(ConnectivityDescription));
        };

        // A tiny Preferences demo: count how many times the app has been launched.
        LaunchCount = _preferences.Get("Sample.LaunchCount", 0) + 1;
        _preferences.Set("Sample.LaunchCount", LaunchCount);

        _heartbeatIntervalSeconds = (_periodicServices.Services
            .FirstOrDefault(service => service.Name == "Heartbeat")?.Interval ?? TimeSpan.FromSeconds(5))
            .TotalSeconds.ToString("0");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Greeting { get; }

    public string EnvironmentDescription { get; }

    public string AppDeviceDescription { get; }

    public string InstallInfoDescription { get; }

    public string PublisherDescription { get; }

    public string VersionTrackingDescription { get; }

    public string DisplayInfoDescription
    {
        get => _displayInfoDescription;
        private set { _displayInfoDescription = value; OnPropertyChanged(); }
    }

    public string ConnectivityDescription { get; private set; }

    /// <summary>
    /// Re-queries <see cref="Barbatos.Wpf.Devices.IDeviceDisplay.MainDisplayInfo"/>. Called
    /// from <see cref="MainWindow"/>'s <c>Loaded</c> event, since a window must exist (and be
    /// shown) before display info can be determined.
    /// </summary>
    public void RefreshDisplayInfo() =>
        DisplayInfoDescription = _greetingService.GetDisplayInfoDescription();

    public int LaunchCount { get; }

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

    public bool PeriodicServicesEnabled
    {
        get => _periodicServices.IsEnabled;
        set
        {
            _periodicServices.SetEnabled(value);
            PersistSettings();
            OnPropertyChanged();
        }
    }

    public string HeartbeatIntervalSeconds
    {
        get => _heartbeatIntervalSeconds;
        set
        {
            // Invalid input simply reverts to the current interval.
            if (double.TryParse(value, out var seconds) && seconds >= 1)
            {
                _periodicServices.UpdateInterval("Heartbeat", TimeSpan.FromSeconds(seconds));
                _heartbeatIntervalSeconds = seconds.ToString("0");
                PersistSettings();
            }

            OnPropertyChanged();
        }
    }

    public bool NotificationsEnabled
    {
        get => _notifications.IsEnabled;
        set
        {
            _notifications.SetEnabled(value);
            PersistSettings();
            OnPropertyChanged();
        }
    }

    public void SendTestNotification()
    {
        LogLifecycleEvent("Notification requested (Test notification)");
        _notifications.Show("Barbatos.Wpf.Core Sample", "This is a test notification pushed from the sample app.");
    }

    public string SecureStorageInput
    {
        get => _secureStorageInput;
        set { _secureStorageInput = value; OnPropertyChanged(); }
    }

    public string SecureStorageResult
    {
        get => _secureStorageResult;
        private set { _secureStorageResult = value; OnPropertyChanged(); }
    }

    public async void SaveSecureValue()
    {
        await _secureStorage.SetAsync("Sample.Secret", SecureStorageInput);
        SecureStorageResult = "Saved (DPAPI-encrypted).";
        LogLifecycleEvent("SecureStorage.SetAsync(\"Sample.Secret\")");
    }

    public async void LoadSecureValue()
    {
        var value = await _secureStorage.GetAsync("Sample.Secret");
        SecureStorageResult = value is null ? "(no value stored)" : $"Decrypted: {value}";
        LogLifecycleEvent("SecureStorage.GetAsync(\"Sample.Secret\")");
    }

    public async void ComposeTestEmail()
    {
        LogLifecycleEvent("Email.ComposeAsync(...)");
        await _email.ComposeAsync("Hello from Barbatos.Wpf.Core", "Sent via Simple MAPI.", []);
    }

    void PersistSettings() =>
        _settingsStore.Save(new SampleSettings(
            TrayIconEnabled: _trayIcon.IsVisible,
            KeepAwakeEnabled: _keepAwake.IsEnabled,
            PeriodicServicesEnabled: _periodicServices.IsEnabled,
            HeartbeatInterval: TimeSpan.FromSeconds(double.Parse(_heartbeatIntervalSeconds)),
            NotificationsEnabled: _notifications.IsEnabled));

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
