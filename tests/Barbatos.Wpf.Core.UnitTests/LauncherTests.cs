// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

public class LauncherTests
{
    [Fact]
    public void DefaultIsCached()
    {
        Assert.NotNull(Launcher.Default);
        Assert.Same(Launcher.Default, Launcher.Default);
    }

    [Fact]
    public async Task CanOpenAsyncThrowsForNullUri()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Launcher.Default.CanOpenAsync((Uri)null!));
    }

    [Fact]
    public async Task OpenAsyncThrowsForNullUri()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Launcher.Default.OpenAsync((Uri)null!));
    }

    [Fact]
    public async Task TryOpenAsyncThrowsForNullUri()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Launcher.Default.TryOpenAsync((Uri)null!));
    }

    [Fact]
    public async Task OpenAsyncThrowsForNullOpenFileRequest()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Launcher.Default.OpenAsync((OpenFileRequest)null!));
    }

    [Fact]
    public async Task OpenAsyncThrowsForOpenFileRequestWithoutAFullPath()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Launcher.Default.OpenAsync(new OpenFileRequest()));
    }

    [Fact]
    public async Task CanOpenAsyncReturnsFalseForAnUnregisteredScheme()
    {
        // No installed application registers this made-up scheme, so the registry lookup
        // in LauncherImplementation must come back empty.
        var canOpen = await Launcher.CanOpenAsync("barbatos-wpf-launcher-tests-unregistered-scheme://ping");

        Assert.False(canOpen);
    }

    [Fact]
    public void OpenFileRequestConstructorSetsTitleAndFullPath()
    {
        var request = new OpenFileRequest("Title", @"C:\temp\file.txt");

        Assert.Equal("Title", request.Title);
        Assert.Equal(@"C:\temp\file.txt", request.FullPath);
    }

    [Fact]
    public void OpenFileRequestConstructorThrowsForNullFullPath()
    {
        Assert.Throws<ArgumentNullException>(() => new OpenFileRequest("Title", null!));
    }

    [Fact]
    public void LauncherIsRegisteredInTheDefaultBuilder()
    {
        var wpfApp = WpfApp.CreateBuilder().Build();

        var launcher = wpfApp.Services.GetService<ILauncher>();

        Assert.NotNull(launcher);
        Assert.Same(Launcher.Default, launcher);
    }

    [Fact]
    public void LauncherIsNotRegisteredWithoutDefaults()
    {
        var wpfApp = WpfApp.CreateBuilder(useDefaults: false).Build();

        Assert.Null(wpfApp.Services.GetService<ILauncher>());
    }
}
