// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Linq;
using Barbatos.Wpf.ApplicationModel;

namespace Barbatos.Wpf.Networking;

/// <summary>
/// The caching/event-diffing shell of <see cref="IConnectivity"/>, ported verbatim from
/// .NET MAUI's shared <c>ConnectivityImplementation</c>. The platform-specific half (in
/// <c>ConnectivityImplementation.Windows.cs</c>) implements <see cref="NetworkAccess"/>,
/// <see cref="ConnectionProfiles"/> and the listener start/stop methods.
/// </summary>
partial class ConnectivityImplementation : IConnectivity
{
    event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChangedInternal;

    // a cache so that events aren't fired unnecessarily
    NetworkAccess currentAccess;
    List<ConnectionProfile> currentProfiles = [];

    public event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged
    {
        add
        {
            if (ConnectivityChangedInternal is null)
            {
                SetCurrent();
                StartListeners();
            }
            ConnectivityChangedInternal += value;
        }
        remove
        {
            ConnectivityChangedInternal -= value;
            if (ConnectivityChangedInternal is null)
                StopListeners();
        }
    }

    void SetCurrent()
    {
        currentAccess = NetworkAccess;
        currentProfiles = new List<ConnectionProfile>(ConnectionProfiles);
    }

    void OnConnectivityChanged()
        => OnConnectivityChanged(new ConnectivityChangedEventArgs(NetworkAccess, ConnectionProfiles));

    void OnConnectivityChanged(ConnectivityChangedEventArgs e)
    {
        if (currentAccess != e.NetworkAccess || !currentProfiles.SequenceEqual(e.ConnectionProfiles))
        {
            SetCurrent();
            Utils.InvokeOnMainThread(() => ConnectivityChangedInternal?.Invoke(null, e));
        }
    }
}
