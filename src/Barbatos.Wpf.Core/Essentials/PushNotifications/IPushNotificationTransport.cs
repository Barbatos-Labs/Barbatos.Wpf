// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.PushNotifications;

/// <summary>
/// Abstraction over however push notifications actually reach this device. Deliberately knows
/// nothing about hub URLs, RPC method names, or handshakes - those are specific to one delivery
/// mechanism (e.g. a SignalR hub) and belong on that mechanism's own implementation/options, not
/// here. Implement this to plug in a different delivery mechanism (Firebase Cloud Messaging,
/// Windows Notification Service, a raw WebSocket, ...) in place of the default
/// <c>SignalRPushNotificationTransport</c> - register your implementation via
/// <c>services.AddSingleton&lt;IPushNotificationTransport, MyTransport&gt;()</c> before calling
/// <c>ConfigurePushNotifications</c>.
/// </summary>
public interface IPushNotificationTransport : IAsyncDisposable
{
    /// <summary>Gets whether the transport is currently able to receive notifications.</summary>
    bool IsConnected { get; }

    /// <summary>Occurs whenever <see cref="IsConnected"/> changes.</summary>
    event EventHandler<bool>? ConnectionStateChanged;

    /// <summary>
    /// Occurs when a notification arrives, carrying its raw JSON text - deserialized by
    /// <see cref="IPushNotificationService"/> into whichever <see cref="IPushNotification"/>
    /// type the app registered, not by the transport itself.
    /// </summary>
    event EventHandler<string>? NotificationReceived;

    /// <summary>Starts (or resumes) receiving notifications.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops receiving notifications, if started.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tells the server that <paramref name="receiptId"/> arrived, so it can stop re-sending it.
    /// Called automatically by <see cref="IPushNotificationService"/> for every notification whose
    /// <see cref="IPushNotification.ReceiptId"/> isn't <see langword="null"/>.
    /// <para>
    /// Not every delivery mechanism has an acknowledgement concept (a push relay that fires and
    /// forgets has nowhere to send one), so the default is to do nothing. Override it when the
    /// server keeps an offline queue that acknowledgements are what drain.
    /// </para>
    /// </summary>
    /// <param name="receiptId">The value of the received notification's <see cref="IPushNotification.ReceiptId"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task AcknowledgeAsync(string receiptId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
