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
    /// <see cref="McpServerDescriptor.Endpoint"/>, <see cref="McpServerDescriptor.AdditionalHeaders"/>,
    /// and <see cref="McpServerDescriptor.HttpTransportMode"/>.
    /// </summary>
    Http,
}

/// <summary>
/// Which HTTP wire format a <see cref="McpTransportKind.Http"/> server is reached with. Mirrors
/// the MCP SDK's own <c>ModelContextProtocol.Client.HttpTransportMode</c> so this module's public
/// surface never leaks the underlying client SDK.
/// </summary>
public enum McpHttpTransportMode
{
    /// <summary>
    /// Try Streamable HTTP first and fall back to the legacy SSE transport if the server does not
    /// support it. The default, and the right choice unless a specific server is known to
    /// mis-handle the probe.
    /// </summary>
    AutoDetect,

    /// <summary>
    /// Always use Streamable HTTP, skipping the fallback probe.
    /// </summary>
    StreamableHttp,

    /// <summary>
    /// Always use the legacy HTTP+SSE transport.
    /// </summary>
    Sse,
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
    /// Environment variables set on the launched <see cref="Command"/>, applied on top of
    /// whatever <see cref="InheritEnvironmentVariables"/> lets through. Most real-world stdio
    /// servers are configured this way (an API token, a workspace path), matching the <c>env</c>
    /// block of a Claude Desktop/Cursor server entry. An entry whose value is
    /// <see langword="null"/> removes that variable instead of setting it, which is how a single
    /// inherited secret can be withheld from one server without giving up inheritance entirely.
    /// Only used when <see cref="TransportKind"/> is <see cref="McpTransportKind.Stdio"/>.
    /// </summary>
    public IDictionary<string, string?> EnvironmentVariables { get; } = new Dictionary<string, string?>();

    /// <summary>
    /// Whether the launched <see cref="Command"/> inherits this process's own environment
    /// variables, with <see cref="EnvironmentVariables"/> then applied on top. Defaults to
    /// <see langword="true"/>, matching the MCP SDK's own default. Set to <see langword="false"/>
    /// to start the server with an empty environment, so only <see cref="EnvironmentVariables"/>
    /// reaches it and none of this process's own credentials, tokens, or proxy settings leak into
    /// a third-party server binary. Only used when <see cref="TransportKind"/> is
    /// <see cref="McpTransportKind.Stdio"/>.
    /// </summary>
    public bool InheritEnvironmentVariables { get; set; } = true;

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
    /// Which HTTP wire format to use. Defaults to <see cref="McpHttpTransportMode.AutoDetect"/>,
    /// which probes for Streamable HTTP and falls back to SSE. Only used when
    /// <see cref="TransportKind"/> is <see cref="McpTransportKind.Http"/>.
    /// </summary>
    public McpHttpTransportMode HttpTransportMode { get; set; } = McpHttpTransportMode.AutoDetect;

    /// <summary>
    /// OAuth settings for a server that requires authorization, or <see langword="null"/> (the
    /// default) for one that does not. An empty <c>new McpOAuthOptions()</c> is enough for any
    /// server following the MCP authorization spec - see <see cref="McpOAuthOptions"/>. Only used
    /// when <see cref="TransportKind"/> is <see cref="McpTransportKind.Http"/>; a stdio server
    /// runs as a local child process and authorizes through its own environment instead (see
    /// <see cref="EnvironmentVariables"/>).
    /// </summary>
    public McpOAuthOptions? OAuth { get; set; }

    /// <summary>
    /// Validates that this descriptor is internally consistent for its <see cref="TransportKind"/>,
    /// throwing <see cref="InvalidOperationException"/> if it is not. This is a pure check of the
    /// descriptor's own fields - no connection is attempted - so it can also be used to validate a
    /// server entry from a settings UI before actually trying to connect, the same way
    /// <c>PeriodicSchedule.Validate()</c> lets a schedule be checked before it's saved.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Name"/> is unset, a field required
    /// by the current <see cref="TransportKind"/> (<see cref="Command"/> for
    /// <see cref="McpTransportKind.Stdio"/>, <see cref="Endpoint"/> for <see cref="McpTransportKind.Http"/>)
    /// is unset, or <see cref="OAuth"/> is set on a transport that cannot use it or fails its own
    /// <see cref="McpOAuthOptions.Validate"/>.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException($"{nameof(McpServerDescriptor)}.{nameof(Name)} must be set.");

        switch (TransportKind)
        {
            case McpTransportKind.Stdio when string.IsNullOrWhiteSpace(Command):
                throw new InvalidOperationException(
                    $"{nameof(McpServerDescriptor)}.{nameof(Command)} must be set for '{Name}' ({nameof(McpTransportKind)}.{nameof(McpTransportKind.Stdio)}).");
            case McpTransportKind.Stdio when OAuth is not null:
                throw new InvalidOperationException(
                    $"{nameof(McpServerDescriptor)}.{nameof(OAuth)} is only supported for '{Name}' on {nameof(McpTransportKind)}.{nameof(McpTransportKind.Http)} - a stdio server authorizes through its own {nameof(EnvironmentVariables)} instead.");
            case McpTransportKind.Http when string.IsNullOrWhiteSpace(Endpoint):
                throw new InvalidOperationException(
                    $"{nameof(McpServerDescriptor)}.{nameof(Endpoint)} must be set for '{Name}' ({nameof(McpTransportKind)}.{nameof(McpTransportKind.Http)}).");
        }

        OAuth?.Validate();
    }
}
