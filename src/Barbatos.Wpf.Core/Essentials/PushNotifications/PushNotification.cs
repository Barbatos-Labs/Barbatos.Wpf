// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Text.Json.Serialization;

namespace Barbatos.Wpf.PushNotifications;

/// <summary>
/// The default push notification payload shape. Used automatically by
/// <c>ConfigurePushNotifications()</c>. If your push server sends a different JSON shape,
/// implement <see cref="IPushNotification"/> on your own type instead and register it with
/// <c>ConfigurePushNotifications&lt;TNotification&gt;()</c>.
/// </summary>
public sealed class PushNotification : IPushNotification
{
    /// <summary>The notification's primary key on the server.</summary>
    [JsonPropertyName("notificationId")]
    public long NotificationId { get; init; }

    /// <summary>The target application's identifier.</summary>
    [JsonPropertyName("appId")]
    public required string AppId { get; init; }

    /// <summary>A dedup key identifying the event this notification represents.</summary>
    [JsonPropertyName("eventKey")]
    public string? EventKey { get; init; }

    /// <inheritdoc />
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <inheritdoc />
    [JsonPropertyName("message")]
    public required string Body { get; init; }

    /// <inheritdoc />
    [JsonPropertyName("image")]
    public string? ImageUrl { get; init; }

    /// <summary>When this notification becomes valid, or <see langword="null"/> if immediate.</summary>
    [JsonPropertyName("scheduledFor")]
    public DateTimeOffset? ScheduledFor { get; init; }

    /// <summary>When this notification stops being valid, or <see langword="null"/> if it never expires.</summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <inheritdoc />
    [JsonPropertyName("action")]
    public PushNotificationAction? Action { get; init; }

    /// <summary>
    /// Free-form extra data (e.g. the sending account's email, plan tier, ...), always present as
    /// an extensibility point.
    /// </summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
