// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Core Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Hosting;

/// <summary>
/// Options for the periodic services feature. Can be configured from code via
/// <c>ConfigurePeriodicServices</c> and/or from configuration files using the
/// <see cref="SectionName"/> section (file values override code values).
/// </summary>
public class PeriodicServiceOptions
{
    /// <summary>
    /// The configuration section the options are bound from.
    /// </summary>
    public const string SectionName = "Barbatos:PeriodicServices";

    /// <summary>
    /// Whether the periodic services start running when the host is built.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Overrides the interval of a named service, for example
    /// <c>"Barbatos:PeriodicServices:Intervals:Sync" = "00:05:00"</c>.
    /// </summary>
    public Dictionary<string, TimeSpan> Intervals { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
