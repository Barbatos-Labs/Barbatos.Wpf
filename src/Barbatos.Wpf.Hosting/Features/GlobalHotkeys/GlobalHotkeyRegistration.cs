// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Hotkeys;

/// <summary>
/// Represents a hotkey registered from code, following the same registration pattern as
/// the lifecycle events (<c>LifecycleEventRegistration</c>).
/// </summary>
public class GlobalHotkeyRegistration
{
    public GlobalHotkeyRegistration(string name, string defaultGesture, Action? callback = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DefaultGesture = defaultGesture ?? throw new ArgumentNullException(nameof(defaultGesture));
        Callback = callback;
    }

    public string Name { get; }

    public string DefaultGesture { get; }

    public Action? Callback { get; }
}

/// <summary>
/// Collects the hotkeys registered through <c>ConfigureGlobalHotkeys</c>.
/// </summary>
public interface IGlobalHotkeyBuilder
{
    /// <summary>
    /// Adds a named hotkey with a default gesture (which can be overridden from
    /// configuration) and an optional callback invoked when the hotkey is pressed.
    /// </summary>
    IGlobalHotkeyBuilder Add(string name, string defaultGesture, Action? callback = null);
}
