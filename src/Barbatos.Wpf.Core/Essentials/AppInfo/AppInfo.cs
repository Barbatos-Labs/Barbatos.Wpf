// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.ApplicationModel;

/// <summary>
/// Represents information about the application.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>IAppInfo</c>.</remarks>
public interface IAppInfo
{
    /// <summary>
    /// Gets the application's stable unique identifier.
    /// </summary>
    /// <remarks>
    /// For packaged (MSIX) applications, this is the package identity name. For unpackaged
    /// applications, this comes from the <c>Barbatos.Wpf.ApplicationModel.AppInfo.AppGuid</c>
    /// assembly metadata, falling back to the assembly title and the assembly name.
    /// <para>
    /// If you ship an installer (Inno Setup, MSI, ...), set this to the same GUID as the
    /// installer's own application identifier (Inno Setup's <c>AppId</c>, which becomes the
    /// <c>HKLM\...\Uninstall\{AppId}_is1</c> registry key) — e.g.
    /// <c>{34D092DC-F20D-4B08-B93F-B45FE60881E6}</c>. Keeping the two in sync means the
    /// storage folder this library derives from <see cref="AppGuid"/>
    /// (see <see cref="Barbatos.Wpf.Storage.FileSystem"/>) and the identity Windows uses for
    /// install/uninstall/upgrade both refer to the same app.
    /// </para>
    /// </remarks>
    string AppGuid { get; }

    /// <summary>
    /// Gets the application name.
    /// </summary>
    /// <remarks>
    /// Comes from the <c>Barbatos.Wpf.ApplicationModel.AppInfo.Name</c> assembly metadata,
    /// falling back to the standard <c>&lt;Product&gt;</c> csproj property
    /// (<see cref="System.Reflection.AssemblyProductAttribute"/>), then <c>&lt;Title&gt;</c>/
    /// <c>&lt;AssemblyTitle&gt;</c>, then the assembly name.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Gets the application version as a string representation.
    /// </summary>
    string VersionString { get; }

    /// <summary>
    /// Gets the application version as a <see cref="Version"/> object.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Gets the application build number.
    /// </summary>
    string BuildString { get; }

    /// <summary>
    /// Open the settings menu or page for this application.
    /// </summary>
    void ShowSettingsUI();

    /// <summary>
    /// Gets the detected theme of the system or application.
    /// </summary>
    /// <remarks>When the theme cannot be detected, <see cref="AppTheme.Unspecified"/> is returned.</remarks>
    AppTheme RequestedTheme { get; }

    /// <summary>
    /// Gets the packaging model of this application.
    /// </summary>
    AppPackagingModel PackagingModel { get; }

    /// <summary>
    /// Gets the requested layout direction of the system or application.
    /// </summary>
    LayoutDirection RequestedLayoutDirection { get; }

    /// <summary>
    /// Gets the date this app was installed, or <see langword="null"/> when unavailable.
    /// </summary>
    /// <remarks>
    /// This has no .NET MAUI counterpart. It is read from the Windows
    /// <c>...\Uninstall\{AppGuid}</c> (or <c>{AppGuid}_is1</c>, the suffix Inno Setup uses)
    /// registry entry — the same entry that backs the "Programs and Features" control panel —
    /// which only exists when <see cref="AppGuid"/> is a real GUID matching the installer's
    /// own application identifier and the app was actually installed through it. Returns
    /// <see langword="null"/> when running unpublished (e.g. from the IDE), when
    /// <see cref="AppGuid"/> isn't a GUID, or when no matching entry is found in
    /// <c>HKEY_LOCAL_MACHINE</c> or <c>HKEY_CURRENT_USER</c> (32-bit or 64-bit registry view).
    /// See <c>Publishing with an installer</c> in the README.
    /// </remarks>
    DateTime? InstallDate { get; }

    /// <summary>
    /// Gets the directory this app was installed into, or <see langword="null"/> when
    /// unavailable. Read from the same registry entry as <see cref="InstallDate"/>, under the
    /// same conditions.
    /// </summary>
    string? InstallLocation { get; }
}

/// <summary>
/// Represents information about the application.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>AppInfo</c>.</remarks>
public static class AppInfo
{
    /// <inheritdoc cref="IAppInfo.AppGuid" />
    public static string AppGuid => Current.AppGuid;

    /// <inheritdoc cref="IAppInfo.Name" />
    public static string Name => Current.Name;

    /// <inheritdoc cref="IAppInfo.VersionString" />
    public static string VersionString => Current.VersionString;

    /// <inheritdoc cref="IAppInfo.Version" />
    public static Version Version => Current.Version;

    /// <inheritdoc cref="IAppInfo.BuildString" />
    public static string BuildString => Current.BuildString;

    /// <inheritdoc cref="IAppInfo.ShowSettingsUI" />
    public static void ShowSettingsUI() => Current.ShowSettingsUI();

    /// <inheritdoc cref="IAppInfo.RequestedTheme" />
    public static AppTheme RequestedTheme => Current.RequestedTheme;

    /// <inheritdoc cref="IAppInfo.PackagingModel" />
    public static AppPackagingModel PackagingModel => Current.PackagingModel;

    /// <inheritdoc cref="IAppInfo.RequestedLayoutDirection" />
    public static LayoutDirection RequestedLayoutDirection => Current.RequestedLayoutDirection;

    /// <inheritdoc cref="IAppInfo.InstallDate" />
    public static DateTime? InstallDate => Current.InstallDate;

    /// <inheritdoc cref="IAppInfo.InstallLocation" />
    public static string? InstallLocation => Current.InstallLocation;

    static IAppInfo? currentImplementation;

    /// <summary>
    /// Provides the default implementation for static usage of this API.
    /// </summary>
    public static IAppInfo Current =>
        currentImplementation ??= new AppInfoImplementation();

    internal static void SetCurrent(IAppInfo? implementation) =>
        currentImplementation = implementation;
}

/// <summary>
/// Describes packaging options for a Windows app.
/// </summary>
public enum AppPackagingModel
{
    /// <summary>The app is packaged and can be distributed through an MSIX or the store.</summary>
    Packaged,

    /// <summary>The app is unpackaged and can be distributed as a collection of executable files.</summary>
    Unpackaged,
}
