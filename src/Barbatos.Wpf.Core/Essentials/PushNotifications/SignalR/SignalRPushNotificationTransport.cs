// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Barbatos.Wpf.ApplicationModel;
using Barbatos.Wpf.Devices;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.PushNotifications;

/// <summary>
/// The default <see cref="IPushNotificationTransport"/>: connects to a SignalR hub, identifies this
/// device/app with a handshake, and forwards whatever the server pushes as raw JSON text. Every
/// SignalR-specific detail (hub URL, method names, the handshake payload itself) lives here and in
/// <see cref="SignalRPushNotificationOptions"/> - none of it leaks into the transport-agnostic
/// <see cref="IPushNotificationTransport"/> contract or <see cref="IPushNotificationService"/>, so
/// swapping in a different delivery mechanism (FCM, WNS, a raw WebSocket, ...) never has to imitate
/// SignalR's shape.
/// </summary>
internal sealed class SignalRPushNotificationTransport : IPushNotificationTransport, IDisposable
{
    // Serializes StartAsync/StopAsync against each other. HubConnection.StartAsync throws if the
    // connection isn't Disconnected, so two overlapping starts - a manual ConnectAsync racing the
    // start-up one, say - would otherwise surface as an exception rather than as a no-op.
    readonly SemaphoreSlim _lifecycle = new(1, 1);
    readonly IOptions<SignalRPushNotificationOptions> _options;
    readonly IDeviceIdentity _deviceIdentity;
    readonly IAppInfo _appInfo;
    readonly IDeviceInfo _deviceInfo;
    readonly ILogger<SignalRPushNotificationTransport>? _logger;

    HubConnection? _connection;
    DeviceRegistration? _handshake;

    public SignalRPushNotificationTransport(
        IOptions<SignalRPushNotificationOptions> options,
        IDeviceIdentity deviceIdentity,
        IAppInfo appInfo,
        IDeviceInfo deviceInfo,
        ILogger<SignalRPushNotificationTransport>? logger = null)
    {
        _options = options;
        _deviceIdentity = deviceIdentity;
        _appInfo = appInfo;
        _deviceInfo = deviceInfo;
        _logger = logger;
    }

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public event EventHandler<bool>? ConnectionStateChanged;

