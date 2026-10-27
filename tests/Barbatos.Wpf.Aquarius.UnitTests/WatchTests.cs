// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace Barbatos.Wpf.Aquarius.UnitTests;

public class WatchTests
{
    [Fact]
    public void OnInvokesCallbackWithNewAndOldValueOnChange()
    {
        var count = new Ref<int>(1);
        (int NewValue, int OldValue)? seen = null;

        using var stop = Watch.On(count, (nv, ov) => seen = (nv, ov));

        count.Value = 2;

        Assert.Equal((2, 1), seen);
    }

    [Fact]
    public void OnDoesNotInvokeImmediatelyByDefault()
    {
        var count = new Ref<int>(1);
        var invoked = false;

        using var stop = Watch.On(count, (_, _) => invoked = true);

        Assert.False(invoked);
    }

    [Fact]
    public void OnWithImmediateInvokesOnceRightAway()
    {
        var count = new Ref<int>(7);
        (int NewValue, int OldValue)? seen = null;

        using var stop = Watch.On(count, (nv, ov) => seen = (nv, ov), immediate: true);

        Assert.Equal((7, 7), seen);
    }

    [Fact]
    public void DisposeStopsFurtherNotifications()
    {
        var count = new Ref<int>(1);
        var invocations = 0;

        var stop = Watch.On(count, (_, _) => invocations++);
        stop.Dispose();

        count.Value = 2;

        Assert.Equal(0, invocations);
    }

    [Fact]
    public void EffectRunsImmediatelyAndOnEachDependencyChange()
    {
        var a = new Ref<int>(1);
        var b = new Ref<int>(2);
        var runs = 0;

        using var stop = Watch.Effect(() => runs++, a, b);

        Assert.Equal(1, runs); // the immediate run

        a.Value = 10;
        b.Value = 20;

        Assert.Equal(3, runs);
    }

    [Fact]
    public void EffectDisposeStopsFurtherRuns()
    {
        var a = new Ref<int>(1);
        var runs = 0;

        var stop = Watch.Effect(() => runs++, a);
        stop.Dispose();

        a.Value = 2;

        Assert.Equal(1, runs); // only the immediate run
    }

    [Fact]
    public void OnceStopsAfterTheFirstTriggeredInvocation()
    {
        var count = new Ref<int>(1);
        var invocations = 0;

        using var stop = Watch.On(count, (_, _) => invocations++, once: true);

        count.Value = 2;
        count.Value = 3;

        Assert.Equal(1, invocations);
    }

    [Fact]
    public void OnCleanupRunsBeforeTheNextInvocationAndOnDispose()
    {
        var count = new Ref<int>(1);
        var cleanups = new List<string>();

        var stop = Watch.On<int>(count, (newValue, _, onCleanup) =>
            onCleanup(() => cleanups.Add($"cleanup-for-{newValue}")));

        count.Value = 2; // registers a cleanup tagged "2"
        count.Value = 3; // should run the "2" cleanup right before this invocation

        Assert.Equal(["cleanup-for-2"], cleanups);

        stop.Dispose(); // should run the "3" cleanup

        Assert.Equal(["cleanup-for-2", "cleanup-for-3"], cleanups);
    }

    [Fact]
    public void DeepReactsToCollectionChangesNotJustWholesaleReplacement()
    {
        var items = new Ref<ObservableCollection<int>>([1, 2]);
        var invocations = 0;

        using var stop = Watch.On(items, (_, _) => invocations++, deep: true);

        items.Value.Add(3);

        Assert.Equal(1, invocations);
    }

    [Fact]
    public void NonDeepDoesNotReactToCollectionChanges()
    {
        var items = new Ref<ObservableCollection<int>>([1, 2]);
        var invocations = 0;

        using var stop = Watch.On(items, (_, _) => invocations++); // deep: false (default)

        items.Value.Add(3);

        Assert.Equal(0, invocations);
    }

    [Fact]
    public void PostFlushCoalescesMultipleSynchronousChangesIntoOneCallback()
    {
        StaThread.Run(() =>
        {
            var count = new Ref<int>(1);
            var invocations = 0;

            using var stop = Watch.On(count, (_, _) => invocations++, flush: WatchFlush.Post);

            count.Value = 2;
            count.Value = 3;

            Assert.Equal(0, invocations); // deferred, hasn't run yet

            StaThread.PumpDispatcher();

            Assert.Equal(1, invocations);
        });
    }

