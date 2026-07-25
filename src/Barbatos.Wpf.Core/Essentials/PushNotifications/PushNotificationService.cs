// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Text.Json;
using Barbatos.Wpf.ApplicationModel;
using Barbatos.Wpf.Dispatching;
using Barbatos.Wpf.Notifications;
using Microsoft.Extensions.Logging;

namespace Barbatos.Wpf.PushNotifications;

/// <summary>
/// The default <see cref="IPushNotificationService"/>. Knows nothing about any specific delivery
/// mechanism - it only talks to <see cref="IPushNotificationTransport"/>'s transport-agnostic
/// contract, deserializing whatever raw JSON it hands over into the notification type registered
/// via <c>ConfigurePushNotifications&lt;TNotification&gt;</c>.
/// </summary>
internal sealed class PushNotificationService : IPushNotificationService
{
    readonly IPushNotificationTransport _transport;
    readonly INotificationService _notifications;
    readonly IPushNotificationFallbackPresenter _fallback;
    readonly ILauncher _launcher;
    readonly IDispatcher? _dispatcher;
    readonly ILogger<PushNotificationService>? _logger;

    Func<string, IPushNotification?>? _deserialize;

    public PushNotificationService(
        IPushNotificationTransport transport,
        INotificationService notifications,
        IPushNotificationFallbackPresenter fallback,
        ILauncher launcher,
        IDispatcherProvider dispatcherProvider,
        ILogger<PushNotificationService>? logger = null)
    {
        _transport = transport;
        _notifications = notifications;
        _fallback = fallback;
        _launcher = launcher;
        _logger = logger;

        // Resolved once, here - this constructor runs synchronously on the UI thread (via the
        // IWpfInitializeService that resolves this singleton during app startup), so this is
        // guaranteed to capture the UI dispatcher even though the transport's own callbacks may
        // arrive on a ThreadPool thread (true for the default SignalR transport).
        _dispatcher = dispatcherProvider.GetForCurrentThread();

        _transport.NotificationReceived += OnRawNotificationReceived;
        _notifications.Activated += OnNotificationActivated;
        _fallback.Activated += (_, notification) => Dispatch(notification.Action);
    }

    public event EventHandler<PushNotificationReceivedEventArgs>? NotificationReceived;

    public event EventHandler<PushNotificationRouteRequestedEventArgs>? RouteRequested;

    public event EventHandler<bool>? ConnectionStateChanged
    {
        add => _transport.ConnectionStateChanged += value;
        remove => _transport.ConnectionStateChanged -= value;
    }

    public bool IsConnected => _transport.IsConnected;

    /// <summary>
    /// Registers <typeparamref name="TNotification"/> as the type incoming raw JSON is
    /// deserialized into. Called exactly once at startup by
    /// <c>ConfigurePushNotifications</c>/<c>ConfigurePushNotifications&lt;TNotification&gt;</c>.
    /// </summary>
    internal void UseNotificationType<TNotification>() where TNotification : class, IPushNotification =>
        _deserialize = json => JsonSerializer.Deserialize<TNotification>(json);

    public Task ConnectAsync(CancellationToken cancellationToken = default) =>
        _transport.StartAsync(cancellationToken);

    public Task DisconnectAsync(CancellationToken cancellationToken = default) =>
        _transport.StopAsync(cancellationToken);

    public Task SimulateNotificationAsync(IPushNotification notification, CancellationToken cancellationToken = default)
    {
        Process(notification);
        return Task.CompletedTask;
    }

    void OnRawNotificationReceived(object? sender, string rawJson)
    {
        IPushNotification? notification;
        try
        {
            notification = _deserialize?.Invoke(rawJson);
        }
        catch (JsonException ex)
        {
            _logger?.LogWarning(ex, "A push notification arrived that could not be deserialized into the registered notification type.");
            return;
        }

        if (notification is not null)
            Process(notification);
    }

    void Process(IPushNotification notification)
    {
        var receivedAt = DateTimeOffset.Now;

        if (_dispatcher is { IsDispatchRequired: true } dispatcher)
            dispatcher.Dispatch(() => Display(notification, receivedAt));
        else
            Display(notification, receivedAt);
    }

    void Display(IPushNotification notification, DateTimeOffset receivedAt)
    {
        var showPrimary = _notifications.Availability == NotificationAvailability.Enabled && _notifications.IsEnabled;
        var usedFallback = !showPrimary;

        if (showPrimary)
        {
            try
            {
                _notifications.Show(BuildContent(notification));
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Showing the push notification through INotificationService failed; using the fallback presenter instead.");
                usedFallback = true;
            }
        }

        if (usedFallback)
            _fallback.Notify(notification, receivedAt);

        NotificationReceived?.Invoke(this, new PushNotificationReceivedEventArgs(notification, receivedAt, usedFallback));
    }

    static NotificationContent BuildContent(IPushNotification notification) => new()
    {
        Title = notification.Title,
        Message = notification.Body,
        // ImagePath is intentionally left unset: ToastNotificationPlatform passes it through
        // Path.GetFullPath, which throws for a remote http(s) URL (confirmed) rather than
        // ignoring it - and a push notification's image is typically an absolute URL, never a
        // local file. The fallback window shows the image itself instead, since a plain WPF
        // Image/BitmapImage accepts a remote Uri natively.
        Arguments = notification.Action is { } action ? JsonSerializer.Serialize(action) : null,
    };

    void OnNotificationActivated(object? sender, NotificationActivatedEventArgs args)
    {
        if (string.IsNullOrEmpty(args.Arguments))
            return;

        PushNotificationAction? action;
        try
        {
            action = JsonSerializer.Deserialize<PushNotificationAction>(args.Arguments);
        }
        catch (JsonException)
        {
            return;
        }

        Dispatch(action);
    }

    void Dispatch(PushNotificationAction? action)
    {
        switch (action?.ActionType)
        {
            case PushNotificationActionType.Url:
            case PushNotificationActionType.Setting:
                if (action.ActionTarget is { } target)
                    _ = _launcher.TryOpenAsync(target);
                break;

            case PushNotificationActionType.Route:
                RouteRequested?.Invoke(this, new PushNotificationRouteRequestedEventArgs(action.ActionTarget));
                break;
        }
    }
}
