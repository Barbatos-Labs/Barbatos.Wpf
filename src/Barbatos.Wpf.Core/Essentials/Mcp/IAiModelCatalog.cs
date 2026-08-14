// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// Asks a provider which models it currently offers, using the end user's own stored API key -
/// the "which model" counterpart to <see cref="IAiChatClientFactory"/>'s "which provider". Exists
/// so a settings UI can offer a live model list instead of a hardcoded one: models are released far
/// more often than most desktop apps ship, and <see cref="AiProviderOptions.Model"/> is a free
/// string precisely so a new one never requires a new release.
/// </summary>
/// <remarks>
/// Strictly optional - an app whose users are happy typing a model name themselves never needs
/// this, and should keep a free-text entry either way (see the caveats below). A few things worth
/// knowing before wiring it to a picker:
/// <list type="bullet">
/// <item><description>
/// <b>An API key must already be stored</b> for the provider being asked (this is a real,
/// authenticated call to that provider) - so a settings screen can only populate the list after the
/// end user has entered and saved their key, not while the fields are still blank.
/// </description></item>
/// <item><description>
/// <b>Nothing is cached</b>: every call is a network round trip. Call it when the user asks (a
/// "Refresh models" button) rather than on every window activation, and keep the result in your own
/// view model.
/// </description></item>
/// <item><description>
/// <b>The list is raw and unfiltered</b>, the same way <see cref="IAiChatService"/> passes message
/// content straight through: OpenAI-compatible endpoints list embedding, audio and image models
/// alongside chat ones, with no reliable flag saying which is which. Filter for what your UI wants
/// (or show all of it and let the user choose) - this library deliberately does not guess.
/// </description></item>
/// <item><description>
/// <b>Not every endpoint implements it.</b> Listing is a standard OpenAI-wire route
/// (<c>GET {endpoint}/models</c>) that OpenRouter, Ollama, LM Studio, vLLM and Gemini's
/// OpenAI-compatible endpoint all answer, but a narrow proxy may not - it throws in that case, so
/// keep a typed-in model name working as the fallback.
/// </description></item>
/// </list>
/// </remarks>
public interface IAiModelCatalog
{
    /// <summary>
    /// Lists the models <paramref name="provider"/> currently offers, in whatever order the
    /// provider returned them (neither provider guarantees one - sort by
    /// <see cref="AiModelInfo.Id"/> or <see cref="AiModelInfo.CreatedAt"/> if the UI needs
    /// stability).
    /// </summary>
    /// <param name="provider">Which provider to ask, spelled the same way
    /// <see cref="AiProviderOptions.Provider"/> is - also the key its API key is looked up under.
    /// Case-insensitively <c>"anthropic"</c> uses Anthropic's own SDK; anything else is treated as
    /// OpenAI-wire-compatible, exactly as <see cref="IAiChatClientFactory"/> decides it.</param>
    /// <param name="endpoint">The API base URL to ask, for any provider other than
    /// <c>"anthropic"</c> - see <see cref="AiProviderOptions.Endpoint"/>. Pass the same value the
    /// chat client uses for this provider, or <see langword="null"/> for the real OpenAI API.
    /// Ignored for <c>"anthropic"</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="ArgumentException"><paramref name="provider"/> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">No API key has been stored for
    /// <paramref name="provider"/> via <see cref="IAiApiKeyProvider"/> yet - the message names
    /// which provider.</exception>
    Task<IReadOnlyList<AiModelInfo>> GetModelsAsync(
        string provider,
        string? endpoint = null,
        CancellationToken cancellationToken = default);
}
