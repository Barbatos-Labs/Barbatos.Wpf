// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Authentication;

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// Bridges the MCP SDK's <see cref="ITokenCache"/> onto this module's <see cref="IMcpTokenStore"/>,
/// serialising the SDK's own token type to JSON so the store only ever handles an opaque string
/// and never has to mirror a type it does not own.
/// </summary>
/// <remarks>
/// One instance per server: <see cref="ITokenCache"/> has no key in its signature, so the server
/// name is captured here instead.
/// </remarks>
internal sealed class McpTokenStoreCache : ITokenCache
{
    readonly IMcpTokenStore _store;
    readonly string _serverName;
    readonly ILogger? _logger;

    public McpTokenStoreCache(IMcpTokenStore store, string serverName, ILogger? logger)
    {
        _store = store;
        _serverName = serverName;
        _logger = logger;
    }

    public async ValueTask<TokenContainer?> GetTokensAsync(CancellationToken cancellationToken)
    {
        var payload = await _store.GetAsync(_serverName, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(payload))
            return null;

        try
        {
            return JsonSerializer.Deserialize<TokenContainer>(payload);
        }
        catch (JsonException ex)
        {
            // A payload written by an older SDK whose shape has since changed, or a truncated
            // write. Returning null just sends the user through consent again, which is a far
            // better outcome than the whole connection throwing.
            _logger?.LogWarning(
                ex,
                "The stored OAuth tokens for the MCP server '{ServerName}' could not be read and will be discarded; the user will be asked to authorize again.",
                _serverName);

            await _store.RemoveAsync(_serverName, cancellationToken).ConfigureAwait(false);
            return null;
        }
    }

    public async ValueTask StoreTokensAsync(TokenContainer tokens, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        await _store.SetAsync(_serverName, JsonSerializer.Serialize(tokens), cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Adapts this module's <see cref="IMcpAuthorizationHandler"/> to the delegate shape the MCP SDK's
/// <see cref="ClientOAuthOptions.AuthorizationCallbackHandler"/> expects, so no part of the
/// module's public surface has to name an SDK type.
/// </summary>
internal static class McpAuthorizationHandlerAdapter
{
    // The SDK's delegate returns a nullable result - null being "no authorization happened". This
    // adapter never returns null: IMcpAuthorizationHandler reports a refusal or timeout as an
    // McpAuthorizationException instead, which carries the reason all the way through to
    // McpServerStatus.LastError rather than collapsing every failure into one blank outcome.
    public static Func<AuthorizationCallbackContext, CancellationToken, Task<AuthorizationResult?>> ToCallbackHandler(
        IMcpAuthorizationHandler handler,
        McpServerDescriptor descriptor) =>
        async (context, cancellationToken) =>
        {
            var result = await handler
                .AuthorizeAsync(new McpAuthorizationContext(descriptor, context.AuthorizationUri, context.RedirectUri), cancellationToken)
                .ConfigureAwait(false);

            return new AuthorizationResult
            {
                Code = result.Code,
                State = result.State,
                Iss = result.Issuer,
            };
        };
}
