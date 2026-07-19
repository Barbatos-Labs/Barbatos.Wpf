// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

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
