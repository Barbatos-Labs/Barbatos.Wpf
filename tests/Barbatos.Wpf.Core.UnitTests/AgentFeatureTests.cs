// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel;
using System.IO;
using Barbatos.Wpf.Mcp;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.Core.UnitTests;

public class AgentFeatureTests
{
    // ---- registration -------------------------------------------------------------------

    [Fact]
    public void AgentServicesAreNotRegisteredByDefault()
    {
        var wpfApp = WpfApp.CreateBuilder().Build();

        Assert.Null(wpfApp.Services.GetService<IAiAgentFactory>());
        Assert.Null(wpfApp.Services.GetService<IAiSessionStore>());
    }

    [Fact]
    public void AgentServicesAreRegisteredWhenMcpIsConfigured()
    {
        var builder = WpfApp.CreateBuilder();
        builder.ConfigureMcp();
        var wpfApp = builder.Build();

        Assert.NotNull(wpfApp.Services.GetService<IAiAgentFactory>());
        Assert.NotNull(wpfApp.Services.GetService<IAiSessionStore>());
    }

    [Fact]
    public void ConfiguringAgentsLeavesTheOlderChatServiceRegistered()
    {
        // The agent layer is additive - an app already built on IAiChatService keeps working
        // untouched, which is the whole premise of adding rather than replacing it.
        var builder = WpfApp.CreateBuilder();
        builder.ConfigureMcp(configureAgent: options => options.Name = "Helper");
        var wpfApp = builder.Build();

        Assert.NotNull(wpfApp.Services.GetService<IAiChatService>());
        Assert.NotNull(wpfApp.Services.GetService<IAiAgentFactory>());
    }

    [Fact]
    public void CustomSessionStoreRegisteredBeforeConfigureMcpTakesPrecedence()
    {
        var builder = WpfApp.CreateBuilder();
        var custom = new FakeAiSessionStore();
        builder.Services.AddSingleton<IAiSessionStore>(custom);
        builder.ConfigureMcp();
        var wpfApp = builder.Build();

        Assert.Same(custom, wpfApp.Services.GetRequiredService<IAiSessionStore>());
    }

