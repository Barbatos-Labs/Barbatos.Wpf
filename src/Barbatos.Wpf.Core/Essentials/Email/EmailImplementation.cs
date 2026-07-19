// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.ApplicationModel;

namespace Barbatos.Wpf.ApplicationModel.Communication;

/// <summary>
/// The WPF implementation of <see cref="IEmail"/>, ported from .NET MAUI's Windows
/// <c>EmailImplementation</c>. Composing itself (<see cref="EmailHelper"/>) uses Simple MAPI,
/// which is pure Win32 (not WinRT), so it is ported almost verbatim.
/// </summary>
class EmailImplementation : IEmail
{
    public bool IsComposeSupported => true;

    public Task ComposeAsync(EmailMessage? message)
    {
        if (!IsComposeSupported)
            throw new FeatureNotSupportedException();

        return PlatformComposeAsync(message);
    }

    async Task PlatformComposeAsync(EmailMessage? message)
    {
        if (message != null && message.BodyFormat != EmailBodyFormat.PlainText)
            throw new FeatureNotSupportedException("Windows can only compose plain text email messages.");

        var platformEmailMessage = new PlatformEmailMessage();

        if (!string.IsNullOrEmpty(message?.Body))
            platformEmailMessage.Body = message.Body;

        if (!string.IsNullOrEmpty(message?.Subject))
            platformEmailMessage.Subject = message.Subject;

        Sync(message?.To, platformEmailMessage.To);
        Sync(message?.Cc, platformEmailMessage.CC);
        Sync(message?.Bcc, platformEmailMessage.Bcc);

        if (message?.Attachments?.Count > 0)
        {
            foreach (var attachment in message.Attachments)
                platformEmailMessage.Attachments.Add(attachment.FullPath);
        }

        await EmailHelper.ShowComposeNewEmailAsync(platformEmailMessage);
    }

    static void Sync(List<string>? recipients, IList<PlatformEmailRecipient> nativeRecipients)
    {
        if (recipients == null)
            return;

        foreach (var recipient in recipients)
        {
            if (string.IsNullOrWhiteSpace(recipient))
                continue;

            nativeRecipients.Add(new PlatformEmailRecipient(recipient));
        }
    }
}
