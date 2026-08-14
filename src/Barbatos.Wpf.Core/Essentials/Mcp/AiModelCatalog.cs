// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Mcp.Providers;

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// The default <see cref="IAiModelCatalog"/>. Splits on the provider string exactly the way
/// <see cref="AiChatClientFactory"/> does - Anthropic's own SDK for <c>"anthropic"</c>, the OpenAI
/// SDK (against whatever endpoint is configured) for everything else - so a provider that chats
/// through one path also lists through the same one.
/// </summary>
/// <remarks>
/// Deliberately stateless and uncached: unlike the chat client, which is expensive enough to keep
/// around and is invalidated on a known set of events, a model list has no such event to invalidate
/// it on (a provider can publish a model at any moment), so caching here would mostly serve stale
/// answers to the one call a user makes explicitly to see fresh ones.
/// </remarks>
internal sealed class AiModelCatalog : IAiModelCatalog
{
    readonly IAiApiKeyProvider _apiKeyProvider;

    public AiModelCatalog(IAiApiKeyProvider apiKeyProvider)
    {
        _apiKeyProvider = apiKeyProvider;
    }

    public async Task<IReadOnlyList<AiModelInfo>> GetModelsAsync(
        string provider,
        string? endpoint = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        var apiKey = await _apiKeyProvider.GetApiKeyAsync(provider, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"No API key has been set for provider '{provider}' - call {nameof(IAiApiKeyProvider)}.{nameof(IAiApiKeyProvider.SetApiKeyAsync)} first " +
                "(listing a provider's models is an authenticated call to that provider, so it cannot run before the end user has entered their own key).");
        }

        return string.Equals(provider, "anthropic", StringComparison.OrdinalIgnoreCase)
            ? await AnthropicModelCatalog.ListAsync(apiKey, cancellationToken).ConfigureAwait(false)
            : await OpenAiModelCatalog.ListAsync(apiKey, endpoint, cancellationToken).ConfigureAwait(false);
    }
}
