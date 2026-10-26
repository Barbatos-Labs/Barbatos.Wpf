// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;

namespace Barbatos.Wpf.Input;

/// <summary>
/// A named group of <see cref="InputAction"/>s, bulk enable/disable-able as one unit - mirrors
/// Unity Input System's own <c>InputActionMap</c>. Maps naturally to a "shortcut context" (e.g.
/// only active while a certain View is showing) - a plain data/config object like <c>RouteRecord</c>,
/// not a DI-registered singleton, so toggling <see cref="Enabled"/> needs no service call.
/// </summary>
public sealed class InputActionMap
{
    readonly List<InputAction> _actions = new();

    /// <summary>Creates an empty, enabled map.</summary>
    /// <param name="name">The name <see cref="IInputSystemService.FindActionMap"/> looks the map up by.</param>
    /// <param name="scope">The initial <see cref="Scope"/>; <see langword="null"/> for application-wide.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    public InputActionMap(string name, FrameworkElement? scope = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Scope = scope;
    }

    /// <summary>The map's name, as passed to the constructor.</summary>
    public string Name { get; }

    /// <summary>
    /// The element this map's <see cref="KeyBinding"/>s require keyboard focus within (via
    /// <see cref="UIElement.IsKeyboardFocusWithin"/>) - e.g. a specific View's root, for shortcuts
    /// that should only fire while that View is showing. <see langword="null"/> (the default)
    /// means "active whenever this application has focus" - never affects
    /// <see cref="GlobalKeyBinding"/>, which is OS-level and focus-independent by definition.
    /// </summary>
    public FrameworkElement? Scope { get; set; }

    /// <summary>Bulk enable/disable for every action in this map.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>The map's actions, in the order they were added.</summary>
    public IReadOnlyList<InputAction> Actions => _actions;

    /// <summary>Adds <paramref name="action"/> to this map.</summary>
    /// <returns>This map, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public InputActionMap AddAction(InputAction action)
    {
        _actions.Add(action ?? throw new ArgumentNullException(nameof(action)));
        return this;
    }

    /// <summary>Creates, adds, and returns a new named <see cref="InputAction"/> - shorthand for constructing one and calling <see cref="AddAction(InputAction)"/>.</summary>
    public InputAction AddAction(string name)
    {
        var action = new InputAction(name);
        AddAction(action);
        return action;
    }
}
