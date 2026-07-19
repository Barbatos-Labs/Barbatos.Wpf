// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Diagnostics;
using System.Net.NetworkInformation;

namespace Barbatos.Wpf.Networking;

/// <summary>
/// The Windows implementation of the platform half of <see cref="IConnectivity"/>, ported
/// from .NET MAUI's Windows <c>ConnectivityImplementation</c> using
/// <see cref="System.Net.NetworkInformation"/> (pure .NET/BCL) instead of the WinRT
/// <c>Windows.Networking.Connectivity.NetworkInformation</c> API.
/// </summary>
partial class ConnectivityImplementation
{
    void StartListeners()
    {
        NetworkChange.NetworkAddressChanged += OnNetworkChanged;
        NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
    }

    void StopListeners()
    {
        NetworkChange.NetworkAddressChanged -= OnNetworkChanged;
        NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
    }

    void OnNetworkChanged(object? sender, EventArgs e) =>
        OnConnectivityChanged();

    void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e) =>
        OnConnectivityChanged();

    public NetworkAccess NetworkAccess
    {
        get
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return NetworkAccess.None;

            var hasUpInterface = false;

            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus is not OperationalStatus.Up ||
                        nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                    {
                        continue;
                    }

                    hasUpInterface = true;

                    // A default gateway means the interface can reach beyond the local subnet.
                    if (nic.GetIPProperties().GatewayAddresses.Count > 0)
                        return NetworkAccess.Internet;
                }
            }
            catch (NetworkInformationException ex)
            {
                Debug.WriteLine($"Unable to get network interfaces. Error: {ex.Message}");
            }

            return hasUpInterface ? NetworkAccess.Local : NetworkAccess.None;
        }
    }

    public IEnumerable<ConnectionProfile> ConnectionProfiles
    {
        get
        {
            NetworkInterface[] networkInterfaces = [];
            try
            {
                networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            }
            catch (NetworkInformationException ex)
            {
                Debug.WriteLine($"Unable to get network interfaces. Error: {ex.Message}");
            }

            foreach (var nic in networkInterfaces)
            {
                if (nic.OperationalStatus is not OperationalStatus.Up ||
                    nic.NetworkInterfaceType is NetworkInterfaceType.Loopback ||
                    nic.NetworkInterfaceType is NetworkInterfaceType.Tunnel)
                {
                    continue;
                }

                var interfaceType = ConnectionProfile.Unknown;
                switch (nic.NetworkInterfaceType)
                {
                    case NetworkInterfaceType.Ethernet:
                        interfaceType = ConnectionProfile.Ethernet;
                        break;
                    case NetworkInterfaceType.Wireless80211:
                        interfaceType = ConnectionProfile.WiFi;
                        break;
                    case NetworkInterfaceType.Wwanpp:
                    case NetworkInterfaceType.Wwanpp2:
                        interfaceType = ConnectionProfile.Cellular;
                        break;
                }

                yield return interfaceType;
            }
        }
    }
}
