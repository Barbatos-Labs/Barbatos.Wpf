// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows.Threading;

namespace Barbatos.Wpf.Reactivity;

/// <summary>
/// Schedules work to run after the current UI update pass - the Aquarius counterpart of
/// Vue's <c>nextTick()</c>.
/// </summary>
/// <remarks>
/// Vue batches synchronous reactive mutations and flushes the DOM once, via a microtask.
/// <see cref="Run"/>/<see cref="RunAsync"/> do the WPF equivalent by posting through the
/// dispatcher at <see cref="DispatcherPriority.Background"/> - below layout and render,
/// so the callback runs after the current dispatcher frame's UI work has settled. This
/// is also the primitive <c>Lifecycle</c> uses to batch <c>IOnUpdated</c> calls.
/// </remarks>
public static class NextTick
{
    /// <summary>
    /// Schedules <paramref name="callback"/> to run once the current dispatcher frame's
    /// UI work has settled, mirroring <c>nextTick(callback)</c>.
    /// </summary>
    public static DispatcherOperation Run(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        return Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, callback);
    }

    /// <summary>
    /// Awaits the next settled dispatcher frame, mirroring <c>await nextTick()</c>.
    /// </summary>
    public static Task RunAsync(Action? callback = null)
    {
        var completion = new TaskCompletionSource();

        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            try
            {
                callback?.Invoke();
                completion.SetResult();
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        });

        return completion.Task;
    }
}
