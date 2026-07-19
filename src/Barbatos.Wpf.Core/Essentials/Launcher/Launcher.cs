// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.ApplicationModel;

/// <summary>
/// The Launcher API enables an application to open a URI by the system. This is often used
/// when deep linking into another application's custom URI schemes.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>ILauncher</c>.</remarks>
public interface ILauncher
{
    /// <summary>
    /// Queries if the device supports opening the given URI scheme.
    /// </summary>
    /// <param name="uri">URI scheme to query.</param>
    /// <returns><see langword="true"/> if opening is supported, otherwise <see langword="false"/>.</returns>
    /// <exception cref="UriFormatException">Thrown when <paramref name="uri"/> is malformed.</exception>
    Task<bool> CanOpenAsync(Uri uri);

    /// <summary>
    /// Opens the app specified by the URI scheme.
    /// </summary>
    /// <param name="uri">URI to open.</param>
    /// <returns><see langword="true"/> if the URI was opened, otherwise <see langword="false"/>.</returns>
    /// <exception cref="UriFormatException">Thrown when <paramref name="uri"/> is malformed.</exception>
    Task<bool> OpenAsync(Uri uri);

    /// <summary>
    /// Requests to open a file in an application based on content type.
    /// </summary>
    /// <param name="request">Request that contains information on the file to open.</param>
    /// <returns><see langword="true"/> if the file was opened, otherwise <see langword="false"/>.</returns>
    Task<bool> OpenAsync(OpenFileRequest request);

    /// <summary>
    /// First checks if the provided URI is supported, then opens the app specified by the URI.
    /// </summary>
    /// <param name="uri">URI to try and open.</param>
    /// <returns><see langword="true"/> if the URI was opened, otherwise <see langword="false"/>.</returns>
    /// <exception cref="UriFormatException">Thrown when <paramref name="uri"/> is malformed.</exception>
    Task<bool> TryOpenAsync(Uri uri);
}

/// <summary>
/// The Launcher API enables an application to open a URI by the system. This is often used
/// when deep linking into another application's custom URI schemes.
/// </summary>
/// <remarks>
/// This is the WPF counterpart of .NET MAUI's <c>Launcher</c>. Opening is implemented through
/// <see cref="System.Diagnostics.Process.Start(System.Diagnostics.ProcessStartInfo)"/> with
/// shell execution, the Win32 desktop equivalent of the WinRT <c>Windows.System.Launcher</c>
/// API .NET MAUI itself uses on Windows.
/// </remarks>
public static class Launcher
{
    /// <inheritdoc cref="ILauncher.CanOpenAsync(Uri)" />
    public static Task<bool> CanOpenAsync(string uri) =>
        Default.CanOpenAsync(uri);

    /// <inheritdoc cref="ILauncher.CanOpenAsync(Uri)" />
    public static Task<bool> CanOpenAsync(Uri uri) =>
        Default.CanOpenAsync(uri);

    /// <inheritdoc cref="ILauncher.OpenAsync(Uri)" />
    public static Task<bool> OpenAsync(string uri) =>
        Default.OpenAsync(uri);

    /// <inheritdoc cref="ILauncher.OpenAsync(Uri)" />
    public static Task<bool> OpenAsync(Uri uri) =>
        Default.OpenAsync(uri);

    /// <inheritdoc cref="ILauncher.OpenAsync(OpenFileRequest)" />
    public static Task<bool> OpenAsync(OpenFileRequest request) =>
        Default.OpenAsync(request);

    /// <inheritdoc cref="ILauncher.TryOpenAsync(Uri)" />
    public static Task<bool> TryOpenAsync(string uri) =>
        Default.TryOpenAsync(uri);

    /// <inheritdoc cref="ILauncher.TryOpenAsync(Uri)" />
    public static Task<bool> TryOpenAsync(Uri uri) =>
        Default.TryOpenAsync(uri);

    static ILauncher? defaultImplementation;

    /// <summary>
    /// Provides the default implementation for static usage of this API.
    /// </summary>
    public static ILauncher Default =>
        defaultImplementation ??= new LauncherImplementation();

    internal static void SetDefault(ILauncher? implementation) =>
        defaultImplementation = implementation;
}

/// <summary>
/// Static class with extension methods for the <see cref="ILauncher"/> APIs.
/// </summary>
public static class LauncherExtensions
{
    /// <inheritdoc cref="ILauncher.CanOpenAsync(Uri)" />
    public static Task<bool> CanOpenAsync(this ILauncher launcher, string uri) =>
        launcher.CanOpenAsync(new Uri(uri));

    /// <inheritdoc cref="ILauncher.OpenAsync(Uri)" />
    public static Task<bool> OpenAsync(this ILauncher launcher, string uri) =>
        launcher.OpenAsync(new Uri(uri));

    /// <inheritdoc cref="ILauncher.TryOpenAsync(Uri)" />
    public static Task<bool> TryOpenAsync(this ILauncher launcher, string uri) =>
        launcher.TryOpenAsync(new Uri(uri));
}

/// <summary>
/// Represents a request for opening a file in another application.
/// </summary>
/// <remarks>
/// This is a simplified counterpart of .NET MAUI's <c>OpenFileRequest</c>: it addresses the
/// file directly by its full path, the same simplification <see cref="Communication.EmailAttachment"/>
/// makes, instead of depending on MAUI's <c>ReadOnlyFile</c>/<c>FileBase</c> file-picker subsystem
/// (which this library does not otherwise port).
/// </remarks>
public class OpenFileRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenFileRequest"/> class.
    /// </summary>
    public OpenFileRequest()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenFileRequest"/> class with the given
    /// title and file path.
    /// </summary>
    /// <param name="title">Title to display on the open dialog.</param>
    /// <param name="fullPath">Full path and filename to the file on the filesystem.</param>
    /// <remarks>The title might not always be displayed on every platform.</remarks>
    public OpenFileRequest(string title, string fullPath)
    {
        Title = title;
        FullPath = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
    }

    /// <summary>
    /// Gets or sets the title to display on the open dialog.
    /// </summary>
    /// <remarks>The title might not always be displayed on every platform.</remarks>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the full path and filename of the file to open.
    /// </summary>
    public string? FullPath { get; set; }
}
