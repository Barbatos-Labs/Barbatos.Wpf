// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Management;
using System.Security.Cryptography;
using System.Text;
using Barbatos.Wpf.Storage;

namespace Barbatos.Wpf.ApplicationModel;

/// <summary>
/// The WPF implementation of <see cref="IDeviceIdentity"/>.
/// </summary>
class DeviceIdentityImplementation : IDeviceIdentity
{
    const string InstanceIdKey = "Barbatos.Wpf.DeviceIdentity.InstanceId";

    // WMI queries involve real (if small) COM/process overhead, and the underlying hardware
    // identifiers never change for the lifetime of this process, so this is computed once and
    // reused - mirroring how AppInfoImplementation caches its installed-app registry lookup.
    readonly Lazy<Task<string>> _hardwareFingerprint = new(ComputeHardwareFingerprintAsync);

    public async Task<string> GetInstanceIdAsync()
    {
        var existing = await SecureStorage.Default.GetAsync(InstanceIdKey).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(existing))
            return existing;

        var instanceId = Guid.NewGuid().ToString();
        await SecureStorage.Default.SetAsync(InstanceIdKey, instanceId).ConfigureAwait(false);
        return instanceId;
    }

    public Task<string> GetHardwareFingerprintAsync() => _hardwareFingerprint.Value;

    static Task<string> ComputeHardwareFingerprintAsync() => Task.Run(() =>
    {
        var hardwareValues = string.Join('|', new[]
            {
                GetWmiProperty("Win32_BaseBoard", "SerialNumber"),
                GetWmiProperty("Win32_BIOS", "SerialNumber"),
                GetWmiProperty("Win32_Processor", "ProcessorId"),
            }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

        if (string.IsNullOrEmpty(hardwareValues))
            hardwareValues = Environment.MachineName;

        // Salted with AppGuid so the resulting fingerprint is scoped to this app - like
        // Apple's IdentifierForVendor - instead of being directly comparable across unrelated
        // apps that happen to run on the same machine.
        var salted = AppInfo.Current.AppGuid + "|" + hardwareValues;

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(salted)));
    });

    /// <summary>
    /// Reads a single WMI property, returning <see langword="null"/> instead of throwing when
    /// WMI is unavailable or restricted (locked-down corporate policy, a sandboxed/CI
    /// environment, ...) - this is a best-effort signal, not something that should ever crash
    /// the caller.
    /// </summary>
    static string? GetWmiProperty(string wmiClass, string propertyName)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {wmiClass}");
            using var results = searcher.Get();

            foreach (ManagementBaseObject result in results)
            {
                using (result)
                {
                    if (result[propertyName]?.ToString() is { Length: > 0 } value)
                        return value.Trim();
                }
            }
        }
        catch
        {
            // no-op - see remarks above.
        }

        return null;
    }
}
