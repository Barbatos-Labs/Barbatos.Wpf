// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.IO;
using Barbatos.Wpf.ApplicationModel;
using Barbatos.Wpf.ApplicationModel.Communication;

namespace Barbatos.Wpf.Core.UnitTests;

public class EmailTests
{
    [Fact]
    public void DefaultIsCached()
    {
        Assert.NotNull(Email.Default);
        Assert.Same(Email.Default, Email.Default);
    }

    [Fact]
    public void IsComposeSupportedIsTrueOnWindows()
    {
        Assert.True(Email.Default.IsComposeSupported);
    }

    [Fact]
    public async Task ComposeAsyncThrowsForHtmlBody()
    {
        var message = new EmailMessage("Subject", "Body") { BodyFormat = EmailBodyFormat.Html };

        await Assert.ThrowsAsync<FeatureNotSupportedException>(() => Email.ComposeAsync(message));
    }

    // Before failures were surfaced, a missing attachment still went to Simple MAPI and the
    // client's MAPI_E_ATTACHMENT_NOT_FOUND was discarded, so the call reported success. The
    // check now runs before any mail client is started - which is also what keeps this test
    // from opening a compose window or a "no email program" prompt on the machine running it.
    // The timeout turns the old behaviour (a call into MAPI that can sit behind such a prompt)
    // into a failure instead of a hung run.
    [Fact]
    public async Task ComposeAsyncThrowsFileNotFoundForAMissingAttachmentBeforeAnyClientStarts()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"Barbatos.Wpf.EmailTests.{Guid.NewGuid():N}.txt");
        var message = new EmailMessage("Subject", "Body") { Attachments = [new EmailAttachment(missingPath)] };

        var exception = await Assert.ThrowsAsync<FileNotFoundException>(
            () => Email.ComposeAsync(message).WaitAsync(TimeSpan.FromSeconds(30)));

        Assert.Equal(missingPath, exception.FileName);
    }

    [Fact]
    public void MessageConstructorDefaultsAreEmptyLists()
    {
        var message = new EmailMessage();

        Assert.NotNull(message.To);
        Assert.Empty(message.To!);
        Assert.NotNull(message.Cc);
        Assert.NotNull(message.Bcc);
        Assert.NotNull(message.Attachments);
        Assert.Equal(EmailBodyFormat.PlainText, message.BodyFormat);
    }

    [Fact]
    public void MessageConstructorSetsSubjectBodyAndRecipients()
    {
        var message = new EmailMessage("Hi", "Body text", "a@example.com", "b@example.com");

        Assert.Equal("Hi", message.Subject);
        Assert.Equal("Body text", message.Body);
        Assert.Equal(["a@example.com", "b@example.com"], message.To);
    }

    [Fact]
    public void AttachmentThrowsForNullPath()
    {
        Assert.Throws<ArgumentNullException>(() => new EmailAttachment(null!));
    }

    [Fact]
    public void AttachmentExposesTheFullPath()
    {
        var attachment = new EmailAttachment(@"C:\temp\file.txt");

        Assert.Equal(@"C:\temp\file.txt", attachment.FullPath);
    }
}
