// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.ApplicationModel;
using Barbatos.Wpf.Devices;
using Barbatos.Wpf.Networking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Barbatos.Wpf.Core.Sample;

public interface IGreetingService
{
    string GetGreeting();

    string GetEnvironmentDescription();

    string GetAppDeviceDescription();

    string GetInstallInfoDescription();

    string GetVersionTrackingDescription();

    string GetPublisherDescription();

    string GetConnectivityDescription();

    string GetDisplayInfoDescription();

    Task<string> GetDeviceIdentityDescriptionAsync();
}

/// <summary>
/// A sample service that demonstrates constructor injection of the host's
/// <see cref="IConfiguration"/>, <see cref="IHostEnvironment"/>, and several of the
/// essentials services (<see cref="IAppInfo"/>, <see cref="IPublisherInfo"/>,
/// <see cref="IDeviceInfo"/>, <see cref="IVersionTracking"/>, <see cref="IConnectivity"/>,
/// <see cref="IDeviceDisplay"/>).
/// </summary>
public class GreetingService : IGreetingService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly IAppInfo _appInfo;
    private readonly IPublisherInfo _publisherInfo;
    private readonly IDeviceInfo _deviceInfo;
    private readonly IVersionTracking _versionTracking;
    private readonly IConnectivity _connectivity;
    private readonly IDeviceDisplay _deviceDisplay;
    private readonly IDeviceIdentity _deviceIdentity;

    public GreetingService(
        IConfiguration configuration,
        IHostEnvironment environment,
        IAppInfo appInfo,
        IPublisherInfo publisherInfo,
        IDeviceInfo deviceInfo,
        IVersionTracking versionTracking,
        IConnectivity connectivity,
        IDeviceDisplay deviceDisplay,
        IDeviceIdentity deviceIdentity)
    {
        _configuration = configuration;
        _environment = environment;
        _appInfo = appInfo;
        _publisherInfo = publisherInfo;
        _deviceInfo = deviceInfo;
        _versionTracking = versionTracking;
        _connectivity = connectivity;
        _deviceDisplay = deviceDisplay;
        _deviceIdentity = deviceIdentity;
    }

    public string GetGreeting() =>
        _configuration["Sample:Greeting"] ?? "Hello!";

    public string GetEnvironmentDescription() =>
        $"Application: {_environment.ApplicationName} | Environment: {_environment.EnvironmentName} | Content root: {_environment.ContentRootPath}";

    public string GetAppDeviceDescription() =>
        $"App: {_appInfo.Name} v{_appInfo.VersionString} [{_appInfo.AppGuid}] ({_appInfo.PackagingModel}, {_appInfo.RequestedTheme} theme) | " +
        $"Device: {_deviceInfo.Manufacturer} {_deviceInfo.Model} \"{_deviceInfo.Name}\" ({_deviceInfo.Platform}, {_deviceInfo.Idiom}, {_deviceInfo.DeviceType}, OS {_deviceInfo.VersionString})";

    // Only resolves once an installer (matching this app's AppGuid) has actually run on this
    // machine - see "Publishing with an installer" in the root README.md. Always null when
    // launched from the IDE/dotnet run, which is the expected case for this sample.
    public string GetInstallInfoDescription() =>
        _appInfo.InstallDate is { } installDate
            ? $"Installed: {installDate:yyyy-MM-dd} at {_appInfo.InstallLocation ?? "(unknown location)"}"
            : "Installed: (not installed through a matching installer - AppInfo.InstallDate/InstallLocation are null)";

    // AppInfo.Name/AppGuid and PublisherInfo.Name/Copyright come from the standard <Product>/
    // <Company>/<Version>/<Copyright> csproj properties; AppInfo.AppGuid and
    // PublisherInfo.Website/SupportEmail have no standard equivalent, so they come from the
    // explicit Barbatos.Wpf.ApplicationModel.* assembly metadata instead — see this sample's
    // .csproj and "Configuring AppInfo and PublisherInfo" in the root README.md.
    public string GetPublisherDescription() =>
        $"Publisher: {_publisherInfo.Name} | Website: {_publisherInfo.Website ?? "(not set)"} | " +
        $"Support: {_publisherInfo.SupportUrl ?? "(not set)"} / {_publisherInfo.SupportEmail ?? "(not set)"} | " +
        $"{_publisherInfo.Copyright ?? "(no copyright set)"}";

    public string GetVersionTrackingDescription() =>
        $"Current: {_versionTracking.CurrentVersion} (build {_versionTracking.CurrentBuild}) | " +
        $"Previous: {_versionTracking.PreviousVersion ?? "(none)"} | " +
        $"First launch ever: {_versionTracking.IsFirstLaunchEver} | First launch of this version: {_versionTracking.IsFirstLaunchForCurrentVersion}";

    public string GetConnectivityDescription() =>
        // .Distinct() because several physical/virtual adapters commonly map to the same
        // ConnectionProfile (this is why the static Connectivity.ConnectionProfiles facade
        // applies it too; IConnectivity.ConnectionProfiles itself does not, matching MAUI).
        $"{_connectivity.NetworkAccess} via [{string.Join(", ", _connectivity.ConnectionProfiles.Distinct())}]";

    public string GetDisplayInfoDescription()
    {
        // DeviceDisplay.MainDisplayInfo needs an active window to query monitor info from,
        // so this only returns a real value once the main window has been shown.
        var info = _deviceDisplay.MainDisplayInfo;
        return $"{info.Width}x{info.Height} @ {info.Density:0.0}x density, {info.Orientation}, {info.RefreshRate} Hz";
    }

    // Loaded on demand (not at startup) since GetHardwareFingerprintAsync() does real WMI
    // I/O the first time it runs - see "License enforcement: DeviceIdentity" in the root
    // README.md for what these are for and the privacy/compliance considerations.
    public async Task<string> GetDeviceIdentityDescriptionAsync()
    {
        var instanceId = await _deviceIdentity.GetInstanceIdAsync();
        var fingerprint = await _deviceIdentity.GetHardwareFingerprintAsync();

        return $"InstanceId (resets on reinstall): {instanceId} | HardwareFingerprint (survives reinstall, salted with AppGuid): {fingerprint}";
    }
}
