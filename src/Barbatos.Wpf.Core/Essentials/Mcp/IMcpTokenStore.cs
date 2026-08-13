// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// Persists the OAuth tokens obtained for each MCP server, so an end user who consented once is
/// not sent back through the browser on every app restart. The default implementation
/// (<see cref="SecureStorageMcpTokenStore"/>) is backed by DPAPI-encrypted <c>SecureStorage</c>,
/// the same place <see cref="IAiApiKeyProvider"/> keeps the user's own API keys; register your own
/// before calling <c>ConfigureMcp</c> to use Credential Manager or a key vault instead.
/// </summary>
/// <remarks>
/// The payload is an opaque string owned by the MCP SDK - it holds the access token, the refresh
/// token, when they were obtained, and the client registration that produced them. Treat it as a
/// secret and store it the way you would a password; do not try to parse it, since its shape
/// belongs to the SDK and can change between versions. This mirrors how MSAL and other OAuth
/// libraries expose a token cache as a blob rather than as typed fields.
/// </remarks>
public interface IMcpTokenStore
{
    /// <summary>
    /// Returns the stored payload for <paramref name="serverName"/>, or <see langword="null"/> if
    /// that server has never been authorized (or was signed out). An unreadable or outdated
    /// payload should also come back as <see langword="null"/> rather than throwing - the caller
    /// then simply re-authorizes.
    /// </summary>
    Task<string?> GetAsync(string serverName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores the payload for <paramref name="serverName"/>, replacing anything already held for
    /// it. Called by the SDK whenever tokens are obtained or refreshed.
    /// </summary>
    Task SetAsync(string serverName, string tokens, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discards the payload for <paramref name="serverName"/> - the "sign out of this server"
    /// operation. Returns <see langword="false"/> if nothing was stored for it.
    /// </summary>
    Task<bool> RemoveAsync(string serverName, CancellationToken cancellationToken = default);
}
