// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.IO;
using System.Runtime.InteropServices;

namespace Barbatos.Wpf.ApplicationModel.Communication;

/// <summary>
/// Composes a new email through Simple MAPI (<c>MAPISendMailW</c>), ported from .NET MAUI's
/// Windows <c>EmailHelper</c> — this was already pure Win32 interop, not WinRT, so no
/// substitution was needed.
/// </summary>
/// <remarks>
/// Two deliberate departures from the MAUI original. It calls the Unicode entry point
/// <c>MAPISendMailW</c> with the <c>MapiMessageW</c>/<c>MapiRecipDescW</c>/<c>MapiFileDescW</c>
/// layouts instead of the ANSI <c>MAPISendMail</c>: the ANSI structures squeeze the subject,
/// body, recipients and attachment paths through the system code page, so text outside it
/// (Vietnamese on an English-locale machine, for one) reached the client as <c>?</c>.
/// <c>MAPISendMailW</c> has been in the Windows MAPI stub since Windows 8, below the
/// Windows 10 1809 floor this package targets, and for a client that implements only the ANSI
/// call the stub converts the strings itself - no worse than calling the ANSI entry point
/// directly. And the MAPI result is no longer discarded: MAUI's version completed normally
/// even when no mail client was installed, so <see cref="ThrowOnFailure"/> turns every failure
/// into an exception, while a user who closes the compose window remains a normal completion.
/// </remarks>
static class EmailHelper
{
    // Simple MAPI result codes, from MAPI.h.
    const int SUCCESS_SUCCESS = 0;
    const int MAPI_USER_ABORT = 1;
    const int MAPI_E_FAILURE = 2;
    const int MAPI_E_LOGIN_FAILURE = 3;
    const int MAPI_E_ATTACHMENT_NOT_FOUND = 11;
    const int MAPI_E_NOT_SUPPORTED = 26;
    const int MAPI_E_UNICODE_NOT_SUPPORTED = 27;

    public static Task ShowComposeNewEmailAsync(PlatformEmailMessage message) =>
        Task.Run(() => ThrowOnFailure(SendMail(message)));

    /// <summary>
    /// Maps a Simple MAPI result to the exception <see cref="IEmail.ComposeAsync"/> documents.
    /// </summary>
    /// <remarks>
    /// The codes that mean "no usable mail client answered" - the generic failure, a logon
    /// failure and the two not-supported codes - become <see cref="FeatureNotSupportedException"/>,
    /// the type Essentials already uses for "this machine cannot do that", so one <c>catch</c>
    /// covers the <c>mailto:</c> fallback. Which of them a machine without a client returns
    /// depends on the Windows build and on what is registered as the default client, so they are
    /// grouped instead of being told apart.
    /// </remarks>
    static void ThrowOnFailure(int result)
    {
        switch (result)
        {
            case SUCCESS_SUCCESS:
            case MAPI_USER_ABORT:
                return;

            case MAPI_E_FAILURE:
            case MAPI_E_LOGIN_FAILURE:
            case MAPI_E_NOT_SUPPORTED:
            case MAPI_E_UNICODE_NOT_SUPPORTED:
                throw new FeatureNotSupportedException(
                    $"No Simple MAPI email client could compose the message (MAPI error {result}). " +
                    "Install a desktop email client and make it the default, or offer a mailto: link instead.");

            case MAPI_E_ATTACHMENT_NOT_FOUND:
                throw new FileNotFoundException("The email client could not find one of the attachments.");

            default:
                throw new InvalidOperationException($"Simple MAPI could not compose the message (MAPI error {result}).");
        }
    }

    static int SendMail(PlatformEmailMessage message)
    {
        var flags = SendMailFlags.MAPI_LOGON_UI | SendMailFlags.MAPI_DIALOG_MODELESS | SendMailFlags.MAPI_DIALOG;

        var recipients = GetRecipients(message);
        var attachments = GetAttachments(message);

        var msg = new MapiMessageW
        {
            subject = message.Subject,
            noteText = message.Body,
            recipCount = recipients?.Length ?? 0,
            recips = GetUnmanagedArray(recipients),
            fileCount = attachments?.Length ?? 0,
            files = GetUnmanagedArray(attachments),
        };

        try
        {
            return NativeMethods.MAPISendMailW(IntPtr.Zero, IntPtr.Zero, ref msg, flags, 0);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            // No Simple MAPI stub at all (a stripped-down Windows image): to the caller that is
            // the same situation as "no mail client installed".
            throw new FeatureNotSupportedException("Simple MAPI (MAPI32.DLL) is not available on this machine.", ex);
        }
        finally
        {
            DestroyUnmanagedArray(msg.recips, recipients);
            DestroyUnmanagedArray(msg.files, attachments);
        }
    }

