// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Input;

/// <summary>
/// The lifecycle of one triggering of an <see cref="InputAction"/> - mirrors Unity Input System's
/// own action phases, minus <c>Waiting</c>'s otherwise-implicit "nothing happening" state (there is
/// no event for it - <see cref="InputAction.Phase"/> simply reports it between triggerings).
/// </summary>
public enum InputActionPhase
{
    Waiting,
    Started,
    Performed,
    Canceled,
}

/// <summary>
/// Describes one <see cref="InputActionPhase"/> transition of an <see cref="InputAction"/>.
/// </summary>
public sealed class InputActionContext : EventArgs
{
    internal InputActionContext(InputAction action, InputBinding binding, InputActionPhase phase, TimeSpan duration)
    {
        Action = action;
        Binding = binding;
        Phase = phase;
        Duration = duration;
    }

    /// <summary>The action that transitioned.</summary>
    public InputAction Action { get; }

    /// <summary>
    /// Which of <see cref="Action"/>'s bindings triggered this transition - a <see cref="KeyBinding"/>,
    /// a <see cref="GlobalKeyBinding"/>, or a <see cref="ChordBinding"/>, distinguishable via a
    /// type check if the handler cares.
    /// </summary>
    public InputBinding Binding { get; }

    public InputActionPhase Phase { get; }

    /// <summary>Elapsed time since the triggering binding's own <see cref="InputActionPhase.Started"/> - <see cref="TimeSpan.Zero"/> for <see cref="InputActionPhase.Started"/> itself.</summary>
    public TimeSpan Duration { get; }
}
