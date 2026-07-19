// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Drawing;
using System.Windows.Forms;

namespace Barbatos.Wpf.Notifications;

/// <summary>
/// Abstraction over the OS mechanism used to push a desktop notification.
/// </summary>
public interface INotificationPlatform
{
    void Show(NotificationOptions options, string title, string message, NotificationSeverity severity);

    /// <summary>
    /// Occurs when the user clicks a notification shown through this platform.
    /// </summary>
    event EventHandler<NotificationActivatedEventArgs>? Activated;
}

/// <summary>
/// The default <see cref="INotificationPlatform"/> that uses
/// <see cref="System.Windows.Forms.NotifyIcon.ShowBalloonTip(int, string, string, ToolTipIcon)"/>.
/// On Windows 10/11 the shell renders this as a modern toast notification (also appearing
/// in the notification center), attributed to the notification's identity icon.
/// </summary>
internal sealed class Win32NotificationPlatform : INotificationPlatform, IDisposable
{
    NotifyIcon? _notifyIcon;
    string _lastTitle = string.Empty;
    string _lastMessage = string.Empty;

    public event EventHandler<NotificationActivatedEventArgs>? Activated;

    public void Show(NotificationOptions options, string title, string message, NotificationSeverity severity)
    {
        var notifyIcon = _notifyIcon ??= CreateNotifyIcon(options);

        _lastTitle = title;
        _lastMessage = message;

        // The notify icon must be visible for the shell to attribute (and display) the
        // balloon/toast to this application.
        notifyIcon.Visible = true;
        notifyIcon.ShowBalloonTip((int)options.Timeout.TotalMilliseconds, title, message, ToToolTipIcon(severity));
    }

    NotifyIcon CreateNotifyIcon(NotificationOptions options)
    {
        var notifyIcon = new NotifyIcon
        {
            Icon = LoadIcon(options),
        };

        notifyIcon.BalloonTipClicked += (sender, args) =>
            Activated?.Invoke(this, new NotificationActivatedEventArgs(_lastTitle, _lastMessage));

        return notifyIcon;
    }

    static Icon LoadIcon(NotificationOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.IconPath))
            return new Icon(options.IconPath);

        if (Environment.ProcessPath is string processPath &&
            Icon.ExtractAssociatedIcon(processPath) is Icon associated)
            return associated;

        return SystemIcons.Application;
    }

    static ToolTipIcon ToToolTipIcon(NotificationSeverity severity) => severity switch
    {
        NotificationSeverity.Info => ToolTipIcon.Info,
        NotificationSeverity.Warning => ToolTipIcon.Warning,
        NotificationSeverity.Error => ToolTipIcon.Error,
        _ => ToolTipIcon.None,
    };

    public void Dispose()
    {
        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }
}
