// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Devices;

/// <summary>
/// Represents the orientation a device display can have.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>DisplayOrientation</c>.</remarks>
public enum DisplayOrientation
{
    /// <summary>Unknown display orientation.</summary>
    Unknown = 0,

    /// <summary>Device display is in portrait orientation.</summary>
    Portrait = 1,

    /// <summary>Device display is in landscape orientation.</summary>
    Landscape = 2,
}

/// <summary>
/// Represents the rotation a device display can have.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>DisplayRotation</c>.</remarks>
public enum DisplayRotation
{
    /// <summary>Unknown display rotation.</summary>
    Unknown = 0,

    /// <summary>The device display is rotated 0 degrees.</summary>
    Rotation0 = 1,

    /// <summary>The device display is rotated 90 degrees.</summary>
    Rotation90 = 2,

    /// <summary>The device display is rotated 180 degrees.</summary>
    Rotation180 = 3,

    /// <summary>The device display is rotated 270 degrees.</summary>
    Rotation270 = 4,
}

/// <summary>
/// Represents information about the device's screen.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>DisplayInfo</c>.</remarks>
public readonly struct DisplayInfo : IEquatable<DisplayInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DisplayInfo"/> struct.
    /// </summary>
    /// <param name="width">The width of the display.</param>
    /// <param name="height">The height of the display.</param>
    /// <param name="density">The screen density.</param>
    /// <param name="orientation">The current orientation.</param>
    /// <param name="rotation">The rotation of the device.</param>
    public DisplayInfo(double width, double height, double density, DisplayOrientation orientation, DisplayRotation rotation)
    {
        Width = width;
        Height = height;
        Density = density;
        Orientation = orientation;
        Rotation = rotation;
        RefreshRate = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DisplayInfo"/> struct.
    /// </summary>
    /// <param name="width">The width of the display.</param>
    /// <param name="height">The height of the display.</param>
    /// <param name="density">The screen density.</param>
    /// <param name="orientation">The current orientation.</param>
    /// <param name="rotation">The rotation of the device.</param>
    /// <param name="rate">The refresh rate of the display.</param>
    public DisplayInfo(double width, double height, double density, DisplayOrientation orientation, DisplayRotation rotation, float rate)
    {
        Width = width;
        Height = height;
        Density = density;
        Orientation = orientation;
        Rotation = rotation;
        RefreshRate = rate;
    }

    /// <summary>
    /// Gets the width of the screen (in pixels) for the current <see cref="Orientation"/>.
    /// </summary>
    public double Width { get; }

    /// <summary>
    /// Gets the height of the screen (in pixels) for the current <see cref="Orientation"/>.
    /// </summary>
    public double Height { get; }

    /// <summary>
    /// Gets a value representing the screen density.
    /// </summary>
    /// <remarks>
    /// The density is the scaling factor between physical pixels and scaled (logical) pixels.
    /// On a monitor whose Windows display scale is set to 100%, the density is 1.0; at 200%
    /// scale it is 2.0.
    /// </remarks>
    public double Density { get; }

    /// <summary>
    /// Gets the orientation of the device's display.
    /// </summary>
    public DisplayOrientation Orientation { get; }

    /// <summary>
    /// Gets the rotation of the device's display.
    /// </summary>
    public DisplayRotation Rotation { get; }

    /// <summary>
    /// Gets the refresh rate (in Hertz) of the device's display.
    /// </summary>
    public float RefreshRate { get; }

    public static bool operator ==(DisplayInfo left, DisplayInfo right) =>
        left.Equals(right);

    public static bool operator !=(DisplayInfo left, DisplayInfo right) =>
        !left.Equals(right);

    /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
    public override bool Equals(object? obj) =>
        obj is DisplayInfo metrics && Equals(metrics);

    /// <summary>
    /// Compares the underlying <see cref="DisplayInfo"/> instances.
    /// </summary>
    /// <remarks>Equality is established by comparing if <see cref="Height"/>, <see cref="Width"/>, <see cref="Density"/>, <see cref="Orientation"/> and <see cref="Rotation"/> are all equal.</remarks>
    public bool Equals(DisplayInfo other) =>
        Width.Equals(other.Width) &&
        Height.Equals(other.Height) &&
        Density.Equals(other.Density) &&
        Orientation.Equals(other.Orientation) &&
        Rotation.Equals(other.Rotation);

    /// <inheritdoc />
    public override int GetHashCode() =>
        (Height, Width, Density, Orientation, Rotation).GetHashCode();

    /// <inheritdoc />
    public override string ToString() =>
        $"{nameof(Height)}: {Height}, {nameof(Width)}: {Width}, " +
        $"{nameof(Density)}: {Density}, {nameof(Orientation)}: {Orientation}, " +
        $"{nameof(Rotation)}: {Rotation}";
}
