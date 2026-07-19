// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

public class DeviceIdentityTests
{
    [Fact]
    public void DefaultIsCached()
    {
        Assert.NotNull(DeviceIdentity.Default);
        Assert.Same(DeviceIdentity.Default, DeviceIdentity.Default);
    }

    [Fact]
    public async Task GetInstanceIdAsyncReturnsAGuid()
    {
        var instanceId = await DeviceIdentity.GetInstanceIdAsync();

        Assert.True(Guid.TryParse(instanceId, out _));
    }

    [Fact]
    public async Task GetInstanceIdAsyncIsStableAcrossCalls()
    {
        var first = await DeviceIdentity.GetInstanceIdAsync();
        var second = await DeviceIdentity.GetInstanceIdAsync();

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task GetHardwareFingerprintAsyncReturnsA64CharacterHexHash()
    {
        var fingerprint = await DeviceIdentity.GetHardwareFingerprintAsync();

        Assert.Equal(64, fingerprint.Length);
        Assert.Matches("^[0-9A-F]+$", fingerprint);
    }

    [Fact]
    public async Task GetHardwareFingerprintAsyncIsStableAcrossCalls()
    {
        var first = await DeviceIdentity.GetHardwareFingerprintAsync();
        var second = await DeviceIdentity.GetHardwareFingerprintAsync();

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task GetHardwareFingerprintAsyncDoesNotContainRawAppGuidOrMachineName()
    {
        // The fingerprint is a one-way SHA-256 hash: none of its salted/hashed inputs
        // (AppInfo.AppGuid, Environment.MachineName, or any raw hardware serial) should be
        // recoverable/visible in the output.
        var fingerprint = await DeviceIdentity.GetHardwareFingerprintAsync();

        Assert.DoesNotContain(AppInfo.AppGuid, fingerprint, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Environment.MachineName, fingerprint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeviceIdentityIsRegisteredInTheDefaultBuilder()
    {
        var wpfApp = WpfApp.CreateBuilder().Build();

        var deviceIdentity = wpfApp.Services.GetService<IDeviceIdentity>();

        Assert.NotNull(deviceIdentity);
        Assert.Same(DeviceIdentity.Default, deviceIdentity);
    }

    [Fact]
    public void DeviceIdentityIsNotRegisteredWithoutDefaults()
    {
        var wpfApp = WpfApp.CreateBuilder(useDefaults: false).Build();

        Assert.Null(wpfApp.Services.GetService<IDeviceIdentity>());
    }
}
