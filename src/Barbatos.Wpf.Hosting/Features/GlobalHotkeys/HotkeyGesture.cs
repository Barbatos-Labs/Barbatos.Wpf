// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Text;
using System.Windows.Input;

namespace Barbatos.Wpf.Hotkeys;

/// <summary>
/// A system-wide keyboard gesture, for example <c>Control+Alt+Space</c>.
/// </summary>
public sealed class HotkeyGesture : IEquatable<HotkeyGesture>
{
    public HotkeyGesture(ModifierKeys modifiers, Key key)
    {
        if (key == Key.None)
            throw new ArgumentException("A hotkey gesture requires a key.", nameof(key));

        Modifiers = modifiers;
        Key = key;
    }

    public ModifierKeys Modifiers { get; }

    public Key Key { get; }

    /// <summary>
    /// Parses a gesture string such as <c>Control+Alt+Space</c> or <c>Ctrl+Shift+K</c>.
    /// </summary>
    public static HotkeyGesture Parse(string gesture)
    {
        if (!TryParse(gesture, out var result))
            throw new FormatException($"'{gesture}' is not a valid hotkey gesture. Expected a format like 'Control+Alt+Space'.");

        return result;
    }

    /// <summary>
    /// Tries to parse a gesture string such as <c>Control+Alt+Space</c>.
    /// </summary>
    public static bool TryParse(string? gesture, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out HotkeyGesture? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(gesture))
            return false;

        var modifiers = ModifierKeys.None;
        var key = Key.None;

        var tokens = gesture.Split('+');
        for (var i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i].Trim();
            if (token.Length == 0)
                return false;

            switch (token.ToUpperInvariant())
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= ModifierKeys.Control;
                    continue;
                case "ALT":
                    modifiers |= ModifierKeys.Alt;
                    continue;
                case "SHIFT":
                    modifiers |= ModifierKeys.Shift;
                    continue;
                case "WIN":
                case "WINDOWS":
                    modifiers |= ModifierKeys.Windows;
                    continue;
            }

            // Everything that is not a modifier must be the (single) key, and it must be last.
            if (i != tokens.Length - 1 || !TryParseKey(token, out key))
                return false;
        }

        if (key == Key.None)
            return false;

        result = new HotkeyGesture(modifiers, key);
        return true;
    }

    static bool TryParseKey(string token, out Key key)
    {
        switch (token.ToUpperInvariant())
        {
            case "ESC":
                key = Key.Escape;
                return true;
            case "DEL":
                key = Key.Delete;
                return true;
            case "INS":
                key = Key.Insert;
                return true;
            case "ENTER":
                key = Key.Enter;
                return true;
            case "PGUP":
                key = Key.PageUp;
                return true;
            case "PGDN":
                key = Key.PageDown;
                return true;
        }

        // Digits map to the D0-D9 keys.
        if (token.Length == 1 && char.IsAsciiDigit(token[0]))
        {
            key = Key.D0 + (token[0] - '0');
            return true;
        }

        return Enum.TryParse(token, ignoreCase: true, out key) && key != Key.None;
    }

    public override string ToString()
    {
        var text = new StringBuilder();

        if (Modifiers.HasFlag(ModifierKeys.Control))
            text.Append("Control+");
        if (Modifiers.HasFlag(ModifierKeys.Alt))
            text.Append("Alt+");
        if (Modifiers.HasFlag(ModifierKeys.Shift))
            text.Append("Shift+");
        if (Modifiers.HasFlag(ModifierKeys.Windows))
            text.Append("Windows+");

        return text.Append(Key).ToString();
    }

    public bool Equals(HotkeyGesture? other) =>
        other is not null && Modifiers == other.Modifiers && Key == other.Key;

    public override bool Equals(object? obj) => Equals(obj as HotkeyGesture);

    public override int GetHashCode() => HashCode.Combine(Modifiers, Key);
}
