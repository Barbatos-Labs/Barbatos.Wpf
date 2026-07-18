// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Notifications;

/// <summary>
/// Pushes desktop notifications (Windows toast notifications) from the application.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Gets whether <see cref="Show"/> currently displays notifications. When
    /// <see langword="false"/>, <see cref="Show"/> is a no-op.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Enables or disables notifications. Intended to be called from a settings UI.
    /// </summary>
    void SetEnabled(bool enabled);

    /// <summary>
    /// Occurs when <see cref="IsEnabled"/> changes.
    /// </summary>
    event EventHandler? IsEnabledChanged;

    /// <summary>
    /// Shows a notification with the given title and message. A no-op when
    /// <see cref="IsEnabled"/> is <see langword="false"/>.
    /// </summary>
    void Show(string title, string message, NotificationSeverity severity = NotificationSeverity.Info);

    /// <summary>
    /// Occurs when the user clicks a notification shown through this service.
    /// </summary>
    event EventHandler<NotificationActivatedEventArgs>? Activated;
}

/// <summary>
/// The severity of a notification, mapped to the OS icon shown alongside it.
/// </summary>
public enum NotificationSeverity
{
    None,
    Info,
    Warning,
    Error,
}

/// <summary>
/// The event data for <see cref="INotificationService.Activated"/>.
/// </summary>
public sealed class NotificationActivatedEventArgs : EventArgs
{
    public NotificationActivatedEventArgs(string title, string message)
    {
        Title = title;
        Message = message;
    }

    public string Title { get; }

    public string Message { get; }
}
