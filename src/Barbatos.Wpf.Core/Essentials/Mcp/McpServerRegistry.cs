// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.ApplicationModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// The default <see cref="IMcpServerRegistry"/>: connects to each server via the official MCP C#
/// SDK (<c>ModelContextProtocol.Core</c>), using <see cref="StdioClientTransport"/> or
/// <see cref="HttpClientTransport"/> depending on <see cref="McpServerDescriptor.TransportKind"/>.
/// </summary>
internal sealed class McpServerRegistry : IMcpServerRegistry, IDisposable
{
    readonly IMcpAuthorizationHandler? _authorizationHandler;
    readonly IMcpTokenStore? _tokenStore;
    readonly ILogger<McpServerRegistry>? _logger;
    readonly object _lock = new();
    readonly List<Entry> _entries = new();

    public McpServerRegistry(
        IMcpAuthorizationHandler? authorizationHandler = null,
        IMcpTokenStore? tokenStore = null,
        ILogger<McpServerRegistry>? logger = null)
    {
        _authorizationHandler = authorizationHandler;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    public IReadOnlyList<McpServerStatus> Servers
    {
        get
        {
            lock (_lock)
                return _entries.Select(e => e.Status).ToList();
        }
    }

    public IReadOnlyList<AITool> Tools
    {
        get
        {
            lock (_lock)
            {
                var byName = new Dictionary<string, AITool>();
                foreach (var entry in _entries)
                {
                    if (!entry.Status.IsConnected)
                        continue;

                    foreach (var tool in entry.Status.Tools)
                    {
                        if (byName.ContainsKey(tool.Name))
                        {
                            _logger?.LogWarning(
                                "MCP tool name collision: '{ToolName}' is exposed by more than one connected server; the most recently connected server's copy wins.",
                                tool.Name);
                        }

                        byName[tool.Name] = tool;
                    }
                }

                return byName.Values.ToList();
            }
        }
    }

    public event EventHandler? Changed;

    public async Task<McpServerStatus> AddServerAsync(McpServerDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        descriptor.Validate();

        var status = new McpServerStatus(descriptor.Name!);
        var entry = new Entry(status);

        lock (_lock)
        {
            if (_entries.Any(e => e.Status.Name == descriptor.Name))
                throw new ArgumentException($"An MCP server named '{descriptor.Name}' is already registered.", nameof(descriptor));

            _entries.Add(entry);
        }

        RaiseChanged();

        try
        {
            var transport = CreateTransport(descriptor, _logger);
            var client = await McpClient.CreateAsync(transport, CreateClientOptions(), cancellationToken: cancellationToken).ConfigureAwait(false);
            var tools = await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            var orphaned = false;
            lock (_lock)
            {
                // RemoveServerAsync may have already dropped this entry while we were still
                // connecting - don't resurrect it, just dispose the connection we just opened.
                if (!_entries.Contains(entry))
                {
                    orphaned = true;
                }
                else
                {
                    entry.Client = client;
                    status.Tools = new List<AITool>(tools);
                    status.IsConnected = true;
                    status.LastError = null;
                }
            }

            if (orphaned)
                await client.DisposeAsync().ConfigureAwait(false);
            else
                WatchForDisconnect(entry, client);

            return status;
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                status.IsConnected = false;
                status.LastError = Describe(ex);
            }

            throw;
        }
        finally
        {
            RaiseChanged();
        }
    }

    /// <summary>
    /// Marks a server disconnected once its session ends on its own - a stdio server process that
    /// crashed or exited, or an HTTP session the server tore down. Without this the entry would
    /// stay <see cref="McpServerStatus.IsConnected"/> forever and keep offering tools that can no
    /// longer be called: <c>McpClient</c> reports a connection failure only when a call is
    /// actually attempted, so nothing else notices an idle server dying.
    /// </summary>
    void WatchForDisconnect(Entry entry, McpClient client)
    {
        // Fire-and-forget: McpClient.Completion is documented to always complete successfully
        // (a failure is reported through the returned details, not by faulting the task).
        _ = MonitorAsync();

        async Task MonitorAsync()
        {
            var details = await client.Completion.ConfigureAwait(false);

            string error;
            lock (_lock)
            {
                // A deliberate RemoveServerAsync/DisposeAsync already dropped this entry, so this
                // completion is the shutdown we asked for rather than a failure worth reporting.
                if (!_entries.Contains(entry) || !entry.Status.IsConnected)
                    return;

                // Status.Tools deliberately keeps the last known list - it is documented as the
                // tools "as of the last successful connection", and IMcpServerRegistry.Tools
                // already filters disconnected servers out of the aggregate.
                error = Describe(details) ?? "The MCP server closed the connection.";
                entry.Status.IsConnected = false;
                entry.Status.LastError = error;
            }

            _logger?.LogWarning("The MCP server '{ServerName}' disconnected: {Error}", entry.Status.Name, error);
            RaiseChanged();
        }
    }

