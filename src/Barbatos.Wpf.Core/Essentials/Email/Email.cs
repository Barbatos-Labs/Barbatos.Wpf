// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Linq;

namespace Barbatos.Wpf.ApplicationModel.Communication;

/// <summary>
/// Provides an easy way to allow the user to send emails.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>IEmail</c>.</remarks>
public interface IEmail
{
    /// <summary>
    /// Gets a value indicating whether composing an email is supported on this device.
    /// </summary>
    bool IsComposeSupported { get; }

    /// <summary>
    /// Opens the default email client to allow the user to send the message.
    /// </summary>
    /// <param name="message">Instance of <see cref="EmailMessage"/> containing details of the email message to compose.</param>
    /// <returns>A <see cref="Task"/> object with the current status of the asynchronous operation.</returns>
    Task ComposeAsync(EmailMessage? message);
}

/// <summary>
/// Static class with extension methods for the <see cref="IEmail"/> APIs.
/// </summary>
public static class EmailExtensions
{
    /// <inheritdoc cref="IEmail.ComposeAsync(EmailMessage)" />
    public static Task ComposeAsync(this IEmail email) =>
        email.ComposeAsync(null);

    /// <summary>
    /// Opens the default email client to allow the user to send the message with the provided subject, body, and recipients.
    /// </summary>
    /// <param name="email">The object this method is invoked on.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="body">The email body.</param>
    /// <param name="to">The email recipients.</param>
    /// <returns>A <see cref="Task"/> object with the current status of the asynchronous operation.</returns>
    public static Task ComposeAsync(this IEmail email, string subject, string body, params string[] to) =>
        email.ComposeAsync(new EmailMessage(subject, body, to));
}

/// <summary>
/// Provides an easy way to allow the user to send emails.
/// </summary>
/// <remarks>
/// This is the WPF counterpart of .NET MAUI's <c>Email</c>. Composing is implemented through
/// the classic Win32 Simple MAPI (<c>MAPISendMail</c>), which opens the user's default
/// desktop email client (Outlook, etc.) — the same mechanism .NET MAUI itself uses on Windows.
/// </remarks>
public static class Email
{
    /// <summary>
    /// Opens the default email client to allow the user to send the message.
    /// </summary>
    /// <returns>A <see cref="Task"/> object with the current status of the asynchronous operation.</returns>
    public static Task ComposeAsync() =>
        Default.ComposeAsync();

    /// <summary>
    /// Opens the default email client to allow the user to send the message with the provided subject, body, and recipients.
    /// </summary>
    /// <param name="subject">The email subject.</param>
    /// <param name="body">The email body.</param>
    /// <param name="to">The email recipients.</param>
    /// <returns>A <see cref="Task"/> object with the current status of the asynchronous operation.</returns>
    public static Task ComposeAsync(string subject, string body, params string[] to) =>
        Default.ComposeAsync(subject, body, to);

    /// <inheritdoc cref="IEmail.ComposeAsync(EmailMessage)" />
    public static Task ComposeAsync(EmailMessage message) =>
        Default.ComposeAsync(message);

    static IEmail? defaultImplementation;

    /// <summary>
    /// Provides the default implementation for static usage of this API.
    /// </summary>
    public static IEmail Default =>
        defaultImplementation ??= new EmailImplementation();

    internal static void SetDefault(IEmail? implementation) =>
        defaultImplementation = implementation;
}

/// <summary>
/// Represents a single email message.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>EmailMessage</c>.</remarks>
public class EmailMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmailMessage"/> class.
    /// </summary>
    public EmailMessage()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailMessage"/> class with the specified subject, body, and recipients.
    /// </summary>
    /// <param name="subject">The email subject.</param>
    /// <param name="body">The email body.</param>
    /// <param name="to">The email recipients.</param>
    public EmailMessage(string subject, string body, params string[] to)
    {
        Subject = subject;
        Body = body;
        To = to?.ToList() ?? [];
    }

    /// <summary>
    /// Gets or sets the email's subject.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the email's body.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the message is in plain text or HTML.
    /// </summary>
    /// <remarks><see cref="EmailBodyFormat.Html"/> is not supported on Windows (Simple MAPI is plain-text only).</remarks>
    public EmailBodyFormat BodyFormat { get; set; }

    /// <summary>
    /// Gets or sets the email's recipients.
    /// </summary>
    public List<string>? To { get; set; } = [];

    /// <summary>
    /// Gets or sets the email's CC (Carbon Copy) recipients.
    /// </summary>
    public List<string>? Cc { get; set; } = [];

    /// <summary>
    /// Gets or sets the email's BCC (Blind Carbon Copy) recipients.
    /// </summary>
    public List<string>? Bcc { get; set; } = [];

    /// <summary>
    /// Gets or sets a list of file attachments as <see cref="EmailAttachment"/> objects.
    /// </summary>
    public List<EmailAttachment>? Attachments { get; set; } = [];
}

/// <summary>
/// Represents various types of email body formats.
/// </summary>
public enum EmailBodyFormat
{
    /// <summary>The email message body is plain text.</summary>
    PlainText,

    /// <summary>The email message body is HTML (not supported on Windows).</summary>
    Html,
}

/// <summary>
/// Represents an email file attachment.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>EmailAttachment</c>.</remarks>
public class EmailAttachment
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmailAttachment"/> class based off the file specified in the provided path.
    /// </summary>
    /// <param name="fullPath">Full path and filename to file on filesystem.</param>
    public EmailAttachment(string fullPath)
    {
        FullPath = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
    }

    /// <summary>
    /// Gets the full path and filename of the attachment.
    /// </summary>
    public string FullPath { get; }
}
