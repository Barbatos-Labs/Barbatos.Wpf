---
name: barbatos-mcp-ai
description: Work with Barbatos.Wpf.Mcp - the Model Context Protocol client and bring-your-own-key AI layer in Barbatos.Wpf.Core. Covers ConfigureMcp, IMcpServerRegistry (stdio and HTTP servers, OAuth sign-in, token storage), IAiApiKeyProvider, IAiChatClientFactory and provider/model selection, IAiModelCatalog, one-shot IAiChatService, IAiAgentFactory with Microsoft Agent Framework agents and tool approval, IAiSessionStore, and image/document attachments. Use this for anything AI-shaped in a Barbatos app - a chat or assistant panel, an agent that remembers the conversation, a provider or model picker in settings, tool calling, connecting an MCP server, signing in to a hosted server, and storing the end user's own API key. Covers "let users bring their own OpenAI/Gemini/Claude key", "ask me before the assistant runs a tool", and "keep the conversation after restart". Vietnamese prompts land here too: chat AI, trợ lý ảo, chọn mô hình, người dùng tự nhập khóa API, gọi công cụ, hỏi trước khi chạy tool, kết nối MCP server, lưu hội thoại.
---

# Barbatos.Wpf.Mcp — MCP client + BYOK AI

Full docs: [`README.md#ai-chat--mcp-barbatoswpfmcp`](../../src/Barbatos.Wpf.Core/README.md) ·
[`API-REFERENCE.md#barbatoswpfmcp-namespace`](../../src/Barbatos.Wpf.Core/API-REFERENCE.md).
Reference: [`samples/Barbatos.Wpf.Core.Sample/WpfProgram.cs`](../../samples/Barbatos.Wpf.Core.Sample/WpfProgram.cs)
and its "AI chat + MCP" tab.

---

## BYOK is non-negotiable

**The application publisher never pays for LLM usage.** The end user supplies their own
Gemini / Claude / ChatGPT / other key at runtime, typically through a settings screen you build,
and `IAiApiKeyProvider` stores it DPAPI-encrypted via `ISecureStorage`.

Never bake a key into code, a config file, or an options default. Never add an API that would
route usage through a publisher-owned account. `AiProviderOptions` deliberately carries **no key
or secret field** — do not add one.

---

## Registration

Everything is one call, and all three parameters are optional and additive:

```csharp
builder.ConfigureMcp(
    options => options.Servers.Add(new McpServerDescriptor
    {
        Name = "NuGet",
        TransportKind = McpTransportKind.Stdio,
        Command = "dotnet",
        Arguments = { "dnx", "NuGet.Mcp.Server", "--version", "1.4.16", "--yes" },
        EnvironmentVariables = { ["NUGET_SOURCE"] = "https://api.nuget.org/v3/index.json" },
    }),
    configureProvider: options =>
    {
        options.Providers.Add(new AiProviderDescriptor { Key = "openai", Provider = "openai", Model = "gpt-5.2" });
        options.Providers.Add(new AiProviderDescriptor { Key = "gemini", Provider = "gemini", Model = "gemini-3.5-flash", Endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/" });
        options.Providers.Add(new AiProviderDescriptor { Key = "anthropic", Provider = "anthropic", Model = "claude-opus-4-8" });

        options.Provider = "gemini";     // active on startup
        options.Model = "gemini-3.5-flash";
    },
    configureAgent: options =>
    {
        options.Name = "Assistant";
        options.Instructions = "Be concise.";
        // ToolApproval stays AiToolApprovalMode.Always by default - see below.
    });
```

`Servers` and `Providers` are seeded from **code**, not configuration — array-index config keys
(`Providers:0:Key`) are unwieldy. `Barbatos:Mcp` still covers `Enabled`, `Provider`, `Model`,
`Endpoint`, and the `Agent` block.

`Providers` is *this app's own suggested catalog* — the library has no fixed provider list.
`AiProviderOptions.Provider` is a free string: only `"anthropic"` is special-cased (official
Anthropic SDK); every other value is assumed OpenAI-wire-compatible, so any OpenAI-compatible
endpoint works by setting `Endpoint`. Adding a provider must never require a library release.

