// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.ApplicationModel;

namespace Barbatos.Wpf.Devices.Sensors;

/// <summary>
/// The Windows implementation of <see cref="IGeolocation"/>. See the remarks on
/// <see cref="Geolocation"/> for why every member throws <see cref="FeatureNotSupportedException"/>.
/// </summary>
class GeolocationImplementation : IGeolocation
{
    const string UnsupportedMessage =
        "Geolocation requires the WinRT Windows.Devices.Geolocation.Geolocator API, " +
        "which is not available to an unpackaged WPF application.";

    public event EventHandler<GeolocationLocationChangedEventArgs>? LocationChanged;

    public event EventHandler<GeolocationListeningFailedEventArgs>? ListeningFailed;

    public bool IsListeningForeground => false;

    public bool IsEnabled => false;

    public Task<Location?> GetLastKnownLocationAsync() =>
        throw new FeatureNotSupportedException(UnsupportedMessage);

    public Task<Location?> GetLocationAsync(GeolocationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        throw new FeatureNotSupportedException(UnsupportedMessage);
    }

    public Task<bool> StartListeningForegroundAsync(GeolocationListeningRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        throw new FeatureNotSupportedException(UnsupportedMessage);
    }

    public void StopListeningForeground()
    {
        // No-op: listening can never have been started successfully.
    }

    // Kept for parity with .NET MAUI's GeolocationImplementation shape, in case a future
    // real implementation (e.g. backed by the Win32 Location API) wants to raise these.
    internal void OnLocationChanged(Location location) =>
        LocationChanged?.Invoke(null, new GeolocationLocationChangedEventArgs(location));

    internal void OnLocationError(GeolocationError geolocationError) =>
        ListeningFailed?.Invoke(null, new GeolocationListeningFailedEventArgs(geolocationError));
}
