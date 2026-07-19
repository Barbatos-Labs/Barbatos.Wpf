// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Linq;
using Barbatos.Wpf.Storage;

namespace Barbatos.Wpf.ApplicationModel;

/// <summary>
/// The VersionTracking API provides an easy way to track an app's version on a device.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>IVersionTracking</c>.</remarks>
public interface IVersionTracking
{
    /// <summary>
    /// Starts tracking version information.
    /// </summary>
    void Track();

    /// <summary>
    /// Gets a value indicating whether this is the first time this app has ever been launched on this device.
    /// </summary>
    bool IsFirstLaunchEver { get; }

    /// <summary>
    /// Gets a value indicating if this is the first launch of the app for the current version number.
    /// </summary>
    bool IsFirstLaunchForCurrentVersion { get; }

    /// <summary>
    /// Gets a value indicating if this is the first launch of the app for the current build number.
    /// </summary>
    bool IsFirstLaunchForCurrentBuild { get; }

    /// <summary>
    /// Gets the current version number of the app.
    /// </summary>
    string CurrentVersion { get; }

    /// <summary>
    /// Gets the current build of the app.
    /// </summary>
    string CurrentBuild { get; }

    /// <summary>
    /// Gets the version number for the previously run version.
    /// </summary>
    string? PreviousVersion { get; }

    /// <summary>
    /// Gets the build number for the previously run version.
    /// </summary>
    string? PreviousBuild { get; }

    /// <summary>
    /// Gets the version number of the first version of the app that was installed on this device.
    /// </summary>
    string? FirstInstalledVersion { get; }

    /// <summary>
    /// Gets the build number of first version of the app that was installed on this device.
    /// </summary>
    string? FirstInstalledBuild { get; }

    /// <summary>
    /// Gets the collection of version numbers of the app that ran on this device.
    /// </summary>
    IReadOnlyList<string> VersionHistory { get; }

    /// <summary>
    /// Gets the collection of build numbers of the app that ran on this device.
    /// </summary>
    IReadOnlyList<string> BuildHistory { get; }

    /// <summary>
    /// Determines if this is the first launch of the app for a specified version number.
    /// </summary>
    /// <param name="version">The version number.</param>
    /// <returns><see langword="true"/> if this is the first launch of the app for the specified version number; otherwise <see langword="false"/>.</returns>
    bool IsFirstLaunchForVersion(string version);

    /// <summary>
    /// Determines if this is the first launch of the app for a specified build number.
    /// </summary>
    /// <param name="build">The build number.</param>
    /// <returns><see langword="true"/> if this is the first launch of the app for the specified build number; otherwise <see langword="false"/>.</returns>
    bool IsFirstLaunchForBuild(string build);
}

/// <summary>
/// The VersionTracking API provides an easy way to track an app's version on a device.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>VersionTracking</c>.</remarks>
public static class VersionTracking
{
    /// <inheritdoc cref="IVersionTracking.Track" />
    public static void Track()
        => Default.Track();

    /// <inheritdoc cref="IVersionTracking.IsFirstLaunchEver" />
    public static bool IsFirstLaunchEver
        => Default.IsFirstLaunchEver;

    /// <inheritdoc cref="IVersionTracking.IsFirstLaunchForCurrentVersion" />
    public static bool IsFirstLaunchForCurrentVersion
        => Default.IsFirstLaunchForCurrentVersion;

    /// <inheritdoc cref="IVersionTracking.IsFirstLaunchForCurrentBuild" />
    public static bool IsFirstLaunchForCurrentBuild
        => Default.IsFirstLaunchForCurrentBuild;

    /// <inheritdoc cref="IVersionTracking.CurrentVersion" />
    public static string CurrentVersion
        => Default.CurrentVersion;

    /// <inheritdoc cref="IVersionTracking.CurrentBuild" />
    public static string CurrentBuild
        => Default.CurrentBuild;

    /// <inheritdoc cref="IVersionTracking.PreviousVersion" />
    public static string? PreviousVersion
        => Default.PreviousVersion;

    /// <inheritdoc cref="IVersionTracking.PreviousBuild" />
    public static string? PreviousBuild
        => Default.PreviousBuild;

    /// <inheritdoc cref="IVersionTracking.FirstInstalledVersion" />
    public static string? FirstInstalledVersion
        => Default.FirstInstalledVersion;

    /// <inheritdoc cref="IVersionTracking.FirstInstalledBuild" />
    public static string? FirstInstalledBuild
        => Default.FirstInstalledBuild;

    /// <inheritdoc cref="IVersionTracking.VersionHistory" />
    public static IEnumerable<string> VersionHistory
        => Default.VersionHistory;

    /// <inheritdoc cref="IVersionTracking.BuildHistory" />
    public static IEnumerable<string> BuildHistory
        => Default.BuildHistory;

    /// <inheritdoc cref="IVersionTracking.IsFirstLaunchForVersion(string)" />
    public static bool IsFirstLaunchForVersion(string version)
        => Default.IsFirstLaunchForVersion(version);

    /// <inheritdoc cref="IVersionTracking.IsFirstLaunchForBuild(string)" />
    public static bool IsFirstLaunchForBuild(string build)
        => Default.IsFirstLaunchForBuild(build);

