// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Extensions.AI;

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// The facade application code calls for AI chat: sends messages through the currently-configured
/// provider (via <see cref="IAiChatClientFactory"/>) with every connected MCP
/// server's tools (from <see cref="IMcpServerRegistry"/>) automatically available to the model -
/// callers never need to wire MCP tools in themselves. Tool calls the model requests execute
/// automatically, with no confirmation step; an application that wants to gate tool execution
/// behind its own confirmation UI should build its own <see cref="ChatOptions"/> from
/// <see cref="IAiChatClientFactory"/>/<see cref="IMcpServerRegistry"/> directly instead of using
/// this facade.
/// </summary>
public interface IAiChatService
{
    /// <summary>
    /// Occurs when the configured provider, model, endpoint, or API key changes in a way that
    /// affects subsequent calls. Forwards <see cref="IAiChatClientFactory.ConfigurationChanged"/>.
    /// </summary>
    event EventHandler? ConfigurationChanged;

    /// <summary>
    /// Checks whether <see cref="GetResponseAsync"/>/<see cref="GetStreamingResponseAsync"/> would
    /// currently succeed - a model is set, and an API key has been stored for the configured
    /// provider - without actually sending anything.
    /// </summary>
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends <paramref name="messages"/> and returns the complete response. <paramref name="options"/>
    /// carries through any settings the caller wants (temperature, response format, its own
    /// additional tools, ...) - every currently-connected MCP server's tools are appended to
    /// <see cref="ChatOptions.Tools"/> automatically, without disturbing tools the caller already
    /// set. Throws <see cref="InvalidOperationException"/>, naming what's missing, if no provider
    /// is fully configured yet - check <see cref="IsConfiguredAsync"/> first if you'd rather show
    /// a friendly message than catch an exception.
    /// </summary>
    Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streaming counterpart of <see cref="GetResponseAsync"/> - same tool-merging behavior, same
    /// exception if unconfigured (thrown before the first item is produced).
    /// </summary>
    IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default);
}
