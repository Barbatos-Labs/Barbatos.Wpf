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
    public static Task DispatchAsync(this IDispatcher dispatcher, Action action)
    {
        _ = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _ = action ?? throw new ArgumentNullException(nameof(action));

        var tcs = new TaskCompletionSource<bool>();

        var dispatched = dispatcher.Dispatch(() =>
        {
            try
            {
                action();
                tcs.SetResult(true);
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

    public static Task DispatchAsync(this IDispatcher dispatcher, Func<Task> funcTask) =>
        dispatcher.DispatchAsync<Task>(funcTask).Unwrap();

    public static Task<T> DispatchAsync<T>(this IDispatcher dispatcher, Func<Task<T>> funcTask) =>
        dispatcher.DispatchAsync<Task<T>>(funcTask).Unwrap();

    public static Task DispatchIfRequiredAsync(this IDispatcher dispatcher, Action action)
    {
        _ = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _ = action ?? throw new ArgumentNullException(nameof(action));

        if (dispatcher.IsDispatchRequired)
            return dispatcher.DispatchAsync(action);

        action();
        return Task.CompletedTask;
    }

    public static Task DispatchIfRequiredAsync(this IDispatcher dispatcher, Func<Task> funcTask)
    {
        _ = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _ = funcTask ?? throw new ArgumentNullException(nameof(funcTask));

        if (dispatcher.IsDispatchRequired)
            return dispatcher.DispatchAsync(funcTask);

        return funcTask();
    }
}
