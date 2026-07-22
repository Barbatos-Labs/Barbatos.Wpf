// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// The default <see cref="IMcpServerRegistry"/>: connects to each server via the official MCP C#
/// SDK (<c>ModelContextProtocol.Core</c>), using <see cref="StdioClientTransport"/> or
/// <see cref="HttpClientTransport"/> depending on <see cref="McpServerDescriptor.TransportKind"/>.
/// </summary>
internal sealed class McpServerRegistry : IMcpServerRegistry, IDisposable
{
    readonly ILogger<McpServerRegistry>? _logger;
    readonly object _lock = new();
    readonly List<Entry> _entries = new();

    public McpServerRegistry(ILogger<McpServerRegistry>? logger = null)
    {
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
            var transport = CreateTransport(descriptor);
            var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken).ConfigureAwait(false);
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

            return status;
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                status.IsConnected = false;
                status.LastError = ex.Message;
            }

            throw;
        }
        finally
        {
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

    // Callers always go through AddServerAsync, which calls descriptor.Validate() first - the
    // null-forgiving operators below are guaranteed safe by that, not an unchecked assumption.
    static IClientTransport CreateTransport(McpServerDescriptor descriptor) => descriptor.TransportKind switch
    {
        McpTransportKind.Stdio => new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = descriptor.Name,
            Command = descriptor.Command!,
            Arguments = descriptor.Arguments,
            WorkingDirectory = descriptor.WorkingDirectory,
        }),
        McpTransportKind.Http => new HttpClientTransport(new HttpClientTransportOptions
        {
            Name = descriptor.Name,
            Endpoint = new Uri(descriptor.Endpoint!),
            AdditionalHeaders = descriptor.AdditionalHeaders,
        }),
        _ => throw new ArgumentOutOfRangeException(nameof(descriptor), descriptor.TransportKind, "Unknown MCP transport kind."),
    };

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
