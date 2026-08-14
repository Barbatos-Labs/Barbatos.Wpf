// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// One model a provider reported from <see cref="IAiModelCatalog.GetModelsAsync"/> - what a
/// settings UI's model picker is populated from, so a model released after this app shipped can be
/// selected without a new release.
/// </summary>
/// <remarks>
/// Only <see cref="Id"/> is ever populated for every provider; everything else is whatever that
/// provider's own listing endpoint happened to include (Anthropic reports a display name and token
/// limits, an OpenAI-compatible endpoint typically reports neither), so treat the rest as optional
/// UI decoration rather than something to branch on.
/// </remarks>
public sealed class AiModelInfo
{
    internal AiModelInfo(string id)
    {
        Id = id;
    }

    /// <summary>
    /// The model name as the provider spells it - exactly the string to hand to
    /// <see cref="IAiChatClientFactory.UpdateProvider"/> or to put in
    /// <see cref="AiProviderOptions.Model"/>.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// A human-friendly name for <see cref="Id"/> if the provider supplied one (e.g. "Claude Opus
    /// 4.8"), otherwise <see langword="null"/> - fall back to <see cref="Id"/> when displaying.
    /// </summary>
    public string? DisplayName { get; internal set; }

    /// <summary>
    /// When the provider says this model was released, or <see langword="null"/> if it did not say.
    /// Useful as a sort key: neither provider guarantees the order models come back in.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; internal set; }

    /// <summary>
    /// The model's context window in tokens if the provider reported it, otherwise
    /// <see langword="null"/> (an OpenAI-compatible listing does not include this).
    /// </summary>
    public long? MaxInputTokens { get; internal set; }

    /// <summary>
    /// The most tokens the model can produce in one response if the provider reported it,
    /// otherwise <see langword="null"/>.
    /// </summary>
    public long? MaxOutputTokens { get; internal set; }

    /// <summary>
    /// Who the provider says owns the model (OpenAI's <c>owned_by</c>, e.g. <c>"openai"</c> or an
    /// organization id), or <see langword="null"/> if it did not say. On an aggregator endpoint
    /// such as OpenRouter this is often the upstream vendor.
    /// </summary>
    public string? OwnedBy { get; internal set; }
}
