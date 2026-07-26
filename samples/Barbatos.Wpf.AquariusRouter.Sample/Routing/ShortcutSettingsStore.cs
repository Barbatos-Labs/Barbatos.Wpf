// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.IO;
using System.Text.Json;
using System.Windows.Input;

namespace Barbatos.Wpf.AquariusRouter.Sample.Routing;

/// <summary>
/// Persists one shortcut's user-chosen key binding into a JSON file shaped to match
/// <see cref="SectionName"/> - the same <see cref="Utf8JsonWriter"/>/nested-object idiom
/// <c>Barbatos.Wpf.Core.Sample</c>'s own <c>SettingsStore</c> uses. Unlike that precedent, which
/// is deliberately loaded without <c>reloadOnChange</c> (restart-required by design there),
/// <see cref="FilePath"/> is loaded *with* <c>reloadOnChange: true</c> in <c>WpfProgram.cs</c> -
/// that difference is the entire point of this type existing separately rather than reusing that
/// one.
/// </summary>
public sealed class ShortcutSettingsStore
{
    /// <summary>
    /// Bound directly to <c>IOptionsMonitor&lt;Dictionary&lt;string, <see cref="KeyBindingOverride"/>&gt;&gt;</c>
    /// (see <see cref="ShortcutRoutingInitializer"/>) - every entry written by <see cref="Save"/>
    /// lands directly at this section, one action name per key, with no wrapper object in between.
    /// </summary>
    public const string SectionName = "Barbatos:InputSystem:KeyBindingOverrides";

    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Barbatos.Wpf.AquariusRouter.Sample",
        "shortcut-settings.json");

    public void Save(string actionName, Key key, ModifierKeys modifiers)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        using var stream = File.Create(FilePath);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteStartObject("Barbatos");
        writer.WriteStartObject("InputSystem");
        writer.WriteStartObject("KeyBindingOverrides");
        writer.WriteStartObject(actionName);
        writer.WriteString("Key", key.ToString());
        writer.WriteString("Modifiers", modifiers.ToString());
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}