    static IntPtr GetUnmanagedArray<TStruct>(TStruct[]? array)
        where TStruct : struct
    {
        if (array?.Length > 0)
        {
            var size = Marshal.SizeOf<TStruct>();

            var intptr = Marshal.AllocHGlobal(array.Length * size);

            var ptr = intptr;
            foreach (var item in array)
            {
                Marshal.StructureToPtr(item, ptr, false);
                ptr += size;
            }

            return intptr;
        }

        return IntPtr.Zero;
    }

    static void DestroyUnmanagedArray<TStruct>(IntPtr intptr, TStruct[]? array)
    {
        var count = array?.Length;
        if (count > 0)
        {
            var size = Marshal.SizeOf<TStruct>();

            var ptr = intptr;
            for (var i = 0; i < count; i++)
            {
                Marshal.DestroyStructure<TStruct>(ptr);
                ptr += size;
            }

            Marshal.FreeHGlobal(intptr);
        }
    }

    static MapiRecipDescW[]? GetRecipients(PlatformEmailMessage message)
    {
        var recipCount = message.To.Count + message.CC.Count + message.Bcc.Count;

        if (recipCount == 0)
            return null;

        var recipients = new MapiRecipDescW[recipCount];

        var idx = 0;
        foreach (var to in message.To)
            recipients[idx++] = Create(to, RecipientClass.MAPI_TO);
        foreach (var cc in message.CC)
            recipients[idx++] = Create(cc, RecipientClass.MAPI_CC);
        foreach (var bcc in message.Bcc)
            recipients[idx++] = Create(bcc, RecipientClass.MAPI_BCC);

        return recipients;

        static MapiRecipDescW Create(PlatformEmailRecipient recipient, RecipientClass type)
        {
            return new MapiRecipDescW
            {
                name = recipient.Address,
                recipClass = type,
            };
        }
    }

    static MapiFileDescW[]? GetAttachments(PlatformEmailMessage message)
    {
        var attachCount = message.Attachments.Count;

        if (attachCount == 0)
            return null;

        var attachments = new MapiFileDescW[attachCount];

        var idx = 0;
        foreach (var file in message.Attachments)
            attachments[idx++] = Create(file);

        return attachments;

        static MapiFileDescW Create(string filename)
        {
            return new MapiFileDescW
            {
                name = Path.GetFileName(filename),
                path = filename,
                position = -1,
            };
        }
    }

    static class NativeMethods
    {
        [DllImport("MAPI32.DLL", ExactSpelling = true)]
        internal static extern int MAPISendMailW(IntPtr sess, IntPtr hwnd, [In] ref MapiMessageW message, SendMailFlags flg, int rsv);
    }

    // MAPI.h's MapiMessageW, MapiFileDescW and MapiRecipDescW: field for field the same layout
    // as the ANSI structures, with every string a PWSTR - hence CharSet.Unicode.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct MapiMessageW
    {
        private int reserved;
        public string? subject;
        public string? noteText;
        public string? messageType;
        public string? dateReceived;
        public string? conversationID;
        public int flags;
        public IntPtr originator;
        public int recipCount;
        public IntPtr recips;
        public int fileCount;
        public IntPtr files;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct MapiFileDescW
    {
        private int reserved;
        public int flags;
        public int position;
        public string? path;
        public string? name;
        public IntPtr type;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct MapiRecipDescW
    {
        private int reserved;
        public RecipientClass recipClass;
        public string? name;
        public string? address;
        public int eIDSize;
        public IntPtr entryID;
    }

    enum RecipientClass
    {
        MAPI_ORIG = 0,
        MAPI_TO,
        MAPI_CC,
        MAPI_BCC,
    }

    [Flags]
    enum SendMailFlags
    {
        MAPI_LOGON_UI = 0x00000001,
        MAPI_DIALOG_MODELESS = 0x00000004,
        MAPI_DIALOG = 0x00000008,
    }
}

/// <summary>
/// The Simple MAPI representation of an <see cref="EmailMessage"/> being composed.
/// </summary>
class PlatformEmailMessage
{
    public string? Body { get; set; }

    public string? Subject { get; set; }

    public List<PlatformEmailRecipient> To { get; } = [];

    public List<PlatformEmailRecipient> CC { get; } = [];

    public List<PlatformEmailRecipient> Bcc { get; } = [];

    public List<string> Attachments { get; } = [];
}

/// <summary>
/// The Simple MAPI representation of an email recipient address.
/// </summary>
class PlatformEmailRecipient
{
    public PlatformEmailRecipient(string address)
    {
        Address = address;
    }

    public string Address { get; set; }
}