    [Fact]
    public void PostFlushPassesTheNewestValueAndTheValueFromBeforeTheBurst()
    {
        // Vue's post-flush arguments. The pair captured at the burst's first change, (1, 0), is
        // what this used to pass - a callback acting on it acted on a value already replaced.
        StaThread.Run(() =>
        {
            var count = new Ref<int>(0);
            var seen = new List<(int, int)>();

            using var stop = Watch.On(count, (newValue, oldValue) => seen.Add((newValue, oldValue)), flush: WatchFlush.Post);

            count.Value = 1;
            count.Value = 2;
            count.Value = 3;
            StaThread.PumpDispatcher();

            Assert.Equal((3, 0), Assert.Single(seen));

            count.Value = 4;
            StaThread.PumpDispatcher();

            // The next burst starts from where the previous one ended.
            Assert.Equal(2, seen.Count);
            Assert.Equal((4, 3), seen[1]);
        });
    }

    [Fact]
    public void PostFlushTriggeredFromAWorkerThreadRunsOnTheWatchersThreadAndKeepsWatching()
    {
        // A change made on a worker used to schedule the callback on the worker's own dispatcher,
        // which nothing pumps: the callback never ran and the "scheduled" flag stayed set, so the
        // watcher ignored every later change too.
        StaThread.Run(() =>
        {
            // In an app the UI thread's dispatcher exists long before any ViewModel builds a watcher.
            _ = Dispatcher.CurrentDispatcher;
            var uiThreadId = Environment.CurrentManagedThreadId;

            var count = new Ref<int>(0);
            var seen = new List<(int, int, int)>();

            using var stop = Watch.On(
                count,
                (newValue, oldValue) => seen.Add((newValue, oldValue, Environment.CurrentManagedThreadId)),
                flush: WatchFlush.Post);

            WorkerThread.Run(() =>
            {
                count.Value = 1;
                count.Value = 2;
            });

            Assert.Empty(seen);

            StaThread.PumpDispatcher();

            Assert.Equal((2, 0, uiThreadId), Assert.Single(seen));

            count.Value = 3;
            StaThread.PumpDispatcher();

            Assert.Equal(2, seen.Count);
            Assert.Equal((3, 2, uiThreadId), seen[1]);
        });
    }

    [Fact]
    public void PostFlushFromAWorkerAfterTheWatchersThreadShutDownIsNotMovedToAnotherThread()
    {
        // The callback belongs to the thread that created the watcher and typically updates that
        // thread's UI. Run on the Application's thread instead, once its own had shut down, those
        // reads would throw on the main UI thread.
        TestApplication.Invoke(() => { }); // an Application exists, as it does in any app

        var ran = false;
        Ref<int>? count = null;
        IDisposable? stop = null;

        StaThread.Run(() =>
        {
            _ = Dispatcher.CurrentDispatcher;
            count = new Ref<int>(0);
            stop = Watch.On(count, (_, _) => ran = true, flush: WatchFlush.Post);

            Dispatcher.CurrentDispatcher.InvokeShutdown();
        });

        try
        {
            WorkerThread.Run(() => count!.Value = 1);

            // Runs everything queued on the Application's dispatcher at Background or above.
            TestApplication.Invoke(StaThread.PumpDispatcher);

            Assert.False(ran);
        }
        finally
        {
            stop?.Dispose();
        }
    }

    [Fact]
    public void EffectOnceStopsAfterTheFirstTriggeredRun()
    {
        var a = new Ref<int>(1);
        var runs = 0;

        using var stop = Watch.Effect(() => runs++, true, WatchFlush.Sync, a);

        a.Value = 2;
        a.Value = 3;

        Assert.Equal(2, runs); // the immediate run, plus exactly one triggered run
    }

    [Fact]
    public void EffectPostFlushCoalescesMultipleSynchronousChangesIntoOneRun()
    {
        StaThread.Run(() =>
        {
            var a = new Ref<int>(1);
            var runs = 0;

            using var stop = Watch.Effect(() => runs++, false, WatchFlush.Post, a);

            Assert.Equal(1, runs); // the immediate run

            a.Value = 2;
            a.Value = 3;

            Assert.Equal(1, runs); // deferred, hasn't run yet

            StaThread.PumpDispatcher();

            Assert.Equal(2, runs);
        });
    }
}
