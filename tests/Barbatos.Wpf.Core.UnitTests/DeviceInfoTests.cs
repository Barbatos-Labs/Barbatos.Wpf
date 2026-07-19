// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Devices;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

public class DeviceInfoTests
{
    [Fact]
    public void CurrentIsCached()
    {
        Assert.NotNull(DeviceInfo.Current);
        Assert.Same(DeviceInfo.Current, DeviceInfo.Current);
    }

    [Fact]
    public void PlatformIsWpf()
    {
        Assert.Equal(DevicePlatform.WPF, DeviceInfo.Platform);
    }

    [Fact]
    public void IdiomIsDesktopOrTablet()
    {
        Assert.True(DeviceInfo.Idiom == DeviceIdiom.Desktop || DeviceInfo.Idiom == DeviceIdiom.Tablet);
    }

    [Fact]
    public void DeviceTypeIsADefinedValue()
    {
        Assert.True(Enum.IsDefined(DeviceInfo.DeviceType));
    }

    [Fact]
    public void NameIsTheMachineName()
    {
        Assert.Equal(Environment.MachineName, DeviceInfo.Name);
    }

    [Fact]
    public void VersionIsTheOsVersion()
    {
        Assert.Equal(Environment.OSVersion.Version, DeviceInfo.Version);
        Assert.Equal(DeviceInfo.Version.ToString(), DeviceInfo.VersionString);
    }

    [Fact]
    public void ModelAndManufacturerAreNotNull()
    {
        Assert.NotNull(DeviceInfo.Model);
        Assert.NotNull(DeviceInfo.Manufacturer);
    }

    [Fact]
    public void DeviceInfoIsRegisteredInTheDefaultBuilder()
    {
        var wpfApp = WpfApp.CreateBuilder().Build();

        var deviceInfo = wpfApp.Services.GetService<IDeviceInfo>();

        Assert.NotNull(deviceInfo);
        Assert.Same(DeviceInfo.Current, deviceInfo);
    }

    [Fact]
    public void DeviceInfoIsNotRegisteredWithoutDefaults()
    {
        var wpfApp = WpfApp.CreateBuilder(useDefaults: false).Build();

        Assert.Null(wpfApp.Services.GetService<IDeviceInfo>());
    }
}
