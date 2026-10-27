using System.Windows.Threading;

namespace Barbatos.Wpf.Aquarius.UnitTests;

public class NextTickTests
{
    [Fact]
    public void RunDoesNotExecuteSynchronously()
    {
        StaThread.Run(() =>
        {
            var ran = false;

            NextTick.Run(() => ran = true);

            Assert.False(ran);

            StaThread.PumpDispatcher();

            Assert.True(ran);
        });
    }

    [Fact]
    public void RunAsyncCompletesOnlyAfterThePump()
    {
        StaThread.Run(() =>
        {
            var task = NextTick.RunAsync(() => { });

            Assert.False(task.IsCompleted);

            StaThread.PumpDispatcher();

            Assert.True(task.IsCompletedSuccessfully);
        });
    }

    [Fact]
    public void RunFromAWorkerThreadRunsOnTheApplicationsDispatcher()
    {
        // Posting to the worker's own dispatcher, as NextTick originally did, queued the callback
        // where nothing ever pumps it: it never ran, and every caller coalescing through NextTick
        // (WatchFlush.Post, IOnUpdated, Valee's FormMeta) stayed "already scheduled" for good.
        var applicationThreadId = 0;
        TestApplication.Invoke(() => applicationThreadId = Environment.CurrentManagedThreadId);

        var ranOnThreadId = 0;

        // Not disposed: if the callback ever ran late, a Set on a disposed event would throw on
        // the dispatcher every other Application test shares.
        var ran = new ManualResetEventSlim();
        Task? completion = null;

        WorkerThread.Run(() =>
        {
            NextTick.Run(() =>
            {
                ranOnThreadId = Environment.CurrentManagedThreadId;
                ran.Set();
            });

            completion = NextTick.RunAsync();
        });

        // Waited for here, outside TestApplication.Invoke - its dispatcher has to run the callback.
        Assert.True(ran.Wait(TimeSpan.FromSeconds(5)), "NextTick.Run's callback never ran.");
        Assert.Equal(applicationThreadId, ranOnThreadId);
        Assert.True(completion!.Wait(TimeSpan.FromSeconds(5)), "NextTick.RunAsync never completed.");
    }

    [Fact]
    public void RunFromAWorkerThreadThatOwnsADispatcherStillRunsOnTheApplicationsDispatcher()
    {
        // A worker gets a dispatcher of its own just by reading Dispatcher.CurrentDispatcher or
        // building any DispatcherObject (a brush decoded off the UI thread, say) - owning one is not
        // pumping one. Treating every dedicated thread's dispatcher as a UI thread's would strand
        // the callback there exactly as before.
        var applicationThreadId = 0;
        TestApplication.Invoke(() => applicationThreadId = Environment.CurrentManagedThreadId);

        var ranOnThreadId = 0;

        // Not disposed, for the same reason as in the test above.
        var ran = new ManualResetEventSlim();

        WorkerThread.Run(() =>
        {
            _ = Dispatcher.CurrentDispatcher;

            NextTick.Run(() =>
            {
                ranOnThreadId = Environment.CurrentManagedThreadId;
                ran.Set();
            });
        });

        Assert.True(ran.Wait(TimeSpan.FromSeconds(5)), "NextTick.Run's callback never ran.");
        Assert.Equal(applicationThreadId, ranOnThreadId);
    }

    [Fact]
    public void RunOnAUiThreadWhoseDispatcherHasShutDownIsNotMovedToTheApplicationsThread()
    {
        // Work queued on a UI thread touches that thread's objects. Moved to the Application's
        // thread because this one's dispatcher had shut down (a window thread closing while its
        // last handlers still run), a callback reading one of them - Lifecycle's IOnUpdated reads
        // its element - throws there instead, on the main UI thread, where it ends the app.
        Dispatcher? applicationDispatcher = null;
        TestApplication.Invoke(() => applicationDispatcher = Dispatcher.CurrentDispatcher);

        var ran = false;
        DispatcherOperation? operation = null;

        StaThread.Run(() =>
        {
            Dispatcher.CurrentDispatcher.InvokeShutdown();

            operation = NextTick.Run(() => ran = true);
        });

        // Runs everything queued on the Application's dispatcher at Background or above - where
        // the callback would be waiting had it been moved there.
        TestApplication.Invoke(StaThread.PumpDispatcher);

        Assert.False(ran);
        Assert.NotSame(applicationDispatcher, operation!.Dispatcher);
    }
}
