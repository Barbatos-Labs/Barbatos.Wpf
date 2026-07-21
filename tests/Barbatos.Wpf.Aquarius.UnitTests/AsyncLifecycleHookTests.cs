using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.Aquarius.UnitTests;

// Mirrors LifecycleTests.cs's own synthetic RaiseEvent style - the *Async dispatch mechanism
// lives entirely in Lifecycle itself, so it doesn't need a real If/Suspense/etc. around it to
// exercise correctly, the same way the plain sync hooks don't either.
public class AsyncLifecycleHookTests
{
    [Fact]
    public void MountingFiresTheAsyncCreateAndMountHooksInOrder()
    {
        StaThread.Run(() =>
        {
            var vm = new FakeAsyncLifecycleViewModel();
            var element = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(element, true);

            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

            // All five fire synchronously, in order, on the same call that raises Loaded -
            // OnActivatedAsync at the end mirrors Vue's own "onActivated is also called on
            // mount" note, same as the sync hooks (see LifecycleTests). OnMountedAsync's own
            // body only records "started" and returns a still-pending Task (MountedGate),
            // proving nothing here waited for it to actually finish.
            Assert.Equal(
                ["OnBeforeCreateAsync", "OnCreatedAsync", "OnBeforeMountAsync", "OnMountedAsync:started", "OnActivatedAsync"],
                vm.Calls);
            Assert.False(vm.MountedGate.Task.IsCompleted);

            vm.MountedGate.SetResult();
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));
        });
    }

    [Fact]
    public void UnmountingFiresTheAsyncUnmountHooksInOrder()
    {
        StaThread.Run(() =>
        {
            var vm = new FakeAsyncLifecycleViewModel();
            var element = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(element, true);
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));
            vm.MountedGate.SetResult();
            vm.Calls.Clear();

            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));

            // OnDeactivatedAsync in the middle mirrors the sync hooks' own real-unmount
            // ordering (Deactivated fires as part of SetVisibleActivated(false), before
            // OnUnmounted itself - see IfControlTests's real-detach ordering note).
            Assert.Equal(["OnBeforeUnmountAsync", "OnDeactivatedAsync", "OnUnmountedAsync"], vm.Calls);
        });
    }

    [Fact]
    public void PendingAsyncMountedHookDoesNotBlockAnythingAndSettlesCleanlyLater()
    {
        // The whole point of fire-and-forget: a slow OnMountedAsync must not hold up mount
        // completion or anything downstream. Proven here by observing the Task is still
        // incomplete right after mounting, then completing it explicitly afterward with no
        // surprises (no double-fire, no hang, no exception).
        StaThread.Run(() =>
        {
            var vm = new FakeAsyncLifecycleViewModel();
            var element = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(element, true);

            var window = new Window { Content = element, Width = 200, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();

            Assert.Contains("OnMountedAsync:started", vm.Calls);
            Assert.False(vm.MountedGate.Task.IsCompleted);

            vm.MountedGate.SetResult();
            StaThread.PumpDispatcher();

            Assert.True(vm.MountedGate.Task.IsCompleted);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void HidingAndReshowingFireTheAsyncActivatedAndDeactivatedHooks()
    {
        StaThread.Run(() =>
        {
            var vm = new FakeAsyncLifecycleViewModel();
            var element = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(element, true);

            var window = new Window { Content = element, Width = 200, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();
            vm.Calls.Clear();

            element.Visibility = Visibility.Collapsed;
            StaThread.PumpDispatcher();

            Assert.Contains("OnDeactivatedAsync", vm.Calls);
            vm.Calls.Clear();

            element.Visibility = Visibility.Visible;
            StaThread.PumpDispatcher();

            Assert.Contains("OnActivatedAsync", vm.Calls);

            vm.MountedGate.SetResult();
            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void FaultedAsyncHookRoutesThroughTheExistingErrorCapturedMechanism()
    {
        // Proves the design's central claim: there is no separate "async error hook" -
        // a fault from any *Async hook rethrows onto the element's own Dispatcher, which is
        // exactly how an ordinary unhandled exception already reaches IOnErrorCaptured today
        // (see LifecycleTests.ErrorCapturedReturningTrueMarksTheExceptionHandled). The wait
        // happens outside TestApplication.Invoke, on a real cross-thread signal, rather than
        // racing a dispatcher pump against the thread-pool continuation that observes the
        // fault (see FakeThrowingAsyncLifecycleViewModel's own remarks).
        var vm = new FakeThrowingAsyncLifecycleViewModel();
        ContentControl? element = null;

        TestApplication.Invoke(() =>
        {
            element = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(element, true);
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));
        });

        Assert.True(vm.ErrorCapturedSignal.Wait(TimeSpan.FromSeconds(5)), "OnErrorCaptured was not reached in time.");
        Assert.Contains("OnErrorCaptured:boom-async", vm.Calls);

        TestApplication.Invoke(() => element!.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element)));
    }
}
