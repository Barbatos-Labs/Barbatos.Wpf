// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Storage;

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// The default <see cref="IMcpTokenStore"/>: keeps each server's OAuth tokens under its own
/// <see cref="ISecureStorage"/> key (DPAPI-backed, current-user scope), so they never reach a
/// config file and cannot be read by another Windows account. Follows the same key-derivation
/// approach as <see cref="SecureStorageAiApiKeyProvider"/>, keyed by
/// <see cref="McpServerDescriptor.Name"/>.
/// </summary>
internal sealed class SecureStorageMcpTokenStore : IMcpTokenStore
{
    readonly ISecureStorage _secureStorage;

    public SecureStorageMcpTokenStore(ISecureStorage secureStorage)
    {
        _secureStorage = secureStorage;
    }

    public Task<string?> GetAsync(string serverName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);

        return _secureStorage.GetAsync(KeyFor(serverName));
    }

    public Task SetAsync(string serverName, string tokens, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokens);

        return _secureStorage.SetAsync(KeyFor(serverName), tokens);
    }

    public Task<bool> RemoveAsync(string serverName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);

        return Task.FromResult(_secureStorage.Remove(KeyFor(serverName)));
    }

    // Server names are user-facing and case-sensitive as far as IMcpServerRegistry is concerned
    // (it rejects duplicates by exact name), so the key is not lowercased the way a free-string
    // provider name is - two servers differing only in case are genuinely two servers here.
    static string KeyFor(string serverName) => $"Barbatos:Mcp:OAuth:{serverName.Trim()}";
}
