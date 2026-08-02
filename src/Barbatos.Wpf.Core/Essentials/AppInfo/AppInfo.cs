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
    /// This does not have to be a GUID — any stable string works, whether that's a GUID
    /// (<c>{34D092DC-F20D-4B08-B93F-B45FE60881E6}</c>) or an ordinary reverse-DNS style name
    /// (<c>com.contoso.myapp</c>). It comes from the
    /// <c>Barbatos.Wpf.ApplicationModel.AppInfo.AppId</c> assembly metadata, falling back to
    /// the deprecated <c>Barbatos.Wpf.ApplicationModel.AppInfo.AppGuid</c> key, then the
    /// assembly title, then the assembly name.
    /// <para>
    /// This is the identifier the app's storage folder is derived from (see
    /// <see cref="Barbatos.Wpf.Storage.FileSystem"/>), so it should stay constant across
    /// releases. It is also the uninstall registry subkey name
    /// <see cref="InstallDate"/>/<see cref="InstallLocation"/> are read from — if you ship an
    /// installer and want those to resolve, set this to the same identifier the installer
    /// registers itself under.
    /// </para>
    /// </remarks>
    string AppId { get; }

    /// <summary>
    /// Gets the application's stable unique identifier.
    /// </summary>
    /// <remarks>Renamed to <see cref="AppId"/>; this member simply forwards to it.</remarks>
    [Obsolete("AppGuid has been renamed to AppId, because the value does not have to be a GUID. Use AppId instead. This property will be removed in a future release.")]
    string AppGuid => AppId;

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
    /// <c>...\Uninstall\{AppId}</c> registry entry — the same entry that backs the "Programs
    /// and Features" control panel — which only exists when the app was actually installed
    /// through an installer that registered itself under that exact name. When your installer
    /// uses a different name (Inno Setup always appends <c>_is1</c> to its own AppId, for
    /// example), set the <c>Barbatos.Wpf.ApplicationModel.AppInfo.UninstallRegistryKey</c>
    /// assembly metadata to the name it actually creates; it defaults to <see cref="AppId"/>.
    /// Returns <see langword="null"/> when running unpublished (e.g. from the IDE), or when no
    /// matching entry is found in <c>HKEY_LOCAL_MACHINE</c> or <c>HKEY_CURRENT_USER</c>
    /// (32-bit or 64-bit registry view). See <c>Publishing with an installer</c> in the README.
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
    /// <inheritdoc cref="IAppInfo.AppId" />
    public static string AppId => Current.AppId;

    /// <inheritdoc cref="IAppInfo.AppGuid" />
    [Obsolete("AppGuid has been renamed to AppId, because the value does not have to be a GUID. Use AppId instead. This property will be removed in a future release.")]
    public static string AppGuid => Current.AppId;

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
