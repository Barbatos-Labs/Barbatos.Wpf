// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Barbatos.Wpf.ApplicationModel;

/// <summary>
/// The WPF implementation of <see cref="IAppInfo"/>, ported from .NET MAUI's Windows
/// <c>AppInfoImplementation</c> using Win32/registry APIs instead of WinRT.
/// </summary>
class AppInfoImplementation : IAppInfo
{
    static readonly Assembly? _launchingAssembly = Assembly.GetEntryAssembly();

    const string SettingsUri = "ms-settings:appsfeatures-app";

    const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    // Computed once (not per-access, unlike the other properties above) since it involves
    // several registry lookups; AppGuid itself never changes for the lifetime of the process.
    readonly Lazy<(DateTime? InstallDate, string? InstallLocation)> _installedAppInfo;

    public AppInfoImplementation()
    {
        _installedAppInfo = new(() => AppInfoUtils.ReadInstalledAppInfo(AppGuid));
    }

    public string AppGuid =>
        _launchingAssembly?.GetAppInfoValue("AppGuid")
        ?? _launchingAssembly?.GetCustomAttribute<AssemblyTitleAttribute>()?.Title
        ?? _launchingAssembly?.GetName().Name
        ?? string.Empty;

    public string Name =>
        _launchingAssembly?.GetAppInfoValue("Name")
        ?? _launchingAssembly?.GetCustomAttribute<AssemblyProductAttribute>()?.Product
        ?? _launchingAssembly?.GetCustomAttribute<AssemblyTitleAttribute>()?.Title
        ?? _launchingAssembly?.GetName().Name
        ?? string.Empty;

    public Version Version =>
        _launchingAssembly?.GetAppInfoVersionValue("Version")
        ?? _launchingAssembly?.GetName().Version
        ?? new Version(0, 0);

    public string VersionString => Version.ToString();

    public string BuildString => Version.Revision.ToString(CultureInfo.InvariantCulture);

    public void ShowSettingsUI() =>
        Process.Start(new ProcessStartInfo { FileName = SettingsUri, UseShellExecute = true });

    public AppTheme RequestedTheme
    {
        get
        {
            try
            {
                // 1 (or missing on very old builds) = light, 0 = dark.
                using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath);
                if (key?.GetValue("AppsUseLightTheme") is int appsUseLightTheme)
                    return appsUseLightTheme == 0 ? AppTheme.Dark : AppTheme.Light;
            }
            catch
            {
                // no-op
            }

            return AppTheme.Unspecified;
        }
    }

    public AppPackagingModel PackagingModel => AppInfoUtils.IsPackagedApp
        ? AppPackagingModel.Packaged
        : AppPackagingModel.Unpackaged;

    public LayoutDirection RequestedLayoutDirection =>
        CultureInfo.CurrentCulture.TextInfo.IsRightToLeft ? LayoutDirection.RightToLeft : LayoutDirection.LeftToRight;

    public DateTime? InstallDate => _installedAppInfo.Value.InstallDate;

    public string? InstallLocation => _installedAppInfo.Value.InstallLocation;
}

static class AppInfoUtils
{
    static readonly Lazy<bool> _isPackagedAppLazy = new(() =>
    {
        try
        {
            // The Win32 equivalent of WinRT's Package.Current: returns
            // APPMODEL_ERROR_NO_PACKAGE when the process has no package identity.
            var length = 0;
            var result = NativeMethods.GetCurrentPackageFullName(ref length, null);
            return result != NativeMethods.AppModelErrorNoPackage;
        }
        catch
        {
            return false;
        }
    });

    /// <summary>
    /// Gets if this app is a packaged (MSIX) app.
    /// </summary>
    public static bool IsPackagedApp => _isPackagedAppLazy.Value;

    /// <summary>
    /// Gets the version information for this app.
    /// </summary>
    public static Version? GetAppInfoVersionValue(this Assembly assembly, string name)
    {
        if (assembly.GetAppInfoValue(name) is string value && !string.IsNullOrEmpty(value))
            return Version.Parse(value);

        return null;
    }

    /// <summary>
    /// Gets the app info from this app's assembly metadata.
    /// </summary>
    /// <param name="assembly">The assembly to retrieve the app info for.</param>
    /// <param name="name">The key of the metadata to be retrieved (e.g. AppGuid or Name).</param>
    public static string? GetAppInfoValue(this Assembly assembly, string name) =>
        assembly.GetMetadataAttributeValue("Barbatos.Wpf.ApplicationModel.AppInfo." + name);

    const string UninstallKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\";

    static readonly (RegistryHive Hive, RegistryView View)[] RegistryLocations =
    [
        // HKLM covers machine-wide installs; HKCU covers per-user installs (Inno Setup's
        // "PrivilegesRequired=lowest"). Both the 64-bit and 32-bit registry views are tried
        // because RegistryView.Registry32 transparently redirects to WOW6432Node, which is
        // where a 32-bit installer registers itself even on 64-bit Windows.
        (RegistryHive.LocalMachine, RegistryView.Registry64),
        (RegistryHive.LocalMachine, RegistryView.Registry32),
        (RegistryHive.CurrentUser, RegistryView.Registry64),
        (RegistryHive.CurrentUser, RegistryView.Registry32),
    ];

    /// <summary>
    /// Reads <c>InstallDate</c>/<c>InstallLocation</c> from the Windows "Programs and
    /// Features" uninstall registry entry for <paramref name="appGuid"/>, trying both the
    /// bare-GUID subkey name (the MSI/WiX convention) and the "_is1" suffixed one (the Inno
    /// Setup convention) across every registry hive/view combination an installer might use.
    /// Returns <c>(null, null)</c> when <paramref name="appGuid"/> isn't a GUID, or no
    /// matching entry is found.
    /// </summary>
    public static (DateTime? InstallDate, string? InstallLocation) ReadInstalledAppInfo(string appGuid)
    {
        if (!Guid.TryParse(appGuid, out var guid))
            return (null, null);

        var braced = guid.ToString("B", CultureInfo.InvariantCulture);
        var subKeyNames = new[] { braced + "_is1", braced };

        try
        {
            foreach (var (hive, view) in RegistryLocations)
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);

                foreach (var subKeyName in subKeyNames)
                {
                    using var key = baseKey.OpenSubKey(UninstallKeyPath + subKeyName);
                    if (key is null)
                        continue;

                    var installLocation = key.GetValue("InstallLocation") as string;

                    DateTime? installDate = null;
                    if (key.GetValue("InstallDate") is string installDateValue &&
                        DateTime.TryParseExact(installDateValue, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                    {
                        installDate = parsedDate;
                    }

                    if (installDate is not null || !string.IsNullOrEmpty(installLocation))
                        return (installDate, string.IsNullOrEmpty(installLocation) ? null : installLocation);
                }
            }
        }
        catch
        {
            // Registry access can fail in restricted environments; treat that the same as
            // "not installed through a matching installer".
        }

        return (null, null);
    }

    static class NativeMethods
    {
        internal const int AppModelErrorNoPackage = 15700;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int GetCurrentPackageFullName(ref int packageFullNameLength, char[]? packageFullName);
    }
}
