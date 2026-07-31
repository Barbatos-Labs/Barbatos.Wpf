// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.PushNotifications;

/// <summary>
/// Options specific to <see cref="SignalRPushNotificationTransport"/>, the default
/// <see cref="IPushNotificationTransport"/>. Can be configured from code via
/// <c>ConfigurePushNotifications</c>'s <c>configureSignalR</c> parameter and/or from
/// configuration files using the <see cref="SectionName"/> section (file values override code
/// values). Irrelevant if you replace <see cref="IPushNotificationTransport"/> with your own
/// implementation - these are not part of the transport-agnostic
/// <see cref="PushNotificationOptions"/>.
/// </summary>
public class SignalRPushNotificationOptions
{
    /// <summary>
    /// The configuration section the options are bound from.
    /// </summary>
    public const string SectionName = "Barbatos:PushNotifications:SignalR";

    /// <summary>
    /// The push server's SignalR hub URL. Required - <c>ConnectAsync</c> throws a clear
    /// <see cref="InvalidOperationException"/> if this is left unset.
    /// </summary>
    public string? ServerUrl { get; set; }

    /// <summary>
    /// This application's identifier on the push server. Required - <c>ConnectAsync</c> throws a
    /// clear <see cref="InvalidOperationException"/> if this is left unset.
    /// </summary>
    public string? AppId { get; set; }

    /// <summary>
    /// A stable identifier for this device/install. Leave unset to use
    /// <see cref="Barbatos.Wpf.ApplicationModel.DeviceIdentity.GetInstanceIdAsync"/> automatically
    /// (a random id generated on first use and persisted for this app installation).
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// This application's version, sent during the handshake. Leave unset to use
    /// <see cref="Barbatos.Wpf.ApplicationModel.AppInfo.VersionString"/> automatically.
    /// </summary>
    public string? AppVersion { get; set; }

    /// <summary>
    /// The platform name sent during the handshake. Leave unset to use
    /// <see cref="Barbatos.Wpf.Devices.DeviceInfo.Platform"/> automatically (<c>"WPF"</c>).
    /// </summary>
    public string? Platform { get; set; }

    /// <summary>
    /// This application's key on the push server, if it requires one. Sent both as the
    /// <see cref="AppKeyHeaderName"/> header and as SignalR's <c>access_token</c>, since which of
    /// the two reaches the server depends on the transport it negotiates.
    /// </summary>
    public string? AppKey { get; set; }

    /// <summary>
    /// The header <see cref="AppKey"/> is sent in. Defaults to <c>"X-App-Key"</c>; change it to
    /// match whatever your push server reads.
    /// </summary>
    public string AppKeyHeaderName { get; set; } = "X-App-Key";

    /// <summary>
    /// Extra labels describing this device (plan tier, permission group, machine role, ...) sent
    /// with the handshake. A server can use them to target a notification at a subset of devices
    /// instead of all of them.
    /// </summary>
    public IDictionary<string, string> Tags { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The SignalR hub method invoked once, right after connecting, to identify this
    /// device/app to the server. Defaults to <c>"RegisterDevice"</c>. Configurable so it can
    /// match whatever your specific push server actually expects.
    /// </summary>
    public string HandshakeMethodName { get; set; } = "RegisterDevice";

    /// <summary>
    /// The SignalR hub method the server invokes on this client to push a notification.
    /// Defaults to <c>"ReceiveNotification"</c>. Configurable so it can match whatever your
    /// specific push server actually expects.
    /// </summary>
    public string NotificationMethodName { get; set; } = "ReceiveNotification";

    /// <summary>
    /// The SignalR hub method invoked to confirm a notification arrived, so a server with an
    /// offline queue can stop re-sending it. Defaults to <c>"AcknowledgeNotification"</c>. Set it
    /// to <see langword="null"/> for a server that doesn't track delivery, in which case nothing
    /// is sent.
    /// </summary>
    public string? AcknowledgeMethodName { get; set; } = "AcknowledgeNotification";
}
