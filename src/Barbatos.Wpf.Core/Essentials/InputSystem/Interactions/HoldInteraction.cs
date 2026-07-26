// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Input;

/// <summary>Performs only once the key has been held for at least <see cref="Duration"/> - releasing earlier cancels instead.</summary>
public sealed class HoldInteraction : IInputInteraction
{
    public HoldInteraction(TimeSpan duration) => Duration = duration;

    public TimeSpan Duration { get; }

    public void Process(ref InputInteractionContext context)
    {
        if (context.IsPressed && !context.IsTimeout)
        {
            context.Started();
            context.SetTimeout(Duration);
        }
        else if (context.IsTimeout)
        {
            context.Performed();
        }
        else
        {
            context.Canceled();
        }
    }

    public void Reset()
    {
    }
}
