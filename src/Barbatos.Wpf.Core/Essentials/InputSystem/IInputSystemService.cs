// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows.Input;

namespace Barbatos.Wpf.Input;

/// <summary>
/// Owns every registered <see cref="InputActionMap"/> and dispatches key events (in-app and OS-level
/// global hotkeys alike) to the actions they match.
/// </summary>
public interface IInputSystemService
{
    /// <summary>Master switch - when <see langword="false"/>, neither in-app dispatch nor global OS registration is active.</summary>
    bool IsEnabled { get; }

    void SetEnabled(bool enabled);

    event EventHandler? IsEnabledChanged;

    IReadOnlyList<InputActionMap> ActionMaps { get; }

    InputActionMap AddActionMap(InputActionMap map);

    bool RemoveActionMap(InputActionMap map);

    /// <summary>
    /// Re-registers every reachable <see cref="GlobalKeyBinding"/> with the OS and reclaims cached
    /// per-binding state for any binding no longer reachable from any current action. Only
    /// strictly *required* after a <see cref="GlobalKeyBinding"/> change made outside
    /// <see cref="AddActionMap"/>/<see cref="RemoveActionMap"/> - a <see cref="KeyBinding"/>/
    /// <see cref="ChordBinding"/> change already takes effect on the next keystroke without this -
    /// but calling it after any binding mutation is harmless and is what reclaims the per-binding
    /// state memory.
    /// </summary>
    void RefreshBindings();

    /// <summary>Finds a previously-registered action by name across every map (first match wins) - the usual way to attach a <see cref="InputAction.Performed"/> handler once app code has a dependency (e.g. a router) that wasn't available yet when the maps were declared.</summary>
    InputAction? FindAction(string name);

    InputActionMap? FindActionMap(string name);

    /// <summary>
    /// Runs a key-down exactly as if it arrived from the keyboard, through the same matching/
    /// scope/interaction pipeline as a real key event. This is what the real WPF event wiring
    /// calls internally - also directly usable to script a shortcut programmatically, or to unit
    /// test action wiring without a real window or keyboard.
    /// </summary>
    void ProcessKeyDown(Key key, ModifierKeys modifiers = ModifierKeys.None);

    void ProcessKeyUp(Key key, ModifierKeys modifiers = ModifierKeys.None);
}
