// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// The default <see cref="IAiAgentFactory"/>. Composes three things this library already owns
/// separately - the BYOK chat client from <see cref="IAiChatClientFactory"/>, the connected
/// servers' tools from <see cref="IMcpServerRegistry"/>, and the agent shape from
/// <see cref="AiAgentOptions"/> - into a <see cref="ChatClientAgent"/>, and keeps that agent only
/// as long as all three are still current.
/// </summary>
internal sealed class AiAgentFactory : IAiAgentFactory, IDisposable
{
    readonly IAiChatClientFactory _chatClientFactory;
    readonly IMcpServerRegistry _serverRegistry;
    readonly AiAgentOptions _options;
    readonly ILoggerFactory? _loggerFactory;
    readonly object _lock = new();

    AIAgent? _cachedAgent;
    IChatClient? _cachedAgentChatClient;

    public AiAgentFactory(
        IOptions<AiAgentOptions> options,
        IAiChatClientFactory chatClientFactory,
        IMcpServerRegistry serverRegistry,
        ILoggerFactory? loggerFactory = null)
    {
        _options = options.Value;
        _chatClientFactory = chatClientFactory;
        _serverRegistry = serverRegistry;
        _loggerFactory = loggerFactory;

        _chatClientFactory.ConfigurationChanged += OnDependencyChanged;
        // A server connecting or disconnecting changes the agent's tool list, and unlike
        // IAiChatService - which re-reads IMcpServerRegistry.Tools on every single call - an agent
        // is handed its tools once, at construction. Without this the agent would keep offering a
        // removed server's tools (and never see a newly connected one's) until the provider
        // happened to change.
        _serverRegistry.Changed += OnDependencyChanged;
    }

    public event EventHandler? ConfigurationChanged;

    public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default) =>
        _chatClientFactory.IsConfiguredAsync(cancellationToken);

    public async Task<AIAgent> GetAgentAsync(CancellationToken cancellationToken = default)
    {
        var chatClient = await _chatClientFactory.GetChatClientAsync(cancellationToken).ConfigureAwait(false);

        lock (_lock)
        {
            // Compared by reference, not just trusted from the events above: IAiChatClientFactory
            // rebuilds (and disposes) its client whenever the provider/model/endpoint/API key it
            // was built from changes, and an app is free to store a new key via IAiApiKeyProvider
            // without calling RefreshApiKeyAsync - in which case no ConfigurationChanged ever
            // fires, and a cached agent would be left holding a disposed chat client.
            if (_cachedAgent is not null && ReferenceEquals(_cachedAgentChatClient, chatClient))
                return _cachedAgent;
        }

        var agent = Build(
            chatClient,
            _options.Name,
            _options.Description,
            _options.Instructions,
            _options.ToolApproval,
            _options.IncludeMcpTools,
            extraTools: null);

        lock (_lock)
        {
            _cachedAgent = agent;
            _cachedAgentChatClient = chatClient;
        }

        return agent;
    }

    public async Task<AIAgent> CreateAgentAsync(AiAgentDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        descriptor.Validate();

        var chatClient = await _chatClientFactory.GetChatClientAsync(cancellationToken).ConfigureAwait(false);

        return Build(
            chatClient,
            descriptor.Name!,
            descriptor.Description,
            descriptor.Instructions,
            descriptor.ToolApproval,
            descriptor.IncludeMcpTools,
            descriptor.Tools);
    }

    AIAgent Build(
        IChatClient chatClient,
        string name,
        string? description,
        string? instructions,
        AiToolApprovalMode approvalMode,
        bool includeMcpTools,
        IEnumerable<AITool>? extraTools)
    {
        var tools = new List<AITool>();
        if (includeMcpTools)
            tools.AddRange(_serverRegistry.Tools);
        if (extraTools is not null)
            tools.AddRange(extraTools);

        if (approvalMode == AiToolApprovalMode.Always)
        {
            for (var i = 0; i < tools.Count; i++)
            {
                // Only an AIFunction can be gated this way - the model-side declaration is
                // unchanged, but FunctionInvokingChatClient turns a call to a wrapped function
                // into a ToolApprovalRequestContent instead of invoking it. Anything that is not
                // an AIFunction (a hosted/server-side tool, say) is passed through untouched:
                // its execution never reaches this process, so there is nothing here to gate.
                if (tools[i] is AIFunction function and not ApprovalRequiredAIFunction)
                    tools[i] = new ApprovalRequiredAIFunction(function);
            }
        }

        AIAgent agent = new ChatClientAgent(
            chatClient,
            instructions: instructions,
            name: name,
            description: description,
            tools: tools);

        var builder = agent.AsBuilder();

        if (approvalMode == AiToolApprovalMode.Always)
        {
            // Turns the raw "approve this one call" protocol into the "don't ask again" behavior
            // an end user expects: an AlwaysApprove answer (see AgentApprovalExtensions) is
            // recorded as a standing rule in the session, so the same tool stops prompting for
            // the rest of that conversation. Also serializes multiple pending requests into one
            // prompt at a time, which is what a modal WPF dialog can actually present.
            builder = builder.UseToolApproval();
        }

        if (_options.EnableLogging && _loggerFactory is not null)
            builder = builder.UseLogging(_loggerFactory);

        if (_options.EnableOpenTelemetry)
            builder = builder.UseOpenTelemetry(_options.OpenTelemetrySourceName);

        return builder.Build();
    }

    void OnDependencyChanged(object? sender, EventArgs e)
    {
        lock (_lock)
        {
            _cachedAgent = null;
            _cachedAgentChatClient = null;
        }

        ConfigurationChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _chatClientFactory.ConfigurationChanged -= OnDependencyChanged;
        _serverRegistry.Changed -= OnDependencyChanged;
    }
}
