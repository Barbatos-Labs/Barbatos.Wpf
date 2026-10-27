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
    /// Gets whether <see cref="Show(string, string, NotificationSeverity)"/> and
    /// <see cref="Show(NotificationContent)"/> currently display notifications. When
    /// <see langword="false"/>, both are no-ops.
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
    /// service. Raised on the application's UI thread.
    /// </summary>
    /// <remarks>
    /// Windows reports toast activations on a background thread, and the default service
    /// marshals each one to the application dispatcher (the one Core's other features use)
    /// before raising this event, so a handler can update UI-bound state directly - appending
    /// to a bound <c>ObservableCollection</c> from the background thread would throw. An
    /// activation that arrives after the dispatcher has shut down (the app is exiting) is
    /// dropped. The event is raised inline, on the platform's thread, only in an app built
    /// with <c>WpfApp.CreateBuilder(useDefaults: false)</c> that never called
    /// <c>ConfigureDispatching()</c>.
    /// </remarks>
    event EventHandler<NotificationActivatedEventArgs>? Activated;

    /// <summary>
    /// Gets whether the OS currently allows this app to display notifications, checked live
    /// against Windows on every access (not cached). Windows silently drops toasts it isn't
    /// allowed to show instead of raising an error, so <see cref="Show(NotificationContent)"/>
    /// alone cannot tell you a notification was blocked - check this first (e.g. when the
    /// window is activated) to show an in-app fallback and, via <see cref="OpenSystemSettings"/>,
    /// a way for the user to fix it.
    /// </summary>
    NotificationAvailability Availability { get; }

    /// <summary>
    /// Opens the Windows Settings page for notifications (the same page shown in
    /// Settings > System > Notifications), so the user can re-enable them after
    /// <see cref="Availability"/> reported they're off.
    /// </summary>
    void OpenSystemSettings();
}

/// <summary>
/// The severity of a notification. Only <see cref="Error"/> changes how a toast is shown; the
/// others are carried through for the app's own use.
/// </summary>
public enum NotificationSeverity
{
    /// <summary>No particular severity.</summary>
    None,

    /// <summary>Informational - the default.</summary>
    Info,

    /// <summary>A warning.</summary>
    Warning,

    /// <summary>
    /// An error. The toast uses the <c>Alarm</c> scenario: it stays on screen until dismissed and
    /// bypasses Focus Assist.
    /// </summary>
    Error,
}

/// <summary>
/// Whether the OS currently allows this app to display notifications, and if not, why.
/// Mirrors <c>Windows.UI.Notifications.NotificationSetting</c>. See
/// <see cref="INotificationService.Availability"/>.
/// </summary>
public enum NotificationAvailability
{
    /// <summary>Notifications are allowed and will be displayed.</summary>
    Enabled,

    /// <summary>
    /// The user turned off notifications for this specific app
    /// (Settings > System > Notifications > this app).
    /// </summary>
    DisabledForApplication,

    /// <summary>
    /// The user turned off notifications system-wide - the main "Notifications" toggle in
    /// Settings > System > Notifications.
    /// </summary>
    DisabledForUser,

    /// <summary>An administrator disabled notifications via Group Policy.</summary>
    DisabledByGroupPolicy,

    /// <summary>Disabled by the app's manifest. Packaged apps only; does not apply to Barbatos.Wpf.</summary>
    DisabledByManifest,
}

/// <summary>
/// The event data for <see cref="INotificationService.Activated"/>.
/// </summary>
public sealed class NotificationActivatedEventArgs : EventArgs
{
    /// <summary>Creates the event data for an activated notification.</summary>
    /// <param name="title">The activated notification's title.</param>
    /// <param name="message">The activated notification's message.</param>
    /// <param name="arguments">The notification's <see cref="NotificationContent.Arguments"/>, if it had any.</param>
    public NotificationActivatedEventArgs(string title, string message, string? arguments = null)
    {
        Title = title;
        Message = message;
        Arguments = arguments;
    }

    /// <summary>The title of the notification that was activated.</summary>
    /// <remarks>
    /// With the default toast platform this is the clicked toast's own title - but only when
    /// this run of the app showed it, and it is among the last 100 shown. A toast left in the
    /// notification center by an earlier run reports an empty string: Windows hands back only
    /// the activation arguments, not the toast's text. Route on <see cref="Arguments"/>, which
    /// survives restarts; treat this as display text.
    /// </remarks>
    public string Title { get; }

    /// <summary>The message of the notification that was activated.</summary>
    /// <remarks>Recovered the same way, with the same limits, as <see cref="Title"/>.</remarks>
    public string Message { get; }

    /// <summary>
    /// The opaque navigation payload set via <see cref="NotificationContent.Arguments"/> (for
    /// the notification body) or <see cref="NotificationButton.Arguments"/> (for the button the
    /// user clicked), or <see langword="null"/> if none was set. Use this to route the user to
    /// the relevant place in the app.
    /// </summary>
    /// <remarks>
    /// With the default toast platform, a button created without arguments of its own reports
    /// the toast's <see cref="NotificationContent.Arguments"/> instead of <see langword="null"/>:
    /// the Windows Community Toolkit copies a toast's arguments onto every button that does not
    /// set the same key itself. Give each button its own arguments when the handler must tell a
    /// button click from a body click.
    /// </remarks>
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