    public event EventHandler<string>? NotificationReceived;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ServerUrl))
            throw new InvalidOperationException($"{nameof(SignalRPushNotificationOptions)}.{nameof(SignalRPushNotificationOptions.ServerUrl)} must be set before connecting.");
        if (string.IsNullOrWhiteSpace(options.AppId))
            throw new InvalidOperationException($"{nameof(SignalRPushNotificationOptions)}.{nameof(SignalRPushNotificationOptions.AppId)} must be set before connecting.");

        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_connection is { State: not HubConnectionState.Disconnected })
                return;

            _connection ??= BuildConnection(options);
            _handshake ??= await BuildHandshakeAsync(options).ConfigureAwait(false);

            await _connection.StartAsync(cancellationToken).ConfigureAwait(false);
            await SendHandshakeAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lifecycle.Release();
        }

        ConnectionStateChanged?.Invoke(this, true);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_connection is not null)
                await _connection.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    public async Task AcknowledgeAsync(string receiptId, CancellationToken cancellationToken = default)
    {
        var methodName = _options.Value.AcknowledgeMethodName;

        if (string.IsNullOrWhiteSpace(methodName) || _connection is not { State: HubConnectionState.Connected } connection)
            return;

        // Sent as a number when it looks like one, so a server whose hub method takes a numeric id
        // binds it without relying on its serializer accepting numbers-in-strings.
        object argument = long.TryParse(receiptId, NumberStyles.None, CultureInfo.InvariantCulture, out var numeric)
            ? numeric
            : receiptId;

        try
        {
            await connection.SendAsync(methodName, argument, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A lost acknowledgement is recoverable by design: the server re-sends on the next
            // handshake and the service's own de-duplication keeps the user from seeing it twice.
            _logger?.LogDebug(ex, "Acknowledging push notification {ReceiptId} failed; the server will re-send it on the next handshake.", receiptId);
        }
    }

    public ValueTask DisposeAsync() =>
        _connection?.DisposeAsync() ?? ValueTask.CompletedTask;

    /// <summary>
    /// <see cref="HubConnection"/> only implements <see cref="IAsyncDisposable"/>, but this type
    /// is registered as a singleton and <c>WpfApp.Dispose()</c>'s shutdown path is entirely
    /// synchronous - .NET's <c>ServiceProvider</c> throws if its synchronous <c>Dispose()</c>
    /// reaches a tracked singleton that only supports async disposal. Fire-and-forget rather than
    /// blocking: blocking app shutdown on a network round-trip is worse than an occasionally
    /// ungraceful disconnect, which a server-side offline queue can already tolerate.
    /// </summary>
    public void Dispose()
    {
        _ = DisposeAsync().AsTask();
        _lifecycle.Dispose();
    }

    HubConnection BuildConnection(SignalRPushNotificationOptions options)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(options.ServerUrl!, http =>
            {
                if (string.IsNullOrWhiteSpace(options.AppKey))
                    return;

                // Both, deliberately: WebSockets carry the header fine, but transports that fall
                // back to server-sent events or long polling in a restricted network can only pass
                // a token through the query string, which is what AccessTokenProvider produces.
                http.Headers[options.AppKeyHeaderName] = options.AppKey;
                http.AccessTokenProvider = () => Task.FromResult<string?>(options.AppKey);
            })
            .WithAutomaticReconnect()
            .Build();

        connection.On<JsonElement>(options.NotificationMethodName, payload =>
            NotificationReceived?.Invoke(this, payload.GetRawText()));

        connection.Reconnected += OnReconnectedAsync;
        connection.Reconnecting += _ => RaiseConnectionStateChanged(false);
        connection.Closed += _ => RaiseConnectionStateChanged(false);

        return connection;
    }

    /// <summary>
    /// Re-identifies this device after an automatic reconnect. This is not optional bookkeeping:
    /// SignalR issues a brand new connection id on reconnect, so from the server's point of view
    /// the reconnected client is an anonymous stranger until it handshakes again - it would be
    /// treated as offline and receive nothing until the app was restarted.
    /// </summary>
    async Task OnReconnectedAsync(string? connectionId)
    {
        try
        {
            await SendHandshakeAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Re-registering this device after a reconnect failed; the server will not push to it until it reconnects again.");
        }

        ConnectionStateChanged?.Invoke(this, true);
    }

    Task SendHandshakeAsync(CancellationToken cancellationToken) =>
        _connection is { } connection && _handshake is { } handshake
            ? connection.SendAsync(_options.Value.HandshakeMethodName, handshake, cancellationToken)
            : Task.CompletedTask;

    async Task<DeviceRegistration> BuildHandshakeAsync(SignalRPushNotificationOptions options) => new()
    {
        DeviceId = options.DeviceId ?? await _deviceIdentity.GetInstanceIdAsync().ConfigureAwait(false),
        AppId = options.AppId!,
        AppVersion = options.AppVersion ?? _appInfo.VersionString,
        Platform = options.Platform ?? _deviceInfo.Platform.ToString(),
        Tags = options.Tags.Count > 0 ? new Dictionary<string, string>(options.Tags) : null,
    };

    Task RaiseConnectionStateChanged(bool isConnected)
    {
        ConnectionStateChanged?.Invoke(this, isConnected);
        return Task.CompletedTask;
    }

    sealed class DeviceRegistration
    {
        [JsonPropertyName("deviceId")]
        public required string DeviceId { get; init; }

        [JsonPropertyName("appId")]
        public required string AppId { get; init; }

        [JsonPropertyName("appVersion")]
        public required string AppVersion { get; init; }

        [JsonPropertyName("platform")]
        public required string Platform { get; init; }

        [JsonPropertyName("tags")]
        public IReadOnlyDictionary<string, string>? Tags { get; init; }
    }
}
