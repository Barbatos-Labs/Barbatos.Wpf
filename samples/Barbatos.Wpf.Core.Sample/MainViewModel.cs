// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Barbatos.Wpf.ApplicationModel.Communication;
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
    readonly IAiChatClientFactory _aiChatClientFactory;
    readonly IAiApiKeyProvider _aiApiKeyProvider;
    readonly IMcpServerRegistry _mcpServerRegistry;
    readonly IReadOnlyList<AiProviderDescriptor> _aiProviderCatalog;
    readonly SettingsStore _settingsStore;

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
    string _aiApiKeyInput = string.Empty;
    string _aiStatusDescription = "Not configured yet.";
    string _aiChatInput = string.Empty;
    bool _aiAwaitingReply;

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
        IAiChatClientFactory aiChatClientFactory,
        IAiApiKeyProvider aiApiKeyProvider,
        IMcpServerRegistry mcpServerRegistry,
        IOptions<AiProviderOptions> aiProviderOptions,
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
        _pushNotifications = pushNotifications;
        _preferences = preferences;
        _secureStorage = secureStorage;
        _email = email;
        _dialogService = dialogService;
        _aiChat = aiChat;
        _aiChatClientFactory = aiChatClientFactory;
        _aiApiKeyProvider = aiApiKeyProvider;
        _mcpServerRegistry = mcpServerRegistry;
        _settingsStore = settingsStore;
        // The catalog WpfProgram.CreateWpfApp seeded via ConfigureMcp's configureProvider - this
        // sample's own choice of which providers to suggest, not anything Barbatos.Wpf.Mcp
        // prescribes (see AiProviderOptions.Providers's own remarks).
        _aiProviderCatalog = [.. aiProviderOptions.Value.Providers];
        AiProviders = _aiProviderCatalog.Select(p => p.Key!).ToList();

        _mcpServerRegistry.Changed += (_, _) => OnPropertyChanged(nameof(McpServersDescription));
        _aiChat.ConfigurationChanged += (_, _) => _ = RefreshAiStatusAsync();
        _ = RefreshAiStatusAsync();

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
        await _email.ComposeAsync("Hello from Barbatos.Wpf.Core", "Sent via Simple MAPI.", []);
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

    public string McpServersDescription =>
        _mcpServerRegistry.Servers.Count == 0
            ? "(connecting to the seeded MCP server...)"
            : string.Join(" | ", _mcpServerRegistry.Servers.Select(server => server.IsConnected
                ? $"{server.Name}: connected, {server.Tools.Count} tool(s)"
                : $"{server.Name}: {server.LastError}"));

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
    /// Sends <see cref="AiChatInput"/> through <see cref="IAiChatService"/>, which automatically
    /// merges every connected MCP server's tools into the request - nothing else in this sample
    /// wires MCP tools in by hand.
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
            LogLifecycleEvent("IAiChatService.GetStreamingResponseAsync(...)");

            // The raw model has no clock or internet access - unlike Gemini's own consumer web
            // app (which layers Google Search grounding on top), a plain API call only knows
            // "now" if it's told. Local time covers "what time is it" for wherever this app
            // happens to be running; UTC is included too so the model can compute any other
            // timezone the user asks about from a fixed reference point.
            var now = DateTimeOffset.Now;
            var options = new ChatOptions
            {
                Instructions = $"Current date/time: {now:yyyy-MM-dd HH:mm} ({TimeZoneInfo.Local.DisplayName}), " +
                                $"{now.ToUniversalTime():yyyy-MM-dd HH:mm} UTC.",
            };

            // Streamed token-by-token instead of awaiting the full reply - IAiChatService's own
            // ConfigureAwait(false) calls are internal to the library and don't affect this loop:
            // each await here still resumes on this window's own captured UI-thread context, so
            // updating AiChatTranscript directly (no Dispatcher.Invoke) is safe.
            var reply = "AI: ";
            await foreach (var update in _aiChat.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, message)], options))
            {
                if (string.IsNullOrEmpty(update.Text))
                    continue;

                reply += update.Text;
                if (replyIndex < 0)
                {
                    replyIndex = AiChatTranscript.Count;
                    AiChatTranscript.Add(reply);
                }
                else
                {
                    AiChatTranscript[replyIndex] = reply;
                }
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
            _aiAwaitingReply = false;
        }
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