    static IVersionTracking? defaultImplementation;

    /// <summary>
    /// Provides the default implementation for static usage of this API.
    /// </summary>
    public static IVersionTracking Default =>
        defaultImplementation ??= new VersionTrackingImplementation(Preferences.Default, AppInfo.Current);

    internal static void SetDefault(IVersionTracking? implementation) =>
        defaultImplementation = implementation;

    internal static void InitVersionTracking() =>
        (Default as VersionTrackingImplementation)?.InitVersionTracking();
}

/// <summary>
/// The default <see cref="IVersionTracking"/> implementation, ported nearly verbatim from
/// .NET MAUI's <c>VersionTrackingImplementation</c> — it is pure C# with no platform-specific
/// code, built entirely on top of <see cref="IPreferences"/> and <see cref="IAppInfo"/>.
/// </summary>
class VersionTrackingImplementation : IVersionTracking
{
    const string versionsKey = "VersionTracking.Versions";
    const string buildsKey = "VersionTracking.Builds";

    static readonly string sharedName = Preferences.GetPrivatePreferencesSharedName("versiontracking");

    readonly IPreferences preferences;
    readonly IAppInfo appInfo;

    Dictionary<string, List<string>> versionTrail = null!;

    string LastInstalledVersion => versionTrail[versionsKey]?.LastOrDefault() ?? string.Empty;

    string LastInstalledBuild => versionTrail[buildsKey]?.LastOrDefault() ?? string.Empty;

    public VersionTrackingImplementation(IPreferences preferences, IAppInfo appInfo)
    {
        this.preferences = preferences;
        this.appInfo = appInfo;

        Track();
    }

    public void Track()
    {
        if (versionTrail != null)
            return;

        InitVersionTracking();
    }

    /// <summary>
    /// Initialize VersionTracking module, load data and track current version
    /// </summary>
    /// <remarks>
    /// For internal use. Usually only called once in production code, but multiple times in unit tests
    /// </remarks>
    internal void InitVersionTracking()
    {
        IsFirstLaunchEver = !preferences.ContainsKey(versionsKey, sharedName) || !preferences.ContainsKey(buildsKey, sharedName);
        if (IsFirstLaunchEver)
        {
            versionTrail = new(StringComparer.Ordinal)
            {
                { versionsKey, new List<string>() },
                { buildsKey, new List<string>() }
            };
        }
        else
        {
            versionTrail = new(StringComparer.Ordinal)
            {
                { versionsKey, ReadHistory(versionsKey).ToList() },
                { buildsKey, ReadHistory(buildsKey).ToList() }
            };
        }

        IsFirstLaunchForCurrentVersion = !versionTrail[versionsKey].Contains(CurrentVersion) || CurrentVersion != LastInstalledVersion;
        if (IsFirstLaunchForCurrentVersion)
        {
            // Avoid duplicates and move current version to end of list if already present
            versionTrail[versionsKey].RemoveAll(v => v == CurrentVersion);
            versionTrail[versionsKey].Add(CurrentVersion);
        }

        IsFirstLaunchForCurrentBuild = !versionTrail[buildsKey].Contains(CurrentBuild) || CurrentBuild != LastInstalledBuild;
        if (IsFirstLaunchForCurrentBuild)
        {
            // Avoid duplicates and move current build to end of list if already present
            versionTrail[buildsKey].RemoveAll(b => b == CurrentBuild);
            versionTrail[buildsKey].Add(CurrentBuild);
        }

        if (IsFirstLaunchForCurrentVersion || IsFirstLaunchForCurrentBuild)
        {
            WriteHistory(versionsKey, versionTrail[versionsKey]);
            WriteHistory(buildsKey, versionTrail[buildsKey]);
        }
    }

    public bool IsFirstLaunchEver { get; private set; }

    public bool IsFirstLaunchForCurrentVersion { get; private set; }

    public bool IsFirstLaunchForCurrentBuild { get; private set; }

    public string CurrentVersion => appInfo.VersionString;

    public string CurrentBuild => appInfo.BuildString;

    public string? PreviousVersion => GetPrevious(versionsKey);

    public string? PreviousBuild => GetPrevious(buildsKey);

    public string? FirstInstalledVersion => versionTrail[versionsKey].FirstOrDefault();

    public string? FirstInstalledBuild => versionTrail[buildsKey].FirstOrDefault();

    public IReadOnlyList<string> VersionHistory => versionTrail[versionsKey].ToArray();

    public IReadOnlyList<string> BuildHistory => versionTrail[buildsKey].ToArray();

    public bool IsFirstLaunchForVersion(string version)
        => CurrentVersion == version && IsFirstLaunchForCurrentVersion;

    public bool IsFirstLaunchForBuild(string build)
        => CurrentBuild == build && IsFirstLaunchForCurrentBuild;

    string[] ReadHistory(string key)
        => preferences.Get<string?>(key, null, sharedName)?.Split(['|'], StringSplitOptions.RemoveEmptyEntries) ?? [];

    void WriteHistory(string key, IEnumerable<string> history)
        => preferences.Set(key, string.Join("|", history), sharedName);

    string? GetPrevious(string key)
    {
        var trail = versionTrail[key];
        return (trail.Count >= 2) ? trail[trail.Count - 2] : null;
    }
}
