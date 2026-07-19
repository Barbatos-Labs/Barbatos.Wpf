// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.IO;
using Barbatos.Wpf.Storage;

namespace Barbatos.Wpf.Core.UnitTests;

public class FileSystemTests
{
    [Fact]
    public void CurrentIsCached()
    {
        Assert.NotNull(FileSystem.Current);
        Assert.Same(FileSystem.Current, FileSystem.Current);
    }

    [Fact]
    public void AppDataDirectoryExistsAndIsUnderLocalAppData()
    {
        Assert.True(Directory.Exists(FileSystem.AppDataDirectory));

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        Assert.StartsWith(localAppData, FileSystem.AppDataDirectory, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CacheDirectoryExistsAndDiffersFromAppDataDirectory()
    {
        Assert.True(Directory.Exists(FileSystem.CacheDirectory));
        Assert.NotEqual(FileSystem.AppDataDirectory, FileSystem.CacheDirectory);
    }

    [Fact]
    public async Task AppPackageFileExistsAsyncReflectsThePresenceOfDeploymentFiles()
    {
        // The test host's own assembly is deployed next to the executable, so it always exists.
        var assemblyFileName = typeof(FileSystemTests).Assembly.GetName().Name + ".dll";

        Assert.True(await FileSystem.AppPackageFileExistsAsync(assemblyFileName));
        Assert.False(await FileSystem.AppPackageFileExistsAsync("this-file-does-not-exist.xyz"));
    }

    [Fact]
    public async Task OpenAppPackageFileAsyncOpensAnExistingFile()
    {
        var assemblyFileName = typeof(FileSystemTests).Assembly.GetName().Name + ".dll";

        await using var stream = await FileSystem.OpenAppPackageFileAsync(assemblyFileName);

        Assert.True(stream.CanRead);
        Assert.True(stream.Length > 0);
    }
}
