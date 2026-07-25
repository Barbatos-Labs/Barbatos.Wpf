// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// Which underlying transport an MCP server described by <see cref="McpServerDescriptor"/> uses.
/// </summary>
public enum McpTransportKind
{
    /// <summary>
    /// A local server launched as a child process, communicating over standard input/output
    /// (<c>ModelContextProtocol.Client.StdioClientTransport</c>). Uses <see cref="McpServerDescriptor.Command"/>,
    /// <see cref="McpServerDescriptor.Arguments"/>, and <see cref="McpServerDescriptor.WorkingDirectory"/>.
    /// </summary>
    Stdio,

    /// <summary>
    /// A remote server reached over HTTP, using Streamable HTTP or falling back to SSE
    /// (<c>ModelContextProtocol.Client.HttpClientTransport</c>). Uses
    /// <see cref="McpServerDescriptor.Endpoint"/> and <see cref="McpServerDescriptor.AdditionalHeaders"/>.
    /// </summary>
    Http,
}

/// <summary>
/// Describes one MCP server to connect to, either seeded from <see cref="McpOptions.Servers"/> at
/// startup or added later at runtime via <see cref="IMcpServerRegistry.AddServerAsync"/>.
/// </summary>
public class McpServerDescriptor
{
    /// <summary>
    /// A unique name identifying this server - used as the key for
    /// <see cref="IMcpServerRegistry.RemoveServerAsync"/> and reported on <see cref="McpServerStatus.Name"/>.
    /// Required - connecting throws a clear <see cref="InvalidOperationException"/> if left unset.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Which transport this server uses. Defaults to <see cref="McpTransportKind.Stdio"/>.
    /// </summary>
    public McpTransportKind TransportKind { get; set; } = McpTransportKind.Stdio;

    /// <summary>
    /// The executable to launch. Only used when <see cref="TransportKind"/> is
    /// <see cref="McpTransportKind.Stdio"/>. Required for that transport - connecting throws a
    /// clear <see cref="InvalidOperationException"/> if left unset.
    /// </summary>
    public string? Command { get; set; }

    /// <summary>
    /// Command-line arguments passed to <see cref="Command"/>. Only used when
    /// <see cref="TransportKind"/> is <see cref="McpTransportKind.Stdio"/>.
    /// </summary>
    public IList<string> Arguments { get; } = new List<string>();

    /// <summary>
    /// The working directory <see cref="Command"/> is launched in, or <see langword="null"/> to
    /// use the host process's own working directory. Only used when <see cref="TransportKind"/>
    /// is <see cref="McpTransportKind.Stdio"/>.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// The server's URL. Only used when <see cref="TransportKind"/> is
    /// <see cref="McpTransportKind.Http"/>. Required for that transport - connecting throws a
    /// clear <see cref="InvalidOperationException"/> if left unset.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Extra HTTP headers sent with every request (e.g. an authorization header the server
    /// itself requires). Only used when <see cref="TransportKind"/> is <see cref="McpTransportKind.Http"/>.
    /// </summary>
    public IDictionary<string, string> AdditionalHeaders { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Validates that this descriptor is internally consistent for its <see cref="TransportKind"/>,
    /// throwing <see cref="InvalidOperationException"/> if it is not. This is a pure check of the
    /// descriptor's own fields - no connection is attempted - so it can also be used to validate a
    /// server entry from a settings UI before actually trying to connect, the same way
    /// <c>PeriodicSchedule.Validate()</c> lets a schedule be checked before it's saved.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Name"/> is unset, or a field
    /// required by the current <see cref="TransportKind"/> (<see cref="Command"/> for
    /// <see cref="McpTransportKind.Stdio"/>, <see cref="Endpoint"/> for <see cref="McpTransportKind.Http"/>)
    /// is unset.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException($"{nameof(McpServerDescriptor)}.{nameof(Name)} must be set.");

        switch (TransportKind)
        {
            case McpTransportKind.Stdio when string.IsNullOrWhiteSpace(Command):
                throw new InvalidOperationException(
                    $"{nameof(McpServerDescriptor)}.{nameof(Command)} must be set for '{Name}' ({nameof(McpTransportKind)}.{nameof(McpTransportKind.Stdio)}).");
            case McpTransportKind.Http when string.IsNullOrWhiteSpace(Endpoint):
                throw new InvalidOperationException(
                    $"{nameof(McpServerDescriptor)}.{nameof(Endpoint)} must be set for '{Name}' ({nameof(McpTransportKind)}.{nameof(McpTransportKind.Http)}).");
        }
    }
}
