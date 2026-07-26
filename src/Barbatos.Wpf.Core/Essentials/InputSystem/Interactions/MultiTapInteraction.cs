// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Input;

/// <summary>Performs only after <see cref="TapCount"/> presses, each held no longer than <see cref="TapTime"/> and no more than <see cref="TapDelay"/> apart - e.g. a double-tap.</summary>
public sealed class MultiTapInteraction : IInputInteraction
{
    public MultiTapInteraction(int tapCount, TimeSpan tapTime, TimeSpan tapDelay)
    {
        TapCount = tapCount;
        TapTime = tapTime;
        TapDelay = tapDelay;
    }

    public int TapCount { get; }

    /// <summary>The longest a single tap may be held before it stops counting as a tap.</summary>
    public TimeSpan TapTime { get; }

    /// <summary>The longest gap allowed between the end of one tap and the start of the next.</summary>
    public TimeSpan TapDelay { get; }

    int _completedTaps;

    public void Process(ref InputInteractionContext context)
    {
        if (context.IsPressed && !context.IsTimeout)
        {
            context.Started();
            context.SetTimeout(TapTime);
        }
        else if (context.IsTimeout)
        {
            // Held too long, or gapped too long since the previous tap - either way, over.
            Reset();
            context.Canceled();
        }
        else
        {
            _completedTaps++;
            if (_completedTaps >= TapCount)
            {
                context.Performed();
                Reset();
            }
            else
            {
                context.SetTimeout(TapDelay);
            }
        }
    }

    public void Reset() => _completedTaps = 0;
}
