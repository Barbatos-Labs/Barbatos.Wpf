// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

public class PublisherInfoTests
{
    [Fact]
    public void CurrentIsCached()
    {
        Assert.NotNull(PublisherInfo.Current);
        Assert.Same(PublisherInfo.Current, PublisherInfo.Current);
    }

    [Fact]
    public void NameIsNotEmpty()
    {
        Assert.False(string.IsNullOrEmpty(PublisherInfo.Name));
    }

    [Fact]
    public void WebsiteIsNullWithoutAssemblyMetadata()
    {
        Assert.Null(PublisherInfo.Website);
    }

    [Fact]
    public void SupportUrlIsNullWithoutAssemblyMetadata()
    {
        Assert.Null(PublisherInfo.SupportUrl);
    }

    [Fact]
    public void SupportEmailIsNullWithoutAssemblyMetadata()
    {
        Assert.Null(PublisherInfo.SupportEmail);
    }

    [Fact]
    public void CopyrightFallsBackToTheAssemblyCopyrightAttribute()
    {
        // Directory.Build.props sets <Copyright>, which the SDK turns into
        // AssemblyCopyrightAttribute on every project in this repo (including the test
        // host), so the fallback chain should resolve a non-empty value here.
        Assert.False(string.IsNullOrEmpty(PublisherInfo.Copyright));
    }

    [Fact]
    public void PublisherInfoIsRegisteredInTheDefaultBuilder()
    {
        var wpfApp = WpfApp.CreateBuilder().Build();

        var publisherInfo = wpfApp.Services.GetService<IPublisherInfo>();

        Assert.NotNull(publisherInfo);
        Assert.Same(PublisherInfo.Current, publisherInfo);
    }

    [Fact]
    public void PublisherInfoIsNotRegisteredWithoutDefaults()
    {
        var wpfApp = WpfApp.CreateBuilder(useDefaults: false).Build();

        Assert.Null(wpfApp.Services.GetService<IPublisherInfo>());
    }
}
