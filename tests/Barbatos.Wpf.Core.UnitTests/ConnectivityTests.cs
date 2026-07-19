// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Networking;

namespace Barbatos.Wpf.Core.UnitTests;

public class ConnectivityTests
{
    [Fact]
    public void CurrentIsCached()
    {
        Assert.NotNull(Connectivity.Current);
        Assert.Same(Connectivity.Current, Connectivity.Current);
    }

    [Fact]
    public void NetworkAccessIsADefinedValue()
    {
        Assert.True(Enum.IsDefined(Connectivity.NetworkAccess));
    }

    [Fact]
    public void ConnectionProfilesDoesNotThrow()
    {
        var profiles = Connectivity.ConnectionProfiles.ToArray();

        Assert.All(profiles, profile => Assert.True(Enum.IsDefined(profile)));
    }

    [Fact]
    public void ConnectionProfilesAreDistinct()
    {
        var profiles = Connectivity.ConnectionProfiles.ToArray();

        Assert.Equal(profiles.Distinct(), profiles);
    }

    [Fact]
    public void SubscribingAndUnsubscribingDoesNotThrow()
    {
        EventHandler<ConnectivityChangedEventArgs> handler = (sender, args) => { };

        Connectivity.ConnectivityChanged += handler;
        Connectivity.ConnectivityChanged -= handler;
    }

    [Fact]
    public void ChangedEventArgsExposeTheGivenValues()
    {
        var profiles = new[] { ConnectionProfile.WiFi, ConnectionProfile.Ethernet };
        var args = new ConnectivityChangedEventArgs(NetworkAccess.Internet, profiles);

        Assert.Equal(NetworkAccess.Internet, args.NetworkAccess);
        Assert.Equal(profiles, args.ConnectionProfiles);
        Assert.Contains("Internet", args.ToString());
        Assert.Contains("WiFi", args.ToString());
    }
}
