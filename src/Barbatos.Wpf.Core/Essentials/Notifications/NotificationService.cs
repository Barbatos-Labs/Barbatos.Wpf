// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Dispatching;
using Barbatos.Wpf.Hosting;
using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.Notifications;

/// <summary>
/// The default <see cref="INotificationService"/> implementation.
/// </summary>
internal sealed class NotificationService : INotificationService
{
    readonly NotificationOptions _options;
    readonly INotificationPlatform _platform;
    readonly IDispatcher? _dispatcher;

    public NotificationService(IOptions<NotificationOptions> options, INotificationPlatform platform, IServiceProvider services)
    {
        _options = options.Value;
        _platform = platform;

        // Captured once, here: the feature's initializer constructs this service inside Build()
        // on the UI thread, whereas the platform's activation callback arrives later on a
        // background thread - resolving the dispatcher from there could bind a fresh dispatcher
        // to that thread, one nothing ever pumps. Null only when an app built with
        // useDefaults: false never called ConfigureDispatching(); Activated is then raised inline.
        _dispatcher = services.GetOptionalApplicationDispatcher();

        _platform.Activated += OnPlatformActivated;
    }

    public event EventHandler? IsEnabledChanged;

    public event EventHandler<NotificationActivatedEventArgs>? Activated;

    public bool IsEnabled { get; private set; } = true;

    public void SetEnabled(bool enabled)
    {
        if (enabled == IsEnabled)
            return;

        IsEnabled = enabled;
        IsEnabledChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Show(string title, string message, NotificationSeverity severity = NotificationSeverity.Info)
    {
        _ = title ?? throw new ArgumentNullException(nameof(title));
        _ = message ?? throw new ArgumentNullException(nameof(message));

        Show(new NotificationContent { Title = title, Message = message, Severity = severity });
    }

    public void Show(NotificationContent content)
    {
        _ = content ?? throw new ArgumentNullException(nameof(content));

        if (!IsEnabled)
            return;

        _platform.Show(_options, content);
    }

    public NotificationAvailability Availability => _platform.GetAvailability();

    void OnPlatformActivated(object? sender, NotificationActivatedEventArgs args)
    {
        // The Toolkit raises toast activations on a background thread (Microsoft documents
        // ToastNotificationManagerCompat.OnActivated that way), and a handler almost always
        // touches UI - navigating, or appending to a bound collection, which throws
        // NotSupportedException off the dispatcher thread. Marshalling once here is what lets
        // every handler (PushNotificationService's RouteRequested included) assume the UI thread.
        if (_dispatcher is { IsDispatchRequired: true } dispatcher)
            dispatcher.Dispatch(() => Activated?.Invoke(this, args));
        else
            Activated?.Invoke(this, args);
    }

    public void OpenSystemSettings() => _platform.OpenSystemSettings();

    /// <summary>
    /// Applies the configured options during application construction.
    /// </summary>
    internal void ApplyOptions()
    {
        if (!_options.Enabled)
            SetEnabled(false);
    }
}
