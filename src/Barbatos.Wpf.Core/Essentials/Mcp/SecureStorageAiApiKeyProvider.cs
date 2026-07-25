// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Storage;

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// The default <see cref="IAiApiKeyProvider"/>: stores each provider's key under its own
/// <see cref="ISecureStorage"/> key (DPAPI-backed, current-user scope), so switching
/// <see cref="AiProviderOptions.Provider"/> back and forth never overwrites a different
/// provider's previously-entered key. The storage key is derived case-insensitively (trimmed and
/// lowercased), so <c>"Anthropic"</c>/<c>"anthropic"</c>/<c>"ANTHROPIC"</c> all resolve to the
/// same stored key - <see cref="AiProviderOptions.Provider"/> is a free string with no fixed
/// casing convention enforced.
/// </summary>
internal sealed class SecureStorageAiApiKeyProvider : IAiApiKeyProvider
{
    readonly ISecureStorage _secureStorage;

    public SecureStorageAiApiKeyProvider(ISecureStorage secureStorage)
    {
        _secureStorage = secureStorage;
    }

    public Task<string?> GetApiKeyAsync(string provider, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        return _secureStorage.GetAsync(KeyFor(provider));
    }

    public Task SetApiKeyAsync(string provider, string apiKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        return _secureStorage.SetAsync(KeyFor(provider), apiKey);
    }

    static string KeyFor(string provider) => $"Barbatos:Mcp:ApiKey:{provider.Trim().ToLowerInvariant()}";
}
