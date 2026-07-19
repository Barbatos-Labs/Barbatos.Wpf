// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Storage;

namespace Barbatos.Wpf.Core.UnitTests;

// SecureStorage.Default is a process-wide singleton backed by a real DPAPI-encrypted file
// (there is no InternalsVisibleTo seam to fake it), so every test uses a key unique to
// itself.
public class SecureStorageTests
{
    [Fact]
    public void DefaultIsCached()
    {
        Assert.NotNull(SecureStorage.Default);
        Assert.Same(SecureStorage.Default, SecureStorage.Default);
    }

    [Fact]
    public async Task GetReturnsNullForMissingKey()
    {
        var value = await SecureStorage.GetAsync(UniqueKey());

        Assert.Null(value);
    }

    [Fact]
    public async Task SetAndGetRoundTrip()
    {
        var key = UniqueKey();

        await SecureStorage.SetAsync(key, "top secret");

        Assert.Equal("top secret", await SecureStorage.GetAsync(key));

        SecureStorage.Remove(key);
    }

    [Fact]
    public async Task SetOverwritesThePreviousValue()
    {
        var key = UniqueKey();

        await SecureStorage.SetAsync(key, "first");
        await SecureStorage.SetAsync(key, "second");

        Assert.Equal("second", await SecureStorage.GetAsync(key));

        SecureStorage.Remove(key);
    }

    [Fact]
    public async Task RemoveDeletesTheValue()
    {
        var key = UniqueKey();
        await SecureStorage.SetAsync(key, "value");

        var removed = SecureStorage.Remove(key);

        Assert.True(removed);
        Assert.Null(await SecureStorage.GetAsync(key));
    }

    [Fact]
    public void RemoveReturnsFalseForMissingKey()
    {
        Assert.False(SecureStorage.Remove(UniqueKey()));
    }

    [Fact]
    public async Task SetAsyncThrowsForNullOrWhitespaceKey()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => SecureStorage.SetAsync("", "value"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => SecureStorage.SetAsync("   ", "value"));
    }

    [Fact]
    public async Task SetAsyncThrowsForNullValue()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => SecureStorage.SetAsync(UniqueKey(), null!));
    }

    [Fact]
    public async Task RemoveAllClearsEveryStoredValue()
    {
        var key1 = UniqueKey();
        var key2 = UniqueKey();
        await SecureStorage.SetAsync(key1, "a");
        await SecureStorage.SetAsync(key2, "b");

        SecureStorage.RemoveAll();

        Assert.Null(await SecureStorage.GetAsync(key1));
        Assert.Null(await SecureStorage.GetAsync(key2));
    }

    static string UniqueKey() => $"Test.{Guid.NewGuid():N}";
}
