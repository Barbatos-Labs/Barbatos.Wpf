// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Barbatos.Wpf.PushNotifications;

/// <summary>
/// The minimum shape <see cref="IPushNotificationService"/> needs to display an incoming push
/// notification. Implement this on your own type (and pass it to
/// <c>ConfigurePushNotifications&lt;TNotification&gt;</c>) when your push server's payload
/// doesn't match <see cref="PushNotification"/>, the default shape.
/// </summary>
public interface IPushNotification
{
    /// <summary>The notification's title.</summary>
    string Title { get; }

    /// <summary>The notification's body text.</summary>
    string Body { get; }

    /// <summary>An absolute URL to an image shown alongside the notification, or <see langword="null"/>.</summary>
    string? ImageUrl { get; }

    /// <summary>What happens when the user interacts with the notification, or <see langword="null"/> for none.</summary>
    PushNotificationAction? Action { get; }
}

/// <summary>
/// Describes what should happen when the user interacts with a push notification.
/// </summary>
public sealed class PushNotificationAction
{
    /// <summary>The kind of action to perform.</summary>
    [JsonPropertyName("actionType")]
    public required PushNotificationActionType ActionType { get; init; }

    /// <summary>
    /// The action's target: a URL for <see cref="PushNotificationActionType.Url"/>/<see cref="PushNotificationActionType.Setting"/>
    /// (e.g. <c>https://...</c> or <c>ms-settings:...</c>), an app-defined route name for
    /// <see cref="PushNotificationActionType.Route"/>, or <see langword="null"/> for <see cref="PushNotificationActionType.None"/>.
    /// </summary>
    [JsonPropertyName("actionTarget")]
    public string? ActionTarget { get; init; }
}

/// <summary>
/// The kind of action a <see cref="PushNotificationAction"/> performs.
/// </summary>
[JsonConverter(typeof(PushNotificationActionTypeJsonConverter))]
public enum PushNotificationActionType
{
    /// <summary>Open the target in the OS's default web browser.</summary>
    Url,

    /// <summary>Navigate to an app-defined internal screen/view named by the target.</summary>
    Route,

    /// <summary>Open the relevant OS settings page.</summary>
    Setting,

    /// <summary>Purely informational; no action on interaction.</summary>
    None,
}

/// <summary>
/// Parses <see cref="PushNotificationActionType"/> case-insensitively, since different push
/// servers may use different casing conventions for their own wire format (e.g. all-caps).
/// </summary>
sealed class PushNotificationActionTypeJsonConverter : JsonConverter<PushNotificationActionType>
{
    public override PushNotificationActionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value is not null && Enum.TryParse<PushNotificationActionType>(value, ignoreCase: true, out var result))
            return result;

        throw new JsonException($"'{value}' is not a recognized push notification action type.");
    }

    public override void Write(Utf8JsonWriter writer, PushNotificationActionType value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
