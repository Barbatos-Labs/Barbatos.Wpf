// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Hosting Contributors.
// All Rights Reserved.

using System.Reflection;
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
    public string EnvironmentName
    {
        get => System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? Environments.Production;
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public string ApplicationName
    {
        get => Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;
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
