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

    /// <summary>
    /// How many <see cref="IPushNotification.ReceiptId"/> values to remember so a re-delivered
    /// notification isn't shown twice. A server that re-sends until acknowledged will occasionally
    /// deliver a duplicate - when an acknowledgement is lost to a dropped connection - and this is
    /// the window in which that stays invisible to the user.
    /// <para>
    /// The history is per-process: it is deliberately not persisted, because the only notification
    /// a restart can bring back is one the app was killed before it could acknowledge, and showing
    /// that again is better than losing it. Set to <c>0</c> to disable de-duplication entirely.
    /// </para>
    /// </summary>
    public int DeduplicationHistorySize { get; set; } = 256;
}
