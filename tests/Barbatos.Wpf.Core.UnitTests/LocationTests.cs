// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Devices.Sensors;

namespace Barbatos.Wpf.Core.UnitTests;

public class LocationTests
{
    [Fact]
    public void ConstructorSetsLatitudeAndLongitude()
    {
        var location = new Location(10.5, -20.25);

        Assert.Equal(10.5, location.Latitude);
        Assert.Equal(-20.25, location.Longitude);
    }

    [Theory]
    [InlineData(90.1)]
    [InlineData(-90.1)]
    public void ConstructorRejectsOutOfRangeLatitude(double latitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Location(latitude, 0));
    }

    [Theory]
    [InlineData(180.1)]
    [InlineData(-180.1)]
    public void ConstructorRejectsOutOfRangeLongitude(double longitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Location(0, longitude));
    }

    [Fact]
    public void CopyConstructorClonesAllFields()
    {
        var original = new Location(1, 2)
        {
            Altitude = 100,
            Accuracy = 5,
            VerticalAccuracy = 3,
            Speed = 2,
            Course = 90,
            IsFromMockProvider = true,
            AltitudeReferenceSystem = AltitudeReferenceSystem.Geoid,
        };

        var copy = new Location(original);

        Assert.Equal(original.Latitude, copy.Latitude);
        Assert.Equal(original.Longitude, copy.Longitude);
        Assert.Equal(original.Altitude, copy.Altitude);
        Assert.Equal(original.Accuracy, copy.Accuracy);
        Assert.Equal(original.VerticalAccuracy, copy.VerticalAccuracy);
        Assert.Equal(original.Speed, copy.Speed);
        Assert.Equal(original.Course, copy.Course);
        Assert.Equal(original.IsFromMockProvider, copy.IsFromMockProvider);
        Assert.Equal(original.AltitudeReferenceSystem, copy.AltitudeReferenceSystem);
    }

    [Fact]
    public void CopyConstructorThrowsForNull()
    {
        Assert.Throws<ArgumentNullException>(() => new Location(null!));
    }

    [Fact]
    public void SameCoordinatesHaveZeroDistance()
    {
        var distance = Location.CalculateDistance(1, 1, 1, 1, DistanceUnits.Kilometers);

        Assert.Equal(0, distance);
    }

    [Fact]
    public void CalculateDistanceMatchesKnownCoordinates()
    {
        // Hanoi to Ho Chi Minh City, ~1140-1150 km great-circle distance.
        var km = Location.CalculateDistance(21.0278, 105.8342, 10.8231, 106.6297, DistanceUnits.Kilometers);

        Assert.InRange(km, 1100, 1200);
    }

    [Fact]
    public void MilesAreLessThanKilometersForTheSameDistance()
    {
        var km = Location.CalculateDistance(0, 0, 10, 10, DistanceUnits.Kilometers);
        var miles = Location.CalculateDistance(0, 0, 10, 10, DistanceUnits.Miles);

        Assert.True(miles < km);
        Assert.True(miles > 0);
    }

    [Fact]
    public void CalculateDistanceOverloadsAgree()
    {
        var start = new Location(0, 0);
        var end = new Location(5, 5);

        var a = Location.CalculateDistance(start, end, DistanceUnits.Kilometers);
        var b = Location.CalculateDistance(start.Latitude, start.Longitude, end, DistanceUnits.Kilometers);
        var c = Location.CalculateDistance(start, end.Latitude, end.Longitude, DistanceUnits.Kilometers);

        Assert.Equal(a, b);
        Assert.Equal(a, c);
    }

    [Fact]
    public void EqualityIsBasedOnLatitudeAndLongitude()
    {
        var a = new Location(1, 2) { Speed = 5 };
        var b = new Location(1, 2) { Speed = 99 };
        var c = new Location(1, 3);

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.NotEqual(a, c);
        Assert.True(a != c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToStringIncludesTheCoordinates()
    {
        var location = new Location(1.5, 2.5);

        Assert.Contains("1.5", location.ToString());
        Assert.Contains("2.5", location.ToString());
    }
}
