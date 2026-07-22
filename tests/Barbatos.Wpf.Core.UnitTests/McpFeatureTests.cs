// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Mcp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.Core.UnitTests;

public class McpFeatureTests
{
    [Fact]
    public void FeatureIsNotRegisteredByDefault()
    {
        var wpfApp = WpfApp.CreateBuilder().Build();

        Assert.Null(wpfApp.Services.GetService<IMcpServerRegistry>());
        Assert.Null(wpfApp.Services.GetService<IAiChatClientFactory>());
        Assert.Null(wpfApp.Services.GetService<IAiChatService>());
        Assert.Null(wpfApp.Services.GetService<IAiApiKeyProvider>());
    }

    [Fact]
    public void FeatureIsRegisteredWhenConfigured()
    {
        var builder = WpfApp.CreateBuilder();
        builder.ConfigureMcp();
        var wpfApp = builder.Build();

        Assert.NotNull(wpfApp.Services.GetService<IMcpServerRegistry>());
        Assert.NotNull(wpfApp.Services.GetService<IAiChatClientFactory>());
        Assert.NotNull(wpfApp.Services.GetService<IAiChatService>());
        Assert.NotNull(wpfApp.Services.GetService<IAiApiKeyProvider>());
    }

    [Fact]
    public void CustomApiKeyProviderRegisteredBeforeConfigureMcpTakesPrecedence()
    {
        var builder = WpfApp.CreateBuilder();
        var custom = new FakeAiApiKeyProvider();
        builder.Services.AddSingleton<IAiApiKeyProvider>(custom);
        builder.ConfigureMcp();
        var wpfApp = builder.Build();

        Assert.Same(custom, wpfApp.Services.GetRequiredService<IAiApiKeyProvider>());
    }

    [Fact]
    public async Task DefaultApiKeyProviderStoresKeysCaseInsensitivelyByProvider()
    {
        // Registered before ConfigureMcp so the real (non-faked) SecureStorageAiApiKeyProvider
        // is what gets constructed, backed by this in-memory storage instead of real DPAPI -
        // TryAddSingleton inside ConfigureMcp won't override a registration already present,
        // the same precedence CustomApiKeyProviderRegisteredBeforeConfigureMcpTakesPrecedence
        // relies on above.
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<Storage.ISecureStorage>(new FakeSecureStorage());
        builder.ConfigureMcp();
        var wpfApp = builder.Build();

        var keyProvider = wpfApp.Services.GetRequiredService<IAiApiKeyProvider>();
        await keyProvider.SetApiKeyAsync("Anthropic", "test-key");

        Assert.Equal("test-key", await keyProvider.GetApiKeyAsync("anthropic"));
        Assert.Equal("test-key", await keyProvider.GetApiKeyAsync("ANTHROPIC"));
    }

