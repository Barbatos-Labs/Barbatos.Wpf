// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using SecureStorageDictionary = System.Collections.Concurrent.ConcurrentDictionary<string, byte[]>;

namespace Barbatos.Wpf.Storage;

/// <summary>
/// The WPF implementation of <see cref="ISecureStorage"/>, ported from .NET MAUI's Windows
/// <c>SecureStorageImplementation</c>. <see cref="ProtectedData"/> (DPAPI) is the Win32/.NET
/// equivalent of the WinRT <c>DataProtectionProvider</c> MAUI uses; storage uses the same
/// unpackaged JSON dictionary-of-bytes pattern as <see cref="PreferencesImplementation"/>.
/// </summary>
class SecureStorageImplementation : ISecureStorage
{
    static readonly string AppSecureStoragePath = Path.Combine(FileSystem.AppDataDirectory, "..", "Settings", "securestorage.dat");

    static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Barbatos.Wpf.SecureStorage");

    // Guards Save()/Load() against the file being written from two threads at once; see the
    // matching field on PreferencesImplementation for why.
    readonly object _fileLock = new();

    readonly SecureStorageDictionary _secureStorage = new();

    public SecureStorageImplementation()
    {
        Load();
    }

    public Task<string?> GetAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        if (!_secureStorage.TryGetValue(key, out var encBytes))
            return Task.FromResult<string?>(null);

        var bytes = ProtectedData.Unprotect(encBytes, Entropy, DataProtectionScope.CurrentUser);
        return Task.FromResult<string?>(Encoding.UTF8.GetString(bytes));
    }

    public Task SetAsync(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var bytes = Encoding.UTF8.GetBytes(value);
        var encBytes = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);

        _secureStorage[key] = encBytes;
        Save();

        return Task.CompletedTask;
    }

    public bool Remove(string key)
    {
        var result = _secureStorage.TryRemove(key, out _);
        Save();
        return result;
    }

    public void RemoveAll()
    {
        _secureStorage.Clear();
        Save();
    }

    void Load()
    {
        lock (_fileLock)
        {
            if (!File.Exists(AppSecureStoragePath))
                return;

            try
            {
                using var stream = File.OpenRead(AppSecureStoragePath);

                var readValues = JsonSerializer.Deserialize(stream, SecureStorageJsonSerializerContext.Default.SecureStorageDictionary);

                if (readValues != null)
                {
                    _secureStorage.Clear();
                    foreach (var pair in readValues)
                        _secureStorage.TryAdd(pair.Key, pair.Value);
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
            var dir = Path.GetDirectoryName(AppSecureStoragePath)!;
            Directory.CreateDirectory(dir);

            using var stream = File.Create(AppSecureStoragePath);
            JsonSerializer.Serialize(stream, _secureStorage, SecureStorageJsonSerializerContext.Default.SecureStorageDictionary);
        }
    }
}

[JsonSerializable(typeof(SecureStorageDictionary), TypeInfoPropertyName = nameof(SecureStorageDictionary))]
partial class SecureStorageJsonSerializerContext : JsonSerializerContext
{
}
