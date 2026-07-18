// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Hosting Contributors.
// All Rights Reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace Barbatos.Wpf.Hosting;

/// <summary>
/// The WPF counterpart of .NET MAUI's <c>MauiMetricsBuilder</c>.
/// </summary>
internal class WpfMetricsBuilder(IServiceCollection services) : IMetricsBuilder
{
    readonly IServiceCollection _services = services;

    public IServiceCollection Services => _services;
}
