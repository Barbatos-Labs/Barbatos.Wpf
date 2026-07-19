// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Devices;

namespace Barbatos.Wpf.Core.UnitTests;

public class DeviceDisplayTests
{
    [Fact]
    public void CurrentIsCached()
    {
        Assert.NotNull(DeviceDisplay.Current);
        Assert.Same(DeviceDisplay.Current, DeviceDisplay.Current);
    }

    [Fact]
    public void MainDisplayInfoIsDefaultWithoutAnActiveWindow()
    {
        // The xunit test host does not run a System.Windows.Application, so there is no
        // active window to query monitor info from.
        var info = DeviceDisplay.MainDisplayInfo;

        Assert.Equal(default, info);
    }

    [Fact]
    public void KeepScreenOnRoundTrips()
    {
        var original = DeviceDisplay.KeepScreenOn;
        try
        {
            DeviceDisplay.KeepScreenOn = true;
            Assert.True(DeviceDisplay.KeepScreenOn);

            DeviceDisplay.KeepScreenOn = false;
            Assert.False(DeviceDisplay.KeepScreenOn);
        }
        finally
        {
            DeviceDisplay.KeepScreenOn = original;
        }
    }

    [Fact]
    public void SubscribingAndUnsubscribingDoesNotThrow()
    {
        EventHandler<DisplayInfoChangedEventArgs> handler = (sender, args) => { };

        DeviceDisplay.MainDisplayInfoChanged += handler;
        DeviceDisplay.MainDisplayInfoChanged -= handler;
    }

    [Fact]
    public void DisplayInfoEquality()
    {
        var a = new DisplayInfo(1920, 1080, 1.0, DisplayOrientation.Landscape, DisplayRotation.Rotation0, 60);
        var b = new DisplayInfo(1920, 1080, 1.0, DisplayOrientation.Landscape, DisplayRotation.Rotation0, 60);
        var c = new DisplayInfo(1280, 720, 1.0, DisplayOrientation.Landscape, DisplayRotation.Rotation0, 60);

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.NotEqual(a, c);
        Assert.True(a != c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void DisplayInfoRefreshRateDefaultsToZero()
    {
        var info = new DisplayInfo(1920, 1080, 1.0, DisplayOrientation.Landscape, DisplayRotation.Rotation0);

        Assert.Equal(0, info.RefreshRate);
    }

    [Fact]
    public void DisplayInfoToStringIncludesTheDimensions()
    {
        var info = new DisplayInfo(1920, 1080, 1.0, DisplayOrientation.Landscape, DisplayRotation.Rotation0);

        Assert.Contains("1920", info.ToString());
        Assert.Contains("1080", info.ToString());
    }

    [Fact]
    public void DisplayInfoChangedEventArgsExposesTheGivenValue()
    {
        var info = new DisplayInfo(1920, 1080, 1.0, DisplayOrientation.Landscape, DisplayRotation.Rotation0);
        var args = new DisplayInfoChangedEventArgs(info);

        Assert.Equal(info, args.DisplayInfo);
    }
}
