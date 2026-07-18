// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Core Contributors.
// All Rights Reserved.

using System.IO;
using System.Text.Json;

namespace Barbatos.Wpf.Hosting.Sample;

/// <summary>
/// Persists the settings changed from the UI into a JSON file that is loaded back into
/// the host configuration on the next start (file and UI configuration share the same
/// configuration sections).
/// </summary>
public sealed class SettingsStore
{
    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Barbatos.Wpf.Hosting.Sample",
        "user-settings.json");

    public void Save(bool trayIconEnabled, bool keepAwakeEnabled, string quickEntryGesture, bool periodicServicesEnabled, TimeSpan heartbeatInterval)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        using var stream = File.Create(FilePath);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteStartObject("Barbatos");

        writer.WriteStartObject("TrayIcon");
        writer.WriteBoolean("Enabled", trayIconEnabled);
        writer.WriteEndObject();

        writer.WriteStartObject("KeepAwake");
        writer.WriteBoolean("Enabled", keepAwakeEnabled);
        writer.WriteEndObject();

        writer.WriteStartObject("GlobalHotkeys");
        writer.WriteStartObject("Gestures");
        writer.WriteString("QuickEntry", quickEntryGesture);
        writer.WriteEndObject();
        writer.WriteEndObject();

        writer.WriteStartObject("PeriodicServices");
        writer.WriteBoolean("Enabled", periodicServicesEnabled);
        writer.WriteStartObject("Intervals");
        writer.WriteString("Heartbeat", heartbeatInterval.ToString("c"));
        writer.WriteEndObject();
        writer.WriteEndObject();

        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}