Replaceable seams — register **before** `ConfigureMcp` (everything uses `TryAddSingleton`):

| Interface | Default | Replace when |
| --- | --- | --- |
| `IAiApiKeyProvider` | `SecureStorageAiApiKeyProvider` (DPAPI) | keys belong in Credential Manager / a vault |
| `IMcpTokenStore` | `SecureStorageMcpTokenStore` (DPAPI) | same, for OAuth tokens |
| `IMcpAuthorizationHandler` | `BrowserMcpAuthorizationHandler` (system browser + loopback) | consent should happen in an embedded WebView2 |
| `IAiSessionStore` | plain JSON files under `IFileSystem.AppDataDirectory` | conversations need encryption |

---

## MCP servers

```csharp
await registry.AddServerAsync(descriptor);
await registry.RemoveServerAsync("NuGet");
await registry.SignOutAsync("Linear");   // forget OAuth tokens, keep the server entry
```

- `Stdio` = a local child process; `Http` = Streamable HTTP with SSE fallback
  (`McpHttpTransportMode.AutoDetect` by default).
- `Validate()` checks internal consistency for the current `TransportKind` **without connecting** —
  call it from a settings UI before saving. `AddServerAsync` throws `InvalidOperationException`
  for an invalid descriptor and `ArgumentException` for a duplicate `Name`.
- `EnvironmentVariables` applies on top of what `InheritEnvironmentVariables` (default `true`)
  lets through; a **`null` value removes** an inherited variable, so one secret can be withheld
  without giving up inheritance. Set `InheritEnvironmentVariables = false` to start a third-party
  server binary with an empty environment.
- A failed connection is **tracked, not discarded** — it stays in `Servers` with
  `IsConnected = false` and `LastError` set, and `AddServerAsync` also rethrows.
  `IsConnected` flips back to `false` on its own when a session dies, raising `Changed` from a
  background thread.
- A stdio server's `LastError` includes the child's exit code and stderr tail — usually the real
  explanation. Every stderr line is logged at `Debug`.
- **Tool name collision:** the most recently connected server wins, logged as a warning.

### OAuth for hosted servers

```csharp
await registry.AddServerAsync(new McpServerDescriptor
{
    Name = "Linear",
    TransportKind = McpTransportKind.Http,
    Endpoint = "https://mcp.example.com/mcp",
    OAuth = new McpOAuthOptions(),   // discovered: auth server, scopes, client registration
});
```

An **empty `McpOAuthOptions()` is the intended common case.** The flow gets the `401` challenge,
finds the authorization server, dynamically registers this app as a client, opens the **user's
real browser** for consent, catches the redirect on a loopback port, exchanges the code, and
persists the tokens — OAuth 2.1 / RFC 8252, so the app never sees the user's password.

Fill in a property only when a server forces it: `ClientId` (no dynamic registration), `Scopes`
(server publishes none — it is a *fallback*, checked after `WWW-Authenticate` and protected
resource metadata), `RedirectUri` (a fixed registered `http://` loopback URL). A `ClientSecret`
without a `ClientId` is rejected; a desktop binary cannot keep a secret, which is why the flow
uses PKCE. `OAuth` is HTTP-only — `Validate()` rejects it on `Stdio`.

`McpAuthorizationException` (denied consent, closed tab, timeout) is an **ordinary outcome to
report**, not a crash; it also lands in `McpServerStatus.LastError`. The token-store payload is
an **opaque SDK-owned string** — treat it as a secret, never parse it. An unreadable payload is
discarded and the user simply re-authorizes.

---

## Chat vs agent — pick deliberately

| | `IAiChatService` | `IAiAgentFactory` |
| --- | --- | --- |
| Memory between calls | none | `AgentSession` carries history |
| Tool calls | execute **immediately, no confirmation** | paused for approval by default |
| Use for | "summarize this", one-shot requests | a conversational assistant |

Both stay registered; neither replaces the other.

