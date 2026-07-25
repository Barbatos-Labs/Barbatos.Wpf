// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.PushNotifications;

/// <summary>
/// Transport-agnostic options for the push notifications client feature - apply regardless of
/// which <see cref="IPushNotificationTransport"/> is registered. Can be configured from code via
/// <c>ConfigurePushNotifications</c> and/or from configuration files using the
/// <see cref="SectionName"/> section (file values override code values). For the default
/// SignalR transport's own settings (server URL, hub method names, ...), see
/// <see cref="SignalRPushNotificationOptions"/>.
/// </summary>
public class PushNotificationOptions
{
    /// <summary>
    /// The configuration section the options are bound from.
    /// </summary>
    public const string SectionName = "Barbatos:PushNotifications";

    /// <summary>
    /// Whether the client starts its transport at all. Defaults to <see langword="true"/>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How long the default in-app fallback notification window stays visible before
    /// auto-dismissing. Mirrors <see cref="Notifications.NotificationOptions.Timeout"/>'s default
    /// for consistency.
    /// </summary>
    public TimeSpan FallbackTimeout { get; set; } = TimeSpan.FromSeconds(5);
}
