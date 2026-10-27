// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Threading;

namespace Barbatos.Wpf.Aquarius.Reactivity;

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
/// <para>
/// <b>Which dispatcher.</b> Called on a UI thread, the callback goes to that thread's own
/// dispatcher. Called off it - a thread-pool continuation after an <see langword="await"/>, a
/// <c>Task.Run</c> body, any worker - it goes to a UI thread instead, because a worker's own
/// dispatcher is one nothing ever pumps. Posting there, as earlier versions did, left the
/// callback queued forever, and every caller that coalesces through this
/// (<see cref="WatchFlush.Post"/>, <c>Lifecycle</c>'s <c>IOnUpdated</c>, AquariusValee's
/// <c>FormMeta</c> notifications) kept its "already scheduled" flag set for good and went
/// silent. The first of these that applies wins:
/// <list type="number">
/// <item>the calling thread's dispatcher, when that thread is a UI thread - it is running a
/// dispatcher operation right now, or it is a dedicated (not thread-pool) STA thread, as every
/// WPF UI thread is. One is created if that thread has none yet;</item>
/// <item>for <see cref="Watch"/> and <c>Lifecycle</c>, the dispatcher of the UI thread that
/// created the watcher, or of the element;</item>
/// <item><see cref="Application.Current"/>'s dispatcher, unless it has begun shutting down;</item>
/// <item>the calling thread's own dispatcher regardless. With no <see cref="Application"/> and no
/// UI thread above to fall back on there is nothing better to choose, and the callback runs only
/// if the calling thread pumps that dispatcher itself.</item>
/// </list>
/// The first two hold even when that dispatcher has shut down: the callback then never runs, as
/// with any <c>Dispatcher.BeginInvoke</c> after shutdown. It is never moved to another thread
/// instead. Work queued for one UI thread touches that thread's objects, and on the main UI thread
/// those reads would throw and end the app. A pool thread is never treated as a UI thread, even
/// when it owns a dispatcher: one is created on any thread that merely reads
/// <see cref="Dispatcher.CurrentDispatcher"/> or builds a <see cref="DispatcherObject"/>, and a
/// pool thread never pumps it.
/// </para>
/// </remarks>
public static class NextTick
{
    /// <summary>
    /// Schedules <paramref name="callback"/> to run once the current dispatcher frame's
    /// UI work has settled, mirroring <c>nextTick(callback)</c>.
    /// </summary>
    /// <remarks>Called off the UI thread, the callback runs on the UI thread - see <see cref="NextTick"/>.</remarks>
    public static DispatcherOperation Run(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        return RunOn(owner: null, callback);
    }

    /// <summary>
    /// Awaits the next settled dispatcher frame, mirroring <c>await nextTick()</c>.
    /// </summary>
    /// <remarks>Called off the UI thread, the callback runs on the UI thread - see <see cref="NextTick"/>.</remarks>
    public static Task RunAsync(Action? callback = null)
    {
        var completion = new TaskCompletionSource();

        RunOn(owner: null, () =>
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

    /// <summary>
    /// <see cref="Run"/> with a dispatcher the caller already knows to be the right one
    /// (a watcher's creating thread, an element's own), preferred over
    /// <see cref="Application.Current"/>'s when called off the UI thread.
    /// </summary>
    internal static DispatcherOperation RunOn(Dispatcher? owner, Action callback) =>
        ResolveDispatcher(owner).BeginInvoke(DispatcherPriority.Background, callback);

    /// <summary>
    /// The calling thread's dispatcher if that thread is a UI thread that has one - what a
    /// watcher captures as its owner. Never creates a dispatcher.
    /// </summary>
    internal static Dispatcher? CurrentUiDispatcher() =>
        IsUiThread() ? Dispatcher.FromThread(Thread.CurrentThread) : null;

    private static Dispatcher ResolveDispatcher(Dispatcher? owner)
    {
        // A UI thread keeps its own work, even once its dispatcher has begun shutting down (its
        // window closing, ShutdownStarted handlers still running): the callback touches that
        // thread's objects, and moved to Application.Current's thread it would throw there - on
        // the main UI thread, ending the app - where a shut-down dispatcher just drops it. On a UI
        // thread with no dispatcher yet (a window's own thread before its first DispatcherObject),
        // the one created here is the one it is about to pump.
        if (IsUiThread())
            return Dispatcher.CurrentDispatcher;

        // A worker: the watcher's or element's thread, for the same reason whatever its state.
        if (owner is not null)
            return owner;

        if (Application.Current?.Dispatcher is { } application && IsUsable(application))
            return application;

        return Dispatcher.CurrentDispatcher;
    }

    // WPF installs a DispatcherSynchronizationContext for every dispatcher operation and frame, so
    // its presence proves this thread's dispatcher is pumping right now, whatever the apartment.
    // Outside an operation - startup code, a test body - only the apartment can tell: every WPF UI
    // thread is STA, while a worker that merely owns a dispatcher usually is not.
    private static bool IsUiThread()
    {
        var thread = Thread.CurrentThread;

        if (thread.IsThreadPoolThread)
            return false;

        return SynchronizationContext.Current is DispatcherSynchronizationContext
            || thread.GetApartmentState() == ApartmentState.STA;
    }

    private static bool IsUsable(Dispatcher dispatcher) => !dispatcher.HasShutdownStarted && dispatcher.Thread.IsAlive;
}
