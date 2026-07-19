// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.ApplicationModel;
using Barbatos.Wpf.Devices.Sensors;

namespace Barbatos.Wpf.Core.UnitTests;

public class GeolocationTests
{
    [Fact]
    public void DefaultIsCached()
    {
        Assert.NotNull(Geolocation.Default);
        Assert.Same(Geolocation.Default, Geolocation.Default);
    }

    [Fact]
    public void IsEnabledAndIsListeningForegroundAreFalse()
    {
        // Windows does not support this without the WinRT Geolocator contract, which is
        // unavailable to an unpackaged WPF app. See the remarks on Geolocation.
        Assert.False(Geolocation.IsEnabled);
        Assert.False(Geolocation.IsListeningForeground);
    }

    [Fact]
    public async Task GetLastKnownLocationAsyncThrowsFeatureNotSupported()
    {
        await Assert.ThrowsAsync<FeatureNotSupportedException>(() => Geolocation.GetLastKnownLocationAsync());
    }

    [Fact]
    public async Task GetLocationAsyncThrowsFeatureNotSupported()
    {
        await Assert.ThrowsAsync<FeatureNotSupportedException>(() => Geolocation.GetLocationAsync(new GeolocationRequest()));
    }

    [Fact]
    public async Task StartListeningForegroundAsyncThrowsFeatureNotSupported()
    {
        await Assert.ThrowsAsync<FeatureNotSupportedException>(() => Geolocation.StartListeningForegroundAsync(new GeolocationListeningRequest()));
    }

    [Fact]
    public void StopListeningForegroundIsANoOp()
    {
        // Listening can never have been started successfully, so stopping must not throw.
        Geolocation.StopListeningForeground();
    }

    [Fact]
    public void GeolocationRequestDefaultsToMediumAccuracyAndNoTimeout()
    {
        var request = new GeolocationRequest();

        Assert.Equal(GeolocationAccuracy.Default, request.DesiredAccuracy);
        Assert.Equal(TimeSpan.Zero, request.Timeout);
    }

    [Fact]
    public void GeolocationListeningRequestDefaultsToOneSecond()
    {
        var request = new GeolocationListeningRequest();

        Assert.Equal(TimeSpan.FromSeconds(1), request.MinimumTime);
    }

    [Fact]
    public void LocationChangedEventArgsThrowsForNullLocation()
    {
        Assert.Throws<ArgumentNullException>(() => new GeolocationLocationChangedEventArgs(null!));
    }

    [Fact]
    public void ListeningFailedEventArgsExposesTheError()
    {
        var args = new GeolocationListeningFailedEventArgs(GeolocationError.Unauthorized);

        Assert.Equal(GeolocationError.Unauthorized, args.Error);
    }
}
