// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Hosting Contributors.
// All Rights Reserved.

using System.IO;
using Microsoft.Extensions.Hosting;

namespace Barbatos.Wpf.Hosting.UnitTests;

public class WpfHostEnvironmentTests
{
    [Fact]
    public void EnvironmentNameDefaultsToProduction()
    {
        var environment = new WpfHostEnvironment();

        var expected = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? Environments.Production;

        Assert.Equal(expected, environment.EnvironmentName);
    }

    [Fact]
    public void ApplicationNameIsTheEntryAssembly()
    {
        var environment = new WpfHostEnvironment();

        Assert.False(string.IsNullOrEmpty(environment.ApplicationName));
    }

    [Fact]
    public void ContentRootPathIsTheBaseDirectory()
    {
        var environment = new WpfHostEnvironment();

        Assert.Equal(AppContext.BaseDirectory, environment.ContentRootPath);
        Assert.True(Directory.Exists(environment.ContentRootPath));
    }

    [Fact]
    public void ContentRootFileProviderIsAvailable()
    {
        var environment = new WpfHostEnvironment();

        Assert.NotNull(environment.ContentRootFileProvider);
    }

    [Fact]
    public void SettersAreNotSupported()
    {
        var environment = new WpfHostEnvironment();

        Assert.Throws<NotSupportedException>(() => environment.EnvironmentName = "x");
        Assert.Throws<NotSupportedException>(() => environment.ApplicationName = "x");
        Assert.Throws<NotSupportedException>(() => environment.ContentRootPath = "x");
        Assert.Throws<NotSupportedException>(() => environment.ContentRootFileProvider = null!);
    }
}
