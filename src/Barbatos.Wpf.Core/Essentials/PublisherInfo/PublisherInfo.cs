// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.ApplicationModel;

/// <summary>
/// Represents information about the application's publisher.
/// </summary>
/// <remarks>
/// This module has no direct .NET MAUI Essentials counterpart — it factors out the publisher
/// identity that <see cref="IAppInfo"/> used to expose ad hoc (as an internal
/// <c>AppInfoImplementation.PublisherName</c> helper used only for the
/// <see cref="Barbatos.Wpf.Storage.FileSystem"/> storage path). It follows the same interface +
/// static facade design as every other essentials module (<see cref="AppInfo"/>, ...).
/// </remarks>
public interface IPublisherInfo
{
    /// <summary>
    /// Gets the publisher (company) name of the application.
    /// </summary>
    /// <remarks>
    /// Comes from the <c>Barbatos.Wpf.ApplicationModel.PublisherInfo.Name</c> assembly
    /// metadata, falling back to the assembly's <see cref="System.Reflection.AssemblyCompanyAttribute"/>.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Gets the publisher's website, or <see langword="null"/> when not configured.
    /// </summary>
    /// <remarks>
    /// Comes from the <c>Barbatos.Wpf.ApplicationModel.PublisherInfo.Website</c> assembly
    /// metadata. There is no built-in fallback. If you ship an Inno Setup/MSI-style
    /// installer, this is the same URL you would put in Inno Setup's <c>AppPublisherURL</c>
    /// (which becomes the <c>URLInfoAbout</c> value under the app's
    /// <c>...\Uninstall\{AppId}</c> registry key).
    /// </remarks>
    string? Website { get; }

    /// <summary>
    /// Gets the publisher's support URL, or <see langword="null"/> when not configured.
    /// </summary>
    /// <remarks>
    /// Comes from the <c>Barbatos.Wpf.ApplicationModel.PublisherInfo.SupportUrl</c> assembly
    /// metadata. There is no built-in fallback. Distinct from <see cref="Website"/>: an
    /// installer typically shows this as a separate "Get help" link — Inno Setup's
    /// <c>AppSupportURL</c> (which becomes the <c>HelpLink</c> registry value), for example.
    /// </remarks>
    string? SupportUrl { get; }

    /// <summary>
    /// Gets the publisher's support email address, or <see langword="null"/> when not configured.
    /// </summary>
    /// <remarks>
    /// Comes from the <c>Barbatos.Wpf.ApplicationModel.PublisherInfo.SupportEmail</c> assembly
    /// metadata. There is no built-in fallback.
    /// </remarks>
    string? SupportEmail { get; }

    /// <summary>
    /// Gets the publisher's copyright notice, or <see langword="null"/> when not configured.
    /// </summary>
    /// <remarks>
    /// Comes from the <c>Barbatos.Wpf.ApplicationModel.PublisherInfo.Copyright</c> assembly
    /// metadata, falling back to the standard <c>&lt;Copyright&gt;</c> csproj property
    /// (<see cref="System.Reflection.AssemblyCopyrightAttribute"/>).
    /// </remarks>
    string? Copyright { get; }
}

/// <summary>
/// Represents information about the application's publisher.
/// </summary>
public static class PublisherInfo
{
    /// <inheritdoc cref="IPublisherInfo.Name" />
    public static string Name => Current.Name;

    /// <inheritdoc cref="IPublisherInfo.Website" />
    public static string? Website => Current.Website;

    /// <inheritdoc cref="IPublisherInfo.SupportUrl" />
    public static string? SupportUrl => Current.SupportUrl;

    /// <inheritdoc cref="IPublisherInfo.SupportEmail" />
    public static string? SupportEmail => Current.SupportEmail;

    /// <inheritdoc cref="IPublisherInfo.Copyright" />
    public static string? Copyright => Current.Copyright;

    static IPublisherInfo? currentImplementation;

    /// <summary>
    /// Provides the default implementation for static usage of this API.
    /// </summary>
    public static IPublisherInfo Current =>
        currentImplementation ??= new PublisherInfoImplementation();

    internal static void SetCurrent(IPublisherInfo? implementation) =>
        currentImplementation = implementation;
}
