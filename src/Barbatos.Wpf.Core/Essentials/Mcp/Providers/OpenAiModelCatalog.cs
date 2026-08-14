// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ClientModel;
using OpenAI;

namespace Barbatos.Wpf.Mcp.Providers;

/// <summary>
/// Lists models over the official OpenAI .NET SDK (<c>GET {endpoint}/models</c>) - the listing
/// counterpart to <see cref="OpenAiChatClientFactory"/>, used for every
/// <see cref="AiProviderOptions.Provider"/> string except (case-insensitively) <c>"anthropic"</c>.
/// </summary>
/// <remarks>
/// The route is part of the OpenAI wire protocol rather than of OpenAI the service, so it answers
/// on OpenRouter, Ollama, LM Studio, vLLM and Gemini's OpenAI-compatible endpoint too - but a proxy
/// that implements only chat completions will fail here, which is why
/// <see cref="IAiModelCatalog"/> documents a typed-in model name as the fallback rather than
/// treating a listing as guaranteed.
/// </remarks>
internal static class OpenAiModelCatalog
{
    public static async Task<IReadOnlyList<AiModelInfo>> ListAsync(string apiKey, string? endpoint, CancellationToken cancellationToken)
    {
        var options = new OpenAIClientOptions();
        if (!string.IsNullOrWhiteSpace(endpoint))
            options.Endpoint = new Uri(endpoint);

        var client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
        var models = await client.GetOpenAIModelClient().GetModelsAsync(cancellationToken).ConfigureAwait(false);

        // Returned in whatever order the endpoint chose - left alone on purpose, the same way the
        // chat path passes content through untransformed; IAiModelCatalog documents sorting as the
        // caller's call.
        return [.. models.Value.Select(model => new AiModelInfo(model.Id)
        {
            CreatedAt = model.CreatedAt,
            OwnedBy = model.OwnedBy,
        })];
    }
}
