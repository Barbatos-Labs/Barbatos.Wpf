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
    void Show(NotificationOptions options, NotificationContent content);

    /// <summary>
    /// Occurs when the user clicks a notification (or one of its buttons) shown through this
    /// platform.
    /// </summary>
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

    string _lastTitle = string.Empty;
    string _lastMessage = string.Empty;

    public ToastNotificationPlatform() =>
        ToastNotificationManagerCompat.OnActivated += OnActivated;

    public event EventHandler<NotificationActivatedEventArgs>? Activated;

    public void Show(NotificationOptions options, NotificationContent content)
    {
        _lastTitle = content.Title;
        _lastMessage = content.Message;

        var builder = new ToastContentBuilder()
            .AddText(content.Title)
            .AddText(content.Message)
            .SetToastScenario(content.Severity == NotificationSeverity.Error ? ToastScenario.Alarm : ToastScenario.Default)
            .SetToastDuration(options.Timeout > TimeSpan.FromSeconds(7) ? ToastDuration.Long : ToastDuration.Short);

        if (!string.IsNullOrWhiteSpace(options.IconPath))
            builder.AddAppLogoOverride(new Uri(Path.GetFullPath(options.IconPath)), ToastGenericAppLogoCrop.Circle);

        if (!string.IsNullOrWhiteSpace(content.ImagePath))
            builder.AddHeroImage(new Uri(Path.GetFullPath(content.ImagePath)));

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
        if (!string.IsNullOrEmpty(e.Argument))
        {
            var parsed = ToastArguments.Parse(e.Argument);
            if (parsed.Contains(ArgumentsKey))
                arguments = parsed[ArgumentsKey];
        }

        Activated?.Invoke(this, new NotificationActivatedEventArgs(_lastTitle, _lastMessage, arguments));
    }

    public void Dispose() =>
        ToastNotificationManagerCompat.OnActivated -= OnActivated;
}
