// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Storage;

/// <summary>
/// The SecureStorage API helps securely store simple key/value pairs.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>ISecureStorage</c>.</remarks>
public interface ISecureStorage
{
    /// <summary>
    /// Gets and decrypts the value for a given key.
    /// </summary>
    /// <param name="key">The key to retrieve the value for.</param>
    /// <returns>The decrypted string value or <see langword="null"/> if a value was not found.</returns>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// Sets and encrypts a value for a given key.
    /// </summary>
    /// <param name="key">The key to set the value for.</param>
    /// <param name="value">Value to set.</param>
    /// <returns>A <see cref="Task"/> object with the current status of the asynchronous operation.</returns>
    Task SetAsync(string key, string value);

    /// <summary>
    /// Removes a key and its associated value if it exists.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    bool Remove(string key);

    /// <summary>
    /// Removes all of the stored encrypted key/value pairs.
    /// </summary>
    void RemoveAll();
}

/// <summary>
/// The SecureStorage API helps securely store simple key/value pairs.
/// </summary>
/// <remarks>
/// This is the WPF counterpart of .NET MAUI's <c>SecureStorage</c>. Values are encrypted with
/// the Windows Data Protection API (DPAPI, <see cref="System.Security.Cryptography.ProtectedData"/>,
/// current-user scope) and stored in a local file under <see cref="FileSystem.AppDataDirectory"/>.
/// </remarks>
public static class SecureStorage
{
    /// <inheritdoc cref="ISecureStorage.GetAsync(string)" />
    public static Task<string?> GetAsync(string key) =>
        Current.GetAsync(key);

    /// <inheritdoc cref="ISecureStorage.SetAsync(string, string)" />
    public static Task SetAsync(string key, string value) =>
        Current.SetAsync(key, value);

    /// <inheritdoc cref="ISecureStorage.Remove(string)" />
    public static bool Remove(string key) =>
        Current.Remove(key);

    /// <inheritdoc cref="ISecureStorage.RemoveAll" />
    public static void RemoveAll() =>
        Current.RemoveAll();

    static ISecureStorage Current => Default;

    static ISecureStorage? defaultImplementation;

    /// <summary>
    /// Provides the default implementation for static usage of this API.
    /// </summary>
    public static ISecureStorage Default =>
        defaultImplementation ??= new SecureStorageImplementation();

    internal static void SetDefault(ISecureStorage? implementation) =>
        defaultImplementation = implementation;
}
