// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Barbatos.Wpf.Mcp.Providers;

/// <summary>
/// Builds an <see cref="IChatClient"/> over the official OpenAI .NET SDK - used for every
/// <see cref="AiProviderOptions.Provider"/> string except (case-insensitively) <c>"anthropic"</c>,
/// including Gemini (pointed at Google's own OpenAI-compatible endpoint - so no separate Gemini
/// SDK dependency is needed). Does not cover Azure OpenAI, which needs its own client for correct
/// auth/routing.
/// </summary>
internal static class OpenAiChatClientFactory
{
    public static IChatClient Create(string apiKey, string model, string? endpoint)
    {
        var options = new OpenAIClientOptions();
        if (!string.IsNullOrWhiteSpace(endpoint))
            options.Endpoint = new Uri(endpoint);

        var client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
        return client.GetChatClient(model).AsIChatClient();
    }
}
