// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Input;

/// <summary>Performs only when the key is pressed and released again within <see cref="MaxDuration"/> - a slower release cancels instead.</summary>
public sealed class TapInteraction : IInputInteraction
{
    public TapInteraction(TimeSpan maxDuration) => MaxDuration = maxDuration;

    public TimeSpan MaxDuration { get; }

    bool _timedOut;

    public void Process(ref InputInteractionContext context)
    {
        if (context.IsPressed && !context.IsTimeout)
        {
            _timedOut = false;
            context.Started();
            context.SetTimeout(MaxDuration);
        }
        else if (context.IsTimeout)
        {
            // Too slow so far - the release, whenever it comes, is now too late to count.
            _timedOut = true;
        }
        else if (_timedOut)
        {
            context.Canceled();
        }
        else
        {
            context.Performed();
        }
    }

    public void Reset() => _timedOut = false;
}
