// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Devices.Sensors;

/// <summary>
/// Represents levels of accuracy when determining the device location.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>GeolocationAccuracy</c>.</remarks>
public enum GeolocationAccuracy
{
    /// <summary>Represents default accuracy (Medium), typically within 30-500 meters.</summary>
    Default = 0,

    /// <summary>Represents the lowest accuracy, using the least power to obtain and typically within 1000-5000 meters.</summary>
    Lowest = 1,

    /// <summary>Represents low accuracy, typically within 300-3000 meters.</summary>
    Low = 2,

    /// <summary>Represents medium accuracy, typically within 30-500 meters.</summary>
    Medium = 3,

    /// <summary>Represents high accuracy, typically within 10-100 meters.</summary>
    High = 4,

    /// <summary>Represents the best accuracy, using the most power to obtain and typically within 10 meters.</summary>
    Best = 5,
}

/// <summary>
/// Represents the criteria for a location request.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>GeolocationRequest</c>.</remarks>
public class GeolocationRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeolocationRequest"/> class with default options.
    /// </summary>
    public GeolocationRequest()
    {
        Timeout = TimeSpan.Zero;
        DesiredAccuracy = GeolocationAccuracy.Default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GeolocationRequest"/> class with the specified accuracy.
    /// </summary>
    /// <param name="accuracy">The desired accuracy for determining the location.</param>
    public GeolocationRequest(GeolocationAccuracy accuracy)
    {
        Timeout = TimeSpan.Zero;
        DesiredAccuracy = accuracy;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GeolocationRequest"/> class with the specified accuracy and timeout.
    /// </summary>
    /// <param name="accuracy">The desired accuracy for determining the location.</param>
    /// <param name="timeout">A timeout value after which the location determination will be cancelled.</param>
    public GeolocationRequest(GeolocationAccuracy accuracy, TimeSpan timeout)
    {
        Timeout = timeout;
        DesiredAccuracy = accuracy;
    }

    /// <summary>
    /// Gets or sets the location request timeout.
    /// </summary>
    public TimeSpan Timeout { get; set; }

    /// <summary>
    /// Gets or sets the desired accuracy of the resulting location.
    /// </summary>
    public GeolocationAccuracy DesiredAccuracy { get; set; }

    /// <inheritdoc />
    public override string ToString() =>
        $"{nameof(DesiredAccuracy)}: {DesiredAccuracy}, {nameof(Timeout)}: {Timeout}";
}

/// <summary>
/// Request options for listening to geolocation updates.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>GeolocationListeningRequest</c>.</remarks>
public class GeolocationListeningRequest
{
    /// <summary>
    /// Creates a new request object with default values.
    /// </summary>
    public GeolocationListeningRequest()
        : this(GeolocationAccuracy.Default)
    {
    }

    /// <summary>
    /// Creates a new request object with given accuracy.
    /// </summary>
    /// <param name="accuracy">The desired geolocation accuracy.</param>
    public GeolocationListeningRequest(GeolocationAccuracy accuracy)
        : this(accuracy, TimeSpan.FromSeconds(1))
    {
    }

    /// <summary>
    /// Creates a new request object with given accuracy and minimum time.
    /// </summary>
    /// <param name="accuracy">The desired geolocation accuracy.</param>
    /// <param name="minimumTime">The minimum time between location updates being sent.</param>
    public GeolocationListeningRequest(GeolocationAccuracy accuracy, TimeSpan minimumTime)
    {
        DesiredAccuracy = accuracy;
        MinimumTime = minimumTime;
    }

    /// <summary>
    /// Minimum time between location updates being sent. This value must be positive.
    /// </summary>
    public TimeSpan MinimumTime { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The desired minimum accuracy for the location updates being sent.
    /// </summary>
    public GeolocationAccuracy DesiredAccuracy { get; set; } = GeolocationAccuracy.Default;
}

/// <summary>
/// Error values for listening for geolocation changes.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>GeolocationError</c>.</remarks>
public enum GeolocationError
{
    /// <summary>The provider was unable to retrieve any position data.</summary>
    PositionUnavailable,

    /// <summary>The app is not, or no longer, authorized to receive location data.</summary>
    Unauthorized,
}

/// <summary>
/// Event args for the geolocation listening error event.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>GeolocationListeningFailedEventArgs</c>.</remarks>
public class GeolocationListeningFailedEventArgs : EventArgs
{
    /// <summary>
    /// Creates a new geolocation error event args object.
    /// </summary>
    /// <param name="geolocationError">The geolocation error to use for this object.</param>
    public GeolocationListeningFailedEventArgs(GeolocationError geolocationError)
    {
        Error = geolocationError;
    }

    /// <summary>
    /// The geolocation error that describes the error that occurred.
    /// </summary>
    public GeolocationError Error { get; }
}

/// <summary>
/// Event arguments containing the current reading of <see cref="IGeolocation.LocationChanged"/>.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>GeolocationLocationChangedEventArgs</c>.</remarks>
public class GeolocationLocationChangedEventArgs : EventArgs
{
    /// <summary>
    /// Public constructor that takes in a reading for event arguments.
    /// </summary>
    /// <param name="location">The location data reading.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="location"/> is <see langword="null"/>.</exception>
    public GeolocationLocationChangedEventArgs(Location location)
    {
        Location = location ?? throw new ArgumentNullException(nameof(location));
    }

    /// <summary>
    /// The current reading's location data.
    /// </summary>
    public Location Location { get; }
}
