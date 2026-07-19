// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Reflection;

namespace Barbatos.Wpf.ApplicationModel;

/// <summary>
/// The WPF implementation of <see cref="IPublisherInfo"/>, sourced from assembly metadata
/// (falling back to <see cref="AssemblyCompanyAttribute"/> for the publisher name, the same
/// fallback <c>AppInfoImplementation</c> previously used internally).
/// </summary>
class PublisherInfoImplementation : IPublisherInfo
{
    static readonly Assembly? _launchingAssembly = Assembly.GetEntryAssembly();

    public string Name =>
        _launchingAssembly?.GetPublisherInfoValue("Name")
        ?? _launchingAssembly?.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company
        ?? string.Empty;

    public string? Website =>
        _launchingAssembly?.GetPublisherInfoValue("Website");

    public string? SupportUrl =>
        _launchingAssembly?.GetPublisherInfoValue("SupportUrl");

    public string? SupportEmail =>
        _launchingAssembly?.GetPublisherInfoValue("SupportEmail");

    public string? Copyright =>
        _launchingAssembly?.GetPublisherInfoValue("Copyright")
        ?? _launchingAssembly?.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
}

static class PublisherInfoUtils
{
    /// <summary>
    /// Gets the publisher info from this app's assembly metadata.
    /// </summary>
    /// <param name="assembly">The assembly to retrieve the publisher info for.</param>
    /// <param name="name">The key of the metadata to be retrieved (e.g. Name, Website or SupportEmail).</param>
    public static string? GetPublisherInfoValue(this Assembly assembly, string name) =>
        assembly.GetMetadataAttributeValue("Barbatos.Wpf.ApplicationModel.PublisherInfo." + name);
}
