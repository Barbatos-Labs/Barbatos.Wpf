// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.IO;
using Barbatos.Wpf.ApplicationModel;

namespace Barbatos.Wpf.Storage;

/// <summary>
/// The WPF implementation of <see cref="IFileSystem"/>, ported from .NET MAUI's Windows
/// <c>FileSystemImplementation</c>'s unpackaged code path (no MSIX package identity).
/// </summary>
class FileSystemImplementation : IFileSystem
{
    readonly Lazy<string> _cacheDirectory = new(() => EnsureDirectory(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppSpecificPath, "Cache")));

    readonly Lazy<string> _appDataDirectory = new(() => EnsureDirectory(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppSpecificPath, "Data")));

    public string CacheDirectory => _cacheDirectory.Value;

    public string AppDataDirectory => _appDataDirectory.Value;

    public Task<Stream> OpenAppPackageFileAsync(string filename)
    {
        _ = filename ?? throw new ArgumentNullException(nameof(filename));

        var file = Path.Combine(AppContext.BaseDirectory, filename);
        return Task.FromResult((Stream)File.OpenRead(file));
    }

    public Task<bool> AppPackageFileExistsAsync(string filename)
    {
        var file = Path.Combine(AppContext.BaseDirectory, filename);
        return Task.FromResult(File.Exists(file));
    }

    static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }

    static string CleanPath(string path) =>
        string.Join("_", path.Split(Path.GetInvalidFileNameChars()));

    static string AppSpecificPath =>
        Path.Combine(CleanPath(PublisherInfo.Name), CleanPath(AppInfo.AppGuid));
}
