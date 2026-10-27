// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Runtime.InteropServices;
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

    // KeepScreenOn used to call SetThreadExecutionState on the caller's thread, replacing that
    // thread's state: turning it off on the UI thread silently cancelled ConfigureKeepAwake's
    // ES_SYSTEM_REQUIRED, which lives on the same thread. The thread below stands in for that
    // UI thread. SetThreadExecutionState returns the calling thread's previous state, which is
    // how its request is read back after the toggle.
    [Fact]
    public void TogglingKeepScreenOnLeavesTheCallingThreadsExecutionStateAlone()
    {
        var original = DeviceDisplay.KeepScreenOn;
        uint stateAfterToggle = 0;
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED);
                try
                {
                    DeviceDisplay.KeepScreenOn = true;
                    DeviceDisplay.KeepScreenOn = false;
                }
                finally
                {
                    stateAfterToggle = NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS);
                }
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        }) { IsBackground = true };

        thread.Start();
        var finished = thread.Join(TimeSpan.FromSeconds(10));
        DeviceDisplay.KeepScreenOn = original;

        Assert.True(finished, "The toggling thread did not finish.");
        Assert.Null(failure);
        Assert.Equal(
            NativeMethods.ES_SYSTEM_REQUIRED,
            stateAfterToggle & (NativeMethods.ES_SYSTEM_REQUIRED | NativeMethods.ES_DISPLAY_REQUIRED));
    }

    // KeepScreenOn used to put the display request on the setting thread, so clearing the
    // property from any other thread reset that other thread and left the setter's request in
    // force for as long as the setter lived. The setter here stays alive until the test thread
    // has cleared the property, then reads its own execution state back: the old code left
    // ES_DISPLAY_REQUIRED there, the dedicated holder thread never touches it.
    [Fact]
    public void KeepScreenOnCanBeSetOnOneThreadAndClearedOnAnother()
    {
        var original = DeviceDisplay.KeepScreenOn;
        var set = new ManualResetEventSlim();
        var cleared = new ManualResetEventSlim();
        uint setterStateAfterClear = 0;

        // Captured rather than left to escape: an exception thrown on a bare thread ends the
        // test host instead of failing this test.
        Exception? failure = null;
        var setter = new Thread(() =>
        {
            try
            {
                DeviceDisplay.KeepScreenOn = true;
                set.Set();

                if (cleared.Wait(TimeSpan.FromSeconds(10)))
                    setterStateAfterClear = NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS);
            }
            catch (Exception ex)
            {
                failure = ex;
                set.Set();
            }
        }) { IsBackground = true };

        try
        {
            // Starts from false, so the setter really makes the request instead of a no-op.
            DeviceDisplay.KeepScreenOn = false;

            setter.Start();
            Assert.True(set.Wait(TimeSpan.FromSeconds(10)), "The setting thread did not set the property.");
            Assert.Null(failure);
            Assert.True(DeviceDisplay.KeepScreenOn);

            DeviceDisplay.KeepScreenOn = false;
            cleared.Set();

            Assert.True(setter.Join(TimeSpan.FromSeconds(10)), "The setting thread did not finish.");
            Assert.Null(failure);
            Assert.False(DeviceDisplay.KeepScreenOn);
            Assert.Equal(0u, setterStateAfterClear & NativeMethods.ES_DISPLAY_REQUIRED);

            DeviceDisplay.KeepScreenOn = true;
            Assert.True(DeviceDisplay.KeepScreenOn);
        }
        finally
        {
            // Releases the setter if an assertion above failed before the clear.
            cleared.Set();
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

    static class NativeMethods
    {
        internal const uint ES_CONTINUOUS = 0x80000000;
        internal const uint ES_SYSTEM_REQUIRED = 0x00000001;
        internal const uint ES_DISPLAY_REQUIRED = 0x00000002;

        [DllImport("kernel32.dll")]
        internal static extern uint SetThreadExecutionState(uint esFlags);
    }
}
