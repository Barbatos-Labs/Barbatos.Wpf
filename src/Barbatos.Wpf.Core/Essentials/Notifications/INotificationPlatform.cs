// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Diagnostics;
using System.IO;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;

namespace Barbatos.Wpf.Notifications;

/// <summary>
/// Abstraction over the OS mechanism used to push a desktop notification.
/// </summary>
public interface INotificationPlatform
{
    /// <summary>Displays <paramref name="content"/>, styled according to the app-wide <paramref name="options"/>.</summary>
    void Show(NotificationOptions options, NotificationContent content);

    /// <summary>
    /// Occurs when the user clicks a notification (or one of its buttons) shown through this
    /// platform. May be raised on any thread.
    /// </summary>
    /// <remarks>
    /// An implementation does not need to marshal: <see cref="INotificationService"/> moves the
    /// event to the UI thread before raising its own <see cref="INotificationService.Activated"/>.
    /// The default platform raises it on the Windows Community Toolkit's background activation
    /// thread.
    /// </remarks>
    event EventHandler<NotificationActivatedEventArgs>? Activated;

    /// <summary>
    /// Gets whether the OS currently allows this app to display notifications. See
    /// <see cref="INotificationService.Availability"/>.
    /// </summary>
    NotificationAvailability GetAvailability();

    /// <summary>
    /// Opens the OS settings page for notifications. See
    /// <see cref="INotificationService.OpenSystemSettings"/>.
    /// </summary>
    void OpenSystemSettings();
}

/// <summary>
/// The default <see cref="INotificationPlatform"/>, backed by <see cref="ToastContentBuilder"/>
/// and <see cref="ToastNotificationManagerCompat"/> (from the Windows Community Toolkit). This
/// renders full adaptive Windows toast notifications - including images and action buttons -
/// for both packaged and non-packaged (plain Win32/WPF) apps, without requiring a Start menu
/// shortcut or manual COM/AUMID registration.
/// </summary>
internal sealed class ToastNotificationPlatform : INotificationPlatform, IDisposable
{
    const string ArgumentsKey = "barbatosArgs";
    const string ToastIdKey = "barbatosToastId";

    // How many shown toasts keep their text for NotificationActivatedEventArgs. Bounded so an app
    // that shows toasts all day does not grow this without limit; a click on anything older
    // reports empty Title/Message, the same as a toast an earlier run of the app showed.
    const int RememberedToastCapacity = 100;

    // Toast id -> the text it was shown with, plus arrival order so the oldest can be forgotten.
    // Locked because Show runs on the caller's thread and OnActivated on the Toolkit's
    // background activation thread.
    readonly Dictionary<string, (string Title, string Message)> _shownToasts = new(StringComparer.Ordinal);
    readonly Queue<string> _shownToastOrder = new();

    public ToastNotificationPlatform() =>
        ToastNotificationManagerCompat.OnActivated += OnActivated;

    public event EventHandler<NotificationActivatedEventArgs>? Activated;

    public void Show(NotificationOptions options, NotificationContent content)
    {
        // An activation carries back only the arguments, never the toast's text, so each toast
        // gets an id to look its own title/message up by. A Guid rather than a counter: toasts
        // outlive the process in the notification center, and a counter restarting at 1 would
        // match an old toast to a new one's text. The text itself stays out of the arguments:
        // the builder copies them onto every button, repeating a long message once per button
        // in the toast's XML.
        var toastId = Guid.NewGuid().ToString("N");
        RememberShownToast(toastId, content.Title, content.Message);

        var builder = new ToastContentBuilder()
            .AddText(content.Title)
            .AddText(content.Message)
            .SetToastScenario(content.Severity == NotificationSeverity.Error ? ToastScenario.Alarm : ToastScenario.Default)
            .SetToastDuration(options.Timeout > TimeSpan.FromSeconds(7) ? ToastDuration.Long : ToastDuration.Short);

        if (!string.IsNullOrWhiteSpace(options.IconPath))
            builder.AddAppLogoOverride(new Uri(Path.GetFullPath(options.IconPath)), ToastGenericAppLogoCrop.Circle);

        if (!string.IsNullOrWhiteSpace(content.ImagePath))
            builder.AddHeroImage(new Uri(Path.GetFullPath(content.ImagePath)));

        // Added through the builder rather than per button: ToastContentBuilder copies its own
        // arguments onto every button that can carry them, whether added before or after, and
        // never overwrites a key the button set itself (AddButton/AddArgumentHelper in Toolkit
        // 7.1.3, read from the decompiled assembly) - so the id comes back whichever part of the
        // toast is clicked. Protocol (LaunchUri) buttons get none, and never activate the app.
        builder.AddArgument(ToastIdKey, toastId);

        if (!string.IsNullOrWhiteSpace(content.Arguments))
            builder.AddArgument(ArgumentsKey, content.Arguments);

        foreach (var button in content.Buttons)
        {
            var toastButton = new ToastButton().SetContent(button.Text);

            if (button.LaunchUri is not null)
            {
                toastButton.SetProtocolActivation(button.LaunchUri);
            }
            else
            {
                toastButton.SetBackgroundActivation();
                if (!string.IsNullOrWhiteSpace(button.Arguments))
                    toastButton.AddArgument(ArgumentsKey, button.Arguments);
            }

            builder.AddButton(toastButton);
        }

        builder.Show();
    }

    public NotificationAvailability GetAvailability() => ToastNotificationManagerCompat.CreateToastNotifier().Setting switch
    {
        NotificationSetting.Enabled => NotificationAvailability.Enabled,
        NotificationSetting.DisabledForApplication => NotificationAvailability.DisabledForApplication,
        NotificationSetting.DisabledForUser => NotificationAvailability.DisabledForUser,
        NotificationSetting.DisabledByGroupPolicy => NotificationAvailability.DisabledByGroupPolicy,
        NotificationSetting.DisabledByManifest => NotificationAvailability.DisabledByManifest,
        _ => NotificationAvailability.Enabled,
    };

    public void OpenSystemSettings() =>
        Process.Start(new ProcessStartInfo("ms-settings:notifications") { UseShellExecute = true });

    void OnActivated(ToastNotificationActivatedEventArgsCompat e)
    {
        string? arguments = null;
        var title = string.Empty;
        var message = string.Empty;

        if (!string.IsNullOrEmpty(e.Argument))
        {
            var parsed = ToastArguments.Parse(e.Argument);
            if (parsed.Contains(ArgumentsKey))
                arguments = parsed[ArgumentsKey];

            if (parsed.Contains(ToastIdKey) && TryGetShownToast(parsed[ToastIdKey], out var shown))
                (title, message) = shown;
        }

        Activated?.Invoke(this, new NotificationActivatedEventArgs(title, message, arguments));
    }

    void RememberShownToast(string toastId, string title, string message)
    {
        lock (_shownToasts)
        {
            _shownToasts[toastId] = (title, message);
            _shownToastOrder.Enqueue(toastId);

            while (_shownToastOrder.Count > RememberedToastCapacity)
                _shownToasts.Remove(_shownToastOrder.Dequeue());
        }
    }

    bool TryGetShownToast(string? toastId, out (string Title, string Message) shown)
    {
        shown = default;
        if (string.IsNullOrEmpty(toastId))
            return false;

        lock (_shownToasts)
        {
            return _shownToasts.TryGetValue(toastId, out shown);
        }
    }

    public void Dispose() =>
        ToastNotificationManagerCompat.OnActivated -= OnActivated;
}
