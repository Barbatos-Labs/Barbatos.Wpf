// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Storage;

namespace Barbatos.Wpf.Core.UnitTests;

// Preferences.Default is a process-wide singleton backed by a real file under
// FileSystem.AppDataDirectory (there is no InternalsVisibleTo seam to fake it), so every
// test uses a key unique to itself to avoid interfering with other tests.
public class PreferencesTests
{
    [Fact]
    public void DefaultIsCached()
    {
        Assert.NotNull(Preferences.Default);
        Assert.Same(Preferences.Default, Preferences.Default);
    }

    [Fact]
    public void StringRoundTrips()
    {
        var key = UniqueKey();

        Preferences.Set(key, "hello");

        Assert.Equal("hello", Preferences.Get(key, (string?)null));

        Preferences.Remove(key);
    }

    [Fact]
    public void BoolIntDoubleFloatLongRoundTrip()
    {
        var key = UniqueKey();

        Preferences.Set(key + ".bool", true);
        Preferences.Set(key + ".int", 42);
        Preferences.Set(key + ".double", 3.14);
        Preferences.Set(key + ".float", 2.5f);
        Preferences.Set(key + ".long", 123456789L);

        Assert.True(Preferences.Get(key + ".bool", false));
        Assert.Equal(42, Preferences.Get(key + ".int", 0));
        Assert.Equal(3.14, Preferences.Get(key + ".double", 0d));
        Assert.Equal(2.5f, Preferences.Get(key + ".float", 0f));
        Assert.Equal(123456789L, Preferences.Get(key + ".long", 0L));

        foreach (var suffix in new[] { ".bool", ".int", ".double", ".float", ".long" })
            Preferences.Remove(key + suffix);
    }

    [Fact]
    public void DateTimeAndDateTimeOffsetRoundTrip()
    {
        var key = UniqueKey();
        var dt = new DateTime(2026, 7, 19, 12, 30, 0, DateTimeKind.Utc);
        var dto = new DateTimeOffset(2026, 7, 19, 12, 30, 0, TimeSpan.Zero);

        Preferences.Set(key + ".dt", dt);
        Preferences.Set(key + ".dto", dto);

        Assert.Equal(dt, Preferences.Get(key + ".dt", DateTime.MinValue));
        Assert.Equal(dto, Preferences.Get(key + ".dto", DateTimeOffset.MinValue));

        Preferences.Remove(key + ".dt");
        Preferences.Remove(key + ".dto");
    }

    [Fact]
    public void GetReturnsDefaultForMissingKey()
    {
        var key = UniqueKey();

        Assert.Equal("fallback", Preferences.Get(key, "fallback"));
        Assert.Equal(7, Preferences.Get(key, 7));
    }

    [Fact]
    public void ContainsKeyReflectsPresence()
    {
        var key = UniqueKey();

        Assert.False(Preferences.ContainsKey(key));

        Preferences.Set(key, "value");
        Assert.True(Preferences.ContainsKey(key));

        Preferences.Remove(key);
        Assert.False(Preferences.ContainsKey(key));
    }

    [Fact]
    public void SharedNameIsolatesKeys()
    {
        var key = UniqueKey();
        var sharedName = UniqueKey();

        Preferences.Set(key, "in-shared-container", sharedName);

        Assert.False(Preferences.ContainsKey(key));
        Assert.True(Preferences.ContainsKey(key, sharedName));
        Assert.Equal("in-shared-container", Preferences.Get(key, (string?)null, sharedName));

        Preferences.Remove(key, sharedName);
    }

    [Fact]
    public void ClearRemovesEverythingInAContainer()
    {
        var sharedName = UniqueKey();
        var key1 = UniqueKey();
        var key2 = UniqueKey();

        Preferences.Set(key1, "a", sharedName);
        Preferences.Set(key2, "b", sharedName);

        Preferences.Clear(sharedName);

        Assert.False(Preferences.ContainsKey(key1, sharedName));
        Assert.False(Preferences.ContainsKey(key2, sharedName));
    }

    static string UniqueKey() => $"Test.{Guid.NewGuid():N}";
}
