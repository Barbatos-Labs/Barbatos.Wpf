// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// Options for the MCP (Model Context Protocol) client feature - which servers to connect to at
/// startup. Can be configured from code via <c>ConfigureMcp</c> and/or from configuration files
/// using the <see cref="SectionName"/> section (file values override code values). For which AI
/// provider drives the chat/tool-calling loop, see <see cref="AiProviderOptions"/> instead - that
/// is a separate, nested section, since it carries no secrets of its own (see
/// <see cref="IAiApiKeyProvider"/>).
/// </summary>
public class McpOptions
{
    /// <summary>
    /// The configuration section the options are bound from.
    /// </summary>
    public const string SectionName = "Barbatos:Mcp";

    /// <summary>
    /// Whether <see cref="IMcpServerRegistry"/> connects <see cref="Servers"/> at startup.
    /// Defaults to <see langword="true"/>. Does not affect <see cref="IMcpServerRegistry.AddServerAsync"/>
    /// calls made later at runtime (e.g. from a settings UI letting the end user add their own
    /// servers) - this only gates the automatic startup connection of the seed list below.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The MCP servers to connect to automatically at startup, in addition to any added later at
    /// runtime via <see cref="IMcpServerRegistry.AddServerAsync"/>. A connection failure for one
    /// entry is logged and recorded on that server's own <see cref="McpServerStatus"/> - it never
    /// prevents the others from connecting.
    /// </summary>
    /// <remarks>
    /// An entry that is rejected before connecting - one failing
    /// <see cref="McpServerDescriptor.Validate"/>, or reusing an earlier entry's
    /// <see cref="McpServerDescriptor.Name"/> - never gets a <see cref="McpServerStatus"/> at all:
    /// <see cref="IMcpServerRegistry.AddServerAsync"/> rejects it before creating one, so it is
    /// missing from <see cref="IMcpServerRegistry.Servers"/> and the startup error log is the only
    /// place it shows up. Call <see cref="McpServerDescriptor.Validate"/> on entries built from
    /// user input before seeding them.
    /// </remarks>
    public IList<McpServerDescriptor> Servers { get; } = new List<McpServerDescriptor>();
}
