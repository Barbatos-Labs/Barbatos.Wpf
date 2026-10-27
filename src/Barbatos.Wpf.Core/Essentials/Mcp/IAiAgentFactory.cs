// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Agents.AI;

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// Builds the <see cref="AIAgent"/> (from <c>Microsoft.Agents.AI</c>, the Microsoft Agent
/// Framework) that this app's AI features run on: a <c>ChatClientAgent</c> over whichever
/// bring-your-own-key provider <see cref="IAiChatClientFactory"/> currently has configured, with
/// every connected MCP server's tools already attached, the configured tool-approval policy
/// applied, and logging/OpenTelemetry middleware wired in.
/// </summary>
/// <remarks>
/// <para>
/// This is the agent-shaped counterpart of <see cref="IAiChatService"/>, and the one to reach for
/// in new code. <see cref="IAiChatService"/> is a single request/response call: it has no memory
/// between calls, and it runs whatever tool the model asks for immediately. An
/// <see cref="AIAgent"/> adds the two things a desktop assistant actually needs on top of that:
/// <list type="bullet">
/// <item><description><b>Conversation memory</b> - an <c>AgentSession</c> (from
/// <c>AIAgent.CreateSessionAsync</c>) carries history across turns, and can be serialized to
/// survive an app restart (see <see cref="IAiSessionStore"/>).</description></item>
/// <item><description><b>Tool approval</b> - under
/// <see cref="AiToolApprovalMode.Always"/> a run pauses and hands the pending tool call back to
/// the app, so a WPF dialog can ask the end user before anything happens. See
/// <see cref="AgentApprovalExtensions"/>.</description></item>
/// </list>
/// </para>
/// <para>
/// Neither service replaces the other and both stay registered - an app can keep calling
/// <see cref="IAiChatService"/> for one-shot "summarize this" style requests while using an agent
/// for its conversational surface. The returned <see cref="AIAgent"/> is the framework's own
/// type, not a Barbatos wrapper, so everything Microsoft's Agent Framework documents about
/// running agents applies directly.
/// </para>
/// </remarks>
public interface IAiAgentFactory
{
    /// <summary>
    /// Occurs after anything that changes what <see cref="GetAgentAsync"/> would build - the
    /// end user picked a different provider/model or saved a new API key (forwarded from
    /// <see cref="IAiChatClientFactory.ConfigurationChanged"/>), or an MCP server connected or
    /// went away and the agent's tool list is no longer current (forwarded from
    /// <see cref="IMcpServerRegistry.Changed"/>).
    /// </summary>
    /// <remarks>
    /// Raised synchronously on the thread of whichever event it forwards. A provider change
    /// arrives on the thread that called <see cref="IAiChatClientFactory.UpdateProvider"/> or
    /// <see cref="IAiChatClientFactory.RefreshApiKeyAsync"/>, but a server connecting or dropping
    /// off normally arrives on a thread-pool thread (see <see cref="IMcpServerRegistry.Changed"/>) -
    /// in practice once at startup, when a seed server finishes connecting. This layer is
    /// deliberately UI-agnostic and does not marshal: a handler that touches bindings, a bound
    /// collection or other UI-thread state must dispatch to the UI thread itself.
    /// </remarks>
    event EventHandler? ConfigurationChanged;

    /// <summary>
    /// Checks whether <see cref="GetAgentAsync"/> would currently succeed - forwards
    /// <see cref="IAiChatClientFactory.IsConfiguredAsync"/>, since an agent needs exactly what a
    /// chat client needs (a model, and the end user's API key for it).
    /// </summary>
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default agent described by <see cref="AiAgentOptions"/>, building (and caching)
    /// one if needed. The cache is dropped whenever <see cref="ConfigurationChanged"/> fires, or
    /// when <see cref="IAiChatClientFactory.GetChatClientAsync"/> returns a different client
    /// (a new key stored without <see cref="IAiChatClientFactory.RefreshApiKeyAsync"/>), so
    /// callers should ask for the agent per conversation rather than holding one forever - an
    /// <c>AgentSession</c>, however, is safe to keep across rebuilds of the same agent shape.
    /// Editing <see cref="AiAgentOptions"/> at runtime does not drop it - see that type's remarks.
    /// </summary>
    /// <exception cref="InvalidOperationException">The model is unset, or no API key has been
    /// stored yet for the configured provider - the message names which one, exactly as
    /// <see cref="IAiChatClientFactory.GetChatClientAsync"/> does.</exception>
    Task<AIAgent> GetAgentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a one-off agent from <paramref name="descriptor"/> instead of
    /// <see cref="AiAgentOptions"/> - for a second, differently-scoped agent (a summarizer, a
    /// code reviewer, ...) alongside the default one. The result is not cached; the caller owns
    /// it, and should hold onto it for as long as the conversation it belongs to.
    /// </summary>
    /// <exception cref="InvalidOperationException"><paramref name="descriptor"/> is not
    /// internally consistent (see <see cref="AiAgentDescriptor.Validate"/>), the model is unset,
    /// or no API key has been stored yet for the configured provider.</exception>
    Task<AIAgent> CreateAgentAsync(AiAgentDescriptor descriptor, CancellationToken cancellationToken = default);
}
