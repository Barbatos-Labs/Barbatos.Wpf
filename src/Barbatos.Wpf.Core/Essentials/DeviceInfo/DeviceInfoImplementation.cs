// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Barbatos.Wpf.Devices;

/// <summary>
/// The WPF implementation of <see cref="IDeviceInfo"/>, ported from .NET MAUI's Windows
/// <c>DeviceInfoImplementation</c> using Win32/registry APIs instead of WinRT.
/// </summary>
class DeviceInfoImplementation : IDeviceInfo
{
    const string BiosKeyPath = @"HARDWARE\DESCRIPTION\System\BIOS";

    DeviceType currentType = DeviceType.Unknown;
    string? systemProductName;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceInfoImplementation"/> class.
    /// </summary>
    public DeviceInfoImplementation()
    {
        try
        {
            systemProductName = GetBiosValue("SystemProductName");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to get system product name. {ex.Message}");
        }
    }

    public string Model => GetBiosValue("SystemProductName") ?? string.Empty;

    public string Manufacturer => GetBiosValue("SystemManufacturer") ?? string.Empty;

    public string Name => Environment.MachineName;

    public string VersionString => Version.ToString();

    public Version Version => Environment.OSVersion.Version;

    public DevicePlatform Platform => DevicePlatform.WPF;

    public DeviceIdiom Idiom => GetIsInTabletMode()
        ? DeviceIdiom.Tablet
        : DeviceIdiom.Desktop;

    public DeviceType DeviceType
    {
        get
        {
            if (currentType != DeviceType.Unknown)
                return currentType;

            try
            {
                if (string.IsNullOrWhiteSpace(systemProductName))
                    systemProductName = GetBiosValue("SystemProductName");

                var isVirtual =
                    (systemProductName?.Contains("Virtual", StringComparison.Ordinal) ?? false) ||
                    systemProductName == "HMV domU";

                currentType = isVirtual ? DeviceType.Virtual : DeviceType.Physical;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get device type. {ex.Message}");
            }

            return currentType;
        }
    }

    static string? GetBiosValue(string name)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(BiosKeyPath);
            return key?.GetValue(name) as string;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Whether or not the device is in "tablet mode" or not. This
    /// has to be implemented by the device manufacturer.
    /// </summary>
    const int SM_CONVERTIBLESLATEMODE = 0x2003;

    /// <summary>
    /// How many fingers (aka touches) are supported for touch control
    /// </summary>
    const int SM_MAXIMUMTOUCHES = 95;

    /// <summary>
    /// Whether a physical keyboard is attached or not.
    /// Manufacturers have to remember to set this.
    /// Defaults to not-attached.
    /// </summary>
    const int SM_ISDOCKED = 0x2004;

    static bool GetIsInTabletMode()
    {
        // Adopt Chromium's methodology for determining tablet mode
        // https://source.chromium.org/chromium/chromium/src/+/main:base/win/win_util.cc
        // Device does not have a touchscreen
        if (NativeMethods.GetSystemMetrics(SM_MAXIMUMTOUCHES) == 0)
        {
            return false;
        }

        // If the device is docked, user is treating as a PC
        if (NativeMethods.GetSystemMetrics(SM_ISDOCKED) != 0)
        {
            return false;
        }

        // Fetch device rotation. Possible for this to fail.
        var rotationState = AutoRotationState.AR_ENABLED;
        var success = NativeMethods.GetAutoRotationState(ref rotationState);

        // Fetch succeeded and device does not support rotation
        if (success && (rotationState & (AutoRotationState.AR_NOT_SUPPORTED | AutoRotationState.AR_LAPTOP | AutoRotationState.AR_NOSENSOR)) != 0)
        {
            return false;
        }

        // Check if power management says we are mobile (laptop) or a tablet
        if ((NativeMethods.PowerDeterminePlatformRoleEx(2) & (PowerPlatformRole.PlatformRoleMobile | PowerPlatformRole.PlatformRoleSlate)) != 0)
        {
            // Check if tablet mode is 0. 0 is default value.
            return NativeMethods.GetSystemMetrics(SM_CONVERTIBLESLATEMODE) == 0;
        }

        return false;
    }

    static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool GetAutoRotationState(ref AutoRotationState pState);

        [DllImport("Powrprof.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern PowerPlatformRole PowerDeterminePlatformRoleEx(ulong Version);
    }
}

/// <summary>
/// Represents the OEM's preferred power management profile,
/// Useful in-case OEM implements one but not the other
/// </summary>
enum PowerPlatformRole
{
    PlatformRoleMobile = 2,
    PlatformRoleSlate = 8,
}

/// <summary>
/// Whether rotation is supported or not.
/// Rotation is only supported if AR_ENABLED is true
/// </summary>
enum AutoRotationState
{
    AR_ENABLED = 0x0,
    AR_NOT_SUPPORTED = 0x20,
    AR_LAPTOP = 0x80,
    AR_NOSENSOR = 0x10,
}
