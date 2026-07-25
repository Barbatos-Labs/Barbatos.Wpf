// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Mcp.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// The default <see cref="IAiChatClientFactory"/>. Seeds its initial provider/model/endpoint from
/// <see cref="AiProviderOptions"/> at construction, then tracks further changes made via
/// <see cref="UpdateProvider"/> directly (like every other runtime-mutable feature in this
/// library, there is no re-read of <c>IOptionsMonitor</c> later).
/// </summary>
internal sealed class AiChatClientFactory : IAiChatClientFactory, IDisposable
{
    readonly IAiApiKeyProvider _apiKeyProvider;
    readonly IReadOnlyList<AiProviderDescriptor> _providerCatalog;
    readonly object _lock = new();

    string _provider;
    string? _model;
    string? _endpoint;

    IChatClient? _cachedClient;
    CacheKey? _cachedKey;

    public AiChatClientFactory(IOptions<AiProviderOptions> options, IAiApiKeyProvider apiKeyProvider)
    {
        _apiKeyProvider = apiKeyProvider;

        var value = options.Value;
        _provider = value.Provider;
        _model = value.Model;
        _endpoint = value.Endpoint;
        // Snapshotted once, same as Provider/Model/Endpoint above - a settings UI that lets end
        // users add their own catalog entries at runtime should keep its own list and call
        // UpdateProvider directly, the same way IMcpServerRegistry.AddServerAsync is the runtime
        // seam for MCP servers rather than McpOptions.Servers itself.
        _providerCatalog = [.. value.Providers];
    }

    public event EventHandler? ConfigurationChanged;

    public void UpdateProvider(string provider, string model, string? endpoint = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        lock (_lock)
        {
            _provider = provider;
            _model = model;
            _endpoint = endpoint;
            InvalidateLocked();
        }

        ConfigurationChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SelectProvider(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var entry = _providerCatalog.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            throw new InvalidOperationException(
                $"No {nameof(AiProviderOptions)}.{nameof(AiProviderOptions.Providers)} entry has {nameof(AiProviderDescriptor)}.{nameof(AiProviderDescriptor.Key)} '{key}' - " +
                $"call {nameof(UpdateProvider)}(...) directly for a provider outside that catalog.");
        }

        entry.Validate();

        if (string.IsNullOrWhiteSpace(entry.Model))
        {
            throw new InvalidOperationException(
                $"{nameof(AiProviderDescriptor)}.{nameof(AiProviderDescriptor.Model)} must be set on the '{key}' entry before it can be selected.");
        }

        UpdateProvider(entry.Provider, entry.Model, entry.Endpoint);
    }

    public Task RefreshApiKeyAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
            InvalidateLocked();

        ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default)
    {
        var (provider, model, _) = CurrentSelection();
        if (string.IsNullOrWhiteSpace(model))
            return false;

        var apiKey = await _apiKeyProvider.GetApiKeyAsync(provider, cancellationToken).ConfigureAwait(false);
        return !string.IsNullOrWhiteSpace(apiKey);
    }

    public async Task<IChatClient> GetChatClientAsync(CancellationToken cancellationToken = default)
    {
        var (provider, model, endpoint) = CurrentSelection();

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new InvalidOperationException(
                $"{nameof(AiProviderOptions)}.{nameof(AiProviderOptions.Model)} must be set before requesting a chat client - " +
                $"call {nameof(UpdateProvider)}(...) or configure it via ConfigureMcp's configureProvider parameter.");
        }

        var apiKey = await _apiKeyProvider.GetApiKeyAsync(provider, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"No API key has been set for provider '{provider}' - call {nameof(IAiApiKeyProvider)}.{nameof(IAiApiKeyProvider.SetApiKeyAsync)} first " +
                "(typically from a settings UI, when the end user pastes in their own key).");
        }

        var key = new CacheKey(provider, model, endpoint, apiKey);

        lock (_lock)
        {
            if (_cachedClient is not null && _cachedKey == key)
                return _cachedClient;
        }

        // Anthropic is the one wire protocol that genuinely isn't OpenAI-compatible and needs its
        // own SDK; every other provider string - including Gemini, via its own documented
        // OpenAI-compatible endpoint - goes through the OpenAI client. A null/unset endpoint
        // there means the real OpenAI API, so there is nothing to validate up front the way
        // AiProvider.Custom used to require an endpoint.
        var rawClient = string.Equals(provider, "anthropic", StringComparison.OrdinalIgnoreCase)
            ? AnthropicChatClientFactory.Create(apiKey, model)
            : OpenAiChatClientFactory.Create(apiKey, model, endpoint);

        // Enables MCP (and any other) tool calls automatically - a no-op whenever ChatOptions.Tools
        // is empty, so this is a safe default even for a caller that never touches MCP.
        var client = rawClient.AsBuilder().UseFunctionInvocation().Build();

        IChatClient? previous;
        lock (_lock)
        {
            previous = _cachedClient;
            _cachedClient = client;
            _cachedKey = key;
        }
        previous?.Dispose();

        return client;
    }

    (string Provider, string? Model, string? Endpoint) CurrentSelection()
    {
        lock (_lock)
            return (_provider, _model, _endpoint);
    }

    void InvalidateLocked()
    {
        _cachedClient = null;
        _cachedKey = null;
    }

    public void Dispose() =>
        _cachedClient?.Dispose();

    sealed record CacheKey(string Provider, string Model, string? Endpoint, string ApiKey);
}
