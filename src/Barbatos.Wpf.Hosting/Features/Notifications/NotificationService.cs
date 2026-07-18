// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Core Contributors.
// All Rights Reserved.

using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.Notifications;

/// <summary>
/// The default <see cref="INotificationService"/> implementation.
/// </summary>
internal sealed class NotificationService : INotificationService
{
    readonly NotificationOptions _options;
    readonly INotificationPlatform _platform;

    public NotificationService(IOptions<NotificationOptions> options, INotificationPlatform platform)
    {
        _options = options.Value;
        _platform = platform;
        _platform.Activated += (sender, args) => Activated?.Invoke(this, args);
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

        if (!IsEnabled)
            return;

        _platform.Show(_options, title, message, severity);
    }

    /// <summary>
    /// Applies the configured options during application construction.
    /// </summary>
    internal void ApplyOptions()
    {
        if (!_options.Enabled)
            SetEnabled(false);
    }
}
