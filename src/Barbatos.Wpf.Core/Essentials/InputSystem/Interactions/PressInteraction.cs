// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Input;

/// <summary>When <see cref="PressInteraction"/> performs, relative to the physical press/release.</summary>
public enum PressInteractionTrigger
{
    /// <summary>Performs on press (the same as <see cref="DefaultInteraction"/>'s own behavior, but explicit).</summary>
    Press,

    /// <summary>Performs on release instead of press.</summary>
    Release,

    /// <summary>Performs twice per gesture: once on press, once again on release.</summary>
    PressAndRelease,
}

/// <summary>An explicit alternative to <see cref="DefaultInteraction"/>, for when a binding needs to perform on release (or both edges) instead of just on press.</summary>
public sealed class PressInteraction : IInputInteraction
{
    /// <summary>Creates a press interaction.</summary>
    /// <param name="trigger">Which edge (or edges) of the press performs the action.</param>
    public PressInteraction(PressInteractionTrigger trigger = PressInteractionTrigger.Press) => Trigger = trigger;

    /// <summary>Which edge (or edges) of the press performs the action.</summary>
    public PressInteractionTrigger Trigger { get; }

    /// <inheritdoc/>
    public void Process(ref InputInteractionContext context)
    {
        if (context.IsPressed)
        {
            context.Started();
            if (Trigger != PressInteractionTrigger.Release)
                context.Performed();
        }
        else if (Trigger != PressInteractionTrigger.Press)
        {
            context.Performed();
        }
        else
        {
            context.Canceled();
        }
    }

    /// <inheritdoc/>
    public void Reset()
    {
    }
}
