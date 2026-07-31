// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Barbatos.Wpf.ApplicationModel;
using Barbatos.Wpf.Devices;
using Barbatos.Wpf.Input;
using Barbatos.Wpf.Mcp;
using Barbatos.Wpf.Notifications;
using Barbatos.Wpf.Power;
using Barbatos.Wpf.PushNotifications;
using Barbatos.Wpf.Startup;
using Barbatos.Wpf.Storage;
using Barbatos.Wpf.Tray;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Barbatos.Wpf.Core.UnitTests;

public sealed class FakeStartupRegistrar : IStartupRegistrar
{
    public Dictionary<string, string> Entries { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsRegistered(string entryName) => Entries.ContainsKey(entryName);

    public void Register(string entryName, string command) => Entries[entryName] = command;

    public void Unregister(string entryName) => Entries.Remove(entryName);
}

public sealed class FakePowerManager : IPowerManager
{
    public List<(bool KeepAwake, bool KeepDisplayOn)> Calls { get; } = new();

    public void SetKeepAwake(bool keepAwake, bool keepDisplayOn) => Calls.Add((keepAwake, keepDisplayOn));
}

/// <summary>Ported from this library's own earlier (removed) <c>ConfigureGlobalHotkeys</c> feature's own identically-shaped test double - dictionaries + <see cref="RaiseHotkeyPressed"/>, adapted off the old string-parsed gesture onto <see cref="Key"/>/<see cref="ModifierKeys"/>.</summary>
public sealed class FakeHotkeyPlatform : IHotkeyPlatform
{
    public Dictionary<int, (Key Key, ModifierKeys Modifiers)> Registered { get; } = new();

    public List<int> Unregistered { get; } = new();

    /// <summary>When set, <see cref="Register"/> returns <see langword="false"/> without recording - simulates the OS rejecting a hotkey another process (or another action) already owns.</summary>
    public bool RejectRegistrations { get; set; }

    public event EventHandler<int>? HotkeyPressed;

    public bool Register(int id, Key key, ModifierKeys modifiers)
    {
        if (RejectRegistrations)
            return false;

        Registered[id] = (key, modifiers);
        return true;
    }

    public void Unregister(int id)
    {
        Registered.Remove(id);
        Unregistered.Add(id);
    }

    public void RaiseHotkeyPressed(int id) => HotkeyPressed?.Invoke(this, id);
}

public sealed class FakeTrayIconPlatform : ITrayIconPlatform
{
    public TrayIconOptions? ShownOptions { get; private set; }

    public int ShowCount { get; private set; }

    public int HideCount { get; private set; }

    public string? LastToolTip { get; private set; }

    public List<(string Title, string Text, TrayIconBalloonIcon Icon)> BalloonTips { get; } = new();

    public event EventHandler? Clicked;

    public event EventHandler? DoubleClicked;

    public void Show(TrayIconOptions options)
    {
        ShownOptions = options;
        ShowCount++;
    }

    public void Hide() => HideCount++;

    public void SetToolTip(string toolTip) => LastToolTip = toolTip;

    public void ShowBalloonTip(string title, string text, TrayIconBalloonIcon icon) =>
        BalloonTips.Add((title, text, icon));

    public void RaiseClicked() => Clicked?.Invoke(this, EventArgs.Empty);

    public void RaiseDoubleClicked() => DoubleClicked?.Invoke(this, EventArgs.Empty);
}

public sealed class FakeNotificationPlatform : INotificationPlatform
{
    public List<(NotificationOptions Options, NotificationContent Content)> Shown { get; } = new();

    public NotificationAvailability Availability { get; set; } = NotificationAvailability.Enabled;

    public int OpenSystemSettingsCallCount { get; private set; }

    /// <summary>When set, <see cref="Show"/> throws instead of recording - simulates a real
    /// toast platform failure (e.g. an unpackaged-app edge case) independent of <see cref="Availability"/>.</summary>
    public bool ThrowOnShow { get; set; }

    public event EventHandler<NotificationActivatedEventArgs>? Activated;

    public void Show(NotificationOptions options, NotificationContent content)
    {
        if (ThrowOnShow)
            throw new InvalidOperationException("Simulated Show() failure.");

        Shown.Add((options, content));
    }

    public NotificationAvailability GetAvailability() => Availability;

    public void OpenSystemSettings() => OpenSystemSettingsCallCount++;

    public void RaiseActivated(string title, string message, string? arguments = null) =>
        Activated?.Invoke(this, new NotificationActivatedEventArgs(title, message, arguments));
}

public sealed class FakePushNotificationTransport : IPushNotificationTransport
{
    public int StartCount { get; private set; }

    public int StopCount { get; private set; }

    public bool IsConnected { get; private set; }

    public event EventHandler<bool>? ConnectionStateChanged;

    public event EventHandler<string>? NotificationReceived;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        StartCount++;
        IsConnected = true;
        ConnectionStateChanged?.Invoke(this, true);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        StopCount++;
        IsConnected = false;
        ConnectionStateChanged?.Invoke(this, false);
        return Task.CompletedTask;
    }