```csharp
var response = await aiChatService.GetResponseAsync(
    [new ChatMessage(ChatRole.User, "What's new in Newtonsoft.Json?")]);
```

MCP tools are merged into `ChatOptions.Tools` automatically — **appended, never clobbering**
caller-supplied tools. Inject `IAiChatClientFactory` directly for a raw `IChatClient` with no
MCP tools merged.

```csharp
var agent = await agentFactory.GetAgentAsync();
var session = await agent.CreateSessionAsync();
var response = await agent.RunAsync("...", session);

while (response.GetToolApprovalRequests() is { Count: > 0 } pending)
{
    var name = (pending[0].ToolCall as FunctionCallContent)?.Name ?? pending[0].ToolCall.CallId;
    var allow = AskUser(name);
    response = await agent.RunAsync(pending.CreateApprovalResponse(allow, alwaysApprove: false), session);
}
```

- The returned type is Microsoft's own `AIAgent`, not a wrapper — `RunAsync`,
  `RunStreamingAsync`, `RunAsync<T>`, `CreateSessionAsync`, `SerializeSessionAsync` all behave
  as the Agent Framework documents.
- **Approval is on by default** (`AiToolApprovalMode.Always`). An MCP server is third-party code
  this library cannot inspect, and in an app where end users add their own servers it is not even
  code the publisher chose. `alwaysApprove: true` records a standing per-tool rule for the
  session — one prompt per distinct tool, not per call. `alwaysApprove` is ignored when denying.
- **The agent is rebuilt when its inputs change** (provider/model/key, or an MCP server
  connecting or disconnecting), so call `GetAgentAsync()` per conversation rather than holding one
  forever. An `AgentSession` survives rebuilds.
- `CreateAgentAsync(descriptor)` builds a second, differently-scoped, uncached agent (a
  summarizer, a reviewer) without disturbing the default one.
- **Saved sessions contain the conversation** — often personal data. The default store is plain
  JSON, per-user but unencrypted. `LoadAsync` returns `null` when nothing was saved, so a "resume
  last conversation" path falls back to `CreateSessionAsync()` with no catch. The **same agent**
  must be passed to `LoadAsync` as to `SaveAsync`.

---

## Provider, model, and key at runtime

```csharp
await apiKeyProvider.SetApiKeyAsync("gemini", theUsersOwnKey);
chatClientFactory.SelectProvider("anthropic");                  // from the catalog
chatClientFactory.UpdateProvider("gemini", "gemini-3.5-flash", endpoint);  // ad hoc
await chatClientFactory.RefreshApiKeyAsync();                   // fire ConfigurationChanged now
```

`IsConfiguredAsync()` reports whether a model is set *and* a key stored, without building a
client. `GetChatClientAsync()` throws `InvalidOperationException` naming what is missing.

`IAiModelCatalog.GetModelsAsync(provider, endpoint)` fills a settings-UI picker with a live list.
Four things it deliberately does not do: **no caching** (drive it from a "Refresh models" button,
never from window activation), no working before a key is stored, no filtering (an
OpenAI-compatible listing includes embedding/audio models with no reliable flag), and no sorting.
Not every endpoint implements the route — **keep a typed-in model name working as the fallback**,
which is free because `Model` is a free string.

**Runtime state does not persist itself.** An `AddServerAsync` or `UpdateProvider` call from a
settings UI is not written back to configuration — persist it yourself.

---

## Attachments

There is no attachment API to learn — build a `ChatMessage` with more than one `AIContent`:

```csharp
new ChatMessage(ChatRole.User,
[
    new TextContent("What's in this screenshot?"),
    new DataContent(await File.ReadAllBytesAsync(path), "image/png"),
])
```

`DataContent` embeds bytes inline (prefer it for local files); `UriContent` references
already-hosted content. Media types are passed straight through — never transformed, downscaled,
or validated — so which types a given provider accepts varies; check that provider's docs.

**`ChatMessage.Text` only reflects `TextContent`.** A transcript UI built on `message.Text` will
silently drop attachments — walk `message.Contents` if the UI must indicate one.
