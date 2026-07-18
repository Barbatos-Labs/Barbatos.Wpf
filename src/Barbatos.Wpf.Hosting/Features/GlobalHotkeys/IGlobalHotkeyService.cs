// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Core Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Hotkeys;

/// <summary>
/// Manages the system-wide hotkeys registered through <c>ConfigureGlobalHotkeys</c>.
/// </summary>
public interface IGlobalHotkeyService
{
    /// <summary>
    /// Gets whether the hotkeys are currently registered with the OS.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Registers or unregisters all hotkeys. Intended to be called from a settings UI.
    /// </summary>
    void SetEnabled(bool enabled);

    /// <summary>
    /// Occurs when <see cref="IsEnabled"/> changes.
    /// </summary>
    event EventHandler? IsEnabledChanged;

    /// <summary>
    /// The hotkeys known to the service.
    /// </summary>
    IReadOnlyList<GlobalHotkey> Hotkeys { get; }

    /// <summary>
    /// Changes the gesture of the named hotkey at runtime, re-registering it when the
    /// service is enabled.
    /// </summary>
    void UpdateGesture(string name, HotkeyGesture gesture);

    /// <summary>
    /// Occurs when any registered hotkey is pressed, in addition to the hotkey's own callback.
    /// </summary>
    event EventHandler<GlobalHotkeyPressedEventArgs>? HotkeyPressed;
}

/// <summary>
/// A named system-wide hotkey.
/// </summary>
public sealed class GlobalHotkey
{
    internal GlobalHotkey(string name, HotkeyGesture gesture, Action? callback)
    {
        Name = name;
        Gesture = gesture;
        Callback = callback;
    }

    public string Name { get; }

    public HotkeyGesture Gesture { get; internal set; }

    internal Action? Callback { get; }
}

/// <summary>
/// The event data for <see cref="IGlobalHotkeyService.HotkeyPressed"/>.
/// </summary>
public sealed class GlobalHotkeyPressedEventArgs : EventArgs
{
    public GlobalHotkeyPressedEventArgs(GlobalHotkey hotkey)
    {
        Hotkey = hotkey;
    }

    public GlobalHotkey Hotkey { get; }
}
