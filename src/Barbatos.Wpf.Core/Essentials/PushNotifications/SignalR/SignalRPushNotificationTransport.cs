// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Text.Json;
using System.Text.Json.Serialization;
using Barbatos.Wpf.ApplicationModel;
using Barbatos.Wpf.Devices;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.PushNotifications;

/// <summary>
/// The default <see cref="IPushNotificationTransport"/>: connects to a SignalR hub, sends a
/// one-time device/app handshake once connected, and forwards whatever the server pushes as raw
/// JSON text. Every SignalR-specific detail (hub URL, method names, the handshake payload
/// itself) lives here and in <see cref="SignalRPushNotificationOptions"/> - none of it leaks
/// into the transport-agnostic <see cref="IPushNotificationTransport"/> contract or
/// <see cref="IPushNotificationService"/>, so swapping in a different delivery mechanism (FCM,
/// WNS, a raw WebSocket, ...) never has to imitate SignalR's shape.
/// </summary>
internal sealed class SignalRPushNotificationTransport : IPushNotificationTransport, IDisposable
{
    readonly IOptions<SignalRPushNotificationOptions> _options;
    readonly IDeviceIdentity _deviceIdentity;
    readonly IAppInfo _appInfo;
    readonly IDeviceInfo _deviceInfo;
    HubConnection? _connection;

    public SignalRPushNotificationTransport(
        IOptions<SignalRPushNotificationOptions> options,
        IDeviceIdentity deviceIdentity,
        IAppInfo appInfo,
        IDeviceInfo deviceInfo)
    {
        _options = options;
        _deviceIdentity = deviceIdentity;
        _appInfo = appInfo;
        _deviceInfo = deviceInfo;
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

        if (_connection is null)
        {
            var connection = new HubConnectionBuilder()
                .WithUrl(options.ServerUrl)
                .WithAutomaticReconnect()
                .Build();

            connection.On<JsonElement>(options.NotificationMethodName, payload =>
                NotificationReceived?.Invoke(this, payload.GetRawText()));

            connection.Reconnected += _ => RaiseConnectionStateChanged(true);
            connection.Reconnecting += _ => RaiseConnectionStateChanged(false);
            connection.Closed += _ => RaiseConnectionStateChanged(false);

            _connection = connection;
        }

        await _connection.StartAsync(cancellationToken).ConfigureAwait(false);

        var handshake = new DeviceRegistration
        {
            DeviceId = options.DeviceId ?? await _deviceIdentity.GetInstanceIdAsync().ConfigureAwait(false),
            AppId = options.AppId,
            AppVersion = options.AppVersion ?? _appInfo.VersionString,
            Platform = options.Platform ?? _deviceInfo.Platform.ToString(),
        };
        await _connection.SendAsync(options.HandshakeMethodName, handshake, cancellationToken).ConfigureAwait(false);

        ConnectionStateChanged?.Invoke(this, true);
    }

    public Task StopAsync(CancellationToken cancellationToken = default) =>
        _connection?.StopAsync(cancellationToken) ?? Task.CompletedTask;

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
    public void Dispose() =>
        _ = DisposeAsync().AsTask();

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
    }
}
