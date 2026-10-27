// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Barbatos.Wpf.ApplicationModel;
using Barbatos.Wpf.ApplicationModel.Communication;
using Barbatos.Wpf.Apsu;
using Barbatos.Wpf.Devices;
using Barbatos.Wpf.Dialogs;
using Barbatos.Wpf.Mcp;
using Barbatos.Wpf.Networking;
using Barbatos.Wpf.Notifications;
using Barbatos.Wpf.Power;
using Barbatos.Wpf.PushNotifications;
using Barbatos.Wpf.SingleInstance;
using Barbatos.Wpf.Startup;
using Barbatos.Wpf.Storage;
using Barbatos.Wpf.Tray;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

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
    readonly IPushNotificationService _pushNotifications;
    readonly IGreetingService _greetingService;
    readonly IPreferences _preferences;
    readonly ISecureStorage _secureStorage;
    readonly IEmail _email;
    readonly IDialogService _dialogService;
    readonly IAiChatService _aiChat;
    readonly IAiAgentFactory _aiAgentFactory;
    readonly IAiChatClientFactory _aiChatClientFactory;
    readonly IAiModelCatalog _aiModelCatalog;
    readonly IAiApiKeyProvider _aiApiKeyProvider;
    readonly IMcpServerRegistry _mcpServerRegistry;
    readonly IReadOnlyList<AiProviderDescriptor> _aiProviderCatalog;
    readonly SettingsStore _settingsStore;
    readonly CounterStore _counterStore;

    string _heartbeatIntervalSeconds;
    string _heartbeatStatusText = string.Empty;
    string _secureStorageInput = string.Empty;
    string _secureStorageResult = string.Empty;
    string _displayInfoDescription;
    string _deviceIdentityDescription = "(not loaded - click \"Show device identity\")";
    string _pushNotificationStatusDescription = "Not connected.";
    string _workspaceSummary = "(not loaded yet)";
    string _selectedAiProvider = "gemini";
    string _aiModel = "gemini-3.5-flash";
    bool _aiModelsBusy;
    string _aiApiKeyInput = string.Empty;
    string _aiStatusDescription = "Not configured yet.";
    string _aiChatInput = string.Empty;
    bool _aiAwaitingReply;

    string _mcpOAuthEndpoint = string.Empty;
    string _mcpOAuthStatus = "Paste a hosted MCP server's URL and connect.";
    bool _mcpOAuthBusy;

    // The conversation the agent remembers, kept across turns - this is what IAiChatService
    // has no equivalent of. Dropped whenever IAiAgentFactory rebuilds the agent (a different
    // provider/key, or an MCP server appearing/disappearing), since a session belongs to the
    // agent that created it.
    AgentSession? _aiSession;

    // Set while the agent is paused waiting for the user to answer the inline approval panel;
    // SendAiChatMessage awaits this, and ResolvePendingApproval completes it.
    TaskCompletionSource<(bool Approved, bool AlwaysApprove)>? _pendingApproval;
    string _pendingApprovalDescription = string.Empty;
    bool _alwaysApproveTool;

    public MainViewModel(
        IGreetingService greetingService,
        IRunOnStartupService runOnStartup,
        IKeepAwakeService keepAwake,
        ITrayIconService trayIcon,
        IPeriodicServiceScheduler periodicServices,
        INotificationService notifications,
        IPushNotificationService pushNotifications,
        ISingleInstanceService singleInstance,
        IConnectivity connectivity,
        IPreferences preferences,
        ISecureStorage secureStorage,
        IEmail email,
        IDialogService dialogService,
        IAiChatService aiChat,
        IAiAgentFactory aiAgentFactory,
        IAiChatClientFactory aiChatClientFactory,
        IAiModelCatalog aiModelCatalog,
        IAiApiKeyProvider aiApiKeyProvider,
        IMcpServerRegistry mcpServerRegistry,
        IOptions<AiProviderOptions> aiProviderOptions,
        SettingsStore settingsStore,
        CounterStore counterStore,
        SampleActivityLog activityLog)
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
        _pushNotifications = pushNotifications;
        _preferences = preferences;
        _secureStorage = secureStorage;
        _email = email;
        _dialogService = dialogService;
        _aiChat = aiChat;
        _aiAgentFactory = aiAgentFactory;
        _aiChatClientFactory = aiChatClientFactory;
        _aiModelCatalog = aiModelCatalog;
        _aiApiKeyProvider = aiApiKeyProvider;
        _mcpServerRegistry = mcpServerRegistry;
        _settingsStore = settingsStore;
        _counterStore = counterStore;
        // The catalog WpfProgram.CreateWpfApp seeded via ConfigureMcp's configureProvider - this
        // sample's own choice of which providers to suggest, not anything Barbatos.Wpf.Mcp
        // prescribes (see AiProviderOptions.Providers's own remarks).
        _aiProviderCatalog = [.. aiProviderOptions.Value.Providers];
        AiProviders = _aiProviderCatalog.Select(p => p.Key!).ToList();

        _mcpServerRegistry.Changed += (_, _) => OnPropertyChanged(nameof(McpServersDescription));
        _counterStore.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(nameof(DoubleCountLabel));
        };
        activityLog.Logged += LogLifecycleEvent;
        _aiChat.ConfigurationChanged += (_, _) => _ = RefreshAiStatusAsync();
        // The agent is rebuilt whenever its inputs change, and an AgentSession belongs to the
        // agent that created it - so the remembered conversation is dropped here rather than
        // carried onto a different agent. In practice this fires once at startup, when the
        // seeded MCP server finishes connecting and its tools become available.
        _aiAgentFactory.ConfigurationChanged += (_, _) =>
        {
            if (_aiSession is null)
                return;

            _aiSession = null;
            LogLifecycleEvent("IAiAgentFactory.ConfigurationChanged - agent rebuilt, conversation reset");
        };
        _ = RefreshAiStatusAsync();

        // The Activated and RouteRequested handlers below append to the UI-bound LifecycleEvents
        // collection directly, with no Dispatcher call of their own: Windows reports a toast
        // click on a background thread, but INotificationService marshals it to the UI thread
        // before raising Activated, and the push client raises RouteRequested from that
        // already-marshalled event (or from the fallback window's own click, on the UI thread).
        _notifications.Activated += (sender, args) =>
            LogLifecycleEvent(args.Arguments is null
                ? $"Notification activated ({args.Title})"
                : $"Notification activated ({args.Title}), navigate: {args.Arguments}");

        _pushNotifications.NotificationReceived += (sender, args) =>
            LogLifecycleEvent($"Push notification received at {args.ReceivedAt:T} ({(args.UsedFallback ? "fallback window" : "real toast")}): {args.Notification.Title}");
        _pushNotifications.RouteRequested += (sender, args) =>
            LogLifecycleEvent($"Push notification route requested: {args.Route}");
        _pushNotifications.ConnectionStateChanged += (sender, isConnected) =>
        {
            PushNotificationStatusDescription = isConnected ? "Connected." : "Not connected.";
        };

        // ConfigureSingleInstance()'s default behavior already brings this window to the
        // foreground; this just logs it too, the same way every other feature does.
        singleInstance.SecondInstanceLaunched += (sender, args) =>
            LogLifecycleEvent("SingleInstance: a second launch attempt was blocked");

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
            .FirstOrDefault(service => service.Name == "Heartbeat")?.Schedule.Interval ?? TimeSpan.FromSeconds(5))
            .TotalSeconds.ToString("0");
        _heartbeatStatusText = BuildHeartbeatStatusText();

        // Keeps the "next run"/description line live as the heartbeat ticks or is toggled.
        _periodicServices.ServiceExecuted += (sender, args) =>
        {
            if (args.Service.Name == "Heartbeat")
                HeartbeatStatusText = BuildHeartbeatStatusText();
        };
        _periodicServices.IsEnabledChanged += (sender, args) => HeartbeatStatusText = BuildHeartbeatStatusText();
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

    public string DeviceIdentityDescription
    {
        get => _deviceIdentityDescription;
        private set { _deviceIdentityDescription = value; OnPropertyChanged(); }
    }

    // Loaded on demand, not at startup - see GreetingService.GetDeviceIdentityDescriptionAsync().
    public async void LoadDeviceIdentity()
    {
        LogLifecycleEvent("DeviceIdentity.GetInstanceIdAsync() / GetHardwareFingerprintAsync()");
        DeviceIdentityDescription = await _greetingService.GetDeviceIdentityDescriptionAsync();
    }

    /// <summary>
    /// Re-queries <see cref="Barbatos.Wpf.Devices.IDeviceDisplay.MainDisplayInfo"/>. Called
    /// from <see cref="MainWindow"/>'s <c>Loaded</c> event, since a window must exist (and be
    /// shown) before display info can be determined.
    /// </summary>
    public void RefreshDisplayInfo() =>
        DisplayInfoDescription = _greetingService.GetDisplayInfoDescription();

    /// <summary>
    /// Stands in for a real app's important-but-slow startup query (a database read, a
    /// license check, warming a cache, ...). Loaded once from <see cref="App.OnStartup"/>
    /// and awaited *before* the splash screen closes and <see cref="MainWindow"/> is shown -
    /// see "SplashScreen" in the root README.md - so this text is already here the moment
    /// the window appears instead of popping in a moment later.
    /// </summary>
    public string WorkspaceSummary
    {
        get => _workspaceSummary;
        private set { _workspaceSummary = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Simulates a slow startup query - swap the delay below for whatever your own app's real
    /// important-but-slow startup work is. Called once, from <see cref="App.OnStartup"/>,
    /// awaited before the splash screen closes.
    /// </summary>
    public async Task LoadWorkspaceSummaryAsync()
    {
        LogLifecycleEvent("MainViewModel.LoadWorkspaceSummaryAsync() - simulating a slow startup query...");
        await Task.Delay(TimeSpan.FromSeconds(3));
        WorkspaceSummary = "Workspace ready - 128 open items across 6 boards.";
        LogLifecycleEvent("MainViewModel.LoadWorkspaceSummaryAsync() - done, MainWindow can show now.");
    }

    public int LaunchCount { get; }

    public ObservableCollection<string> LifecycleEvents { get; } = new();

    public void LogLifecycleEvent(string message) =>
        LifecycleEvents.Add($"[{DateTime.Now:HH:mm:ss.fff}] {message}");

    /// <summary>State, read straight through from <see cref="CounterStore"/> - forwarded into
    /// this ViewModel's own <see cref="PropertyChanged"/> by the subscription set up above.</summary>
    public int Count => _counterStore.Count;

    /// <summary>A getter, read straight through from <see cref="CounterStore"/>.</summary>
    public string DoubleCountLabel => _counterStore.DoubleCountLabel;

    /// <summary>Calls the store's action directly - no ViewModel-side logic involved.</summary>
    public void IncrementCounter() => _counterStore.Increment();

    /// <summary>
    /// Deliberately does not catch anything - see <see cref="CounterStore.SimulateError"/> and
    /// <see cref="WpfProgram.CreateWpfApp"/>'s <c>OnDispatcherUnhandledException</c> lifecycle
    /// hook for what happens to the exception.
    /// </summary>
    public void SimulateCounterError() => _counterStore.SimulateError();

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
                _periodicServices.UpdateSchedule("Heartbeat", new PeriodicSchedule
                {
                    Frequency = PeriodicFrequency.Custom,
                    Interval = TimeSpan.FromSeconds(seconds),
                    Description = HeartbeatService.DescriptionText,
                });
                _heartbeatIntervalSeconds = seconds.ToString("0");
                HeartbeatStatusText = BuildHeartbeatStatusText();
                PersistSettings();
            }

            OnPropertyChanged();
        }
    }

    /// <summary>
    /// A read-only "next run / description" line for the Periodic section, demonstrating
    /// <see cref="PeriodicServiceStatus.NextRunTime"/> and <see cref="PeriodicSchedule.Description"/>
    /// - refreshed after every heartbeat tick and whenever the scheduler is toggled.
    /// </summary>
    public string HeartbeatStatusText
    {
        get => _heartbeatStatusText;
        private set { _heartbeatStatusText = value; OnPropertyChanged(); }
    }

    string BuildHeartbeatStatusText()
    {
        var status = _periodicServices.Services.FirstOrDefault(service => service.Name == "Heartbeat");
        if (status is null)
            return "(not registered)";

        var nextRun = status.NextRunTime is { } next ? next.LocalDateTime.ToString("T") : "-";
        return $"{status.Schedule.Description} Next run: {nextRun}. Runs so far: {status.RunCount}.";
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
        LogNotificationRequest("Test notification");
        _notifications.Show("Barbatos.Wpf.Core Sample", "This is a test notification pushed from the sample app.");
    }

    /// <summary>
    /// Demonstrates the rich notification content: a button that raises
    /// <see cref="INotificationService.Activated"/> with a navigation payload, and a button
    /// that opens a URL directly.
    /// </summary>
    public void SendRichTestNotification()
    {
        LogNotificationRequest("rich test notification");

        var content = new NotificationContent
        {
            Title = "Barbatos.Wpf.Core Sample",
            Message = "This notification has action buttons and a navigation payload.",
            Arguments = "page=settings",
        };
        content.Buttons.Add(new NotificationButton("Open settings", "page=settings"));
        content.Buttons.Add(new NotificationButton("View on GitHub", new Uri("https://github.com/Barbatos-Labs/Barbatos.Wpf")));

        _notifications.Show(content);
    }

    /// <summary>
    /// Demonstrates <see cref="NotificationContent.ImagePath"/>: a "hero image" rendered inside
    /// the notification body, shipped as a Content asset (see Assets/notification-hero.png)
    /// and resolved relative to the app's install directory.
    /// </summary>
    public void SendImageTestNotification()
    {
        LogNotificationRequest("image test notification");

        var content = new NotificationContent
        {
            Title = "Barbatos.Wpf.Core Sample",
            Message = "This notification includes a hero image.",
            ImagePath = Path.Combine(AppContext.BaseDirectory, "Assets", "notification-hero.png"),
        };

        _notifications.Show(content);
    }

    /// <summary>
    /// Windows silently drops toasts it isn't allowed to show instead of raising an error, so
    /// every "send" action logs <see cref="INotificationService.Availability"/> alongside the
    /// request - otherwise a blocked notification looks identical to a successful one.
    /// </summary>
    void LogNotificationRequest(string label) =>
        LogLifecycleEvent(_notifications.Availability == NotificationAvailability.Enabled
            ? $"Notification requested ({label})"
            : $"Notification requested ({label}) - Windows will not display it: {_notifications.Availability}");

    /// <summary>
    /// Whether the OS currently allows this app to display notifications. Bound by the
    /// settings row to show an in-app warning (and an "Open notification settings" button)
    /// instead of silently relying on a toast Windows won't display - see
    /// <see cref="RefreshNotificationsAvailability"/> for when this is refreshed.
    /// </summary>
    public bool NotificationsAvailable => _notifications.Availability == NotificationAvailability.Enabled;

    public bool NotificationsUnavailable => !NotificationsAvailable;

    public string NotificationsStatusDescription => _notifications.Availability switch
    {
        NotificationAvailability.Enabled => "Allow this app to push desktop notifications",
        NotificationAvailability.DisabledForApplication => "Notifications for this app are turned off in Windows Settings.",
        NotificationAvailability.DisabledForUser => "Notifications are turned off in Windows Settings (System > Notifications).",
        NotificationAvailability.DisabledByGroupPolicy => "Notifications are disabled by your organization's policy.",
        NotificationAvailability.DisabledByManifest => "Notifications are disabled for this app.",
        _ => "Allow this app to push desktop notifications",
    };

    public void OpenNotificationSettings()
    {
        LogLifecycleEvent("Notifications.OpenSystemSettings()");
        _notifications.OpenSystemSettings();
    }

    /// <summary>
    /// Re-checks <see cref="INotificationService.Availability"/> and refreshes the bindings
    /// above. Called when the window is activated (see <see cref="MainWindow"/>), since the
    /// user may have just come back from toggling notifications in Windows Settings.
    /// </summary>
    public void RefreshNotificationsAvailability()
    {
        OnPropertyChanged(nameof(NotificationsAvailable));
        OnPropertyChanged(nameof(NotificationsUnavailable));
        OnPropertyChanged(nameof(NotificationsStatusDescription));
    }

    public string PushNotificationStatusDescription
    {
        get => _pushNotificationStatusDescription;
        private set { _pushNotificationStatusDescription = value; OnPropertyChanged(); }
    }

    public async void ConnectPushNotifications()
    {
        LogLifecycleEvent("IPushNotificationService.ConnectAsync()");
        try
        {
            await _pushNotifications.ConnectAsync();
        }
        catch (Exception ex)
        {
            // No real push server exists yet, so a missing ServerUrl/AppId or an unreachable
            // host is expected here - logged instead of thrown so the sample keeps running.
            LogLifecycleEvent($"IPushNotificationService.ConnectAsync() failed: {ex.Message}");
        }
    }

    public async void DisconnectPushNotifications()
    {
        LogLifecycleEvent("IPushNotificationService.DisconnectAsync()");
        await _pushNotifications.DisconnectAsync();
    }

    /// <summary>
    /// Feeds a synthetic notification through the same display/fallback pipeline a real server
    /// push would go through - the only way to see the feature work end-to-end before a real
    /// push server exists. Uncheck "Notifications" above first to see the fallback window
    /// instead of a real toast.
    /// </summary>
    public void SimulatePushNotification()
    {
        var notification = new PushNotification
        {
            NotificationId = Random.Shared.Next(1000, 9999),
            AppId = "Barbatos.Wpf.Core.Sample",
            EventKey = $"SAMPLE_{DateTimeOffset.Now:yyyyMMddHHmmss}",
            Title = "Bản cập nhật mới đã sẵn sàng",
            Body = "Đã có phiên bản mới tối ưu hiệu năng, vui lòng cập nhật ngay.",
            Action = new PushNotificationAction { ActionType = PushNotificationActionType.Url, ActionTarget = "https://github.com/Barbatos-Labs/Barbatos.Wpf" },
        };

        LogLifecycleEvent("IPushNotificationService.SimulateNotificationAsync(...)");
        _ = _pushNotifications.SimulateNotificationAsync(notification);
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
        try
        {
            await _email.ComposeAsync("Hello from Barbatos.Wpf.Core", "Sent via Simple MAPI.", []);
        }
        catch (FeatureNotSupportedException ex)
        {
            // No Simple MAPI client answered (common on machines with only webmail or the new
            // Outlook): fall back to the default mailto: handler, as the README recommends.
            LogLifecycleEvent($"Email.ComposeAsync: {ex.Message} - falling back to mailto:");
            await Launcher.OpenAsync($"mailto:?subject={Uri.EscapeDataString("Hello from Barbatos.Wpf.Core")}");
        }
    }

    public void ShowAbout()
    {
        // No owner argument: resolves to IDialogService.ActiveWindow, which is this window.
        // Double-clicking this button is safe - the second call activates the already-open
        // About instead of showing a duplicate.
        var shown = _dialogService.Show<AboutWindow>();
        LogLifecycleEvent(shown ? "DialogService.Show<AboutWindow>() - opened" : "DialogService.Show<AboutWindow>() - already open, activated instead");
    }

    public void ShowDialogAbout()
    {
        LogLifecycleEvent("DialogService.ShowDialog<AboutWindow>() - opening modally");
        _dialogService.ShowDialog<AboutWindow>();
        LogLifecycleEvent("DialogService.ShowDialog<AboutWindow>() - closed");
    }

    public void CloseAllDialogs()
    {
        var allClosed = _dialogService.CloseAll();
        LogLifecycleEvent(allClosed ? "DialogService.CloseAll() - all dialogs closed" : "DialogService.CloseAll() - one or more dialogs vetoed closing");
    }

    /// <summary>
    /// This sample's own suggested provider list, read from the <see cref="AiProviderDescriptor"/>
    /// catalog <see cref="WpfProgram.CreateWpfApp"/> seeded via <c>ConfigureMcp</c>'s
    /// <c>configureProvider</c> - Barbatos.Wpf.Mcp has no fixed enum of providers (see
    /// <see cref="AiProviderOptions"/>'s remarks for why), so which ones to suggest, and under
    /// what spelling, is entirely this app's call.
    /// </summary>
    public IReadOnlyList<string> AiProviders { get; }

    /// <summary>
    /// Which of <see cref="AiProviders"/> "Save AI settings" below applies to - the actual
    /// provider only changes once that button is clicked (via
    /// <see cref="IAiChatClientFactory.UpdateProvider"/>), not on every ComboBox selection.
    /// </summary>
    public string SelectedAiProvider
    {
        get => _selectedAiProvider;
        set { _selectedAiProvider = value; OnPropertyChanged(); }
    }

    public string AiModel
    {
        get => _aiModel;
        set { _aiModel = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// The model names <see cref="RefreshAiModels"/> last got back from
    /// <see cref="IAiModelCatalog"/>, shown as suggestions in an editable ComboBox - empty until
    /// that button is clicked. The box stays editable on purpose: a provider that doesn't implement
    /// model listing, or a model published minutes ago, must still be reachable by typing its name,
    /// which is the whole reason <see cref="AiProviderOptions.Model"/> is a free string.
    /// </summary>
    public ObservableCollection<string> AiModels { get; } = new();

    /// <summary>Whether "Refresh models" is idle - it makes a real network call per click.</summary>
    public bool CanRefreshAiModels => !_aiModelsBusy;

    /// <summary>
    /// Asks the selected provider which models it currently offers, using the API key already
    /// stored for it. Deliberately a button rather than something that runs on startup:
    /// <see cref="IAiModelCatalog"/> caches nothing and cannot work at all until the end user has
    /// saved their own key, so the sensible time to ask is when the user asks.
    /// </summary>
    public async void RefreshAiModels()
    {
        _aiModelsBusy = true;
        OnPropertyChanged(nameof(CanRefreshAiModels));

        try
        {
            // Same endpoint this sample's catalog entry would chat through - listing has to be
            // pointed at the same place, or it would answer for a different service entirely.
            var endpoint = _aiProviderCatalog.FirstOrDefault(p => string.Equals(p.Key, SelectedAiProvider, StringComparison.OrdinalIgnoreCase))?.Endpoint;
            LogLifecycleEvent($"IAiModelCatalog.GetModelsAsync({SelectedAiProvider})");
            var models = await _aiModelCatalog.GetModelsAsync(SelectedAiProvider, endpoint);

            AiModels.Clear();
            // Sorted here, not by the library: providers return their own order (Anthropic newest
            // first, an OpenAI-compatible endpoint whatever it likes), and a picker is easier to
            // scan alphabetically. The list is also unfiltered - an OpenAI-compatible endpoint
            // lists embedding and audio models next to chat ones, with no reliable flag to tell
            // them apart, so this sample shows everything rather than guessing wrong.
            foreach (var id in models.Select(m => m.Id).OrderBy(id => id, StringComparer.OrdinalIgnoreCase))
                AiModels.Add(id);

            AiStatusDescription = AiModels.Count > 0
                ? $"{AiModels.Count} model(s) listed for {SelectedAiProvider} - pick one, or keep typing your own."
                : $"{SelectedAiProvider} returned no models - type the model name yourself.";
        }
        catch (Exception ex)
        {
            // Neither failure is fatal: no key saved yet is the common one, and a narrow proxy that
            // implements only chat completions has no /models route at all. Typing a model name
            // keeps working in both cases.
            AiStatusDescription = $"Could not list models for {SelectedAiProvider}: {ex.Message}";
            LogLifecycleEvent($"IAiModelCatalog.GetModelsAsync({SelectedAiProvider}) failed - {ex.GetType().Name}");
        }
        finally
        {
            _aiModelsBusy = false;
            OnPropertyChanged(nameof(CanRefreshAiModels));
        }
    }

    /// <summary>
    /// The end user's own API key for <see cref="SelectedAiProvider"/> - never persisted by this
    /// property itself; "Save AI settings" below stores it via <see cref="IAiApiKeyProvider"/>
    /// (DPAPI-encrypted) and immediately clears this field back out.
    /// </summary>
    public string AiApiKeyInput
    {
        get => _aiApiKeyInput;
        set { _aiApiKeyInput = value; OnPropertyChanged(); }
    }

    public string AiStatusDescription
    {
        get => _aiStatusDescription;
        private set { _aiStatusDescription = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Lists each connected server's tools by name, not just how many there are - knowing what a
    /// server actually exposes is the difference between asking something that reaches a tool and
    /// asking something the model can just answer from memory (the seeded NuGet server, for
    /// instance, has no "search for a package" tool at all).
    /// </summary>
    public string McpServersDescription =>
        _mcpServerRegistry.Servers.Count == 0
            ? "(connecting to the seeded MCP server...)"
            : string.Join(" | ", _mcpServerRegistry.Servers.Select(server => server.IsConnected
                ? $"{server.Name}: connected - {string.Join(", ", server.Tools.Select(tool => tool.Name))}"
                : $"{server.Name}: {server.LastError}"));

    /// <summary>
    /// The URL of a hosted, OAuth-protected MCP server to connect to at runtime - the "let the end
    /// user add their own server" case <see cref="IMcpServerRegistry"/> exists for, the same way
    /// Claude Desktop and Cursor let a user paste one in.
    /// </summary>
    public string McpOAuthEndpoint
    {
        get => _mcpOAuthEndpoint;
        set { _mcpOAuthEndpoint = value; OnPropertyChanged(); }
    }

    public string McpOAuthStatus
    {
        get => _mcpOAuthStatus;
        private set { _mcpOAuthStatus = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Whether a browser authorization is currently in flight - the connect button stays disabled
    /// until it resolves, since a second attempt would open a second browser tab and bind a second
    /// loopback port for the same server.
    /// </summary>
    public bool McpOAuthBusy
    {
        get => _mcpOAuthBusy;
        private set { _mcpOAuthBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanConnectMcpOAuthServer)); }
    }

    public bool CanConnectMcpOAuthServer => !McpOAuthBusy;

    /// <summary>
    /// Connects <see cref="McpOAuthEndpoint"/> with OAuth turned on. Everything the flow needs is
    /// discovered from the server itself, so <c>new McpOAuthOptions()</c> with nothing filled in
    /// is genuinely all this takes: the SDK reads the server's 401 challenge, finds its
    /// authorization server, registers this app as a client, and
    /// <see cref="IMcpAuthorizationHandler"/> sends the user to their browser to consent. The
    /// resulting tokens land in DPAPI-encrypted SecureStorage via <see cref="IMcpTokenStore"/>,
    /// so a restart reconnects without asking again.
    /// </summary>
    public async void ConnectMcpOAuthServer()
    {
        var endpoint = McpOAuthEndpoint.Trim();
        if (string.IsNullOrEmpty(endpoint))
        {
            McpOAuthStatus = "Enter the server's URL first.";
            return;
        }

        // Name the entry after the host so two different hosted servers don't collide, and so the
        // stored tokens (keyed by name) follow the server they belong to.
        var name = Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ? uri.Host : endpoint;

        var descriptor = new McpServerDescriptor
        {
            Name = name,
            TransportKind = McpTransportKind.Http,
            Endpoint = endpoint,
            OAuth = new McpOAuthOptions(),
        };

        McpOAuthBusy = true;
        McpOAuthStatus = $"Connecting to {name} - finish signing in in your browser...";
        LogLifecycleEvent($"IMcpServerRegistry.AddServerAsync(\"{name}\") with OAuth");

        try
        {
            // A previous run may have left this server registered; removing first keeps the button
            // idempotent instead of throwing on the duplicate name.
            await _mcpServerRegistry.RemoveServerAsync(name);

            var status = await _mcpServerRegistry.AddServerAsync(descriptor);
            McpOAuthStatus = $"{status.Name}: connected - {status.Tools.Count} tool(s). The agent can use them now.";
        }
        catch (McpAuthorizationException ex)
        {
            // Denying consent or closing the browser is an ordinary outcome here, not a bug -
            // report it plainly rather than as a crash.
            McpOAuthStatus = $"Not signed in: {ex.Message}";
        }
        catch (Exception ex)
        {
            McpOAuthStatus = $"Could not connect: {ex.Message}";
        }
        finally
        {
            McpOAuthBusy = false;
        }
    }

    /// <summary>
    /// Forgets the stored tokens for <see cref="McpOAuthEndpoint"/>'s server and disconnects it,
    /// so the next connect goes through the browser again - the "sign out" a settings screen
    /// needs. <see cref="IMcpServerRegistry.SignOutAsync"/> only discards credentials, which is
    /// why <see cref="IMcpServerRegistry.RemoveServerAsync"/> is called alongside it here.
    /// </summary>
    public async void SignOutMcpOAuthServer()
    {
        var endpoint = McpOAuthEndpoint.Trim();
        var name = Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ? uri.Host : endpoint;
        if (string.IsNullOrEmpty(name))
        {
            McpOAuthStatus = "Enter the server's URL first.";
            return;
        }

        LogLifecycleEvent($"IMcpServerRegistry.SignOutAsync(\"{name}\")");

        var hadTokens = await _mcpServerRegistry.SignOutAsync(name);
        await _mcpServerRegistry.RemoveServerAsync(name);

        McpOAuthStatus = hadTokens
            ? $"Signed out of {name}. Connecting again will ask for consent."
            : $"Nothing stored for {name}.";
    }

    /// <summary>
    /// Applies <see cref="SelectedAiProvider"/>/<see cref="AiModel"/> and, if entered, saves
    /// <see cref="AiApiKeyInput"/> - the only two places BYOK credentials touch this sample:
    /// <see cref="IAiChatClientFactory.UpdateProvider"/> (no secret involved) and
    /// <see cref="IAiApiKeyProvider.SetApiKeyAsync"/> (the end user's own key, DPAPI-encrypted,
    /// never written to <see cref="SettingsStore"/> or any config file).
    /// </summary>
    public async void SaveAiSettings()
    {
        // The endpoint comes from this sample's own catalog entry for the selected provider
        // (null for one with none set, e.g. "custom" - Barbatos.Wpf.Mcp then defaults that to
        // the real OpenAI API). UpdateProvider (not the catalog-driven IAiChatClientFactory.
        // SelectProvider convenience) is what's called here specifically so AiModel stays
        // freely editable rather than always following the catalog's own suggested model.
        var endpoint = _aiProviderCatalog.FirstOrDefault(p => string.Equals(p.Key, SelectedAiProvider, StringComparison.OrdinalIgnoreCase))?.Endpoint;
        LogLifecycleEvent($"IAiChatClientFactory.UpdateProvider({SelectedAiProvider}, \"{AiModel}\")");
        _aiChatClientFactory.UpdateProvider(SelectedAiProvider, AiModel, endpoint);

        if (!string.IsNullOrWhiteSpace(AiApiKeyInput))
        {
            await _aiApiKeyProvider.SetApiKeyAsync(SelectedAiProvider, AiApiKeyInput);
            AiApiKeyInput = string.Empty;
            LogLifecycleEvent($"IAiApiKeyProvider.SetApiKeyAsync({SelectedAiProvider}, ...)");
        }

        await _aiChatClientFactory.RefreshApiKeyAsync();
        await RefreshAiStatusAsync();
    }

    async Task RefreshAiStatusAsync()
    {
        var configured = await _aiChat.IsConfiguredAsync();
        AiStatusDescription = configured
            ? $"Ready - {SelectedAiProvider} ({AiModel})."
            : "Not configured yet - pick a provider, enter a model name, paste your own API key, then click \"Save AI settings\".";
    }

    public string AiChatInput
    {
        get => _aiChatInput;
        set { _aiChatInput = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> AiChatTranscript { get; } = new();

    /// <summary>
    /// Whether the agent is currently paused waiting for the user to answer the inline approval
    /// panel - drives that panel's visibility. Send needs no separate guard while this is showing:
    /// the run it belongs to is still in flight, so <see cref="SendAiChatMessage"/>'s existing
    /// "already awaiting a reply" check already ignores it.
    /// </summary>
    public bool HasPendingApproval => _pendingApproval is not null;

    /// <summary>The tool call awaiting an answer, as shown in the approval panel.</summary>
    public string PendingApprovalDescription
    {
        get => _pendingApprovalDescription;
        private set { _pendingApprovalDescription = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// The panel's "Don't ask again for this tool" checkbox. Passed straight through to
    /// <see cref="AgentApprovalExtensions.CreateApprovalResponse"/>'s <c>alwaysApprove</c>, which
    /// records a standing rule on the session so the same tool stops prompting for the rest of
    /// the conversation.
    /// </summary>
    public bool AlwaysApproveTool
    {
        get => _alwaysApproveTool;
        set { _alwaysApproveTool = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Sends <see cref="AiChatInput"/> through the agent from <see cref="IAiAgentFactory"/>. The
    /// agent already has every connected MCP server's tools attached, so nothing here wires tools
    /// in by hand - and because it runs against a persistent <c>AgentSession</c>, the model
    /// remembers earlier turns, which the older <see cref="IAiChatService"/> path does not do.
    /// </summary>
    public async void SendAiChatMessage()
    {
        if (_aiAwaitingReply)
            return;

        var message = AiChatInput.Trim();
        if (message.Length == 0)
            return;

        AiChatInput = string.Empty;
        AiChatTranscript.Add($"You: {message}");

        _aiAwaitingReply = true;
        var replyIndex = -1;
        try
        {
            LogLifecycleEvent("IAiAgentFactory.GetAgentAsync(...)");
            var agent = await _aiAgentFactory.GetAgentAsync();
            _aiSession ??= await agent.CreateSessionAsync();

            // The raw model has no clock or internet access - unlike Gemini's own consumer web
            // app (which layers Google Search grounding on top), a plain API call only knows
            // "now" if it's told. Local time covers "what time is it" for wherever this app
            // happens to be running; UTC is included too so the model can compute any other
            // timezone the user asks about from a fixed reference point. These are set per run
            // rather than on AiAgentOptions.Instructions precisely because they change every
            // turn - run options take precedence over the agent's own, so this is the whole
            // instruction string, not an addition to one.
            var now = DateTimeOffset.Now;
            var runOptions = new ChatClientAgentRunOptions(new ChatOptions
            {
                Instructions =
                    "You are the Barbatos.Wpf.Core sample assistant. " +
                    $"Current date/time: {now:yyyy-MM-dd HH:mm} ({TimeZoneInfo.Local.DisplayName}), " +
                    $"{now.ToUniversalTime():yyyy-MM-dd HH:mm} UTC. " +
                    // Most of the seeded NuGet server's tools take a required absolute
                    // solutionDirectory/rootDirectory argument. Without being told one the model
                    // has nothing to put there, so it quietly answers from its own knowledge
                    // instead of calling the tool - which looks exactly like tool-calling being
                    // broken. Any app exposing path-taking MCP tools has to do something like this.
                    $"This application runs from '{AppContext.BaseDirectory}' - use that path for " +
                    "any tool argument asking for a solution directory or repository root. " +
                    "Prefer calling a tool over answering from memory whenever one fits the question.",
            });

            AgentResponse response;
            (response, replyIndex) = await RunStreamingIntoTranscriptAsync(
                agent, new ChatMessage(ChatRole.User, message), runOptions, replyIndex);

            // Under AiToolApprovalMode.Always a run that wants a tool comes back paused instead
            // of answering - nothing has run yet. Requests are surfaced one at a time even when
            // the model asked for several, so this has to loop until none are left rather than
            // answering once.
            while (response.GetToolApprovalRequests() is { Count: > 0 } pending)
            {
                LogLifecycleEvent($"Tool approval requested: {ToolCallName(pending[0])}");
                var (approved, alwaysApprove) = await RequestApprovalAsync(pending);
                LogLifecycleEvent($"Tool approval {(approved ? "granted" : "denied")}{(approved && alwaysApprove ? " (don't ask again)" : string.Empty)}");

                (response, replyIndex) = await RunStreamingIntoTranscriptAsync(
                    agent, pending.CreateApprovalResponse(approved, alwaysApprove), runOptions, replyIndex);
            }

            if (replyIndex < 0)
                AiChatTranscript.Add("AI: (empty response)");
        }
        catch (Exception ex)
        {
            // Expected until the end user saves their own provider/API key above, or if the
            // seeded MCP server hasn't finished connecting yet - logged instead of thrown so the
            // sample keeps running. Appended to the partial reply already streamed in, if any,
            // rather than added as a separate line.
            if (replyIndex >= 0)
                AiChatTranscript[replyIndex] += $" (error: {ex.Message})";
            else
                AiChatTranscript.Add($"(error: {ex.Message})");
        }
        finally
        {
            CancelPendingApproval();
            _aiAwaitingReply = false;
        }
    }

    /// <summary>
    /// Streams one agent run into <see cref="AiChatTranscript"/> as it arrives, and returns the
    /// assembled <see cref="AgentResponse"/> so the caller can check it for pending tool
    /// approvals - <c>ToAgentResponse()</c> is what makes "stream the text" and "inspect the
    /// finished response" possible at the same time.
    /// </summary>
    /// <param name="replyIndex">The transcript line this reply is being written into, or -1 if
    /// nothing has been written yet; returned back out (rather than taken by <c>ref</c>, which an
    /// async method cannot have) so a follow-up run after an approval keeps appending to the same
    /// line instead of starting a new one.</param>
    async Task<(AgentResponse Response, int ReplyIndex)> RunStreamingIntoTranscriptAsync(
        AIAgent agent,
        ChatMessage message,
        ChatClientAgentRunOptions runOptions,
        int replyIndex)
    {
        var index = replyIndex;
        var updates = new List<AgentResponseUpdate>();
        var reply = index < 0 ? "AI: " : AiChatTranscript[index];

        // Each await here resumes on this window's own captured UI-thread context, so updating
        // AiChatTranscript directly (no Dispatcher.Invoke) is safe.
        await foreach (var update in agent.RunStreamingAsync(message, _aiSession, runOptions))
        {
            updates.Add(update);
            if (string.IsNullOrEmpty(update.Text))
                continue;

            reply += update.Text;
            if (index < 0)
            {
                index = AiChatTranscript.Count;
                AiChatTranscript.Add(reply);
            }
            else
            {
                AiChatTranscript[index] = reply;
            }
        }

        return (updates.ToAgentResponse(), index);
    }

    /// <summary>
    /// Shows the inline approval panel and waits for the user to click Allow or Deny. Returning a
    /// <see cref="Task{TResult}"/> completed by <see cref="ResolvePendingApproval"/> is what lets
    /// the agent loop above read like ordinary sequential code while the UI stays responsive.
    /// </summary>
    Task<(bool Approved, bool AlwaysApprove)> RequestApprovalAsync(IReadOnlyList<ToolApprovalRequestContent> pending)
    {
        // Arguments are worth showing: the model chose them, and they are exactly the part a user
        // needs to see before allowing anything ("delete_file(path: C:\...)" reads very
        // differently from "delete_file").
        var arguments = pending[0].ToolCall is FunctionCallContent { Arguments: { Count: > 0 } values }
            ? string.Join(", ", values.Select(argument => $"{argument.Key}: {argument.Value}"))
            : "(no arguments)";

        PendingApprovalDescription = pending.Count > 1
            ? $"{ToolCallName(pending[0])}({arguments})  -  {pending.Count - 1} more queued"
            : $"{ToolCallName(pending[0])}({arguments})";

        AlwaysApproveTool = false;
        _pendingApproval = new TaskCompletionSource<(bool, bool)>(TaskCreationOptions.RunContinuationsAsynchronously);
        OnPropertyChanged(nameof(HasPendingApproval));

        return _pendingApproval.Task;
    }

    /// <summary>
    /// The tool's name for display. It lives on <see cref="FunctionCallContent"/>, not on the
    /// <c>ToolCallContent</c> base that <c>ToolApprovalRequestContent.ToolCall</c> is typed as -
    /// every MCP tool is a function call in practice, but the fallback keeps a hosted/server-side
    /// tool from rendering as an empty name.
    /// </summary>
    static string ToolCallName(ToolApprovalRequestContent request) =>
        request.ToolCall is FunctionCallContent function ? function.Name : request.ToolCall.CallId;

    /// <summary>
    /// Answers the pending approval - called by the panel's Allow/Deny buttons.
    /// </summary>
    public void ResolvePendingApproval(bool approved)
    {
        var pending = _pendingApproval;
        if (pending is null)
            return;

        _pendingApproval = null;
        OnPropertyChanged(nameof(HasPendingApproval));
        pending.TrySetResult((approved, AlwaysApproveTool));
    }

    /// <summary>
    /// Tears the panel down if a run ends while it is still showing (an error mid-loop, say), so
    /// a stale prompt can't be left on screen with nothing awaiting it.
    /// </summary>
    void CancelPendingApproval()
    {
        var pending = _pendingApproval;
        if (pending is null)
            return;

        _pendingApproval = null;
        OnPropertyChanged(nameof(HasPendingApproval));
        pending.TrySetResult((false, false));
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
