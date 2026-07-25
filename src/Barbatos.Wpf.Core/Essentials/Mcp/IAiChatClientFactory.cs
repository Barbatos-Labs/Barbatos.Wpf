// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Extensions.AI;

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// Builds the provider-agnostic <see cref="IChatClient"/> (from <c>Microsoft.Extensions.AI</c>)
/// backing <see cref="IAiChatService"/>, for whichever provider is currently configured (see
/// <see cref="AiProviderOptions.Provider"/>). This is the "which provider" seam - structurally
/// the same role <c>IPushNotificationTransport</c> plays for push notifications - so an app that
/// wants a raw, BYOK-configured <see cref="IChatClient"/> with no MCP tools merged in can inject
/// this directly instead of <see cref="IAiChatService"/>.
/// </summary>
public interface IAiChatClientFactory
{
    /// <summary>
    /// Occurs after <see cref="UpdateProvider"/> or <see cref="RefreshApiKeyAsync"/> changes what
    /// <see cref="GetChatClientAsync"/> would build. Does not itself mean the new configuration is
    /// valid/usable - call <see cref="IsConfiguredAsync"/> to check that.
    /// </summary>
    event EventHandler? ConfigurationChanged;

    /// <summary>
    /// Checks whether <see cref="GetChatClientAsync"/> would currently succeed - a model is set,
    /// and an API key has been stored for the configured provider - without actually building a
    /// client or making a network call.
    /// </summary>
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the <see cref="IChatClient"/> for the currently-configured provider/model, building
    /// (and caching) one if needed.
    /// </summary>
    /// <exception cref="InvalidOperationException">The model is unset, or no API key has been
    /// stored yet for the configured provider via <see cref="IAiApiKeyProvider"/> - the message
    /// names which one.</exception>
    Task<IChatClient> GetChatClientAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes which provider/model/endpoint <see cref="GetChatClientAsync"/> builds a client
    /// for, invalidating any cached client. Intended to be called from a settings UI when the end
    /// user changes their provider choice. See <see cref="AiProviderOptions"/>'s own remarks for
    /// what <paramref name="provider"/>/<paramref name="endpoint"/> mean.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="provider"/> or
    /// <paramref name="model"/> is null or whitespace.</exception>
    void UpdateProvider(string provider, string model, string? endpoint = null);

    /// <summary>
    /// Convenience over <see cref="UpdateProvider"/>: switches to the
    /// <see cref="AiProviderOptions.Providers"/> entry whose
    /// <see cref="AiProviderDescriptor.Key"/> matches <paramref name="key"/>
    /// (case-insensitively), calling <see cref="UpdateProvider"/> with that entry's own
    /// <see cref="AiProviderDescriptor.Provider"/>/<see cref="AiProviderDescriptor.Model"/>/
    /// <see cref="AiProviderDescriptor.Endpoint"/>. For a provider not in that catalog (e.g. one
    /// the end user typed in themselves), call <see cref="UpdateProvider"/> directly instead.
    /// </summary>
    /// <exception cref="InvalidOperationException">No entry in <see cref="AiProviderOptions.Providers"/>
    /// has a matching <see cref="AiProviderDescriptor.Key"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is null or whitespace.</exception>
    void SelectProvider(string key);

    /// <summary>
    /// Invalidates any cached client so the next <see cref="GetChatClientAsync"/> call re-reads
    /// the API key from <see cref="IAiApiKeyProvider"/>. <see cref="GetChatClientAsync"/> already
    /// re-checks the stored key on every call, so this is mainly useful to force
    /// <see cref="ConfigurationChanged"/> to fire immediately after a settings UI calls
    /// <see cref="IAiApiKeyProvider.SetApiKeyAsync"/>, rather than waiting for the next chat call.
    /// </summary>
    Task RefreshApiKeyAsync(CancellationToken cancellationToken = default);
}
