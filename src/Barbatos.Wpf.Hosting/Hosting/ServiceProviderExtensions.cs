// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Hosting Contributors.
// All Rights Reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Barbatos.Wpf;

internal static class ServiceProviderExtensions
{
    internal static ILogger<T>? CreateLogger<T>(this IServiceProvider services) =>
        services.GetService<ILogger<T>>();

    internal static ILogger? CreateLogger(this IServiceProvider services, string loggerName) =>
        services.GetService<ILoggerFactory>()?.CreateLogger(loggerName);

    internal static ILogger CreateLogger(this IServiceProvider services, Type type) =>
        services.GetService<ILoggerFactory>()?.CreateLogger(type) ?? NullLogger.Instance;
}
