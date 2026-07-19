// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Devices;

/// <summary>
/// Types of devices.
/// </summary>
public enum DeviceType
{
    /// <summary>An unknown device type.</summary>
    Unknown = 0,

    /// <summary>The device is a physical device, such as a Windows desktop or laptop.</summary>
    Physical = 1,

    /// <summary>The device is virtual, such as a virtual machine.</summary>
    Virtual = 2,
}

/// <summary>
/// Represents information about the device.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>IDeviceInfo</c>.</remarks>
public interface IDeviceInfo
{
    /// <summary>
    /// Gets the model of the device.
    /// </summary>
    string Model { get; }

    /// <summary>
    /// Gets the manufacturer of the device.
    /// </summary>
    string Manufacturer { get; }

    /// <summary>
    /// Gets the name of the device.
    /// </summary>
    /// <remarks>This value is often specified by the user of the device.</remarks>
    string Name { get; }

    /// <summary>
    /// Gets the string representation of the version of the operating system.
    /// </summary>
    string VersionString { get; }

    /// <summary>
    /// Gets the version of the operating system.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Gets the platform or operating system of the device.
    /// </summary>
    DevicePlatform Platform { get; }

    /// <summary>
    /// Gets the idiom (form factor) of the device.
    /// </summary>
    DeviceIdiom Idiom { get; }

    /// <summary>
    /// Gets the type of device the application is running on.
    /// </summary>
    DeviceType DeviceType { get; }
}

/// <summary>
/// Represents information about the device.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>DeviceInfo</c>.</remarks>
public static class DeviceInfo
{
    /// <inheritdoc cref="IDeviceInfo.Model" />
    public static string Model => Current.Model;

    /// <inheritdoc cref="IDeviceInfo.Manufacturer" />
    public static string Manufacturer => Current.Manufacturer;

    /// <inheritdoc cref="IDeviceInfo.Name" />
    public static string Name => Current.Name;

    /// <inheritdoc cref="IDeviceInfo.VersionString" />
    public static string VersionString => Current.VersionString;

    /// <inheritdoc cref="IDeviceInfo.Version" />
    public static Version Version => Current.Version;

    /// <inheritdoc cref="IDeviceInfo.Platform" />
    public static DevicePlatform Platform => Current.Platform;

    /// <inheritdoc cref="IDeviceInfo.Idiom" />
    public static DeviceIdiom Idiom => Current.Idiom;

    /// <inheritdoc cref="IDeviceInfo.DeviceType" />
    public static DeviceType DeviceType => Current.DeviceType;

    static IDeviceInfo? currentImplementation;

    /// <summary>
    /// Provides the default implementation for static usage of this API.
    /// </summary>
    public static IDeviceInfo Current =>
        currentImplementation ??= new DeviceInfoImplementation();

    internal static void SetCurrent(IDeviceInfo? implementation) =>
        currentImplementation = implementation;
}
