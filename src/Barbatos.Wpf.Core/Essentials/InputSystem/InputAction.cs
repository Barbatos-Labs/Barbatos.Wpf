// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows.Input;

namespace Barbatos.Wpf.Input;

/// <summary>
/// A named, triggerable action - decoupled from what actually triggers it, mirroring Unity Input
/// System's own <c>InputAction</c>. Add one or more <see cref="KeyBinding"/>/<see cref="GlobalKeyBinding"/>
/// via <see cref="AddBinding"/>; the same <see cref="Performed"/> (and <see cref="Started"/>/
/// <see cref="Canceled"/>) events fire regardless of which binding actually triggered it - that
/// unification (one action, an in-app binding AND a global hotkey binding both driving it) is the
/// whole point of combining both under one model instead of two disconnected systems.
/// </summary>
public sealed class InputAction
{
    readonly List<InputBinding> _bindings = new();

    public InputAction(string name) => Name = name ?? throw new ArgumentNullException(nameof(name));

    public string Name { get; }

    public IReadOnlyList<InputBinding> Bindings => _bindings;

    /// <summary>The phase from this action's most recent Started/Performed/Canceled transition, across any of its bindings.</summary>
    public InputActionPhase Phase { get; private set; } = InputActionPhase.Waiting;

    public InputAction AddBinding(InputBinding binding)
    {
        _bindings.Add(binding ?? throw new ArgumentNullException(nameof(binding)));
        return this;
    }

    /// <summary>
    /// Removes a previously-added binding by reference identity. Takes effect on the very next
    /// keystroke for a <see cref="KeyBinding"/>/<see cref="ChordBinding"/> - <see cref="IInputSystemService"/>
    /// reads an action's bindings fresh on every keystroke, never a cache. Call
    /// <see cref="IInputSystemService.RefreshBindings"/> afterward if <paramref name="binding"/>
    /// (or any other binding touched in the same batch) is a <see cref="GlobalKeyBinding"/> - the
    /// OS registration IS cached and needs to be told explicitly.
    /// </summary>
    public bool RemoveBinding(InputBinding binding) =>
        _bindings.Remove(binding ?? throw new ArgumentNullException(nameof(binding)));

    /// <summary>Removes every binding from this action. Same live-vs-refresh distinction as <see cref="RemoveBinding"/>.</summary>
    public InputAction ClearBindings()
    {
        _bindings.Clear();
        return this;
    }

    /// <summary>Adds an in-app <see cref="KeyBinding"/> - shorthand for <c>AddBinding(new KeyBinding(...))</c>.</summary>
    public InputAction AddKeyBinding(Key key, ModifierKeys modifiers = ModifierKeys.None, IInputInteraction? interaction = null) =>
        AddBinding(new KeyBinding(key, modifiers, interaction));

    /// <summary>Adds an OS-level <see cref="GlobalKeyBinding"/> - shorthand for <c>AddBinding(new GlobalKeyBinding(...))</c>.</summary>
    public InputAction AddGlobalBinding(Key key, ModifierKeys modifiers = ModifierKeys.None) =>
        AddBinding(new GlobalKeyBinding(key, modifiers));

    /// <summary>Adds a <see cref="ChordBinding"/> using its default <see cref="ChordBinding.StepTimeout"/> - shorthand for <c>AddBinding(new ChordBinding(...))</c>.</summary>
    public InputAction AddChordBinding(params (Key Key, ModifierKeys Modifiers)[] steps) =>
        AddBinding(new ChordBinding(steps));

    /// <summary>Adds a <see cref="ChordBinding"/> with an explicit <paramref name="stepTimeout"/> - shorthand for <c>AddBinding(new ChordBinding(...) { StepTimeout = stepTimeout })</c>.</summary>
    public InputAction AddChordBinding(TimeSpan stepTimeout, params (Key Key, ModifierKeys Modifiers)[] steps) =>
        AddBinding(new ChordBinding(steps) { StepTimeout = stepTimeout });

    public event EventHandler<InputActionContext>? Started;

    public event EventHandler<InputActionContext>? Performed;

    public event EventHandler<InputActionContext>? Canceled;

    internal void RaiseStarted(InputBinding binding, TimeSpan duration)
    {
        Phase = InputActionPhase.Started;
        Started?.Invoke(this, new InputActionContext(this, binding, InputActionPhase.Started, duration));
    }

    internal void RaisePerformed(InputBinding binding, TimeSpan duration)
    {
        Phase = InputActionPhase.Performed;
        Performed?.Invoke(this, new InputActionContext(this, binding, InputActionPhase.Performed, duration));
    }

    internal void RaiseCanceled(InputBinding binding, TimeSpan duration)
    {
        Phase = InputActionPhase.Waiting;
        Canceled?.Invoke(this, new InputActionContext(this, binding, InputActionPhase.Canceled, duration));
    }
}
