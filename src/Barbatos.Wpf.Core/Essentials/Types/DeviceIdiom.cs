// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Devices;

/// <summary>
/// Represents the idiom (form factor) of the device.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>DeviceIdiom</c>.</remarks>
public readonly struct DeviceIdiom : IEquatable<DeviceIdiom>
{
    readonly string? deviceIdiom;

    /// <summary>
    /// Gets an instance of <see cref="DeviceIdiom"/> that represents a (mobile) phone idiom.
    /// </summary>
    public static DeviceIdiom Phone { get; } = new DeviceIdiom(nameof(Phone));

    /// <summary>
    /// Gets an instance of <see cref="DeviceIdiom"/> that represents a tablet idiom.
    /// </summary>
    public static DeviceIdiom Tablet { get; } = new DeviceIdiom(nameof(Tablet));

    /// <summary>
    /// Gets an instance of <see cref="DeviceIdiom"/> that represents a desktop computer idiom.
    /// </summary>
    public static DeviceIdiom Desktop { get; } = new DeviceIdiom(nameof(Desktop));

    /// <summary>
    /// Gets an instance of <see cref="DeviceIdiom"/> that represents a television (TV) idiom.
    /// </summary>
    public static DeviceIdiom TV { get; } = new DeviceIdiom(nameof(TV));

    /// <summary>
    /// Gets an instance of <see cref="DeviceIdiom"/> that represents a watch idiom.
    /// </summary>
    public static DeviceIdiom Watch { get; } = new DeviceIdiom(nameof(Watch));

    /// <summary>
    /// Gets an instance of <see cref="DeviceIdiom"/> that represents an unknown idiom.
    /// </summary>
    public static DeviceIdiom Unknown { get; } = new DeviceIdiom(nameof(Unknown));

    DeviceIdiom(string deviceIdiom)
    {
        if (deviceIdiom == null)
            throw new ArgumentNullException(nameof(deviceIdiom));

        if (deviceIdiom.Length == 0)
            throw new ArgumentException(nameof(deviceIdiom));

        this.deviceIdiom = deviceIdiom;
    }

    /// <summary>
    /// Creates a new device idiom instance. This can be used to define your custom idioms.
    /// </summary>
    public static DeviceIdiom Create(string deviceIdiom) =>
        new(deviceIdiom);

    /// <summary>
    /// Compares the underlying <see cref="DeviceIdiom"/> instances.
    /// </summary>
    public bool Equals(DeviceIdiom other) =>
        Equals(other.deviceIdiom);

    internal bool Equals(string? other) =>
        string.Equals(deviceIdiom, other, StringComparison.Ordinal);

    /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
    public override bool Equals(object? obj) =>
        obj is DeviceIdiom other && Equals(other);

    /// <summary>
    /// Gets the hash code for this idiom instance.
    /// </summary>
    public override int GetHashCode() =>
        deviceIdiom == null ? 0 : deviceIdiom.GetHashCode(StringComparison.Ordinal);

    /// <summary>
    /// Returns a string representation of the current device idiom.
    /// </summary>
    public override string ToString() =>
        deviceIdiom ?? string.Empty;

    public static bool operator ==(DeviceIdiom left, DeviceIdiom right) =>
        left.Equals(right);

    public static bool operator !=(DeviceIdiom left, DeviceIdiom right) =>
        !left.Equals(right);
}
