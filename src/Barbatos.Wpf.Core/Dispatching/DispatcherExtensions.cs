// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Dispatching;

/// <summary>
/// Task-based helpers on top of <see cref="IDispatcher"/>, mirroring .NET MAUI's
/// <c>DispatcherExtensions</c>.
/// </summary>
public static class DispatcherExtensions
{
    /// <summary>
    /// Queues <paramref name="action"/> on <paramref name="dispatcher"/> and returns a task that
    /// completes once it has run.
    /// </summary>
    /// <param name="dispatcher">The dispatcher to run <paramref name="action"/> on.</param>
    /// <param name="action">The work to run.</param>
    /// <returns>
    /// A task that completes when <paramref name="action"/> returns, or faults with whatever it
    /// threw. Faults with <see cref="InvalidOperationException"/> if
    /// <see cref="IDispatcher.Dispatch"/> refuses the work (for example, the dispatcher is
    /// shutting down).
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="dispatcher"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    public static Task DispatchAsync(this IDispatcher dispatcher, Action action)
    {
        _ = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _ = action ?? throw new ArgumentNullException(nameof(action));

        return dispatcher.DispatchAsync(() =>
        {
            action();
            return true;
        });
    }

    /// <summary>
    /// Queues <paramref name="func"/> on <paramref name="dispatcher"/> and returns a task for its
    /// result.
    /// </summary>
    /// <typeparam name="T">The type of the value <paramref name="func"/> returns.</typeparam>
    /// <param name="dispatcher">The dispatcher to run <paramref name="func"/> on.</param>
    /// <param name="func">The work to run.</param>
    /// <returns>
    /// A task that completes with <paramref name="func"/>'s return value, or faults with
    /// whatever it threw. Faults with <see cref="InvalidOperationException"/> if
    /// <see cref="IDispatcher.Dispatch"/> refuses the work.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="dispatcher"/> or <paramref name="func"/> is <see langword="null"/>.</exception>
    public static Task<T> DispatchAsync<T>(this IDispatcher dispatcher, Func<T> func)
    {
        _ = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _ = func ?? throw new ArgumentNullException(nameof(func));

        var tcs = new TaskCompletionSource<T>();

        var dispatched = dispatcher.Dispatch(() =>
        {
            try
            {
                tcs.SetResult(func());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        if (!dispatched)
            tcs.SetException(new InvalidOperationException("Unable to dispatch the action on the dispatcher."));

        return tcs.Task;
    }

    /// <summary>
    /// Starts the asynchronous <paramref name="funcTask"/> on <paramref name="dispatcher"/> and
    /// returns a task that completes when the task it returns completes.
    /// </summary>
    /// <param name="dispatcher">The dispatcher to start <paramref name="funcTask"/> on.</param>
    /// <param name="funcTask">The asynchronous work to start.</param>
    /// <returns>
    /// The unwrapped task: it completes, faults or is cancelled exactly as the task
    /// <paramref name="funcTask"/> returns does, or faults if <paramref name="funcTask"/> throws
    /// synchronously or the dispatch is refused.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="dispatcher"/> or <paramref name="funcTask"/> is <see langword="null"/>.</exception>
    public static Task DispatchAsync(this IDispatcher dispatcher, Func<Task> funcTask) =>
        dispatcher.DispatchAsync<Task>(funcTask).Unwrap();

    /// <summary>
    /// Starts the asynchronous <paramref name="funcTask"/> on <paramref name="dispatcher"/> and
    /// returns a task for the result of the task it returns.
    /// </summary>
    /// <typeparam name="T">The result type of the task <paramref name="funcTask"/> returns.</typeparam>
    /// <param name="dispatcher">The dispatcher to start <paramref name="funcTask"/> on.</param>
    /// <param name="funcTask">The asynchronous work to start.</param>
    /// <returns>
    /// The unwrapped task: it completes with the same result as, or faults or is cancelled
    /// exactly as, the task <paramref name="funcTask"/> returns.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="dispatcher"/> or <paramref name="funcTask"/> is <see langword="null"/>.</exception>
    public static Task<T> DispatchAsync<T>(this IDispatcher dispatcher, Func<Task<T>> funcTask) =>
        dispatcher.DispatchAsync<Task<T>>(funcTask).Unwrap();

    /// <summary>
    /// Runs <paramref name="action"/> inline when the caller is already on
    /// <paramref name="dispatcher"/>'s thread, and queues it there otherwise.
    /// </summary>
    /// <param name="dispatcher">The dispatcher that owns the thread <paramref name="action"/> must run on.</param>
    /// <param name="action">The work to run.</param>
    /// <returns>
    /// <see cref="Task.CompletedTask"/> when <paramref name="action"/> ran inline (an exception
    /// it throws propagates directly to the caller); otherwise the task from
    /// <see cref="DispatchAsync(IDispatcher, Action)"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="dispatcher"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    public static Task DispatchIfRequiredAsync(this IDispatcher dispatcher, Action action)
    {
        _ = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _ = action ?? throw new ArgumentNullException(nameof(action));

        if (dispatcher.IsDispatchRequired)
            return dispatcher.DispatchAsync(action);

        action();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts the asynchronous <paramref name="funcTask"/> inline when the caller is already on
    /// <paramref name="dispatcher"/>'s thread, and on that thread otherwise.
    /// </summary>
    /// <param name="dispatcher">The dispatcher that owns the thread <paramref name="funcTask"/> must start on.</param>
    /// <param name="funcTask">The asynchronous work to start.</param>
    /// <returns>
    /// The task <paramref name="funcTask"/> returns when it was started inline; otherwise the
    /// task from <see cref="DispatchAsync(IDispatcher, Func{Task})"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="dispatcher"/> or <paramref name="funcTask"/> is <see langword="null"/>.</exception>
    public static Task DispatchIfRequiredAsync(this IDispatcher dispatcher, Func<Task> funcTask)
    {
        _ = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _ = funcTask ?? throw new ArgumentNullException(nameof(funcTask));

        if (dispatcher.IsDispatchRequired)
            return dispatcher.DispatchAsync(funcTask);

        return funcTask();
    }
}
