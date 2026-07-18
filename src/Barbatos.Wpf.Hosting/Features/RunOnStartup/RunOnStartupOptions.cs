// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Core Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Startup;

/// <summary>
/// Options for the "run on startup" feature. Can be configured from code via
/// <c>ConfigureRunOnStartup</c> and/or from configuration files using the
/// <see cref="SectionName"/> section (file values override code values).
/// </summary>
public class RunOnStartupOptions
{
    /// <summary>
    /// The configuration section the options are bound from.
    /// </summary>
    public const string SectionName = "Barbatos:RunOnStartup";

    /// <summary>
    /// Whether the application is registered to run on startup when the host is built.
    /// When <see langword="false"/>, the current registration is left untouched so the
    /// user's runtime (UI) choice is preserved.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The name of the startup entry. Defaults to the application name.
    /// </summary>
    public string? EntryName { get; set; }

    /// <summary>
    /// The executable to launch. Defaults to the current process path.
    /// </summary>
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// Optional command line arguments passed to the executable at startup.
    /// </summary>
    public string? Arguments { get; set; }
}
