// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Input;

/// <summary>
/// The implicit behavior when <see cref="SingleKeyBinding.Interaction"/> is left <see langword="null"/>:
/// <c>Started</c>+<c>Performed</c> together on press, <c>Canceled</c> on release - mirrors Unity
/// Input System's own default Button-action-type state machine.
/// </summary>
public sealed class DefaultInteraction : IInputInteraction
{
    /// <summary>The shared instance - the interaction is stateless, so one is enough.</summary>
    public static readonly DefaultInteraction Instance = new();

    /// <inheritdoc/>
    public void Process(ref InputInteractionContext context)
    {
        if (context.IsPressed)
        {
            context.Started();
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
