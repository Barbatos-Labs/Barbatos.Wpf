// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Devices;

/// <summary>
/// Represents the device platform that the application is running on.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>DevicePlatform</c>.</remarks>
public readonly struct DevicePlatform : IEquatable<DevicePlatform>
{
    readonly string? devicePlatform;

    /// <summary>
    /// Gets an instance of <see cref="DevicePlatform"/> that represents WPF.
    /// </summary>
    public static DevicePlatform WPF { get; } = new DevicePlatform(nameof(WPF));

    /// <summary>
    /// Gets an instance of <see cref="DevicePlatform"/> that represents WinUI.
    /// </summary>
    public static DevicePlatform WinUI { get; } = new DevicePlatform(nameof(WinUI));

    /// <summary>
    /// Gets an instance of <see cref="DevicePlatform"/> that represents an unknown platform.
    /// </summary>
    public static DevicePlatform Unknown { get; } = new DevicePlatform(nameof(Unknown));

    DevicePlatform(string devicePlatform)
    {
        if (devicePlatform == null)
            throw new ArgumentNullException(nameof(devicePlatform));

        if (devicePlatform.Length == 0)
            throw new ArgumentException(nameof(devicePlatform));

        this.devicePlatform = devicePlatform;
    }

    /// <summary>
    /// Creates a new device platform instance. This can be used to define your custom platforms.
    /// </summary>
    public static DevicePlatform Create(string devicePlatform) =>
        new(devicePlatform);

    /// <summary>
    /// Compares the underlying <see cref="DevicePlatform"/> instances.
    /// </summary>
    public bool Equals(DevicePlatform other) =>
        Equals(other.devicePlatform);

    internal bool Equals(string? other) =>
        string.Equals(devicePlatform, other, StringComparison.Ordinal);

    /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
    public override bool Equals(object? obj) =>
        obj is DevicePlatform other && Equals(other);

    /// <summary>
    /// Gets the hash code for this platform instance.
    /// </summary>
    public override int GetHashCode() =>
        devicePlatform == null ? 0 : devicePlatform.GetHashCode(StringComparison.Ordinal);

    /// <summary>
    /// Returns a string representation of the current value of the device platform.
    /// </summary>
    public override string ToString() =>
        devicePlatform ?? string.Empty;

    public static bool operator ==(DevicePlatform left, DevicePlatform right) =>
        left.Equals(right);

    public static bool operator !=(DevicePlatform left, DevicePlatform right) =>
        !left.Equals(right);
}
