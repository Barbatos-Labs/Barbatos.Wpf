// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Devices.Sensors;

/// <summary>Distance unit for use in conversion.</summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>DistanceUnits</c>.</remarks>
public enum DistanceUnits
{
    /// <summary>Kilometers.</summary>
    Kilometers,

    /// <summary>Miles.</summary>
    Miles,
}

/// <summary>
/// Indicates the altitude reference system to be used in defining a location.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>AltitudeReferenceSystem</c>.</remarks>
public enum AltitudeReferenceSystem
{
    /// <summary>The altitude reference system was not specified.</summary>
    Unspecified = 0,

    /// <summary>The altitude reference system is based on distance above terrain or ground level.</summary>
    Terrain = 1,

    /// <summary>The altitude reference system is based on an ellipsoid (usually WGS84), which is a mathematical approximation of the shape of the Earth.</summary>
    Ellipsoid = 2,

    /// <summary>The altitude reference system is based on the distance above sea level (parametrized by a so-called Geoid).</summary>
    Geoid = 3,

    /// <summary>The altitude reference system is based on the distance above the tallest surface structures, such as buildings, trees, roads, etc., above terrain or ground level.</summary>
    Surface = 4,
}

/// <summary>
/// Represents a physical location with the latitude, longitude, altitude and time information reported by the device.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>Location</c> — pure C#, ported verbatim.</remarks>
public class Location
{
    const double MeanEarthRadiusInKilometers = 6371.0;
    const double MilesToKilometers = 1.609344;
    const double KilometersToMiles = 1.0 / MilesToKilometers;

    /// <summary>
    /// Initializes a new instance of the <see cref="Location"/> class.
    /// </summary>
    public Location()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Location"/> class with the specified latitude and longitude.
    /// </summary>
    /// <param name="latitude">Latitude in degrees. Must be in the interval [-90, 90].</param>
    /// <param name="longitude">Longitude in degrees. Will be projected to the interval (-180, 180].</param>
    public Location(double latitude, double longitude) : this(latitude, longitude, DateTimeOffset.UtcNow)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Location"/> class with the specified latitude, longitude, and timestamp.
    /// </summary>
    /// <param name="latitude">Latitude in degrees. Must be in the interval [-90, 90].</param>
    /// <param name="longitude">Longitude in degrees. Will be projected to the interval (-180, 180].</param>
    /// <param name="timestamp">UTC timestamp for the location.</param>
    public Location(double latitude, double longitude, DateTimeOffset timestamp)
    {
        if (Math.Abs(latitude) > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude));
        Latitude = latitude;

