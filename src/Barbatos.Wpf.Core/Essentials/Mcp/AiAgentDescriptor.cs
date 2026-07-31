// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Extensions.AI;

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// Whether the tools an agent is given have to be approved by the end user before they actually
/// run. Only affects tools reaching the model through <see cref="IAiAgentFactory"/> - the older
/// <see cref="IAiChatService"/> path is unchanged and always auto-executes.
/// </summary>
public enum AiToolApprovalMode
{
    /// <summary>
    /// Every MCP tool call pauses the run and comes back to the caller as a
    /// <c>ToolApprovalRequestContent</c> (see <see cref="AgentApprovalExtensions"/>) before it is
    /// invoked. The default, because an MCP server is third-party code whose tools this library
    /// cannot inspect for side effects - and in an app that lets end users add their own servers,
    /// it is not even code the publisher chose. The end user's "always allow" answer is
    /// remembered for the rest of the session, so this costs one prompt per distinct tool, not
    /// one per call.
    /// </summary>
    Always,

    /// <summary>
    /// Tool calls are invoked as soon as the model asks for them, with no confirmation - the same
    /// behavior <see cref="IAiChatService"/> has always had, and what Claude Desktop/Cursor do by
    /// default. Reasonable when the app itself picks every MCP server it connects to and trusts
    /// them; a poor fit when end users can add their own.
    /// </summary>
    Never,
}

/// <summary>
/// Describes one agent to build through <see cref="IAiAgentFactory.CreateAgentAsync"/> - the
/// per-call counterpart of <see cref="AiAgentOptions"/>, which describes the single default agent
/// <see cref="IAiAgentFactory.GetAgentAsync"/> returns. Use this for a second, differently-scoped
/// agent (a summarizer, a code reviewer, ...) without disturbing the default one.
/// </summary>
public class AiAgentDescriptor
{
    /// <summary>
    /// The agent's name, surfaced to the model and used in logs/traces. Required - building
    /// throws a clear <see cref="InvalidOperationException"/> if left unset.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// A short human-readable description of what this agent is for. Optional; carried through to
    /// <c>AIAgent.Description</c>, and used by the model when this agent is itself exposed to
    /// another agent as a tool.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The system instructions ("you are a ...") that shape this agent's behavior. Optional, but
    /// this is the main thing that makes an agent an agent rather than a raw chat call.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Whether this agent's tool calls need end-user approval. Defaults to
    /// <see cref="AiToolApprovalMode.Always"/> - see that member's own remarks for why.
    /// </summary>
    public AiToolApprovalMode ToolApproval { get; set; } = AiToolApprovalMode.Always;

    /// <summary>
    /// Whether every connected MCP server's tools are handed to this agent, the same way
    /// <see cref="IAiChatService"/> merges them into <c>ChatOptions.Tools</c>. Defaults to
    /// <see langword="true"/>; set to <see langword="false"/> for an agent that should only ever
    /// see the tools in <see cref="Tools"/> below.
    /// </summary>
    public bool IncludeMcpTools { get; set; } = true;

    /// <summary>
    /// The app's own tools to give this agent, in addition to the MCP ones. Build them with
    /// <c>Microsoft.Extensions.AI.AIFunctionFactory.Create(...)</c> from an ordinary C# method.
    /// Tools added here are subject to the same <see cref="ToolApproval"/> policy as the MCP ones.
    /// </summary>
    public IList<AITool> Tools { get; } = new List<AITool>();

    /// <summary>
    /// Validates that this descriptor is internally consistent, throwing
    /// <see cref="InvalidOperationException"/> if it is not. A pure check of this descriptor's own
    /// fields - no agent is built and no network call is made - so a settings UI can validate an
    /// entry before saving it, the same way <see cref="McpServerDescriptor.Validate"/> and
    /// <see cref="AiProviderDescriptor.Validate"/> do.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Name"/> is unset.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException($"{nameof(AiAgentDescriptor)}.{nameof(Name)} must be set.");
    }
}
