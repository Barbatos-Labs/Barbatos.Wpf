# Barbatos.Wpf.Mcp - worked patterns

Complete versions of the flows `SKILL.md` summarizes. Every snippet is adapted from
`samples/Barbatos.Wpf.Core.Sample/MainViewModel.cs` or the Core test suite unless it says
otherwise. `using` lines assumed: `Barbatos.Wpf.Hosting`, `Barbatos.Wpf.Mcp`,
`Microsoft.Extensions.AI`, `Microsoft.Extensions.Options`, `Microsoft.Agents.AI`.

Contents:

- [Settings screen: provider, model, key](#settings-screen-provider-model-key)
- [Streaming chat with an inline approval panel](#streaming-chat-with-an-inline-approval-panel)
- [Server status on screen](#server-status-on-screen)
- [OAuth connect and sign-out buttons](#oauth-connect-and-sign-out-buttons)
- [Resume a conversation](#resume-a-conversation)
- [Replacing a seam](#replacing-a-seam)
- [Testing](#testing)

## Settings screen: provider, model, key

The provider list comes from the catalog seeded in `configureProvider`; read it back from
`IOptions<AiProviderOptions>`. The key goes from a `PasswordBox` straight to
`IAiApiKeyProvider` and is cleared - it never touches a bound property that outlives the click,
a settings file, or a log line.

```csharp
public MainViewModel(IAiChatClientFactory chatClientFactory, IAiApiKeyProvider apiKeyProvider,
    IAiModelCatalog modelCatalog, IOptions<AiProviderOptions> providerOptions)
{
    _providerCatalog = [.. providerOptions.Value.Providers];
    AiProviders = _providerCatalog.Select(p => p.Key!).ToList();
}

string? EndpointFor(string key) =>
    _providerCatalog.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase))?.Endpoint;

public async void SaveAiSettings()
{
    // UpdateProvider rather than SelectProvider, so the typed model wins over the catalog's.
    _chatClientFactory.UpdateProvider(SelectedAiProvider, AiModel, EndpointFor(SelectedAiProvider));

    if (!string.IsNullOrWhiteSpace(AiApiKeyInput))
    {
        await _apiKeyProvider.SetApiKeyAsync(SelectedAiProvider, AiApiKeyInput);
        AiApiKeyInput = string.Empty;
    }

    await _chatClientFactory.RefreshApiKeyAsync();   // SetApiKeyAsync alone raises no event
    AiStatus = await _chatClientFactory.IsConfiguredAsync() ? "Ready" : "Not configured yet";
}
```

The sample can pass the catalog `Key` to `UpdateProvider`, `SetApiKeyAsync` and `GetModelsAsync`
only because every entry there has `Key == Provider`. With entries like
`Key = "claude", Provider = "anthropic"`, pass the entry's `Provider` to all three - otherwise
`UpdateProvider("claude", ...)` routes through the OpenAI SDK and the key lands under the wrong name.

"Refresh models" is a button, never an activation hook - every call is an authenticated round trip:

```csharp
public async void RefreshAiModels()
{
    try
    {
        // Same endpoint the chat uses, or the list describes a different service.
        var models = await _modelCatalog.GetModelsAsync(SelectedAiProvider, EndpointFor(SelectedAiProvider));
        AiModels.Clear();
        foreach (var id in models.Select(m => m.Id).OrderBy(id => id, StringComparer.OrdinalIgnoreCase))
            AiModels.Add(id);   // unfiltered on purpose: no reliable "is chat" flag exists
    }
    catch (Exception ex)
    {
        // No key saved yet, or a proxy with no /models route. Typing a name keeps working.
        AiStatus = $"Could not list models: {ex.Message}";
    }
}
```

The model box from `MainWindow.xaml` stays editable, and text search is off so a half-typed
name is not autocompleted into a different listed model:

```xml
<ComboBox IsEditable="True"
          IsTextSearchEnabled="False"
          ItemsSource="{Binding AiModels}"
          Text="{Binding AiModel, UpdateSourceTrigger=PropertyChanged}" />
```

`PasswordBox.Password` is not bindable; the sample forwards it from `PasswordChanged` in
code-behind and calls `AiApiKeyPasswordBox.Clear()` after saving.

## Streaming chat with an inline approval panel

A modal `MessageBox` works (see `AgentApprovalExtensions` remarks); the sample instead shows an
inline panel and awaits a `TaskCompletionSource` that the Allow/Deny buttons complete, so the
agent loop still reads as sequential code.

```csharp
AgentSession? _session;
TaskCompletionSource<(bool Approved, bool AlwaysApprove)>? _pendingApproval;

public async void SendAiChatMessage()
{
    if (_awaitingReply) return;
    _awaitingReply = true;
    try
    {
        var agent = await _agentFactory.GetAgentAsync();
        _session ??= await agent.CreateSessionAsync();

        // Per-turn instructions; the agent keeps its MCP tools.
        var runOptions = new ChatClientAgentRunOptions(new ChatOptions
        {
            Instructions = $"Current date/time: {DateTimeOffset.Now:yyyy-MM-dd HH:mm}. " +
                           $"Use '{AppContext.BaseDirectory}' for any tool argument asking for a folder.",
        });

        var response = await RunStreamingAsync(agent, new ChatMessage(ChatRole.User, input), runOptions);

        while (response.GetToolApprovalRequests() is { Count: > 0 } pending)
        {
            var (approved, alwaysApprove) = await RequestApprovalAsync(pending);
            response = await RunStreamingAsync(agent, pending.CreateApprovalResponse(approved, alwaysApprove), runOptions);
        }
    }
    catch (Exception ex)
    {
        // Expected until the user saves a provider and key.
        Transcript.Add($"(error: {ex.Message})");
    }
    finally
    {
        CancelPendingApproval();     // never leave a stale prompt on screen
        _awaitingReply = false;
    }
}

async Task<AgentResponse> RunStreamingAsync(AIAgent agent, ChatMessage message, ChatClientAgentRunOptions options)
{
    var updates = new List<AgentResponseUpdate>();
    // Started on the UI thread, so each await resumes there and the transcript can be updated directly.
    await foreach (var update in agent.RunStreamingAsync(message, _session, options))
    {
        updates.Add(update);
        AppendToReply(update.Text);
    }
    return updates.ToAgentResponse();   // what makes the approval check possible after streaming
}

Task<(bool, bool)> RequestApprovalAsync(IReadOnlyList<ToolApprovalRequestContent> pending)
{
    var call = pending[0].ToolCall as FunctionCallContent;
    var args = call?.Arguments is { Count: > 0 } values
        ? string.Join(", ", values.Select(a => $"{a.Key}: {a.Value}"))
        : "(no arguments)";
    PendingApprovalDescription = $"{call?.Name ?? pending[0].ToolCall.CallId}({args})";
    AlwaysApproveTool = false;   // the "Don't ask again for this tool" checkbox
    _pendingApproval = new(TaskCreationOptions.RunContinuationsAsynchronously);
    OnPropertyChanged(nameof(HasPendingApproval));
    return _pendingApproval.Task;
}

public void ResolvePendingApproval(bool approved)   // Allow / Deny buttons
{
    var pending = _pendingApproval;
    if (pending is null) return;
    _pendingApproval = null;
    OnPropertyChanged(nameof(HasPendingApproval));
    pending.TrySetResult((approved, AlwaysApproveTool));
}

void CancelPendingApproval()
{
    var pending = _pendingApproval;
    if (pending is null) return;
    _pendingApproval = null;
    OnPropertyChanged(nameof(HasPendingApproval));
    pending.TrySetResult((false, false));
}
```

`HasPendingApproval => _pendingApproval is not null` drives the panel's `Visibility`. The
`_awaitingReply` guard also blocks Send while the panel is up, because the run it belongs to is
still in flight. `RunContinuationsAsynchronously` keeps the button click from running the rest
of the agent loop inline inside the click handler.

The sample drops `_session` on `IAiAgentFactory.ConfigurationChanged`. That event is forwarded
from `IMcpServerRegistry.Changed` and can arrive on a thread-pool thread, so a handler that also
writes to a bound collection must dispatch first (next section).

## Server status on screen

```csharp
// Captured in the ViewModel constructor, which runs on the UI thread. (The sample calls
// OnPropertyChanged directly; dispatch as here once the handler touches a collection.)
readonly IDispatcher _dispatcher = Barbatos.Wpf.Dispatching.Dispatcher.GetForCurrentThread()!;

_registry.Changed += (_, _) => _dispatcher.Dispatch(() => OnPropertyChanged(nameof(ServersDescription)));

public string ServersDescription => string.Join(" | ", _registry.Servers.Select(s =>
    s.IsConnected ? $"{s.Name}: {string.Join(", ", s.Tools.Select(t => t.Name))}"
    : s.LastError is null ? $"{s.Name}: connecting..."
    : $"{s.Name}: {s.LastError}"));
```

Listing tool names, not just a count, is what tells a user whether a question can reach a tool
at all. Seed servers are already in `Servers` (connecting) by the time `Build()` returns. A retry
button for a failed or dropped server is `RemoveServerAsync(name)` then
`AddServerAsync(descriptor)` - keep the descriptor, since the status does not carry it.

## OAuth connect and sign-out buttons

```csharp
public async void ConnectMcpOAuthServer()
{
    // Named after the host: unique per server, and the stored tokens (keyed by name) follow it.
    var name = Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ? uri.Host : endpoint;

    McpOAuthBusy = true;             // disables the button: a second click = second tab + second port
    McpOAuthStatus = $"Connecting to {name} - finish signing in in your browser...";
    try
    {
        await _registry.RemoveServerAsync(name);   // idempotent re-click instead of ArgumentException
        var status = await _registry.AddServerAsync(new McpServerDescriptor
        {
            Name = name,
            TransportKind = McpTransportKind.Http,
            Endpoint = endpoint,
            OAuth = new McpOAuthOptions(),
        });
        McpOAuthStatus = $"{status.Name}: connected - {status.Tools.Count} tool(s).";
    }
    catch (McpAuthorizationException ex)
    {
        McpOAuthStatus = $"Not signed in: {ex.Message}";   // denied, closed tab, timeout - not a bug
    }
    catch (Exception ex)
    {
        McpOAuthStatus = $"Could not connect: {ex.Message}";
    }
    finally
    {
        McpOAuthBusy = false;
    }
}

public async void SignOutMcpOAuthServer()
{
    var hadTokens = await _registry.SignOutAsync(name);   // forgets tokens only
    await _registry.RemoveServerAsync(name);              // drops the live connection too
}
```

The first connect opens the browser; a later connect for the same `Name` - after a restart too -
reuses the stored tokens without it. The registry does not remember the server itself: re-add it
at startup from your own saved list.

## Resume a conversation

```csharp
var agent = await agentFactory.GetAgentAsync();
var ids = await sessionStore.ListAsync();                       // unordered - sort or keep your own index
var session = await sessionStore.LoadAsync(id, agent) ?? await agent.CreateSessionAsync();
// ... after each completed turn:
await sessionStore.SaveAsync(id, agent, session);
await sessionStore.DeleteAsync(id);                             // false if nothing was saved
```

New ids: `Guid.NewGuid().ToString("N")`; keep the human-readable title in your own index, since
`.`, `:` and separators are rejected. Load and save with the agent from `IAiAgentFactory` both
times.

## Replacing a seam

Register before `ConfigureMcp`; `TryAddSingleton` then leaves yours alone.

Keep the default browser handler but change its timeout or completion page
(`BrowserMcpAuthorizationHandler` is public for exactly this; `ILauncher` is in
`Barbatos.Wpf.ApplicationModel`):

```csharp
builder.Services.AddSingleton<IMcpAuthorizationHandler>(sp =>
    new BrowserMcpAuthorizationHandler(
        sp.GetRequiredService<ILauncher>(),
        sp.GetService<ILogger<BrowserMcpAuthorizationHandler>>())
    {
        Timeout = TimeSpan.FromMinutes(10),
    });
builder.ConfigureMcp();
```

A custom handler (an embedded WebView2 window, say) implements two members:

```csharp
public sealed class WebViewAuthorizationHandler : IMcpAuthorizationHandler
{
    public Uri GetRedirectUri(McpServerDescriptor descriptor) =>
        descriptor.OAuth?.RedirectUri ?? new Uri("http://127.0.0.1:<port>/");   // honour a configured value

    public async Task<McpAuthorizationResult> AuthorizeAsync(McpAuthorizationContext context, CancellationToken cancellationToken = default)
    {
        // Navigate to context.AuthorizationUri; wait for a navigation to context.RedirectUri.
        // Throw McpAuthorizationException { Error = ..., ErrorDescription = ... } on error/denial.
        // Pass code, state and iss through unchanged.
        return new McpAuthorizationResult(code, state, issuer);
    }
}
```

`IAiApiKeyProvider` (`GetApiKeyAsync`, `SetApiKeyAsync`), `IMcpTokenStore` (`GetAsync`,
`SetAsync`, `RemoveAsync` - return `null` from `GetAsync` for an unreadable payload rather than
throwing) and `IAiSessionStore` (`SaveAsync`, `LoadAsync`, `ListAsync`, `DeleteAsync`) are
replaced the same way. Treat key lookups case-insensitively, as the default does.

## Testing

From `AgentFeatureTests.CreateFactoryWithParts`: register fakes first, then `ConfigureMcp`, then
resolve the real internal implementation through DI.

```csharp
var chatClient = new ScriptedChatClient();
var chatClientFactory = new FakeAiChatClientFactory { Client = chatClient };
var registry = new FakeMcpServerRegistry();

var builder = WpfApp.CreateBuilder();
builder.Services.AddSingleton<IAiChatClientFactory>(chatClientFactory);
builder.Services.AddSingleton<IMcpServerRegistry>(registry);
builder.ConfigureMcp(configureAgent: o => o.ToolApproval = AiToolApprovalMode.Always);
var agentFactory = builder.Build().Services.GetRequiredService<IAiAgentFactory>();

registry.ToolsList.Add(AIFunctionFactory.Create(() => "done", "dangerous_tool", "Does something irreversible."));
chatClient.EnqueueToolCall("call-1", "dangerous_tool");
chatClient.EnqueueText("All finished.");
```

Then `RunAsync`, assert one pending request and that the tool has not run, answer with
`CreateApprovalResponse(approved: true)`, assert it ran (`AnApprovalGatedToolIsNotRunUntilTheUserApprovesIt`).
`registry.RaiseChanged()` and `chatClientFactory.RaiseConfigurationChanged()` drive cache
invalidation; `chatClient.LastOptions` shows exactly which tools and instructions the model got.

- Real key provider without DPAPI: register `FakeSecureStorage` as `ISecureStorage` before
  `ConfigureMcp` (`DefaultApiKeyProviderStoresKeysCaseInsensitivelyByProvider`).
- Real session store in a temp folder: register `new FakeFileSystem(path)` as `IFileSystem`
  (`AgentFeatureTests.CreateSessionStore`).
- Model listing without a real provider: the loopback `FakeOpenAiModelsEndpoint` in
  `McpFeatureTests` stands in for any OpenAI-compatible `/models` route.
- The OAuth handler is driven end to end against a real `HttpListener` with a fake launcher acting
  as the browser (`McpOAuthTests`).

Thread, dispatcher and per-TFM rules are in `barbatos-wpf-testing`.
