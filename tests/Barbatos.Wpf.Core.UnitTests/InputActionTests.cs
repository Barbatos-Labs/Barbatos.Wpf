// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows.Input;
using Barbatos.Wpf.Input;
// WPF's own System.Windows.Input.KeyBinding collides with Barbatos.Wpf.Input.KeyBinding - this
// alias resolves every unqualified KeyBinding reference in this file to ours.
using KeyBinding = Barbatos.Wpf.Input.KeyBinding;

namespace Barbatos.Wpf.Core.UnitTests;

/// <summary>
/// <see cref="InputAction"/>'s own binding-list surface (<see cref="InputAction.AddBinding"/>/
/// <see cref="InputAction.RemoveBinding"/>/<see cref="InputAction.ClearBindings"/>), tested with
/// no DI/service involved at all - only ever exercised indirectly elsewhere, through
/// <see cref="InputActionMap.AddAction(string)"/> inside the DI-built-service tests.
/// </summary>
public class InputActionTests
{
    [Fact]
    public void AddBindingThrowsOnNull()
    {
        var action = new InputAction("Test");

        Assert.Throws<ArgumentNullException>(() => action.AddBinding(null!));
    }

    [Fact]
    public void RemoveBindingRemovesAndReturnsTrue()
    {
        var action = new InputAction("Test");
        var binding = new KeyBinding(Key.F8);
        action.AddBinding(binding);

        Assert.True(action.RemoveBinding(binding));
        Assert.Empty(action.Bindings);
    }

    [Fact]
    public void RemoveBindingReturnsFalseWhenNotPresent()
    {
        var action = new InputAction("Test");

        Assert.False(action.RemoveBinding(new KeyBinding(Key.F8)));
    }

    [Fact]
    public void RemoveBindingThrowsOnNull()
    {
        var action = new InputAction("Test");

        Assert.Throws<ArgumentNullException>(() => action.RemoveBinding(null!));
    }

    [Fact]
    public void ClearBindingsRemovesEveryBinding()
    {
        var action = new InputAction("Test");
        action.AddKeyBinding(Key.F8);
        action.AddGlobalBinding(Key.F9);

        var result = action.ClearBindings();

        Assert.Empty(action.Bindings);
        Assert.Same(action, result);
    }

    [Fact]
    public void ClearBindingsOnAnEmptyActionIsANoOpAndReturnsThis()
    {
        var action = new InputAction("Test");

        var result = action.ClearBindings();

        Assert.Empty(action.Bindings);
        Assert.Same(action, result);
    }
}
