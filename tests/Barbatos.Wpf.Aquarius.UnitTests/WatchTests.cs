using System.Collections.ObjectModel;

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