    public async Task<bool> RemoveServerAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Entry? removed;
        lock (_lock)
        {
            removed = _entries.FirstOrDefault(e => e.Status.Name == name);
            if (removed is null)
                return false;

            _entries.Remove(removed);
        }

        if (removed.Client is { } client)
            await client.DisposeAsync().ConfigureAwait(false);

        RaiseChanged();
        return true;
    }

    public Task<bool> SignOutAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // No store configured means nothing was ever persisted - the answer is simply "nothing to
        // forget", not an error.
        return _tokenStore is null
            ? Task.FromResult(false)
            : _tokenStore.RemoveAsync(name, cancellationToken);
    }

    // Callers always go through AddServerAsync, which calls descriptor.Validate() first - the
    // null-forgiving operators below are guaranteed safe by that, not an unchecked assumption.
    IClientTransport CreateTransport(McpServerDescriptor descriptor, ILogger? logger) => descriptor.TransportKind switch
    {
        McpTransportKind.Stdio => new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = descriptor.Name,
            Command = descriptor.Command!,
            Arguments = descriptor.Arguments,
            WorkingDirectory = descriptor.WorkingDirectory,
            EnvironmentVariables = descriptor.EnvironmentVariables,
            InheritEnvironmentVariables = descriptor.InheritEnvironmentVariables,
            // A local server's stderr is where its own startup/config errors show up. Logging it
            // at Debug keeps it out of the way normally but makes "why won't this server start"
            // answerable without attaching a console to the child process.
            StandardErrorLines = line =>
                logger?.LogDebug("MCP server '{ServerName}' stderr: {Line}", descriptor.Name, line),
        }),
        McpTransportKind.Http => new HttpClientTransport(new HttpClientTransportOptions
        {
            Name = descriptor.Name,
            Endpoint = new Uri(descriptor.Endpoint!),
            AdditionalHeaders = descriptor.AdditionalHeaders,
            TransportMode = descriptor.HttpTransportMode switch
            {
                McpHttpTransportMode.StreamableHttp => HttpTransportMode.StreamableHttp,
                McpHttpTransportMode.Sse => HttpTransportMode.Sse,
                _ => HttpTransportMode.AutoDetect,
            },
            OAuth = CreateOAuthOptions(descriptor),
        }),
        _ => throw new ArgumentOutOfRangeException(nameof(descriptor), descriptor.TransportKind, "Unknown MCP transport kind."),
    };

    /// <summary>
    /// Translates <see cref="McpServerDescriptor.OAuth"/> into the SDK's own OAuth options, or
    /// returns <see langword="null"/> for a server that needs no authorization. Everything the MCP
    /// authorization spec can discover for itself - which authorization server to use, which
    /// scopes it offers, PKCE, registering this client - is left to the SDK; only the two pieces
    /// that need the desktop (sending the user to a browser, and remembering the result across
    /// restarts) are supplied here.
    /// </summary>
    ClientOAuthOptions? CreateOAuthOptions(McpServerDescriptor descriptor)
    {
        if (descriptor.OAuth is not { } oauth)
            return null;

        if (_authorizationHandler is null)
        {
            throw new InvalidOperationException(
                $"The MCP server '{descriptor.Name}' is configured for OAuth but no {nameof(IMcpAuthorizationHandler)} is registered. Call ConfigureMcp() on the app builder, or register your own handler.");
        }

        var options = new ClientOAuthOptions
        {
            RedirectUri = _authorizationHandler.GetRedirectUri(descriptor),
            AuthorizationCallbackHandler = McpAuthorizationHandlerAdapter.ToCallbackHandler(_authorizationHandler, descriptor),
            ClientId = oauth.ClientId,
            ClientSecret = oauth.ClientSecret,
            DynamicClientRegistration = new DynamicClientRegistrationOptions
            {
                // What the authorization server shows on the consent screen. Falling back to the
                // app's own name beats letting the server display nothing identifiable.
                ClientName = string.IsNullOrWhiteSpace(oauth.ClientName) ? AppInfo.Current.Name : oauth.ClientName,
            },
        };

        // Left unset when empty: an empty list would override the scopes the server advertises,
        // whereas null keeps the spec's "WWW-Authenticate -> PRM -> configured -> omit" order.
        if (oauth.Scopes.Count > 0)
            options.Scopes = new List<string>(oauth.Scopes);

        foreach (var parameter in oauth.AdditionalAuthorizationParameters)
            options.AdditionalAuthorizationParameters[parameter.Key] = parameter.Value;

        if (oauth.PersistTokens && _tokenStore is not null)
            options.TokenCache = new McpTokenStoreCache(_tokenStore, descriptor.Name!, _logger);

        return options;
    }

    /// <summary>
    /// Identifies this application to every server it connects to. Servers log the client they
    /// are talking to (and some vary behaviour by it), so reporting the real app name/version
    /// beats the SDK's fallback of whatever the host process happens to be called.
    /// </summary>
    static McpClientOptions CreateClientOptions()
    {
        var appInfo = AppInfo.Current;

        return new McpClientOptions
        {
            ClientInfo = new Implementation
            {
                // IAppInfo.Name is empty when the launching assembly carries no product/title
                // metadata at all; Implementation.Name is required, so don't pass "" along.
                Name = string.IsNullOrWhiteSpace(appInfo.Name) ? "Barbatos.Wpf" : appInfo.Name,
                Version = appInfo.VersionString,
            },
        };
    }

    /// <summary>
    /// Turns a connect-time failure into a message worth showing a user. A transport that closed
    /// carries structured <c>ClientCompletionDetails</c> - for a stdio server that is the child
    /// process's exit code and the tail of its stderr, which is nearly always the actual
    /// explanation ("command not found", "MISSING_API_KEY") behind an otherwise opaque
    /// "transport closed" message.
    /// </summary>
    static string Describe(Exception exception) =>
        exception is ClientTransportClosedException closed
            ? Describe(closed.Details) ?? closed.Message
            : exception.Message;

    /// <summary>
    /// Renders <c>ClientCompletionDetails</c> - including the stdio/HTTP-specific subtypes - into
    /// a single message, or <see langword="null"/> if it carries nothing worth saying.
    /// </summary>
    static string? Describe(ClientCompletionDetails details)
    {
        var parts = new List<string>();

        if (details.Exception?.Message is { Length: > 0 } message)
            parts.Add(message);

        switch (details)
        {
            case StdioClientCompletionDetails stdio:
                if (stdio.ExitCode is { } exitCode)
                    parts.Add($"The server process exited with code {exitCode}.");
                if (stdio.StandardErrorTail is { Count: > 0 } standardError)
                    parts.Add("stderr: " + string.Join(" ", standardError));
                break;

            case HttpClientCompletionDetails { HttpStatusCode: { } statusCode }:
                parts.Add($"The server responded with HTTP {(int)statusCode} ({statusCode}).");
                break;
        }

        return parts.Count > 0 ? string.Join(" ", parts) : null;
    }

    void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);

    public async ValueTask DisposeAsync()
    {
        List<Entry> entries;
        lock (_lock)
        {
            entries = new List<Entry>(_entries);
            _entries.Clear();
        }

        foreach (var entry in entries)
        {
            if (entry.Client is { } client)
                await client.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// <see cref="ModelContextProtocol.Client.McpClient"/> only implements
    /// <see cref="IAsyncDisposable"/>, but this type is registered as a singleton and
    /// <c>WpfApp.Dispose()</c>'s shutdown path is entirely synchronous - .NET's
    /// <c>ServiceProvider</c> throws if its synchronous <c>Dispose()</c> reaches a tracked
    /// singleton that only supports async disposal. Fire-and-forget rather than blocking, the
    /// same tradeoff <c>SignalRPushNotificationTransport</c> makes for the same reason.
    /// </summary>
    public void Dispose() =>
        _ = DisposeAsync().AsTask();

    sealed class Entry
    {
        public Entry(McpServerStatus status) => Status = status;

        public McpServerStatus Status { get; }

        public McpClient? Client { get; set; }
    }
}