    [Fact]
    public void ConfigurationOverridesCodeForAgentOptions()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Barbatos:Mcp:Agent:Instructions"] = "from-config",
            ["Barbatos:Mcp:Agent:ToolApproval"] = "Never",
        });
        builder.ConfigureMcp(configureAgent: options => options.Instructions = "from-code");
        var wpfApp = builder.Build();

        var options = wpfApp.Services.GetRequiredService<IOptions<AiAgentOptions>>().Value;
        Assert.Equal("from-config", options.Instructions);
        Assert.Equal(AiToolApprovalMode.Never, options.ToolApproval);
    }

    [Fact]
    public void ToolApprovalDefaultsToAlwaysSoThirdPartyMcpToolsCannotRunUnasked()
    {
        Assert.Equal(AiToolApprovalMode.Always, new AiAgentOptions().ToolApproval);
        Assert.Equal(AiToolApprovalMode.Always, new AiAgentDescriptor().ToolApproval);
    }

    // ---- descriptor validation ----------------------------------------------------------

    [Fact]
    public void DescriptorValidateThrowsWhenNameIsUnset()
    {
        var descriptor = new AiAgentDescriptor { Instructions = "do things" };

        var exception = Assert.Throws<InvalidOperationException>(descriptor.Validate);
        Assert.Contains(nameof(AiAgentDescriptor.Name), exception.Message);
    }

    [Fact]
    public void DescriptorValidateDoesNotThrowForAWellFormedDescriptor() =>
        Assert.Null(Record.Exception(new AiAgentDescriptor { Name = "Reviewer" }.Validate));

    [Fact]
    public async Task CreateAgentValidatesTheDescriptorBeforeBuildingAnything()
    {
        var (factory, _, _) = CreateFactory();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => factory.CreateAgentAsync(new AiAgentDescriptor()));
    }

    // ---- tool composition ---------------------------------------------------------------

    [Fact]
    public async Task AgentIsGivenEveryConnectedMcpServersTools()
    {
        var (factory, chatClient, registry) = CreateFactory();
        registry.ToolsList.Add(CreateTool("lookup_package"));

        var agent = await factory.GetAgentAsync();
        await agent.RunAsync("hello");

        Assert.Contains(ToolNames(chatClient), name => name == "lookup_package");
    }

    [Fact]
    public async Task McpToolsAreLeftOutWhenIncludeMcpToolsIsOff()
    {
        var (factory, chatClient, registry) = CreateFactory(options => options.IncludeMcpTools = false);
        registry.ToolsList.Add(CreateTool("lookup_package"));

        var agent = await factory.GetAgentAsync();
        await agent.RunAsync("hello");

        Assert.DoesNotContain(ToolNames(chatClient), name => name == "lookup_package");
    }

    [Fact]
    public async Task ToolsAreWrappedForApprovalUnderTheAlwaysMode()
    {
        var (factory, chatClient, registry) = CreateFactory(options => options.ToolApproval = AiToolApprovalMode.Always);
        registry.ToolsList.Add(CreateTool("delete_everything"));

        var agent = await factory.GetAgentAsync();
        await agent.RunAsync("hello");

        Assert.Contains(chatClient.LastOptions?.Tools ?? [], tool => tool is ApprovalRequiredAIFunction);
    }

    [Fact]
    public async Task ToolsAreNotWrappedForApprovalUnderTheNeverMode()
    {
        var (factory, chatClient, registry) = CreateFactory(options => options.ToolApproval = AiToolApprovalMode.Never);
        registry.ToolsList.Add(CreateTool("read_something"));

        var agent = await factory.GetAgentAsync();
        await agent.RunAsync("hello");

        Assert.DoesNotContain(chatClient.LastOptions?.Tools ?? [], tool => tool is ApprovalRequiredAIFunction);
    }

    [Fact]
    public async Task CreateAgentAddsTheDescriptorsOwnToolsAlongsideTheMcpOnes()
    {
        var (factory, chatClient, registry) = CreateFactory();
        registry.ToolsList.Add(CreateTool("from_mcp"));

        var descriptor = new AiAgentDescriptor { Name = "Reviewer", ToolApproval = AiToolApprovalMode.Never };
        descriptor.Tools.Add(CreateTool("from_app"));

        var agent = await factory.CreateAgentAsync(descriptor);
        await agent.RunAsync("hello");

        var tools = ToolNames(chatClient);
        Assert.Contains(tools, name => name == "from_mcp");
        Assert.Contains(tools, name => name == "from_app");
    }

    [Fact]
    public async Task CreateAgentIsNotCachedSoEachCallOwnsItsOwnAgent()
    {
        var (factory, _, _) = CreateFactory();
        var descriptor = new AiAgentDescriptor { Name = "Reviewer" };

        Assert.NotSame(await factory.CreateAgentAsync(descriptor), await factory.CreateAgentAsync(descriptor));
    }

    [Fact]
    public async Task PerRunChatOptionsDoNotDropTheAgentsTools()
    {
        // The sample passes ChatClientAgentRunOptions on every turn (its instructions carry the
        // current date/time), and those run options set no Tools of their own. If run-level
        // options replaced rather than unioned the agent's tools, the model would silently be
        // offered nothing to call - which looks exactly like "the AI just answered from memory".
        var (factory, chatClient, registry) = CreateFactory();
        registry.ToolsList.Add(CreateTool("lookup_package"));

        var agent = await factory.GetAgentAsync();
        await agent.RunAsync(
            new ChatMessage(ChatRole.User, "hello"),
            await agent.CreateSessionAsync(),
            new ChatClientAgentRunOptions(new ChatOptions { Instructions = "per-run instructions" }));

        Assert.Contains(ToolNames(chatClient), name => name == "lookup_package");
        Assert.Equal("per-run instructions", chatClient.LastOptions?.Instructions);
    }

    [Fact]
    public async Task ApprovalWrappingKeepsTheToolDescribableToTheModel()
    {
        // A wrapper that lost the description or the parameter schema would leave the model
        // unable to call the tool at all - and the symptom would be indistinguishable from the
        // model simply choosing not to, so it is worth pinning down here rather than guessing
        // at it from a chat transcript.
        var (factory, chatClient, registry) = CreateFactory(options => options.ToolApproval = AiToolApprovalMode.Always);
        registry.ToolsList.Add(CreateTool("lookup_package"));

        var agent = await factory.GetAgentAsync();
        await agent.RunAsync("hello");

        var tool = Assert.IsAssignableFrom<AIFunction>(
            Assert.Single(chatClient.LastOptions?.Tools ?? [], t => t is ApprovalRequiredAIFunction));

        Assert.Equal("lookup_package", tool.Name);
        Assert.Equal("The lookup_package tool.", tool.Description);
        Assert.Contains("input", tool.JsonSchema.ToString());
    }

    // ---- caching / invalidation ---------------------------------------------------------

    [Fact]
    public async Task TheDefaultAgentIsCachedBetweenCalls()
    {
        var (factory, _, _) = CreateFactory();

        Assert.Same(await factory.GetAgentAsync(), await factory.GetAgentAsync());
    }

    [Fact]
    public async Task TheCachedAgentIsDroppedWhenTheProviderConfigurationChanges()
    {
        var (factory, _, _, chatClientFactory) = CreateFactoryWithParts();

        var first = await factory.GetAgentAsync();
        chatClientFactory.RaiseConfigurationChanged();

        Assert.NotSame(first, await factory.GetAgentAsync());
    }

    [Fact]
    public async Task TheCachedAgentIsDroppedWhenTheConnectedServersChange()
    {
        // An agent is handed its tools once, at construction - unlike IAiChatService, which
        // re-reads IMcpServerRegistry.Tools on every call. Without this the agent would keep
        // offering the tools of a server that has since gone away.
        var (factory, chatClient, registry) = CreateFactory();

        var first = await factory.GetAgentAsync();
        registry.ToolsList.Add(CreateTool("appeared_later"));
        registry.RaiseChanged();

        var second = await factory.GetAgentAsync();
        Assert.NotSame(first, second);

        await second.RunAsync("hello");
        Assert.Contains(ToolNames(chatClient), name => name == "appeared_later");
    }

    [Fact]
    public async Task TheCachedAgentIsDroppedWhenTheChatClientIsSwappedWithoutAnEvent()
    {
        // IAiChatClientFactory rebuilds and disposes its client whenever the key it was built
        // from changes, and an app can store a new API key without calling RefreshApiKeyAsync -
        // no ConfigurationChanged fires in that case, so the reference check is the only thing
        // stopping a cached agent from holding a disposed chat client.
        var (factory, _, _, chatClientFactory) = CreateFactoryWithParts();

        var first = await factory.GetAgentAsync();
        chatClientFactory.Client = new ScriptedChatClient();

        Assert.NotSame(first, await factory.GetAgentAsync());
    }

    [Fact]
    public async Task ConfigurationChangedIsRaisedForBothProviderAndServerChanges()
    {
        var (factory, _, registry, chatClientFactory) = CreateFactoryWithParts();
        var raised = 0;
        factory.ConfigurationChanged += (_, _) => raised++;

        chatClientFactory.RaiseConfigurationChanged();
        registry.RaiseChanged();

        Assert.Equal(2, raised);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task IsConfiguredForwardsTheChatClientFactorysAnswer()
    {
        var (factory, _, _, chatClientFactory) = CreateFactoryWithParts();

        chatClientFactory.Configured = false;
        Assert.False(await factory.IsConfiguredAsync());

        chatClientFactory.Configured = true;
        Assert.True(await factory.IsConfiguredAsync());
    }

    // ---- end-to-end approval round trip -------------------------------------------------

    [Fact]
    public async Task AnApprovalGatedToolIsNotRunUntilTheUserApprovesIt()
    {
        var invoked = 0;
        var (factory, chatClient, registry) = CreateFactory(options => options.ToolApproval = AiToolApprovalMode.Always);
        registry.ToolsList.Add(AIFunctionFactory.Create(
            () => { invoked++; return "done"; },
            "dangerous_tool",
            "Does something irreversible."));

        chatClient.EnqueueToolCall("call-1", "dangerous_tool");
        chatClient.EnqueueText("All finished.");

        var agent = await factory.GetAgentAsync();
        var session = await agent.CreateSessionAsync();

        var response = await agent.RunAsync("please do the thing", session);

        // Paused: the model asked for the tool, but nothing has run yet.
        var pending = response.GetToolApprovalRequests();
        Assert.Single(pending);
        Assert.Equal(0, invoked);

        // Approve, and only now does it actually execute.
        var final = await agent.RunAsync(pending.CreateApprovalResponse(approved: true), session);

        Assert.Equal(1, invoked);
        Assert.Empty(final.GetToolApprovalRequests());
        Assert.Contains("All finished.", final.Text);
    }

    [Fact]
    public async Task DenyingAnApprovalLeavesTheToolUnrun()
    {
        var invoked = 0;
        var (factory, chatClient, registry) = CreateFactory(options => options.ToolApproval = AiToolApprovalMode.Always);
        registry.ToolsList.Add(AIFunctionFactory.Create(
            () => { invoked++; return "done"; },
            "dangerous_tool",
            "Does something irreversible."));

        chatClient.EnqueueToolCall("call-1", "dangerous_tool");
        chatClient.EnqueueText("Understood, I won't do that.");

        var agent = await factory.GetAgentAsync();
        var session = await agent.CreateSessionAsync();

        var response = await agent.RunAsync("please do the thing", session);
        var pending = response.GetToolApprovalRequests();
        Assert.Single(pending);

        await agent.RunAsync(pending.CreateApprovalResponse(approved: false, reason: "user declined"), session);

        Assert.Equal(0, invoked);
    }

    [Fact]
    public async Task AToolRunsWithoutPromptingUnderTheNeverMode()
    {
        var invoked = 0;
        var (factory, chatClient, registry) = CreateFactory(options => options.ToolApproval = AiToolApprovalMode.Never);
        registry.ToolsList.Add(AIFunctionFactory.Create(
            () => { invoked++; return "done"; },
            "harmless_tool",
            "Reads something."));

        chatClient.EnqueueToolCall("call-1", "harmless_tool");
        chatClient.EnqueueText("All finished.");

        var agent = await factory.GetAgentAsync();
        var response = await agent.RunAsync("please do the thing", await agent.CreateSessionAsync());

        Assert.Equal(1, invoked);
        Assert.Empty(response.GetToolApprovalRequests());
    }

    // ---- approval helpers ---------------------------------------------------------------

    [Fact]
    public void GetToolApprovalRequestsIsEmptyForAnOrdinaryAnswer() =>
        Assert.Empty(new AgentResponse(new ChatMessage(ChatRole.Assistant, "just an answer")).GetToolApprovalRequests());

    [Fact]
    public void CreateApprovalResponseMarksEveryRequestApproved()
    {
        var requests = new[] { CreateApprovalRequest("call-1"), CreateApprovalRequest("call-2") };

        var message = requests.CreateApprovalResponse(approved: true);

        Assert.Equal(ChatRole.User, message.Role);
        var responses = message.Contents.OfType<ToolApprovalResponseContent>().ToList();
        Assert.Equal(2, responses.Count);
        Assert.All(responses, response => Assert.True(response.Approved));
    }

    [Fact]
    public void CreateApprovalResponseWrapsInAlwaysApproveOnlyWhenApproving()
    {
        var requests = new[] { CreateApprovalRequest("call-1") };

        var approved = requests.CreateApprovalResponse(approved: true, alwaysApprove: true);
        Assert.Single(approved.Contents.OfType<AlwaysApproveToolApprovalResponseContent>());

        // There is no such thing as a standing denial - a rule only ever widens what runs
        // without asking - so alwaysApprove is ignored when denying.
        var denied = requests.CreateApprovalResponse(approved: false, alwaysApprove: true);
        Assert.Empty(denied.Contents.OfType<AlwaysApproveToolApprovalResponseContent>());
    }

    [Fact]
    public void CreateApprovalResponseRejectsAnEmptyRequestList() =>
        Assert.Throws<ArgumentException>(() => Array.Empty<ToolApprovalRequestContent>().CreateApprovalResponse(approved: true));

    // ---- session store ------------------------------------------------------------------

    [Fact]
    public async Task SessionSurvivesASaveAndLoadRoundTrip()
    {
        using var temp = new TempDirectory();
        var store = CreateSessionStore(temp);
        var (factory, chatClient, _) = CreateFactory();
        chatClient.EnqueueText("Nice to meet you, Hung.");

        var agent = await factory.GetAgentAsync();
        var session = await agent.CreateSessionAsync();
        await agent.RunAsync("My name is Hung.", session);

        await store.SaveAsync("chat-1", agent, session);
        var restored = await store.LoadAsync("chat-1", agent);

        Assert.NotNull(restored);
        Assert.Contains("chat-1", await store.ListAsync());
    }

    [Fact]
    public async Task LoadReturnsNullForAnUnknownSessionSoResumeCanFallBack()
    {
        using var temp = new TempDirectory();
        var store = CreateSessionStore(temp);
        var (factory, _, _) = CreateFactory();

        Assert.Null(await store.LoadAsync("never-saved", await factory.GetAgentAsync()));
    }

    [Fact]
    public async Task DeleteRemovesASavedSessionAndReportsWhetherThereWasOne()
    {
        using var temp = new TempDirectory();
        var store = CreateSessionStore(temp);
        var (factory, _, _) = CreateFactory();
        var agent = await factory.GetAgentAsync();

        await store.SaveAsync("chat-1", agent, await agent.CreateSessionAsync());

        Assert.True(await store.DeleteAsync("chat-1"));
        Assert.False(await store.DeleteAsync("chat-1"));
        Assert.Empty(await store.ListAsync());
    }

    [Fact]
    public async Task SavingOverAnExistingSessionIdReplacesIt()
    {
        using var temp = new TempDirectory();
        var store = CreateSessionStore(temp);
        var (factory, _, _) = CreateFactory();
        var agent = await factory.GetAgentAsync();

        await store.SaveAsync("chat-1", agent, await agent.CreateSessionAsync());
        await store.SaveAsync("chat-1", agent, await agent.CreateSessionAsync());

        Assert.Single(await store.ListAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sub/chat")]
    [InlineData("..")]
    [InlineData("chat:1")]
    public async Task SessionIdsThatCannotBecomeAFileNameAreRejectedRatherThanSanitized(string sessionId)
    {
        using var temp = new TempDirectory();
        var store = CreateSessionStore(temp);

        await Assert.ThrowsAsync<ArgumentException>(() => store.DeleteAsync(sessionId));
    }

    [Fact]
    public async Task ListIsEmptyBeforeAnythingHasBeenSaved()
    {
        using var temp = new TempDirectory();
        var store = CreateSessionStore(temp);

        Assert.Empty(await store.ListAsync());
    }

    // ---- helpers ------------------------------------------------------------------------

    static (IAiAgentFactory Factory, ScriptedChatClient ChatClient, FakeMcpServerRegistry Registry) CreateFactory(
        Action<AiAgentOptions>? configure = null)
    {
        var (factory, chatClient, registry, _) = CreateFactoryWithParts(configure);
        return (factory, chatClient, registry);
    }

    /// <summary>
    /// Builds the real (internal) <c>AiAgentFactory</c> over fakes the same way
    /// <c>McpFeatureTests.BuildChatService</c> builds the real <c>AiChatService</c>: registering
    /// the fakes before <c>ConfigureMcp</c> so its <c>TryAddSingleton</c> calls leave them alone,
    /// then resolving the implementation through DI. There is no <c>InternalsVisibleTo</c> seam
    /// in this repo, and the implementation has no business being public just to be tested.
    /// </summary>
    static (IAiAgentFactory Factory, ScriptedChatClient ChatClient, FakeMcpServerRegistry Registry, FakeAiChatClientFactory ChatClientFactory) CreateFactoryWithParts(
        Action<AiAgentOptions>? configure = null)
    {
        var chatClient = new ScriptedChatClient();
        var chatClientFactory = new FakeAiChatClientFactory { Client = chatClient };
        var registry = new FakeMcpServerRegistry();

        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IAiChatClientFactory>(chatClientFactory);
        builder.Services.AddSingleton<IMcpServerRegistry>(registry);
        builder.ConfigureMcp(configureAgent: configure);
        var wpfApp = builder.Build();

        return (wpfApp.Services.GetRequiredService<IAiAgentFactory>(), chatClient, registry, chatClientFactory);
    }

    /// <summary>
    /// Resolves the real (internal) <c>FileAiSessionStore</c> pointed at a throwaway directory,
    /// via the same register-the-fake-first route as <see cref="CreateFactoryWithParts"/>.
    /// </summary>
    static IAiSessionStore CreateSessionStore(TempDirectory temp)
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<Storage.IFileSystem>(new FakeFileSystem(temp.Path));
        builder.ConfigureMcp();

        return builder.Build().Services.GetRequiredService<IAiSessionStore>();
    }

    static AIFunction CreateTool(string name) =>
        AIFunctionFactory.Create(([Description("An argument.")] string input) => input, name, $"The {name} tool.");

    static ToolApprovalRequestContent CreateApprovalRequest(string callId) =>
        new(callId, new FunctionCallContent(callId, "some_tool", new Dictionary<string, object?>()));

    static IReadOnlyList<string> ToolNames(ScriptedChatClient chatClient) =>
        [.. (chatClient.LastOptions?.Tools ?? []).Select(tool => tool is AIFunction function ? function.Name : tool.GetType().Name)];

    sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "barbatos-agent-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
                // A leftover temp directory is not worth failing a test over.
            }
        }
    }
}
