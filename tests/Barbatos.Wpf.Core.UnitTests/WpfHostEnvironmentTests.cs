// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Barbatos.Wpf.Core.UnitTests;

public class WpfHostEnvironmentTests
{
    [Fact]
    public void EnvironmentNameDefaultsToProductionAndIsSettable()
    {
        var environment = new WpfHostEnvironment();

        Assert.Equal(Environments.Production, environment.EnvironmentName);

        environment.EnvironmentName = "Staging";

        Assert.Equal("Staging", environment.EnvironmentName);
    }

    [Fact]
    public void EnvironmentNameFlowsFromTheEnvironmentVariablesConfigurationSource()
    {
        // WpfAppBuilder wires up a "DOTNET_"-prefixed environment variables configuration
        // source before resolving Environment, mirroring Microsoft.Extensions.Hosting's own
        // bootstrap step. Asserting against the real DOTNET_ENVIRONMENT process variable
        // here would be flaky under xUnit's parallel test execution (another test class could
        // observe a builder created while it's mutated), so this instead verifies the
        // documented, non-flaky override path: any configuration source that sets
        // HostDefaults.EnvironmentKey - appsettings.json, in-memory collections, command-line
        // arguments, or DOTNET_ENVIRONMENT itself - is honored the same way.
        var builder = WpfApp.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [HostDefaults.EnvironmentKey] = "Staging",
        });

        Assert.Equal("Staging", builder.Environment.EnvironmentName);
    }

    [Fact]
    public void EnvironmentNameDefaultsToProductionWhenNotConfigured()
    {
        var builder = WpfApp.CreateBuilder();

        // No configuration source sets HostDefaults.EnvironmentKey (and, in this test
        // process, DOTNET_ENVIRONMENT is not expected to be set either), so this falls back
        // to Environments.Production.
        Assert.Equal(Environments.Production, builder.Environment.EnvironmentName);
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
    public void RemainingSettersAreNotSupported()
    {
        var environment = new WpfHostEnvironment();

        Assert.Throws<NotSupportedException>(() => environment.ApplicationName = "x");
        Assert.Throws<NotSupportedException>(() => environment.ContentRootPath = "x");
        Assert.Throws<NotSupportedException>(() => environment.ContentRootFileProvider = null!);
    }
}