    [Fact]
    public void ConfigurationOverridesCodeForProviderOptions()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Barbatos:Mcp:Provider:Model"] = "from-config",
        });
        builder.ConfigureMcp(configureProvider: options => options.Model = "from-code");
        var wpfApp = builder.Build();

        var options = wpfApp.Services.GetRequiredService<IOptions<AiProviderOptions>>().Value;
        Assert.Equal("from-config", options.Model);
    }

    [Fact]
    public void ValidateDoesNotThrowForWellFormedDescriptors()
    {
        Assert.Null(Record.Exception(() =>
            new McpServerDescriptor { Name = "a", TransportKind = McpTransportKind.Stdio, Command = "dotnet" }.Validate()));
        Assert.Null(Record.Exception(() =>
            new McpServerDescriptor { Name = "b", TransportKind = McpTransportKind.Http, Endpoint = "https://example.com/mcp" }.Validate()));
    }

    [Fact]
    public async Task AddServerAsyncThrowsWhenNameIsMissing()
    {
        var registry = BuildRegistry();

        await Assert.ThrowsAsync<InvalidOperationException>(() => registry.AddServerAsync(new McpServerDescriptor()));
    }

    [Fact]
    public async Task AddServerAsyncThrowsWhenStdioCommandIsMissing()
    {
        var registry = BuildRegistry();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            registry.AddServerAsync(new McpServerDescriptor { Name = "test", TransportKind = McpTransportKind.Stdio }));
        Assert.Contains(nameof(McpServerDescriptor.Command), exception.Message);
    }

    [Fact]
    public async Task AddServerAsyncThrowsWhenHttpEndpointIsMissing()
    {
        var registry = BuildRegistry();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            registry.AddServerAsync(new McpServerDescriptor { Name = "test", TransportKind = McpTransportKind.Http }));
        Assert.Contains(nameof(McpServerDescriptor.Endpoint), exception.Message);
    }

    [Fact]
    public async Task AddServerAsyncThrowsForDuplicateNamesEvenIfThePreviousAttemptFailedToConnect()
    {
        var registry = BuildRegistry();
        var descriptor = new McpServerDescriptor
        {
            Name = "dup",
            TransportKind = McpTransportKind.Stdio,
            Command = "this-executable-does-not-exist-barbatos-test",
        };

        // Passes Validate() (Command is set) but fails to actually launch - an immediate
        // process-start failure, not real network I/O, so this stays fast and deterministic.
        await Assert.ThrowsAnyAsync<Exception>(() => registry.AddServerAsync(descriptor));

        // The name was still registered (as a failed entry) before the launch failed.
        await Assert.ThrowsAsync<ArgumentException>(() => registry.AddServerAsync(descriptor));
    }

    [Fact]
    public async Task RemoveServerAsyncReturnsFalseForUnknownName()
    {
        var registry = BuildRegistry();

        Assert.False(await registry.RemoveServerAsync("unknown"));
    }

    [Fact]
    public async Task GetChatClientAsyncThrowsWhenModelIsMissing()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IAiApiKeyProvider>(new FakeAiApiKeyProvider());
        builder.ConfigureMcp(); // Model left unset.
        var wpfApp = builder.Build();

        var factory = wpfApp.Services.GetRequiredService<IAiChatClientFactory>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => factory.GetChatClientAsync());
        Assert.Contains(nameof(AiProviderOptions.Model), exception.Message);
    }

    [Fact]
    public async Task GetChatClientAsyncThrowsWhenApiKeyIsMissing()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IAiApiKeyProvider>(new FakeAiApiKeyProvider());
        builder.ConfigureMcp(configureProvider: options =>
        {
            options.Provider = "openai";
            options.Model = "gpt-5.2";
        });
        var wpfApp = builder.Build();

        var factory = wpfApp.Services.GetRequiredService<IAiChatClientFactory>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => factory.GetChatClientAsync());
        Assert.Contains("API key", exception.Message);
    }

    [Fact]
    public async Task GetChatClientAsyncDefaultsToRealOpenAiWhenNoEndpointIsSetForANonAnthropicProvider()
    {
        var keyProvider = new FakeAiApiKeyProvider();
        await keyProvider.SetApiKeyAsync("my-custom-provider", "test-key");

        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IAiApiKeyProvider>(keyProvider);
        builder.ConfigureMcp(configureProvider: options =>
        {
            // Any provider string works here, not just a fixed "Custom" enum value - and no
            // Endpoint is set, which no longer requires one: every non-"anthropic" provider
            // with an unset endpoint is simply routed to the real OpenAI API.
            options.Provider = "my-custom-provider";
            options.Model = "local-model";
        });
        var wpfApp = builder.Build();

        var factory = wpfApp.Services.GetRequiredService<IAiChatClientFactory>();

        var client = await factory.GetChatClientAsync();

        Assert.NotNull(client);
    }

    [Fact]
    public async Task GetChatClientAsyncRoutesToAnthropicCaseInsensitively()
    {
        var keyProvider = new FakeAiApiKeyProvider();
        await keyProvider.SetApiKeyAsync("Anthropic", "test-key");

        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IAiApiKeyProvider>(keyProvider);
        builder.ConfigureMcp(configureProvider: options =>
        {
            // Mixed case on purpose - the "is this Anthropic" check must not be case-sensitive.
            options.Provider = "Anthropic";
            options.Model = "claude-opus-4-8";
        });
        var wpfApp = builder.Build();

        var factory = wpfApp.Services.GetRequiredService<IAiChatClientFactory>();

        var client = await factory.GetChatClientAsync();

        Assert.NotNull(client);
    }

    [Fact]
    public void AiProviderDescriptorValidateDoesNotThrowForAWellFormedEntry()
    {
        Assert.Null(Record.Exception(() =>
            new AiProviderDescriptor { Key = "openai", Provider = "openai" }.Validate()));
    }

    [Fact]
    public void AiProviderDescriptorValidateThrowsWhenKeyIsMissing()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new AiProviderDescriptor { Provider = "openai" }.Validate());
        Assert.Contains(nameof(AiProviderDescriptor.Key), exception.Message);
    }

    [Fact]
    public void AiProviderDescriptorValidateThrowsWhenProviderIsMissing()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new AiProviderDescriptor { Key = "custom", Provider = "" }.Validate());
        Assert.Contains(nameof(AiProviderDescriptor.Provider), exception.Message);
    }

    [Fact]
    public async Task SelectProviderSwitchesToACatalogEntryByKey()
    {
        var keyProvider = new FakeAiApiKeyProvider();
        await keyProvider.SetApiKeyAsync("anthropic", "test-key");

        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IAiApiKeyProvider>(keyProvider);
        builder.ConfigureMcp(configureProvider: options =>
        {
            options.Providers.Add(new AiProviderDescriptor { Key = "claude", Provider = "anthropic", Model = "claude-opus-4-8" });
            // Active on startup - SelectProvider below must override this, not just add to it.
            options.Provider = "openai";
            options.Model = "gpt-5.2";
        });
        var wpfApp = builder.Build();

        var factory = wpfApp.Services.GetRequiredService<IAiChatClientFactory>();
        factory.SelectProvider("claude");

        // Succeeding at all proves Provider="anthropic" (from the catalog entry, not the "claude"
        // key) was what got used - the API key above was stored under "anthropic", so a lookup
        // under the wrong provider string would fail with "no API key" instead.
        var client = await factory.GetChatClientAsync();
        Assert.NotNull(client);
    }

    [Fact]
    public void SelectProviderThrowsClearlyForAnUnknownKey()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IAiApiKeyProvider>(new FakeAiApiKeyProvider());
        builder.ConfigureMcp();
        var wpfApp = builder.Build();

        var factory = wpfApp.Services.GetRequiredService<IAiChatClientFactory>();

        var exception = Assert.Throws<InvalidOperationException>(() => factory.SelectProvider("nonexistent"));
        Assert.Contains("nonexistent", exception.Message);
    }

    [Fact]
    public void SelectProviderThrowsClearlyWhenTheCatalogEntryHasNoModel()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IAiApiKeyProvider>(new FakeAiApiKeyProvider());
        builder.ConfigureMcp(configureProvider: options =>
        {
            options.Providers.Add(new AiProviderDescriptor { Key = "incomplete", Provider = "openai" });
        });
        var wpfApp = builder.Build();

        var factory = wpfApp.Services.GetRequiredService<IAiChatClientFactory>();

        var exception = Assert.Throws<InvalidOperationException>(() => factory.SelectProvider("incomplete"));
        Assert.Contains(nameof(AiProviderDescriptor.Model), exception.Message);
    }

    [Fact]
    public void ProvidersListBindsFromConfiguration()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Barbatos:Mcp:Provider:Providers:0:Key"] = "openai",
            ["Barbatos:Mcp:Provider:Providers:0:Provider"] = "openai",
            ["Barbatos:Mcp:Provider:Providers:0:Model"] = "gpt-5.2",
        });
        builder.ConfigureMcp();
        var wpfApp = builder.Build();

        var options = wpfApp.Services.GetRequiredService<IOptions<AiProviderOptions>>().Value;

        Assert.Single(options.Providers);
        Assert.Equal("openai", options.Providers[0].Key);
        Assert.Equal("gpt-5.2", options.Providers[0].Model);
    }

    [Fact]
    public async Task IsConfiguredAsyncReflectsWhetherAnApiKeyIsStored()
    {
        var keyProvider = new FakeAiApiKeyProvider();
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IAiApiKeyProvider>(keyProvider);
        builder.ConfigureMcp(configureProvider: options =>
        {
            options.Provider = "openai";
            options.Model = "gpt-5.2";
        });
        var wpfApp = builder.Build();

        var factory = wpfApp.Services.GetRequiredService<IAiChatClientFactory>();

        Assert.False(await factory.IsConfiguredAsync());

        await keyProvider.SetApiKeyAsync("openai", "sk-test");

        Assert.True(await factory.IsConfiguredAsync());
    }

    [Fact]
    public async Task GetResponseAsyncMergesMcpToolsWithoutClobberingCallerSuppliedTools()
    {
        var chatClient = new FakeChatClient();
        var chatClientFactory = new FakeAiChatClientFactory { Client = chatClient };
        var serverRegistry = new FakeMcpServerRegistry();
        var mcpTool = AIFunctionFactory.Create(() => "mcp result", "mcp_tool");
        serverRegistry.ToolsList.Add(mcpTool);
        var callerTool = AIFunctionFactory.Create(() => "caller result", "caller_tool");

        var service = BuildChatService(chatClientFactory, serverRegistry);

        await service.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "hi")],
            new ChatOptions { Tools = [callerTool] });

        var tools = chatClient.LastOptions?.Tools;
        Assert.NotNull(tools);
        Assert.Equal(2, tools!.Count);
        Assert.Contains(callerTool, tools);
        Assert.Contains(mcpTool, tools);
    }

    [Fact]
    public async Task GetResponseAsyncAddsNoToolsWhenNoServerIsConnected()
    {
        var chatClient = new FakeChatClient();
        var chatClientFactory = new FakeAiChatClientFactory { Client = chatClient };
        var serverRegistry = new FakeMcpServerRegistry();

        var service = BuildChatService(chatClientFactory, serverRegistry);

        await service.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        Assert.Null(chatClient.LastOptions?.Tools);
    }

    [Fact]
    public async Task GetResponseAsyncPropagatesExceptionWhenFactoryThrows()
    {
        var chatClientFactory = new FakeAiChatClientFactory { Client = null };
        var service = BuildChatService(chatClientFactory, new FakeMcpServerRegistry());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]));
    }

    [Fact]
    public async Task IsConfiguredAsyncDelegatesToTheChatClientFactory()
    {
        var chatClientFactory = new FakeAiChatClientFactory { Configured = false };
        var service = BuildChatService(chatClientFactory, new FakeMcpServerRegistry());

        Assert.False(await service.IsConfiguredAsync());

        chatClientFactory.Configured = true;

        Assert.True(await service.IsConfiguredAsync());
    }

    [Fact]
    public void ConfigurationChangedForwardsFromTheChatClientFactory()
    {
        var chatClientFactory = new FakeAiChatClientFactory();
        var service = BuildChatService(chatClientFactory, new FakeMcpServerRegistry());

        var raised = 0;
        service.ConfigurationChanged += (_, _) => raised++;

        chatClientFactory.RaiseConfigurationChanged();

        Assert.Equal(1, raised);
    }

    [Fact]
    public void RealRegistryAndFactoryImplementSynchronousDisposableForContainerSafety()
    {
        var builder = WpfApp.CreateBuilder();
        builder.ConfigureMcp();
        var wpfApp = builder.Build();

        // McpClient/IChatClient-backed resources only implement async disposal; the DI
        // container's synchronous Dispose() throws if a tracked singleton doesn't also
        // implement IDisposable - this checks the real (non-faked) registry and factory
        // registered by ConfigureMcp, the same check PushNotificationFeatureTests runs for
        // SignalRPushNotificationTransport.
        var registry = wpfApp.Services.GetRequiredService<IMcpServerRegistry>();
        var registryDisposable = Assert.IsAssignableFrom<IDisposable>(registry);
        Assert.Null(Record.Exception(() => registryDisposable.Dispose()));

        var factory = wpfApp.Services.GetRequiredService<IAiChatClientFactory>();
        var factoryDisposable = Assert.IsAssignableFrom<IDisposable>(factory);
        Assert.Null(Record.Exception(() => factoryDisposable.Dispose()));
    }

    [Fact]
    public void HostDisposesCleanlyWhenMcpIsConfigured()
    {
        var builder = WpfApp.CreateBuilder();
        builder.ConfigureMcp();
        var wpfApp = builder.Build();

        Assert.Null(Record.Exception(() => wpfApp.Dispose()));
    }

    static IMcpServerRegistry BuildRegistry()
    {
        var builder = WpfApp.CreateBuilder();
        builder.ConfigureMcp();
        var wpfApp = builder.Build();

        return wpfApp.Services.GetRequiredService<IMcpServerRegistry>();
    }

    static IAiChatService BuildChatService(IAiChatClientFactory chatClientFactory, IMcpServerRegistry serverRegistry)
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton(chatClientFactory);
        builder.Services.AddSingleton(serverRegistry);
        builder.ConfigureMcp();
        var wpfApp = builder.Build();

        return wpfApp.Services.GetRequiredService<IAiChatService>();
    }
}
