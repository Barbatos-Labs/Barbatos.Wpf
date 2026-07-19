// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Barbatos.Wpf.Core.UnitTests;

public class HostBuilderLoggingTests
{
    [Fact]
    public void DefaultLoggerIsNoOp()
    {
        var builder = WpfApp.CreateBuilder();
        var wpfApp = builder.Build();

        var logger = wpfApp.Services.GetService<ILogger<HostBuilderLoggingTests>>();

        Assert.NotNull(logger);
        Assert.False(logger.IsEnabled(LogLevel.Critical));
    }

    [Fact]
    public void DefaultLoggerFactoryIsAvailable()
    {
        var builder = WpfApp.CreateBuilder();
        var wpfApp = builder.Build();

        var factory = wpfApp.Services.GetService<ILoggerFactory>();

        Assert.NotNull(factory);
        Assert.NotNull(factory.CreateLogger("category"));
    }

    [Fact]
    public void AccessingLoggingRegistersRealLogging()
    {
        var builder = WpfApp.CreateBuilder();
        var logging = builder.Logging;

        Assert.NotNull(logging);
        Assert.Same(builder.Services, logging.Services);

        var wpfApp = builder.Build();

        var factory = wpfApp.Services.GetRequiredService<ILoggerFactory>();

        Assert.IsType<LoggerFactory>(factory);
    }

    [Fact]
    public void AccessingLoggingReturnsSameInstance()
    {
        var builder = WpfApp.CreateBuilder();

        Assert.Same(builder.Logging, builder.Logging);
    }

    [Fact]
    public void UserRegisteredLoggingIsNotOverridden()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Trace));
        var wpfApp = builder.Build();

        var factory = wpfApp.Services.GetRequiredService<ILoggerFactory>();

        Assert.IsType<LoggerFactory>(factory);
    }
}
