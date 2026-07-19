// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Devices;

namespace Barbatos.Wpf.Core.UnitTests;

public class DeviceTypesTests
{
    [Fact]
    public void DevicePlatformEquality()
    {
        Assert.Equal(DevicePlatform.WPF, DevicePlatform.WPF);
        Assert.NotEqual(DevicePlatform.WPF, DevicePlatform.WinUI);
        Assert.True(DevicePlatform.WPF == DevicePlatform.Create("WPF"));
        Assert.True(DevicePlatform.WPF != DevicePlatform.Unknown);
        Assert.Equal(DevicePlatform.WPF.GetHashCode(), DevicePlatform.Create("WPF").GetHashCode());
    }

    [Fact]
    public void DevicePlatformToString()
    {
        Assert.Equal("WPF", DevicePlatform.WPF.ToString());
        Assert.Equal(string.Empty, default(DevicePlatform).ToString());
    }

    [Fact]
    public void DevicePlatformCreateValidatesInput()
    {
        Assert.Throws<ArgumentNullException>(() => DevicePlatform.Create(null!));
        Assert.Throws<ArgumentException>(() => DevicePlatform.Create(string.Empty));
    }

    [Fact]
    public void DeviceIdiomEquality()
    {
        Assert.Equal(DeviceIdiom.Desktop, DeviceIdiom.Desktop);
        Assert.NotEqual(DeviceIdiom.Desktop, DeviceIdiom.Tablet);
        Assert.True(DeviceIdiom.Desktop == DeviceIdiom.Create("Desktop"));
        Assert.True(DeviceIdiom.Desktop != DeviceIdiom.Unknown);
        Assert.Equal(DeviceIdiom.Desktop.GetHashCode(), DeviceIdiom.Create("Desktop").GetHashCode());
    }

    [Fact]
    public void DeviceIdiomToString()
    {
        Assert.Equal("Desktop", DeviceIdiom.Desktop.ToString());
        Assert.Equal(string.Empty, default(DeviceIdiom).ToString());
    }

    [Fact]
    public void DeviceIdiomCreateValidatesInput()
    {
        Assert.Throws<ArgumentNullException>(() => DeviceIdiom.Create(null!));
        Assert.Throws<ArgumentException>(() => DeviceIdiom.Create(string.Empty));
    }
}
