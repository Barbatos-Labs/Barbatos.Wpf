// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Linq;
using Barbatos.Wpf.ApplicationModel;
using Barbatos.Wpf.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

// Shares a collection with SecureStorageTests: its RemoveAll test wipes the stored instance id,
// which would race the id assertions here if the two classes ran in parallel.
[Collection(SecureStorageTests.CollectionName)]
public class DeviceIdentityTests
{
    // Documented in IDeviceIdentity.GetInstanceIdAsync, so a test may remove it to force a
    // first call.
    const string InstanceIdKey = "Barbatos.Wpf.DeviceIdentity.InstanceId";

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

    // GetInstanceIdAsync used to read and then write without a lock, so two first calls racing
    // (a startup licence check plus a background job) could each mint a different id, and a
    // licence server could be handed one that is never seen again. Each round removes the
    // stored id (the documented key) and releases eight dedicated threads at once through a
    // Barrier; the DPAPI encryption inside SetAsync sits in the old read-to-write window, so
    // across ten rounds the unguarded code mints a second id instead of all callers agreeing.
    [Fact]
    public async Task ConcurrentFirstCallsAllReceiveTheSameInstanceId()
    {
        const int callers = 8;

        for (var round = 0; round < 10; round++)
        {
            SecureStorage.Default.Remove(InstanceIdKey);

            var ids = new string?[callers];
            var failures = new Exception?[callers];
            using var start = new Barrier(callers);

            var threads = Enumerable.Range(0, callers).Select(index => new Thread(() =>
            {
                try
                {
                    start.SignalAndWait();
                    ids[index] = DeviceIdentity.GetInstanceIdAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    failures[index] = ex;
                }
            }) { IsBackground = true }).ToArray();

            foreach (var thread in threads)
                thread.Start();
            foreach (var thread in threads)
                Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "A caller did not finish.");

            Assert.All(failures, failure => Assert.Null(failure));
            Assert.Single(ids.Distinct());
            Assert.Equal(ids[0], await SecureStorage.Default.GetAsync(InstanceIdKey));
        }
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
    public async Task GetHardwareFingerprintAsyncDoesNotContainRawAppIdOrMachineName()
    {
        // The fingerprint is a one-way SHA-256 hash: none of its salted/hashed inputs
        // (AppInfo.AppId, Environment.MachineName, or any raw hardware serial) should be
        // recoverable/visible in the output.
        var fingerprint = await DeviceIdentity.GetHardwareFingerprintAsync();

        Assert.DoesNotContain(AppInfo.AppId, fingerprint, StringComparison.OrdinalIgnoreCase);
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
