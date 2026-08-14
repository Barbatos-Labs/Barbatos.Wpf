// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Anthropic;
using Anthropic.Models.Models;

namespace Barbatos.Wpf.Mcp.Providers;

/// <summary>
/// Lists models over the official Anthropic .NET SDK - the listing counterpart to
/// <see cref="AnthropicChatClientFactory"/>, used when <see cref="AiProviderOptions.Provider"/>
/// case-insensitively equals <c>"anthropic"</c>.
/// </summary>
/// <remarks>
/// Anthropic's listing is paginated and richer than an OpenAI-compatible one - it carries a display
/// name and token limits - so <see cref="AiModelInfo.DisplayName"/>/
/// <see cref="AiModelInfo.MaxInputTokens"/>/<see cref="AiModelInfo.MaxOutputTokens"/> are populated
/// here and left null on the OpenAI path.
/// </remarks>
internal static class AnthropicModelCatalog
{
    /// <summary>
    /// How many pages to follow before giving up. Anthropic publishes a few dozen models, so a
    /// hundred pages of 100 is far past any real answer - this only stops a paging loop that never
    /// reports itself finished, rather than truncating a genuine list.
    /// </summary>
    const int MaxPages = 100;

    public static async Task<IReadOnlyList<AiModelInfo>> ListAsync(string apiKey, CancellationToken cancellationToken)
    {
        using var client = new AnthropicClient { ApiKey = apiKey };

        var results = new List<AiModelInfo>();
        var page = await client.Models.List(new ModelListParams { Limit = 100 }, cancellationToken).ConfigureAwait(false);

        for (var pageCount = 0; pageCount < MaxPages; pageCount++)
        {
            foreach (var model in page.Items)
            {
                results.Add(new AiModelInfo(model.ID)
                {
                    DisplayName = model.DisplayName,
                    CreatedAt = model.CreatedAt,
                    MaxInputTokens = model.MaxInputTokens,
                    MaxOutputTokens = model.MaxTokens,
                });
            }

            if (!page.HasNext())
                break;

            page = await page.Next(cancellationToken).ConfigureAwait(false);
        }

        return results;
    }
}
