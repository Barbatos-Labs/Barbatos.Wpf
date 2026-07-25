// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// Which AI provider/model backs <see cref="IAiChatService"/>, nested under
/// <see cref="McpOptions.SectionName"/> the same way <c>SignalRPushNotificationOptions</c> nests
/// under <c>PushNotificationOptions</c>. Can be configured from code via <c>ConfigureMcp</c>'s
/// <c>configureProvider</c> parameter and/or from configuration files using
/// <see cref="SectionName"/> (file values override code values).
/// </summary>
/// <remarks>
/// Deliberately carries no API key/secret - that always belongs to the application's end user,
/// never to configuration files or source control, and is resolved separately through
/// <see cref="IAiApiKeyProvider"/> (backed by <see cref="Storage.ISecureStorage"/> by default).
/// <para>
/// <see cref="Provider"/> is a plain string, not a fixed enum: this library only ever draws a
/// real technical distinction between exactly two shapes - Anthropic's own SDK (used when
/// <see cref="Provider"/> case-insensitively equals <c>"anthropic"</c>) and everything else,
/// which is assumed OpenAI-wire-compatible (the overwhelming majority of providers today,
/// including Gemini via its own documented OpenAI-compatible endpoint). Baking every provider
/// name an app might ever want into a shared library enum would need a new release for each one;
/// a string lets an app configure
/// whatever list of providers (and suggested models, and endpoints) fits its own users, without
/// waiting on this library.
/// </para>
/// </remarks>
public class AiProviderOptions
{
    /// <summary>
    /// The configuration section the options are bound from.
    /// </summary>
    public const string SectionName = "Barbatos:Mcp:Provider";

    /// <summary>
    /// Which provider <see cref="IAiChatClientFactory"/> builds an <c>IChatClient</c> for - also
    /// the key <see cref="IAiApiKeyProvider"/> stores/looks up this provider's API key under
    /// (case-insensitively). Defaults to <c>"openai"</c>. See the type remarks for how this
    /// string decides which SDK gets used.
    /// </summary>
    public string Provider { get; set; } = "openai";

    /// <summary>
    /// The model name to request (e.g. <c>"gpt-5.2"</c>, <c>"gemini-3.5-flash"</c>,
    /// <c>"claude-opus-4-8"</c>) - meaning is entirely up to the selected <see cref="Provider"/>.
    /// Required - <see cref="IAiChatClientFactory.GetChatClientAsync"/> throws a clear
    /// <see cref="InvalidOperationException"/> if left unset.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// The API base URL to call, for any <see cref="Provider"/> other than (case-insensitively)
    /// <c>"anthropic"</c>. Left <see langword="null"/>, the real OpenAI API is used - set this to
    /// point at Gemini (<c>https://generativelanguage.googleapis.com/v1beta/openai/</c>), a
    /// self-hosted model via Ollama/LM Studio/vLLM, OpenRouter, a proxy, or any other
    /// OpenAI-wire-compatible endpoint. Ignored for <c>"anthropic"</c>, which always uses
    /// Anthropic's own official endpoint.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// The providers this app wants to offer as choices (e.g. in a settings UI's provider
    /// picker) - seeded here the same way <see cref="McpOptions.Servers"/> seeds MCP servers,
    /// and switched to at runtime via <see cref="IAiChatClientFactory.SelectProvider"/>. Purely
    /// a catalog: nothing here takes effect until <see cref="IAiChatClientFactory.SelectProvider"/>
    /// is called (or <see cref="Provider"/>/<see cref="Model"/>/<see cref="Endpoint"/> above are
    /// set directly) - leave this empty if your app hardcodes its own list instead, or only ever
    /// supports one provider.
    /// </summary>
    public IList<AiProviderDescriptor> Providers { get; } = new List<AiProviderDescriptor>();
}
