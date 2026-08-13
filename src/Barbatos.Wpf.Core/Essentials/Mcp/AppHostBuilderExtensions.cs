// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.Hosting;

public static partial class AppHostBuilderExtensions
{
    /// <summary>
    /// Adds the optional MCP (Model Context Protocol) client + bring-your-own-key AI chat
    /// feature: <see cref="IMcpServerRegistry"/> connects to the configured MCP servers,
    /// <see cref="IAiChatClientFactory"/>/<see cref="IAiApiKeyProvider"/> provide the BYOK model
    /// layer (the application's own end user supplies their Gemini/Claude/ChatGPT/other API key -
    /// the publisher never pays for usage), and <see cref="IAiChatService"/> ties both together
    /// so callers get MCP tool-calling "for free". Configurable from code via
    /// <paramref name="configure"/>/<paramref name="configureProvider"/>/<paramref name="configureAgent"/>
    /// and/or from the <c>Barbatos:Mcp</c>/<c>Barbatos:Mcp:Provider</c>/<c>Barbatos:Mcp:Agent</c>
    /// configuration sections (configuration values override code values).
    /// </summary>
    /// <remarks>
    /// Also registers the Microsoft Agent Framework layer on top of the same BYOK provider and
    /// MCP servers: <see cref="IAiAgentFactory"/> builds an <c>AIAgent</c> that remembers a
    /// conversation across turns and can pause for end-user tool approval, and
    /// <see cref="IAiSessionStore"/> persists those conversations across app restarts. Both
    /// layers stay available - see <see cref="IAiAgentFactory"/>'s own remarks for which to use
    /// when.
    /// <para>
    /// OAuth for HTTP servers is wired up here too: <see cref="IMcpAuthorizationHandler"/>
    /// (browser + loopback redirect by default) and <see cref="IMcpTokenStore"/>
    /// (DPAPI-encrypted <c>SecureStorage</c> by default) are registered with
    /// <c>TryAddSingleton</c>, so registering your own before this call replaces them. Neither is
    /// used unless a server sets <see cref="McpServerDescriptor.OAuth"/>.
    /// </para>
    /// </remarks>
    public static WpfAppBuilder ConfigureMcp(
        this WpfAppBuilder builder,
        Action<McpOptions>? configure = null,
        Action<AiProviderOptions>? configureProvider = null,
        Action<AiAgentOptions>? configureAgent = null)
    {
        var options = builder.Services.AddOptions<McpOptions>();
        if (configure != null)
            options.Configure(configure);
        options.Bind(builder.Configuration.GetSection(McpOptions.SectionName));

        var providerOptions = builder.Services.AddOptions<AiProviderOptions>();
        if (configureProvider != null)
            providerOptions.Configure(configureProvider);
        providerOptions.Bind(builder.Configuration.GetSection(AiProviderOptions.SectionName));

        var agentOptions = builder.Services.AddOptions<AiAgentOptions>();
        if (configureAgent != null)
            agentOptions.Configure(configureAgent);
        agentOptions.Bind(builder.Configuration.GetSection(AiAgentOptions.SectionName));

        builder.Services.TryAddSingleton<IAiApiKeyProvider, SecureStorageAiApiKeyProvider>();
        builder.Services.TryAddSingleton<IMcpAuthorizationHandler, BrowserMcpAuthorizationHandler>();
        builder.Services.TryAddSingleton<IMcpTokenStore, SecureStorageMcpTokenStore>();
        builder.Services.TryAddSingleton<IMcpServerRegistry, McpServerRegistry>();
        builder.Services.TryAddSingleton<IAiChatClientFactory, AiChatClientFactory>();
        builder.Services.TryAddSingleton<IAiChatService, AiChatService>();
        builder.Services.TryAddSingleton<IAiAgentFactory, AiAgentFactory>();
        builder.Services.TryAddSingleton<IAiSessionStore, FileAiSessionStore>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IWpfInitializeService, McpInitializer>());

        return builder;
    }

    class McpInitializer : IWpfInitializeService
    {
        public void Initialize(IServiceProvider services)
        {
            var options = services.GetRequiredService<IOptions<McpOptions>>().Value;
            if (!options.Enabled)
                return;

            var registry = services.GetRequiredService<IMcpServerRegistry>();
            var logger = services.GetService<ILoggerFactory>()?.CreateLogger<IMcpServerRegistry>();

            // Fire-and-forget, one server at a time: IWpfInitializeService.Initialize() runs
            // synchronously with no surrounding try/catch (an uncaught exception here would crash
            // app startup), and one server failing to connect must be logged, not fatal - and must
            // not prevent the others from being attempted.
            foreach (var descriptor in options.Servers)
                _ = ConnectWithLoggingAsync(registry, descriptor, logger);
        }

        static async Task ConnectWithLoggingAsync(IMcpServerRegistry registry, McpServerDescriptor descriptor, ILogger? logger)
        {
            try
            {
                await registry.AddServerAsync(descriptor).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "The MCP server '{ServerName}' could not connect at startup.", descriptor.Name);
            }
        }
    }
}
