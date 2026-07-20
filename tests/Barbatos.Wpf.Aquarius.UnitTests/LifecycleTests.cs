using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Barbatos.Wpf.Aquarius.UnitTests;

public class LifecycleTests
{
    [Fact]
    public void MountedFiresAfterBeforeMountOnLoaded()
    {
        StaThread.Run(() =>
        {
            var vm = new FakeLifecycleViewModel();
            var element = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(element, true);

            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

            // Vue's own note applies here too: "onActivated is also called on mount."
            Assert.Equal(["OnBeforeMount", "OnMounted", "OnActivated"], vm.Calls);
        });
    }

    [Fact]
    public void UnmountedFiresBeforeUnmountThenUnmountedOnUnloaded()
    {
        StaThread.Run(() =>
        {
            var vm = new FakeLifecycleViewModel();
            var element = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(element, true);
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));
            vm.Calls.Clear();

            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));

            // Vue's own note applies here too: "...and onDeactivated on unmount."
            Assert.Equal(["OnBeforeUnmount", "OnDeactivated", "OnUnmounted"], vm.Calls);
        });
    }

    [Fact]
    public void DisablingEnableStopsFurtherHookDispatch()
    {
        StaThread.Run(() =>
        {
            var vm = new FakeLifecycleViewModel();
            var element = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(element, true);
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

            Lifecycle.SetEnable(element, false);
            vm.Calls.Clear();
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));

            Assert.Empty(vm.Calls);
        });
    }

    [Fact]
    public void MultiplePropertyChangesCoalesceIntoOneUpdatedCall()
    {
        StaThread.Run(() =>
        {
            var vm = new FakeLifecycleViewModel();
            var element = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(element, true);
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));
            vm.Calls.Clear();

            vm.Counter++;
            vm.Counter++;

            // OnBeforeUpdate is synchronous (fires on the first change in the batch);
            // OnUpdated is coalesced through NextTick, so it hasn't run yet.
            Assert.Equal(["OnBeforeUpdate"], vm.Calls);

            StaThread.PumpDispatcher();

            Assert.Equal(["OnBeforeUpdate", "OnUpdated"], vm.Calls);
        });
    }

    [Fact]
    public void NoUpdateHooksFireWhileUnmounted()
    {
        StaThread.Run(() =>
        {
            var vm = new FakeLifecycleViewModel();
            var element = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(element, true);

            vm.Counter++;
            StaThread.PumpDispatcher();

            Assert.DoesNotContain("OnBeforeUpdate", vm.Calls);
            Assert.DoesNotContain("OnUpdated", vm.Calls);
        });
    }

    [Fact]
    public void MountingAVisibleElementFiresActivatedExactlyOnce()
    {
        StaThread.Run(() =>
        {
            var vm = new FakeLifecycleViewModel();
            var element = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(element, true);

            var window = new Window { Content = element, Width = 200, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();

            // Loaded's own SetVisibleActivated(true) and the IsVisibleChanged notification
            // that fires around the same real show-a-window transition must dedup to one call.
            Assert.Single(vm.Calls, "OnActivated");

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void HidingAMountedElementFiresDeactivatedAndReshowingItFiresActivatedAgain()
    {
        StaThread.Run(() =>
        {
            var vm = new FakeLifecycleViewModel();
            var element = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(element, true);

            var window = new Window { Content = element, Width = 200, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();
            vm.Calls.Clear();

            // The KeepAlive case: the element is hidden but never unmounted (still the
            // Window's Content, same instance) - no Unloaded, only IsVisibleChanged.
            element.Visibility = Visibility.Collapsed;
            StaThread.PumpDispatcher();

            Assert.Equal(["OnDeactivated"], vm.Calls);

            vm.Calls.Clear();
            element.Visibility = Visibility.Visible;
            StaThread.PumpDispatcher();

            Assert.Equal(["OnActivated"], vm.Calls);
            Assert.DoesNotContain("OnMounted", vm.Calls); // still the same mounted instance

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void UnmountingAVisibleElementFiresDeactivatedBeforeUnmounted()
    {
        StaThread.Run(() =>
        {
            var vm = new FakeLifecycleViewModel();
            var element = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(element, true);

            var window = new Window { Content = element, Width = 200, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();
            vm.Calls.Clear();

            window.Content = null;
            StaThread.PumpDispatcher();

            Assert.Contains("OnDeactivated", vm.Calls);
            Assert.Contains("OnBeforeUnmount", vm.Calls);
            Assert.Equal("OnUnmounted", vm.Calls[^1]);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void ErrorCapturedReturningTrueMarksTheExceptionHandled()
    {
        TestApplication.Invoke(() =>
        {
            var vm = new FakeLifecycleViewModel { ShouldHandleError = true };
            var element = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(element, true);
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

            Exception? escaped = null;
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() => throw new InvalidOperationException("boom")));

            try
            {
                StaThread.PumpDispatcher();
            }
            catch (Exception ex)
            {
                escaped = ex;
            }

            Assert.Null(escaped);
            Assert.Contains("OnErrorCaptured:boom", vm.Calls);

            // TestApplication is one shared, process-lifetime dispatcher - unmount so this
            // element's DispatcherUnhandledException subscription doesn't outlive the test
            // and intercept exceptions meant for a later one.
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));
        });
    }

    [Fact]
    public void ErrorCapturedReturningFalseLeavesTheExceptionUnhandled()
    {
        TestApplication.Invoke(() =>
        {
            var vm = new FakeLifecycleViewModel { ShouldHandleError = false };
            var element = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(element, true);
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

            // Observe what Lifecycle left e.Handled as, without depending on how (or
            // whether) an unhandled dispatcher exception happens to unwind back out of a
            // nested Dispatcher.Invoke pump - that's WPF's own concern, not this hook's.
            bool? observedHandled = null;

            void Observer(object sender, DispatcherUnhandledExceptionEventArgs e)
            {
                observedHandled = e.Handled;
                e.Handled = true; // keep the test process itself from crashing
            }

            Application.Current.DispatcherUnhandledException += Observer;
            try
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() => throw new InvalidOperationException("boom")));
                StaThread.PumpDispatcher();
            }
            finally
            {
                Application.Current.DispatcherUnhandledException -= Observer;
            }

            Assert.False(observedHandled);
            Assert.Contains("OnErrorCaptured:boom", vm.Calls);

            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));
        });
    }
}