        if (Math.Abs(longitude) > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude));
        Longitude = longitude;

        Timestamp = timestamp;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Location"/> class with the specified latitude, longitude, and altitude.
    /// </summary>
    /// <param name="latitude">Latitude in degrees. Must be in the interval [-90, 90].</param>
    /// <param name="longitude">Longitude in degrees. Will be projected to the interval (-180, 180].</param>
    /// <param name="altitude">Altitude in meters.</param>
    public Location(double latitude, double longitude, double altitude) : this(latitude, longitude)
    {
        Altitude = altitude;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Location"/> class from an existing instance.
    /// </summary>
    /// <param name="point">A <see cref="Location"/> instance that will be used to clone.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="point"/> is <see langword="null"/>.</exception>
    public Location(Location point)
    {
        _ = point ?? throw new ArgumentNullException(nameof(point));

        Latitude = point.Latitude;
        Longitude = point.Longitude;
        Timestamp = DateTime.UtcNow;
        Altitude = point.Altitude;
        AltitudeReferenceSystem = point.AltitudeReferenceSystem;
        Accuracy = point.Accuracy;
        VerticalAccuracy = point.VerticalAccuracy;
        ReducedAccuracy = point.ReducedAccuracy;
        Speed = point.Speed;
        Course = point.Course;
        IsFromMockProvider = point.IsFromMockProvider;
    }

    /// <summary>
    /// Gets or sets the timestamp of the location in UTC.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the latitude coordinate of this location.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Gets or sets the longitude coordinate of this location.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Gets the altitude in meters (if available) in a reference system which is specified by <see cref="AltitudeReferenceSystem"/>.
    /// </summary>
    /// <remarks>Returns 0 or <see langword="null"/> if not available.</remarks>
    public double? Altitude { get; set; }

    /// <summary>
    /// Gets or sets the horizontal accuracy (in meters) of the location.
    /// </summary>
    public double? Accuracy { get; set; }

    /// <summary>
    /// Gets or sets the vertical accuracy (in meters) of the location.
    /// </summary>
    public double? VerticalAccuracy { get; set; }

    /// <summary>
    /// Gets or sets whether this location has a reduced accuracy reading.
    /// </summary>
    public bool ReducedAccuracy { get; set; }

    /// <summary>
    /// Gets or sets the current speed in meters per second at the time when this location was determined.
    /// </summary>
    /// <remarks>Returns 0 or <see langword="null"/> if not available. Otherwise the value will range between 0-360.</remarks>
    public double? Speed { get; set; }

    /// <summary>
    /// Gets or sets the current degrees relative to true north at the time when this location was determined.
    /// </summary>
    /// <remarks>Returns 0 or <see langword="null"/> if not available.</remarks>
    public double? Course { get; set; }

    /// <summary>
    /// Gets or sets whether this location originates from a mocked sensor and thus might not be the real location of the device.
    /// </summary>
    public bool IsFromMockProvider { get; set; }

    /// <summary>
    /// Specifies the reference system in which the <see cref="Altitude"/> value is expressed.
    /// </summary>
    public AltitudeReferenceSystem AltitudeReferenceSystem { get; set; }

    /// <summary>
    /// Calculate distance between two locations.
    /// </summary>
    public static double CalculateDistance(double latitudeStart, double longitudeStart, Location locationEnd, DistanceUnits units) =>
        CalculateDistance(latitudeStart, longitudeStart, locationEnd.Latitude, locationEnd.Longitude, units);

    /// <summary>
    /// Calculate distance between two locations.
    /// </summary>
    public static double CalculateDistance(Location locationStart, double latitudeEnd, double longitudeEnd, DistanceUnits units) =>
        CalculateDistance(locationStart.Latitude, locationStart.Longitude, latitudeEnd, longitudeEnd, units);

    /// <summary>
    /// Calculate distance between two locations.
    /// </summary>
    public static double CalculateDistance(Location locationStart, Location locationEnd, DistanceUnits units) =>
        CalculateDistance(locationStart.Latitude, locationStart.Longitude, locationEnd.Latitude, locationEnd.Longitude, units);

    /// <summary>
    /// Calculate distance between two <see cref="Location"/> instances.
    /// </summary>
    public static double CalculateDistance(
        double latitudeStart,
        double longitudeStart,
        double latitudeEnd,
        double longitudeEnd,
        DistanceUnits units)
    {
        var kilometers = CoordinatesToKilometers(latitudeStart, longitudeStart, latitudeEnd, longitudeEnd);

        return units switch
        {
            DistanceUnits.Kilometers => kilometers,
            DistanceUnits.Miles => kilometers * KilometersToMiles,
            _ => throw new ArgumentOutOfRangeException(nameof(units)),
        };
    }

    static double CoordinatesToKilometers(double lat1, double lon1, double lat2, double lon2)
    {
        if (lat1 == lat2 && lon1 == lon2)
            return 0;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        lat1 = DegreesToRadians(lat1);
        lat2 = DegreesToRadians(lat2);

        var dLat2 = Math.Sin(dLat / 2) * Math.Sin(dLat / 2);
        var dLon2 = Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var a = dLat2 + dLon2 * Math.Cos(lat1) * Math.Cos(lat2);
        var c = 2 * Math.Asin(Math.Sqrt(a));

        return MeanEarthRadiusInKilometers * c;
    }

    static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    /// <inheritdoc />
    public override string ToString() =>
        $"{nameof(Latitude)}: {Latitude}, " +
        $"{nameof(Longitude)}: {Longitude}, " +
        $"{nameof(Altitude)}: {Altitude}, " +
        $"{nameof(Accuracy)}: {Accuracy}, " +
        $"{nameof(VerticalAccuracy)}: {VerticalAccuracy}, " +
        $"{nameof(Speed)}: {Speed}, " +
        $"{nameof(Course)}: {Course}, " +
        $"{nameof(Timestamp)}: {Timestamp}";

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        var other = (Location)obj;
        return Latitude == other.Latitude && Longitude == other.Longitude;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Latitude.GetHashCode();
            hashCode = (hashCode * 397) ^ Longitude.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(Location? left, Location? right) => Equals(left, right);

    public static bool operator !=(Location? left, Location? right) => !Equals(left, right);
}
