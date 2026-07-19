// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.ApplicationModel;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Barbatos.Wpf.Hosting;

/// <summary>
/// Provides information about the hosting environment a WPF application is running in.
/// </summary>
/// <remarks>
/// This is the WPF counterpart of .NET MAUI's <c>MauiHostEnvironment</c>. Unlike the mobile
/// platforms, a WPF desktop application has a real content root on disk, so
/// <see cref="ContentRootPath"/> and <see cref="ContentRootFileProvider"/> are fully functional.
/// </remarks>
public class WpfHostEnvironment : IHostEnvironment
{
    private IFileProvider? _contentRootFileProvider;

    /// <inheritdoc />
    /// <remarks>
    /// Set once when the <see cref="WpfAppBuilder"/> constructs this environment, from the
    /// <see cref="HostDefaults.EnvironmentKey"/> configuration key of
    /// <see cref="WpfAppBuilder.Configuration"/> — which in turn is populated by the
    /// <c>DOTNET_ENVIRONMENT</c> environment variable via a "DOTNET_"-prefixed environment
    /// variables configuration source, the same bootstrap step
    /// <c>Microsoft.Extensions.Hosting.HostBuilder</c> performs before building the rest of its
    /// configuration. Because it is a normal property (unlike the previous implementation,
    /// which recomputed it from <see cref="System.Environment.GetEnvironmentVariable(string)"/>
    /// on every access and always threw on <c>set</c>), it can also be overridden after the
    /// fact — from code, or from any other configuration source (appsettings.json,
    /// command-line arguments, ...) added to <see cref="WpfAppBuilder.Configuration"/> before
    /// <see cref="WpfAppBuilder.Build"/> runs.
    /// </remarks>
    public string EnvironmentName { get; set; } = Environments.Production;

    /// <inheritdoc />
    /// <remarks>Mirrors .NET MAUI's <c>MauiHostEnvironment</c>, which uses <c>AppInfo.Current.Name</c>.</remarks>
    public string ApplicationName
    {
        get => AppInfo.Current.Name;
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public string ContentRootPath
    {
        get => AppContext.BaseDirectory;
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public IFileProvider ContentRootFileProvider
    {
        get => _contentRootFileProvider ??= new PhysicalFileProvider(ContentRootPath);
        set => throw new NotSupportedException();
    }
}
