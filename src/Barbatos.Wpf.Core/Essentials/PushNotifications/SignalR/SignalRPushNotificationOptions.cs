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
}
