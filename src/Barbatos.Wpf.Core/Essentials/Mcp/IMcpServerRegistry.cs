// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Extensions.AI;

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// Manages the set of MCP (Model Context Protocol) servers this application is connected to, and
/// aggregates the tools they expose for <see cref="IAiChatService"/>. Seeded from
/// <see cref="McpOptions.Servers"/> at startup, but also mutable at runtime - servers can be
/// added or removed later (e.g. from a settings UI letting the end user paste in their own MCP
/// server, the same way Claude Desktop/Cursor let a user manage their own server list). Added
/// servers are not persisted by this registry itself; like every other runtime-mutable feature in
/// this library (<c>ITrayIconService</c>, <c>IPeriodicServiceScheduler</c>), persisting a changed
/// server list across app restarts is the consuming application's own responsibility.
/// </summary>
public interface IMcpServerRegistry : IAsyncDisposable
{
    /// <summary>
    /// The live status of every server currently tracked (connected, connecting, or failed).
    /// </summary>
    IReadOnlyList<McpServerStatus> Servers { get; }

    /// <summary>
    /// The aggregated tools of every currently-connected server, recomputed from
    /// <see cref="Servers"/> on each access (no I/O involved - each server's own tool list was
    /// already fetched once, at connect time). Pass this straight into <c>ChatOptions.Tools</c>,
    /// or rely on <see cref="IAiChatService"/> to do so automatically. If two connected servers
    /// expose a same-named tool, the one from whichever server connected most recently wins - a
    /// warning is logged when this happens.
    /// </summary>
    /// <remarks>
    /// Typed as the general <see cref="AITool"/> (rather than the MCP-SDK-specific
    /// <c>ModelContextProtocol.Client.McpClientTool</c>, which every tool here actually is under
    /// the hood) so this interface's public surface never leaks the underlying MCP client SDK.
    /// </remarks>
    IReadOnlyList<AITool> Tools { get; }

    /// <summary>
    /// Occurs whenever <see cref="Servers"/> changes - a server was added, removed, connected, or
    /// failed to connect.
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    /// Connects to a new MCP server and adds it to <see cref="Servers"/>. A connection failure
    /// (as opposed to a malformed/duplicate <paramref name="descriptor"/>, checked upfront - see
    /// the exceptions below) is instead recorded on the returned/tracked <see cref="McpServerStatus"/>
    /// (<see cref="McpServerStatus.IsConnected"/> stays <see langword="false"/>,
    /// <see cref="McpServerStatus.LastError"/> is set) and the exception is also rethrown, so a
    /// caller can choose to observe it either way - through the live <see cref="Servers"/> list,
    /// or by awaiting/catching this call directly.
    /// </summary>
    /// <exception cref="ArgumentException">A server with the same <see cref="McpServerDescriptor.Name"/>
    /// is already registered.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="descriptor"/> is not internally
    /// consistent - see <see cref="McpServerDescriptor.Validate"/>.</exception>
    Task<McpServerStatus> AddServerAsync(McpServerDescriptor descriptor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects and removes the named server. Returns <see langword="false"/> if no server
    /// with that name is currently tracked.
    /// </summary>
    Task<bool> RemoveServerAsync(string name, CancellationToken cancellationToken = default);
}

/// <summary>
/// The live status of one MCP server tracked by <see cref="IMcpServerRegistry"/>.
/// </summary>
public sealed class McpServerStatus
{
    internal McpServerStatus(string name)
    {
        Name = name;
    }

    /// <summary>This server's unique name, matching <see cref="McpServerDescriptor.Name"/>.</summary>
    public string Name { get; }

    /// <summary>Whether this server is currently connected.</summary>
    public bool IsConnected { get; internal set; }

    /// <summary>
    /// The error message from the most recent failed connection attempt, or
    /// <see langword="null"/> if the last attempt succeeded (or none has been made yet).
    /// </summary>
    public string? LastError { get; internal set; }

    /// <summary>
    /// The tools this server exposed as of the last successful connection, or empty if it has
    /// never connected successfully.
    /// </summary>
    public IReadOnlyList<AITool> Tools { get; internal set; } = Array.Empty<AITool>();
}
