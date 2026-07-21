// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.PushNotifications;

/// <summary>
/// A client that connects to a realtime push-notification server, listens for incoming
/// notifications, and displays them - through <see cref="Notifications.INotificationService"/>
/// when available, falling back to <see cref="IPushNotificationFallbackPresenter"/> otherwise.
/// </summary>
public interface IPushNotificationService
{
    /// <summary>Gets whether the connection to the push server is currently up.</summary>
    bool IsConnected { get; }

    /// <summary>Occurs whenever <see cref="IsConnected"/> changes.</summary>
    event EventHandler<bool>? ConnectionStateChanged;

    /// <summary>Occurs whenever a push notification is received, whichever display path was used.</summary>
    event EventHandler<PushNotificationReceivedEventArgs>? NotificationReceived;

    /// <summary>Occurs when the user activates a notification whose action is <see cref="PushNotificationActionType.Route"/>.</summary>
    event EventHandler<PushNotificationRouteRequestedEventArgs>? RouteRequested;

    /// <summary>
    /// Starts the registered <see cref="IPushNotificationTransport"/>. The default SignalR
    /// transport throws <see cref="InvalidOperationException"/> if
    /// <see cref="SignalRPushNotificationOptions.ServerUrl"/> or
    /// <see cref="SignalRPushNotificationOptions.AppId"/> haven't been set; a different
    /// transport may have its own required configuration instead.
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Disconnects, if connected.</summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Feeds <paramref name="notification"/> through the same display/fallback pipeline a real
    /// server push would go through, without any network involved. Useful to verify the
    /// display/fallback/action-dispatch behavior before a real push server exists.
    /// </summary>
    Task SimulateNotificationAsync(IPushNotification notification, CancellationToken cancellationToken = default);
}

/// <summary>
/// Event data for <see cref="IPushNotificationService.NotificationReceived"/>.
/// </summary>
public sealed class PushNotificationReceivedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PushNotificationReceivedEventArgs"/> class.
    /// </summary>
    public PushNotificationReceivedEventArgs(IPushNotification notification, DateTimeOffset receivedAt, bool usedFallback)
    {
        Notification = notification;
        ReceivedAt = receivedAt;
        UsedFallback = usedFallback;
    }

    /// <summary>The notification that was received.</summary>
    public IPushNotification Notification { get; }

    /// <summary>The local wall-clock time the client received this notification.</summary>
    public DateTimeOffset ReceivedAt { get; }

    /// <summary>
    /// Whether <see cref="IPushNotificationFallbackPresenter"/> was used instead of the OS's own
    /// notification system.
    /// </summary>
    public bool UsedFallback { get; }
}

/// <summary>
/// Event data for <see cref="IPushNotificationService.RouteRequested"/>.
/// </summary>
public sealed class PushNotificationRouteRequestedEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of the <see cref="PushNotificationRouteRequestedEventArgs"/> class.</summary>
    public PushNotificationRouteRequestedEventArgs(string? route) =>
        Route = route;

    /// <summary>The app-defined route/screen name from the notification's action target.</summary>
    public string? Route { get; }
}
