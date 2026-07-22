// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// Describes one AI provider choice an app wants to offer (e.g. in a settings UI's provider
/// picker), seeded from <see cref="AiProviderOptions.Providers"/> at startup and looked up by
/// <see cref="IAiChatClientFactory.SelectProvider"/>.
/// </summary>
public class AiProviderDescriptor
{
    /// <summary>
    /// A unique name identifying this entry - used as the key for
    /// <see cref="IAiChatClientFactory.SelectProvider"/>. Required - selecting throws a clear
    /// <see cref="InvalidOperationException"/> if left unset.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// The provider string this entry resolves to (see <see cref="AiProviderOptions.Provider"/>
    /// for what this means) - not necessarily the same string as <see cref="Key"/>, e.g. an app
    /// could register two entries both backed by <c>"openai"</c> but pointed at different
    /// models. Defaults to <c>"openai"</c>.
    /// </summary>
    public string Provider { get; set; } = "openai";

    /// <summary>The model name to request - see <see cref="AiProviderOptions.Model"/>.</summary>
    public string? Model { get; set; }

    /// <summary>The API base URL to call - see <see cref="AiProviderOptions.Endpoint"/>.</summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Validates that this descriptor is internally consistent, throwing
    /// <see cref="InvalidOperationException"/> if it is not. A pure check of this entry's own
    /// fields - no chat client is built - so it can also validate a provider entry from a
    /// settings UI before it's saved, the same way <see cref="McpServerDescriptor.Validate"/>
    /// does for MCP servers.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Key"/> or <see cref="Provider"/>
    /// is unset.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Key))
            throw new InvalidOperationException($"{nameof(AiProviderDescriptor)}.{nameof(Key)} must be set.");

        if (string.IsNullOrWhiteSpace(Provider))
            throw new InvalidOperationException($"{nameof(AiProviderDescriptor)}.{nameof(Provider)} must be set for '{Key}'.");
    }
}
