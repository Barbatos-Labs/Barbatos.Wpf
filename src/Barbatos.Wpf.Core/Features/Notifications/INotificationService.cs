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
    /// Shows a notification built from <paramref name="content"/>, which may include an image
    /// and action buttons, and carry navigation arguments retrievable from
    /// <see cref="NotificationActivatedEventArgs.Arguments"/> when the user acts on it. A no-op
    /// when <see cref="IsEnabled"/> is <see langword="false"/>.
    /// </summary>
    void Show(NotificationContent content);

    /// <summary>
    /// Occurs when the user clicks a notification (or one of its buttons) shown through this
    /// service.
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
    public NotificationActivatedEventArgs(string title, string message, string? arguments = null)
    {
        Title = title;
        Message = message;
        Arguments = arguments;
    }

    public string Title { get; }

    public string Message { get; }

    /// <summary>
    /// The opaque navigation payload set via <see cref="NotificationContent.Arguments"/> (for
    /// the notification body) or <see cref="NotificationButton.Arguments"/> (for the button the
    /// user clicked), or <see langword="null"/> if none was set. Use this to route the user to
    /// the relevant place in the app.
    /// </summary>
    public string? Arguments { get; }
}

/// <summary>
/// Describes a notification to show via <see cref="INotificationService.Show(NotificationContent)"/>,
/// including optional rich content (an image and action buttons) beyond plain title/message.
/// </summary>
public sealed class NotificationContent
{
    /// <summary>
    /// The notification's title. Required.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// The notification's body text. Required.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// The notification's severity. Defaults to <see cref="NotificationSeverity.Info"/>.
    /// </summary>
    public NotificationSeverity Severity { get; init; } = NotificationSeverity.Info;

    /// <summary>
    /// The path of an image file shown within the notification body (a "hero image"). Optional;
    /// unlike <see cref="NotificationOptions.IconPath"/> (the app's identity icon, set once for
    /// every notification), this is per-notification.
    /// </summary>
    public string? ImagePath { get; init; }

    /// <summary>
    /// An opaque navigation payload delivered back via
    /// <see cref="NotificationActivatedEventArgs.Arguments"/> when the user clicks the
    /// notification body (as opposed to one of its <see cref="Buttons"/>). Interpret it however
    /// suits the app, e.g. as a route or an entity id.
    /// </summary>
    public string? Arguments { get; init; }

    /// <summary>
    /// Action buttons shown on the notification. Empty by default.
    /// </summary>
    public IList<NotificationButton> Buttons { get; } = new List<NotificationButton>();
}

/// <summary>
/// A single action button on a <see cref="NotificationContent"/>.
/// </summary>
public sealed class NotificationButton
{
    /// <summary>
    /// Creates a button that, when clicked, delivers <paramref name="arguments"/> back via
    /// <see cref="NotificationActivatedEventArgs.Arguments"/> without launching anything else.
    /// </summary>
    public NotificationButton(string text, string? arguments = null)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Arguments = arguments;
    }

    /// <summary>
    /// Creates a button that, when clicked, opens <paramref name="launchUri"/> directly (e.g. a
    /// website or a custom protocol) instead of raising <see cref="INotificationService.Activated"/>.
    /// </summary>
    public NotificationButton(string text, Uri launchUri)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        LaunchUri = launchUri ?? throw new ArgumentNullException(nameof(launchUri));
    }

    /// <summary>
    /// The button's label.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// The opaque navigation payload delivered back when this button is clicked, or
    /// <see langword="null"/> when <see cref="LaunchUri"/> is set instead.
    /// </summary>
    public string? Arguments { get; }

    /// <summary>
    /// The URI opened directly when this button is clicked, or <see langword="null"/> when the
    /// button instead raises <see cref="INotificationService.Activated"/> with <see cref="Arguments"/>.
    /// </summary>
    public Uri? LaunchUri { get; }
}
