// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.IO;

namespace Barbatos.Wpf.Storage;

/// <summary>
/// Provides an easy way to access the locations for device folders.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>IFileSystem</c>.</remarks>
public interface IFileSystem
{
    /// <summary>
    /// Gets the location where temporary data can be stored.
    /// </summary>
    /// <remarks>This location usually is not visible to the user, and may be cleared by the operating system at any time.</remarks>
    string CacheDirectory { get; }

    /// <summary>
    /// Gets the location where app data can be stored.
    /// </summary>
    /// <remarks>This location usually is not visible to the user.</remarks>
    string AppDataDirectory { get; }

    /// <summary>
    /// Opens a stream to a file contained within the app's installation directory.
    /// </summary>
    /// <param name="filename">The name of the file (excluding the path) to load from the app's installation directory.</param>
    /// <returns>A <see cref="Stream"/> containing the (read-only) file data.</returns>
    Task<Stream> OpenAppPackageFileAsync(string filename);

    /// <summary>
    /// Determines whether or not a file exists in the app's installation directory.
    /// </summary>
    /// <param name="filename">The name of the file (excluding the path) to load from the app's installation directory.</param>
    /// <returns><see langword="true"/> when the specified file exists, otherwise <see langword="false"/>.</returns>
    Task<bool> AppPackageFileExistsAsync(string filename);
}

/// <summary>
/// Provides an easy way to access the locations for device folders.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>FileSystem</c>.</remarks>
public static class FileSystem
{
    /// <inheritdoc cref="IFileSystem.CacheDirectory" />
    public static string CacheDirectory
        => Current.CacheDirectory;

    /// <inheritdoc cref="IFileSystem.AppDataDirectory" />
    public static string AppDataDirectory
        => Current.AppDataDirectory;

    /// <inheritdoc cref="IFileSystem.OpenAppPackageFileAsync(string)" />
    public static Task<Stream> OpenAppPackageFileAsync(string filename)
        => Current.OpenAppPackageFileAsync(filename);

    /// <inheritdoc cref="IFileSystem.AppPackageFileExistsAsync(string)" />
    public static Task<bool> AppPackageFileExistsAsync(string filename)
        => Current.AppPackageFileExistsAsync(filename);

    static IFileSystem? currentImplementation;

    /// <summary>
    /// Provides the default implementation for static usage of this API.
    /// </summary>
    public static IFileSystem Current =>
        currentImplementation ??= new FileSystemImplementation();

    internal static void SetCurrent(IFileSystem? implementation) =>
        currentImplementation = implementation;
}
