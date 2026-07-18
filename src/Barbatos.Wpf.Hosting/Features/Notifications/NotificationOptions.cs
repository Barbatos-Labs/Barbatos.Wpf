// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Notifications;

/// <summary>
/// Options for the push notifications feature. Can be configured from code via
/// <c>ConfigureNotifications</c> and/or from configuration files using the
/// <see cref="SectionName"/> section (file values override code values).
/// </summary>
public class NotificationOptions
{
    /// <summary>
    /// The configuration section the options are bound from.
    /// </summary>
    public const string SectionName = "Barbatos:Notifications";

    /// <summary>
    /// Whether <see cref="INotificationService.Show"/> actually displays notifications.
    /// Defaults to <see langword="true"/>. Useful to let users disable notifications
    /// entirely from a settings screen without changing every call site.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The path of an <c>.ico</c> file used as the notification's identity icon. Defaults
    /// to the icon associated with the application executable.
    /// </summary>
    public string? IconPath { get; set; }

    /// <summary>
    /// A hint for how long the notification stays visible. Modern Windows versions manage
    /// this themselves (via the user's accessibility settings) and may ignore this value.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
}
