// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Linq;

namespace Barbatos.Wpf.Networking;

/// <summary>
/// Describes the type of connection the device is using.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>ConnectionProfile</c>.</remarks>
public enum ConnectionProfile
{
    /// <summary>Other unknown type of connection.</summary>
    Unknown = 0,

    /// <summary>The bluetooth data connection.</summary>
    Bluetooth = 1,

    /// <summary>The mobile/cellular data connection.</summary>
    Cellular = 2,

    /// <summary>The ethernet data connection.</summary>
    Ethernet = 3,

    /// <summary>The Wi-Fi data connection.</summary>
    WiFi = 4,
}

/// <summary>
/// Various states of the connection to the internet.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>NetworkAccess</c>.</remarks>
public enum NetworkAccess
{
    /// <summary>The state of the connectivity is not known.</summary>
    Unknown = 0,

    /// <summary>No connectivity.</summary>
    None = 1,

    /// <summary>Local network access only.</summary>
    Local = 2,

    /// <summary>Limited internet access.</summary>
    ConstrainedInternet = 3,

    /// <summary>Local and Internet access.</summary>
    Internet = 4,
}

/// <summary>
/// The Connectivity API lets you monitor for changes in the device's network conditions,
/// check the current network access, and determine how it is currently connected.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>IConnectivity</c>.</remarks>
public interface IConnectivity
{
    /// <summary>
    /// Gets the active connectivity types for the device.
    /// </summary>
    IEnumerable<ConnectionProfile> ConnectionProfiles { get; }

    /// <summary>
    /// Gets the current state of network access.
    /// </summary>
    NetworkAccess NetworkAccess { get; }

    /// <summary>
    /// Occurs when network access or profile has changed.
    /// </summary>
    event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged;
}

/// <summary>
/// The Connectivity API lets you monitor for changes in the device's network conditions,
/// check the current network access, and how it is currently connected.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>Connectivity</c>.</remarks>
public static class Connectivity
{
    /// <inheritdoc cref="IConnectivity.NetworkAccess" />
    public static NetworkAccess NetworkAccess => Current.NetworkAccess;

    /// <inheritdoc cref="IConnectivity.ConnectionProfiles" />
    public static IEnumerable<ConnectionProfile> ConnectionProfiles => Current.ConnectionProfiles.Distinct();

    /// <inheritdoc cref="IConnectivity.ConnectivityChanged" />
    public static event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged
    {
        add => Current.ConnectivityChanged += value;
        remove => Current.ConnectivityChanged -= value;
    }

    static IConnectivity? currentImplementation;

    /// <summary>
    /// Provides the default implementation for static usage of this API.
    /// </summary>
    public static IConnectivity Current =>
        currentImplementation ??= new ConnectivityImplementation();

    internal static void SetCurrent(IConnectivity? implementation) =>
        currentImplementation = implementation;
}

/// <summary>
/// The current connectivity information from the change event.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>ConnectivityChangedEventArgs</c>.</remarks>
public class ConnectivityChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectivityChangedEventArgs"/> class.
    /// </summary>
    /// <param name="access">The current access of the network.</param>
    /// <param name="connectionProfiles">The connection profiles changing correspondingto this event.</param>
    public ConnectivityChangedEventArgs(NetworkAccess access, IEnumerable<ConnectionProfile> connectionProfiles)
    {
        NetworkAccess = access;
        ConnectionProfiles = connectionProfiles;
    }

    /// <summary>
    /// Gets the current state of network access.
    /// </summary>
    public NetworkAccess NetworkAccess { get; }

    /// <summary>
    /// Gets the active connectivity profiles for the device.
    /// </summary>
    public IEnumerable<ConnectionProfile> ConnectionProfiles { get; }

    /// <summary>
    /// Returns a string representation of the current values of <see cref="ConnectivityChangedEventArgs"/>.
    /// </summary>
    /// <returns>A string representation of this instance in the format of <c>NetworkAccess: {value}, ConnectionProfiles: [{value1}, {value2}]</c>.</returns>
    public override string ToString() =>
        $"{nameof(NetworkAccess)}: {NetworkAccess}, " +
        $"{nameof(ConnectionProfiles)}: [{string.Join(", ", ConnectionProfiles)}]";
}
