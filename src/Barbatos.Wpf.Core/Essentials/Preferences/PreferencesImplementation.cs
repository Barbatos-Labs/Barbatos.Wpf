// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using PreferencesDictionary = System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentDictionary<string, string>>;
using ShareNameDictionary = System.Collections.Concurrent.ConcurrentDictionary<string, string>;

namespace Barbatos.Wpf.Storage;

/// <summary>
/// The WPF implementation of <see cref="IPreferences"/>, ported from .NET MAUI's Windows
/// <c>UnpackagedPreferencesImplementation</c> (no MSIX package identity, so this is the only
/// code path this desktop-only build needs).
/// </summary>
class PreferencesImplementation : IPreferences
{
    static readonly string AppPreferencesPath = Path.Combine(FileSystem.AppDataDirectory, "..", "Settings", "preferences.dat");

    // Guards Save()/Load() against the file being written from two threads at once. The
    // in-memory _preferences dictionary is a ConcurrentDictionary and needs no locking of
    // its own; this only protects the disk I/O, mirroring the locking MAUI's own
    // PackagedPreferencesImplementation (the Windows ApplicationDataContainer-backed sibling
    // of this class) applies for the same reason.
    readonly object _fileLock = new();

    readonly PreferencesDictionary _preferences = new();

    public PreferencesImplementation()
    {
        Load();

        _preferences.GetOrAdd(string.Empty, _ => new ShareNameDictionary());
    }

    public bool ContainsKey(string key, string? sharedName = null)
    {
        if (_preferences.TryGetValue(CleanSharedName(sharedName), out var inner))
        {
            return inner.ContainsKey(key);
        }

        return false;
    }

    public void Remove(string key, string? sharedName = null)
    {
        if (_preferences.TryGetValue(CleanSharedName(sharedName), out var inner))
        {
            inner.TryRemove(key, out _);
            Save();
        }
    }

    public void Clear(string? sharedName = null)
    {
        if (_preferences.TryGetValue(CleanSharedName(sharedName), out var prefs))
        {
            prefs.Clear();
            Save();
        }
    }

    public void Set<T>(string key, T value, string? sharedName = null)
    {
        Preferences.CheckIsSupportedType<T>();

        var prefs = _preferences.GetOrAdd(CleanSharedName(sharedName), _ => new ShareNameDictionary());

        if (value is null)
            prefs.TryRemove(key, out _);
        else if (value is DateTime dt)
            prefs[key] = string.Format(CultureInfo.InvariantCulture, "{0}", dt.ToBinary());
        else if (value is DateTimeOffset dto)
            prefs[key] = dto.ToString("O");
        else
            prefs[key] = string.Format(CultureInfo.InvariantCulture, "{0}", value);

        Save();
    }

    public T Get<T>(string key, T defaultValue, string? sharedName = null)
    {
        if (_preferences.TryGetValue(CleanSharedName(sharedName), out var inner))
        {
            if (inner.TryGetValue(key, out var value) && value is not null)
            {
                if (defaultValue is DateTime)
                {
                    if (long.TryParse(value, CultureInfo.InvariantCulture, out var longValue))
                        return (T)(object)DateTime.FromBinary(longValue);
                    if (DateTime.TryParse(value, CultureInfo.InvariantCulture, out var datetimeValue))
                        return (T)(object)datetimeValue;
                }
                else if (defaultValue is DateTimeOffset)
                {
                    if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, out var dateTimeOffset))
                    {
                        return (T)(object)dateTimeOffset;
                    }
                }

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                    // bad get, fall back to default
                }
            }
        }

        return defaultValue;
    }

    void Load()
    {
        lock (_fileLock)
        {
            if (!File.Exists(AppPreferencesPath))
                return;

            try
            {
                using var stream = File.OpenRead(AppPreferencesPath);

                var readPreferences = JsonSerializer.Deserialize(stream, PreferencesJsonSerializerContext.Default.PreferencesDictionary);

                if (readPreferences != null)
                {
                    _preferences.Clear();
                    foreach (var pair in readPreferences)
                        _preferences.TryAdd(pair.Key, pair.Value);
                }
            }
            catch (JsonException)
            {
                // if deserialization fails proceed with empty settings
            }
        }
    }

    void Save()
    {
        lock (_fileLock)
        {
            var dir = Path.GetDirectoryName(AppPreferencesPath)!;
            Directory.CreateDirectory(dir);

            using var stream = File.Create(AppPreferencesPath);
            JsonSerializer.Serialize(stream, _preferences, PreferencesJsonSerializerContext.Default.PreferencesDictionary);
        }
    }

    static string CleanSharedName(string? sharedName) =>
        string.IsNullOrEmpty(sharedName) ? string.Empty : sharedName;
}

[JsonSerializable(typeof(PreferencesDictionary), TypeInfoPropertyName = nameof(PreferencesDictionary))]
partial class PreferencesJsonSerializerContext : JsonSerializerContext
{
}