    /// <summary>Receipt ids the service has acknowledged, in order, including repeats.</summary>
    public List<string> Acknowledged { get; } = new();

    /// <summary>Simulates the transport delivering a raw notification payload.</summary>
    public void RaiseNotificationReceived(string rawJson) =>
        NotificationReceived?.Invoke(this, rawJson);

    public Task AcknowledgeAsync(string receiptId, CancellationToken cancellationToken = default)
    {
        Acknowledged.Add(receiptId);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public sealed class FakePushNotificationFallbackPresenter : IPushNotificationFallbackPresenter
{
    public List<(IPushNotification Notification, DateTimeOffset ReceivedAt)> Notified { get; } = new();

    public event EventHandler<IPushNotification>? Activated;

    public void Notify(IPushNotification notification, DateTimeOffset receivedAt) =>
        Notified.Add((notification, receivedAt));

    public void RaiseActivated(IPushNotification notification) =>
        Activated?.Invoke(this, notification);
}

public sealed class FakeLauncher : ILauncher
{
    public List<Uri> Opened { get; } = new();

    public List<Uri> TryOpened { get; } = new();

    public Task<bool> CanOpenAsync(Uri uri) => Task.FromResult(true);

    public Task<bool> OpenAsync(Uri uri)
    {
        Opened.Add(uri);
        return Task.FromResult(true);
    }

    public Task<bool> OpenAsync(OpenFileRequest request) => Task.FromResult(true);

    public Task<bool> TryOpenAsync(Uri uri)
    {
        TryOpened.Add(uri);
        return Task.FromResult(true);
    }
}

public sealed class FakeAppInfo : IAppInfo
{
    public string AppGuid { get; set; } = "test-app-guid";

    public string Name { get; set; } = "Test App";

    public string VersionString { get; set; } = "1.2.3";

    public Version Version { get; set; } = new(1, 2, 3);

    public string BuildString { get; set; } = "0";

    public void ShowSettingsUI()
    {
    }

    public AppTheme RequestedTheme { get; set; } = AppTheme.Unspecified;

    public AppPackagingModel PackagingModel { get; set; } = AppPackagingModel.Unpackaged;

    public LayoutDirection RequestedLayoutDirection { get; set; } = LayoutDirection.LeftToRight;

    public DateTime? InstallDate { get; set; }

    public string? InstallLocation { get; set; }
}

public sealed class FakeDeviceInfo : IDeviceInfo
{
    public string Model { get; set; } = "Test Model";

    public string Manufacturer { get; set; } = "Test Manufacturer";

    public string Name { get; set; } = "Test Device";

    public string VersionString { get; set; } = "10.0";

    public Version Version { get; set; } = new(10, 0);

    public DevicePlatform Platform { get; set; } = DevicePlatform.WPF;

    public DeviceIdiom Idiom { get; set; } = DeviceIdiom.Desktop;

    public DeviceType DeviceType { get; set; } = DeviceType.Physical;
}

public sealed class FakeDeviceIdentity : IDeviceIdentity
{
    public string InstanceId { get; set; } = "test-instance-id";

    public string HardwareFingerprint { get; set; } = "test-fingerprint";

    public Task<string> GetInstanceIdAsync() => Task.FromResult(InstanceId);

    public Task<string> GetHardwareFingerprintAsync() => Task.FromResult(HardwareFingerprint);
}

/// <summary>In-memory <see cref="ISecureStorage"/> so <see cref="SecureStorageAiApiKeyProvider"/>'s own key-normalization logic can be tested without touching real DPAPI-backed storage.</summary>
public sealed class FakeSecureStorage : ISecureStorage
{
    readonly Dictionary<string, string> _values = new();

    public Task<string?> GetAsync(string key) => Task.FromResult(_values.GetValueOrDefault(key));

    public Task SetAsync(string key, string value)
    {
        _values[key] = value;
        return Task.CompletedTask;
    }

    public bool Remove(string key) => _values.Remove(key);

    public void RemoveAll() => _values.Clear();
}

public sealed class FakeAiApiKeyProvider : IAiApiKeyProvider
{
    readonly Dictionary<string, string> _keys = new(StringComparer.OrdinalIgnoreCase);

    public Task<string?> GetApiKeyAsync(string provider, CancellationToken cancellationToken = default) =>
        Task.FromResult(_keys.TryGetValue(provider, out var key) ? key : null);

    public Task SetApiKeyAsync(string provider, string apiKey, CancellationToken cancellationToken = default)
    {
        _keys[provider] = apiKey;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Only exposes <see cref="Tools"/> directly (set by the test) - used to test
/// <see cref="AiChatService"/>'s own tool-merging logic in isolation, the same way
/// <c>FakePushNotificationTransport</c> is used to test <c>PushNotificationService</c> without a
/// real connection. <see cref="AddServerAsync"/>/<see cref="RemoveServerAsync"/> are not used by
/// any test that relies on this fake (<see cref="McpServerStatus"/> can only be constructed by
/// the real <c>McpServerRegistry</c> - there is no <c>InternalsVisibleTo</c> seam for it, same
/// situation as <c>PeriodicServiceStatus</c>), so they throw rather than fake a value nothing
/// observes.
/// </summary>
public sealed class FakeMcpServerRegistry : IMcpServerRegistry
{
    public List<AITool> ToolsList { get; } = new();

    public IReadOnlyList<McpServerStatus> Servers => Array.Empty<McpServerStatus>();

    public IReadOnlyList<AITool> Tools => ToolsList;

    public event EventHandler? Changed;

    public Task<McpServerStatus> AddServerAsync(McpServerDescriptor descriptor, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException($"{nameof(FakeMcpServerRegistry)} only exposes {nameof(Tools)} directly.");

    public Task<bool> RemoveServerAsync(string name, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException($"{nameof(FakeMcpServerRegistry)} only exposes {nameof(Tools)} directly.");

    public void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public sealed class FakeAiChatClientFactory : IAiChatClientFactory
{
    public IChatClient? Client { get; set; }

    public bool Configured { get; set; } = true;

    public event EventHandler? ConfigurationChanged;

    public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default) => Task.FromResult(Configured);

    public Task<IChatClient> GetChatClientAsync(CancellationToken cancellationToken = default) =>
        Client is not null
            ? Task.FromResult(Client)
            : throw new InvalidOperationException("FakeAiChatClientFactory has no Client set.");

    public void UpdateProvider(string provider, string model, string? endpoint = null)
    {
    }

    public void SelectProvider(string key)
    {
    }

    public Task RefreshApiKeyAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void RaiseConfigurationChanged() => ConfigurationChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>Captures the last <see cref="ChatOptions"/> it was called with, so tests can assert
/// on what <see cref="AiChatService"/> actually sent - never makes a real network call.</summary>
public sealed class FakeChatClient : IChatClient
{
    public ChatOptions? LastOptions { get; private set; }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        LastOptions = options;
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "fake response")));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LastOptions = options;
        await Task.CompletedTask;
        yield return new ChatResponseUpdate(ChatRole.Assistant, "fake");
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}

/// <summary>
/// Points <see cref="IFileSystem.AppDataDirectory"/> at a throwaway temp directory so
/// <see cref="FileAiSessionStore"/> can be exercised without writing into the real per-user app
/// data folder. Deleting the directory is the owning test's job (see <c>TempDirectory</c>).
/// </summary>
public sealed class FakeFileSystem : IFileSystem
{
    public FakeFileSystem(string root)
    {
        AppDataDirectory = root;
        CacheDirectory = root;
    }

    public string CacheDirectory { get; }

    public string AppDataDirectory { get; }

    public Task<Stream> OpenAppPackageFileAsync(string filename) =>
        throw new NotSupportedException($"{nameof(FakeFileSystem)} only exposes the directory properties.");

    public Task<bool> AppPackageFileExistsAsync(string filename) => Task.FromResult(false);
}

/// <summary>
/// Returns a pre-scripted sequence of responses, one per call, so a multi-turn agent run (ask for
/// a tool, then answer once the tool result comes back) can be driven without a real model.
/// Unlike <see cref="FakeChatClient"/> it records <em>every</em> <see cref="ChatOptions"/> it saw,
/// since what matters for the agent tests is what reached the model on a particular turn.
/// </summary>
public sealed class ScriptedChatClient : IChatClient
{
    readonly Queue<ChatResponse> _responses = new();

    public List<ChatOptions?> ReceivedOptions { get; } = new();

    public List<IReadOnlyList<ChatMessage>> ReceivedMessages { get; } = new();

    public ChatOptions? LastOptions => ReceivedOptions.Count == 0 ? null : ReceivedOptions[^1];

    public ScriptedChatClient Enqueue(ChatResponse response)
    {
        _responses.Enqueue(response);
        return this;
    }

    public ScriptedChatClient EnqueueText(string text) =>
        Enqueue(new ChatResponse(new ChatMessage(ChatRole.Assistant, text)));

    public ScriptedChatClient EnqueueToolCall(string callId, string toolName, IDictionary<string, object?>? arguments = null) =>
        Enqueue(new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [new FunctionCallContent(callId, toolName, arguments ?? new Dictionary<string, object?>())])));

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        ReceivedOptions.Add(options);
        ReceivedMessages.Add(messages.ToList());

        return Task.FromResult(_responses.Count > 0
            ? _responses.Dequeue()
            : new ChatResponse(new ChatMessage(ChatRole.Assistant, "scripted default")));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken);
        foreach (var update in response.ToChatResponseUpdates())
            yield return update;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}

/// <summary>
/// A no-op <see cref="IAiSessionStore"/> used only to prove that a store registered before
/// <c>ConfigureMcp</c> wins over the built-in file-backed default - the same precedence check
/// <see cref="FakeAiApiKeyProvider"/> backs for API keys.
/// </summary>
public sealed class FakeAiSessionStore : IAiSessionStore
{
    public Task SaveAsync(string sessionId, AIAgent agent, AgentSession session, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<AgentSession?> LoadAsync(string sessionId, AIAgent agent, CancellationToken cancellationToken = default) =>
        Task.FromResult<AgentSession?>(null);

    public Task<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

    public Task<bool> DeleteAsync(string sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
