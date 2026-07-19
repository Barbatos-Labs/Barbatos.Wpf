// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.ApplicationModel;

namespace Barbatos.Wpf.Devices.Sensors;

/// <summary>
/// Provides a way to get the current location of the device.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>IGeolocation</c>.</remarks>
public interface IGeolocation
{
    /// <summary>
    /// Returns the last known location of the device.
    /// </summary>
    /// <returns>A <see cref="Location"/> object containing recent location information or <see langword="null"/> if no location is known.</returns>
    Task<Location?> GetLastKnownLocationAsync();

    /// <summary>
    /// Returns the current location of the device.
    /// </summary>
    /// <param name="request">The criteria to use when determining the location of the device.</param>
    /// <param name="cancelToken">A token that can be used for cancelling the operation.</param>
    /// <returns>A <see cref="Location"/> object containing current location information or <see langword="null"/> if no location could be determined.</returns>
    Task<Location?> GetLocationAsync(GeolocationRequest request, CancellationToken cancelToken);

    /// <summary>
    /// Indicates if currently listening to location updates while the app is in foreground.
    /// </summary>
    bool IsListeningForeground { get; }

    /// <summary>
    /// Returns <see langword="true"/> when the device's location services are enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Occurs while listening to location updates.
    /// </summary>
    event EventHandler<GeolocationLocationChangedEventArgs>? LocationChanged;

    /// <summary>
    /// Occurs when an error during listening for location updates arises.
    /// </summary>
    event EventHandler<GeolocationListeningFailedEventArgs>? ListeningFailed;

    /// <summary>
    /// Starts listening to location updates using the <see cref="LocationChanged"/> event.
    /// </summary>
    /// <param name="request">The listening request parameters to use.</param>
    /// <returns><see langword="true"/> when listening was started, or <see langword="false"/> when listening couldn't be started.</returns>
    Task<bool> StartListeningForegroundAsync(GeolocationListeningRequest request);

    /// <summary>
    /// Stop listening for location updates when the app is in the foreground.
    /// </summary>
    void StopListeningForeground();
}

/// <summary>
/// Provides a way to get the current location of the device.
/// </summary>
/// <remarks>
/// This is the WPF counterpart of .NET MAUI's <c>Geolocation</c>. .NET MAUI's Windows
/// implementation is built on the WinRT <c>Windows.Devices.Geolocation.Geolocator</c>
/// contract, which is not available to a plain unpackaged desktop WPF app. Every member
/// therefore throws <see cref="FeatureNotSupportedException"/> on this platform; the type
/// surface (this interface, <see cref="Location"/>, <see cref="GeolocationRequest"/>,
/// <see cref="GeolocationListeningRequest"/>, ...) is kept identical so code written against
/// it compiles and can be swapped for a real implementation via <c>Geolocation.SetDefault</c>
/// — for example one backed by the classic Win32 Location API (<c>ILocation</c>) or an IP
/// geolocation service.
/// </remarks>
public static partial class Geolocation
{
    /// <inheritdoc cref="IGeolocation.GetLastKnownLocationAsync" />
    public static Task<Location?> GetLastKnownLocationAsync() =>
        Current.GetLastKnownLocationAsync();

    /// <summary>
    /// Returns the current location of the device.
    /// </summary>
    /// <returns>A <see cref="Location"/> object containing current location information or <see langword="null"/> if no location could be determined.</returns>
    public static Task<Location?> GetLocationAsync() =>
        Current.GetLocationAsync();

    /// <summary>
    /// Returns the current location of the device.
    /// </summary>
    /// <param name="request">The criteria to use when determining the location of the device.</param>
    /// <returns>A <see cref="Location"/> object containing current location information or <see langword="null"/> if no location could be determined.</returns>
    public static Task<Location?> GetLocationAsync(GeolocationRequest request) =>
        Current.GetLocationAsync(request);

    /// <inheritdoc cref="IGeolocation.GetLocationAsync(GeolocationRequest, CancellationToken)" />
    public static Task<Location?> GetLocationAsync(GeolocationRequest request, CancellationToken cancelToken) =>
        Current.GetLocationAsync(request, cancelToken);

    /// <inheritdoc cref="IGeolocation.IsListeningForeground" />
    public static bool IsListeningForeground => Current.IsListeningForeground;

    /// <inheritdoc cref="IGeolocation.IsEnabled" />
    public static bool IsEnabled => Current.IsEnabled;

    /// <inheritdoc cref="IGeolocation.LocationChanged" />
    public static event EventHandler<GeolocationLocationChangedEventArgs> LocationChanged
    {
        add => Current.LocationChanged += value;
        remove => Current.LocationChanged -= value;
    }

    /// <inheritdoc cref="IGeolocation.ListeningFailed" />
    public static event EventHandler<GeolocationListeningFailedEventArgs> ListeningFailed
    {
        add => Current.ListeningFailed += value;
        remove => Current.ListeningFailed -= value;
    }

    /// <inheritdoc cref="IGeolocation.StartListeningForegroundAsync(GeolocationListeningRequest)" />
    public static Task<bool> StartListeningForegroundAsync(GeolocationListeningRequest request) =>
        Current.StartListeningForegroundAsync(request);

    /// <inheritdoc cref="IGeolocation.StopListeningForeground" />
    public static void StopListeningForeground() =>
        Current.StopListeningForeground();

    static IGeolocation Current => Default;

    static IGeolocation? defaultImplementation;

    /// <summary>
    /// Provides the default implementation for static usage of this API.
    /// </summary>
    public static IGeolocation Default =>
        defaultImplementation ??= new GeolocationImplementation();

    internal static void SetDefault(IGeolocation? implementation) =>
        defaultImplementation = implementation;
}

/// <summary>
/// Static class with extension methods for the <see cref="IGeolocation"/> APIs.
/// </summary>
public static class GeolocationExtensions
{
    /// <summary>
    /// Returns the current location of the device.
    /// </summary>
    public static Task<Location?> GetLocationAsync(this IGeolocation geolocation) =>
        geolocation.GetLocationAsync(new GeolocationRequest(), default);

    /// <summary>
    /// Returns the current location of the device.
    /// </summary>
    public static Task<Location?> GetLocationAsync(this IGeolocation geolocation, GeolocationRequest request) =>
        geolocation.GetLocationAsync(request ?? new GeolocationRequest(), default);
}
