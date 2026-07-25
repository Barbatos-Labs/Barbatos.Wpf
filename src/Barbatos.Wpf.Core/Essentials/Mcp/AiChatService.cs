// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// The default <see cref="IAiChatService"/>: delegates to <see cref="IAiChatClientFactory"/> for
/// the actual model call, after merging <see cref="IMcpServerRegistry"/>'s currently-aggregated
/// tools into the caller's own <see cref="ChatOptions"/>.
/// </summary>
internal sealed class AiChatService : IAiChatService
{
    readonly IAiChatClientFactory _chatClientFactory;
    readonly IMcpServerRegistry _serverRegistry;

    public AiChatService(IAiChatClientFactory chatClientFactory, IMcpServerRegistry serverRegistry)
    {
        _chatClientFactory = chatClientFactory;
        _serverRegistry = serverRegistry;
        _chatClientFactory.ConfigurationChanged += (_, _) => ConfigurationChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? ConfigurationChanged;

    public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default) =>
        _chatClientFactory.IsConfiguredAsync(cancellationToken);

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var client = await _chatClientFactory.GetChatClientAsync(cancellationToken).ConfigureAwait(false);
        return await client.GetResponseAsync(messages, MergeMcpTools(options), cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var client = await _chatClientFactory.GetChatClientAsync(cancellationToken).ConfigureAwait(false);

        await foreach (var update in client.GetStreamingResponseAsync(messages, MergeMcpTools(options), cancellationToken).ConfigureAwait(false))
            yield return update;
    }

    ChatOptions MergeMcpTools(ChatOptions? options)
    {
        var mcpTools = _serverRegistry.Tools;

        var merged = options?.Clone() ?? new ChatOptions();
        if (mcpTools.Count == 0)
            return merged;

        var tools = new List<AITool>(merged.Tools ?? []);
        tools.AddRange(mcpTools);
        merged.Tools = tools;
        return merged;
    }
}
