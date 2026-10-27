// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// Shapes the default agent <see cref="IAiAgentFactory.GetAgentAsync"/> returns, nested under
/// <see cref="McpOptions.SectionName"/> alongside <see cref="AiProviderOptions"/>. Can be
/// configured from code via <c>ConfigureMcp</c>'s <c>configureAgent</c> parameter and/or from
/// configuration files using <see cref="SectionName"/> (file values override code values).
/// </summary>
/// <remarks>
/// <para>
/// This is the "what kind of assistant is it" layer, deliberately separate from
/// <see cref="AiProviderOptions"/>'s "which model answers" layer: swapping the end user's
/// provider from OpenAI to Anthropic should not disturb the agent's instructions, and rewording
/// the instructions should not invalidate a stored API key.
/// </para>
/// <para>
/// These values are read when the default agent is <i>built</i>, and
/// <see cref="IAiAgentFactory.GetAgentAsync"/> caches that agent until
/// <see cref="IAiChatClientFactory.ConfigurationChanged"/> or
/// <see cref="IMcpServerRegistry.Changed"/> fires, or the chat client factory hands back a
/// different client instance. Changing a property of this options object at runtime therefore
/// does not by itself change the agent <see cref="IAiAgentFactory.GetAgentAsync"/> returns - it
/// shows up at the next rebuild, whenever that happens. The options are also bound from
/// configuration once, so a reloaded configuration file does not reach them either. For
/// different instructions or tools at runtime, build an agent with
/// <see cref="IAiAgentFactory.CreateAgentAsync"/> and an <see cref="AiAgentDescriptor"/>, or pass
/// per-run instructions in the run options of each <c>RunAsync</c> call.
/// </para>
/// </remarks>
public class AiAgentOptions
{
    /// <summary>
    /// The configuration section the options are bound from.
    /// </summary>
    public const string SectionName = "Barbatos:Mcp:Agent";

    /// <summary>
    /// The default agent's name, surfaced to the model and used in logs/traces. Defaults to
    /// <c>"Assistant"</c>.
    /// </summary>
    public string Name { get; set; } = "Assistant";

    /// <summary>
    /// A short human-readable description of what the default agent is for. Optional - see
    /// <see cref="AiAgentDescriptor.Description"/>.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The system instructions ("you are a ...") shaping the default agent's behavior. Optional,
    /// but this is what most apps will actually want to set - see
    /// <see cref="AiAgentDescriptor.Instructions"/>.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Whether the default agent's tool calls need end-user approval. Defaults to
    /// <see cref="AiToolApprovalMode.Always"/> - see that member's own remarks for why, and
    /// <see cref="AgentApprovalExtensions"/> for how to answer a request from a WPF dialog.
    /// </summary>
    public AiToolApprovalMode ToolApproval { get; set; } = AiToolApprovalMode.Always;

    /// <summary>
    /// Whether every connected MCP server's tools are handed to the default agent. Defaults to
    /// <see langword="true"/> - see <see cref="AiAgentDescriptor.IncludeMcpTools"/>.
    /// </summary>
    public bool IncludeMcpTools { get; set; } = true;

    /// <summary>
    /// Whether agent runs are logged through the host's own <c>ILoggerFactory</c>. Defaults to
    /// <see langword="true"/>, and is a no-op when no logging has been configured, so this
    /// costs nothing to leave on.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Whether agent runs emit OpenTelemetry activities and metrics (run duration, token usage,
    /// tool calls) under <see cref="OpenTelemetrySourceName"/>. Defaults to
    /// <see langword="false"/>: unlike logging, this only pays off once the app actually exports
    /// telemetry somewhere, so it is opt-in.
    /// </summary>
    public bool EnableOpenTelemetry { get; set; }

    /// <summary>
    /// The <c>ActivitySource</c>/<c>Meter</c> name used when <see cref="EnableOpenTelemetry"/> is
    /// on - this is the name the app subscribes to in its own OpenTelemetry setup. Defaults to
    /// <c>"Barbatos.Wpf.Mcp"</c>.
    /// </summary>
    public string OpenTelemetrySourceName { get; set; } = "Barbatos.Wpf.Mcp";
}
