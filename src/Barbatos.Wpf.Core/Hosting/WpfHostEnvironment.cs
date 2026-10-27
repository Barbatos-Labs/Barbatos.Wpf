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
    /// <see cref="WpfAppBuilder"/> creates this environment lazily and reads the value once, at
    /// that moment: on the first access of <see cref="WpfAppBuilder.Environment"/>, or - if the
    /// builder never touches it - when <see cref="IHostEnvironment"/> is first resolved from the
    /// built container. It comes from the <see cref="HostDefaults.EnvironmentKey"/> key of
    /// <see cref="WpfAppBuilder.Configuration"/>, which the <c>DOTNET_ENVIRONMENT</c>
    /// environment variable feeds through a "DOTNET_"-prefixed environment variables source
    /// added first (lowest priority) - the same bootstrap step
    /// <c>Microsoft.Extensions.Hosting.HostBuilder</c> performs. So a configuration source that
    /// sets that key (appsettings.json, command-line arguments, an in-memory collection, ...)
    /// overrides the variable when it is added before that first read; one added afterwards is
    /// never seen. Being a plain settable property, it can also be assigned directly from code
    /// at any time.
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
