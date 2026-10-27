// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Diagnostics;
using Microsoft.Win32;

namespace Barbatos.Wpf.ApplicationModel;

/// <summary>
/// The WPF implementation of <see cref="ILauncher"/>, ported from .NET MAUI's Windows
/// <c>LauncherImplementation</c> using <see cref="Process.Start(ProcessStartInfo)"/> with shell
/// execution instead of the WinRT <c>Windows.System.Launcher</c> API.
/// </summary>
class LauncherImplementation : ILauncher
{
    public Task<bool> CanOpenAsync(Uri uri)
    {
        if (uri == null)
            throw new ArgumentNullException(nameof(uri));

        return Task.FromResult(PlatformCanOpen(uri));
    }

    public Task<bool> OpenAsync(Uri uri)
    {
        if (uri == null)
            throw new ArgumentNullException(nameof(uri));

        return Task.FromResult(PlatformOpen(uri.OriginalString));
    }

    public Task<bool> OpenAsync(OpenFileRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrEmpty(request.FullPath))
            throw new ArgumentNullException(nameof(request.FullPath));

        return Task.FromResult(PlatformOpen(request.FullPath));
    }

    public async Task<bool> TryOpenAsync(Uri uri)
    {
        if (uri == null)
            throw new ArgumentNullException(nameof(uri));

        var canOpen = await CanOpenAsync(uri).ConfigureAwait(false);

        if (canOpen)
            return PlatformOpen(uri.OriginalString);

        return canOpen;
    }

    /// <summary>
    /// Hands <paramref name="target"/> to the shell; <see langword="true"/> unless the shell
    /// refused it.
    /// </summary>
    /// <remarks>
    /// Success is "<see cref="Process.Start(ProcessStartInfo)"/> did not throw", not "a
    /// <see cref="Process"/> came back": with shell execution, <c>Process.Start</c> returns
    /// <see langword="null"/> whenever the shell satisfies the request without starting a new
    /// process - a DDE hand-off to an application that is already running, or a COM-activated
    /// handler such as a packaged app. The target did open in those cases, and reporting
    /// <see langword="false"/> made callers such as the MCP browser sign-in treat a working
    /// launch as a failure. A real refusal (no association, a missing file) surfaces as a
    /// <see cref="System.ComponentModel.Win32Exception"/> and still yields
    /// <see langword="false"/>.
    /// </remarks>
    static bool PlatformOpen(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true })?.Dispose();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks whether Windows has a registered handler for the given URI's scheme, by looking
    /// it up under <c>HKEY_CLASSES_ROOT</c> the same way Explorer resolves a custom protocol —
    /// the closest Win32/registry equivalent of WinRT's <c>Launcher.QueryUriSupportAsync</c>.
    /// </summary>
    static bool PlatformCanOpen(Uri uri)
    {
        try
        {
            using var key = Registry.ClassesRoot.OpenSubKey(uri.Scheme);
            if (key is null)
                return false;

            // A registered custom protocol handler declares a "URL Protocol" value at its
            // scheme key; a regular file-type/ProgID association instead has a
            // shell\open\command subkey. Either one means something can handle the URI.
            return key.GetValue("URL Protocol") != null || key.OpenSubKey(@"shell\open\command") != null;
        }
        catch
        {
            return false;
        }
    }
}
