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
            Assert.Equal(["OnBeforeCreate", "OnCreated", "OnBeforeMount", "OnMounted", "OnActivated"], vm.Calls);

            // Every test in this file that mounts an element unmounts it again before
            // returning, even when the test itself has nothing to do with unmounting: a mounted
            // element subscribes its dispatcher's UnhandledException, and in the tests that run
            // on TestApplication that dispatcher is shared by every later test, where a leftover
            // subscription would intercept exceptions meant for them. (Lifecycle once subscribed
            // the Application's from every thread, which made a leaked mount here poison the
            // shared dispatcher too - see ErrorCapturedOnTheApplicationsThreadIsNotSeenByAnElementOnAnotherThread.)
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));
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
    public void RemountingAfterUnloadFiresTheWholeCreateThroughMountSequenceAgain()
    {
        // OnBeforeMount already re-fires on a real remount (see IfControlTests etc.) -
        // this is the same fact, isolated at the Lifecycle mechanism level with synthetic
        // events: OnBeforeCreate/OnCreated are not "only once per ViewModel object", they
        // follow the element's own mount/unmount cycle exactly like every other hook here,
        // firing again even though `vm` is the exact same instance both times.
        StaThread.Run(() =>
        {
            var vm = new FakeLifecycleViewModel();
            var element = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(element, true);

            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));
            vm.Calls.Clear();

            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

            Assert.Equal(["OnBeforeCreate", "OnCreated", "OnBeforeMount", "OnMounted", "OnActivated"], vm.Calls);

            // See the cleanup note in MountedFiresAfterBeforeMountOnLoaded above.
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));
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

            // See the cleanup note in MountedFiresAfterBeforeMountOnLoaded above.
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));
        });
    }

    [Fact]
    public void PropertyChangesRaisedOnAWorkerThreadReachTheUpdateHooksOnTheElementsThread()
    {
        // Handled on the worker, the first change threw into the ViewModel's own setter (the
        // element's DataContext cannot be read from another thread), and the batch it had opened
        // never closed: later changes, even on the UI thread, produced no hooks at all.
        StaThread.Run(() =>
        {
            var vm = new FakeLifecycleViewModel();
            var element = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(element, true);
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));
            vm.Calls.Clear();

            WorkerThread.Run(() =>
            {
                vm.Counter++;
                vm.Counter++;
            });

            Assert.Empty(vm.Calls); // marshalled to this thread, not run on the worker

            // The first pump runs the marshalled notifications, which queue IOnUpdated behind
            // that pump's own marker; the second pump runs it.
            StaThread.PumpDispatcher();
            StaThread.PumpDispatcher();

            Assert.Equal(["OnBeforeUpdate", "OnUpdated"], vm.Calls);

            vm.Counter++;
            StaThread.PumpDispatcher();

            Assert.Equal(["OnBeforeUpdate", "OnUpdated", "OnBeforeUpdate", "OnUpdated"], vm.Calls);

            // See the cleanup note in MountedFiresAfterBeforeMountOnLoaded above.
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));
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

    [Fact]
    public void ErrorCapturedSeesExceptionsOnTheElementsOwnThreadEvenWhenItIsNotTheApplicationsThread()
    {
        // The hook used to be wired to Application.Current.DispatcherUnhandledException, which
        // only ever reports the Application's own thread. An element on any other UI thread - a
        // window given its own thread, or this StaThread - never heard about its own exceptions:
        // "boom" escaped the pump below unhandled.
        StaThread.Run(() =>
        {
            var vm = new FakeLifecycleViewModel { ShouldHandleError = true };
            var element = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(element, true);
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

            try
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() => throw new InvalidOperationException("boom")));

                var escaped = Record.Exception(StaThread.PumpDispatcher);

                Assert.Null(escaped);
                Assert.Contains("OnErrorCaptured:boom", vm.Calls);
            }
            finally
            {
                element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));
            }
        });
    }

    [Fact]
    public void ErrorCapturedOnTheApplicationsThreadIsNotSeenByAnElementOnAnotherThread()
    {
        // The crash behind the first Windows CI run of this suite. Mounted on another thread, the
        // element still subscribed the Application's DispatcherUnhandledException; when an
        // exception reached the Application's thread, the element's handler read its DataContext
        // from the wrong thread and threw "The calling thread cannot access this object" in place
        // of the real exception - before the app's own handler could mark it handled. Between two
        // tests nothing catches that, and it ended the test process.
        TestApplication.Invoke(() => { }); // an Application exists, as it does in any app

        StaThread.Run(() =>
        {
            var vm = new FakeLifecycleViewModel { ShouldHandleError = true };
            var element = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(element, true);
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

            try
            {
                Exception? escaped = null;
                bool? observedHandled = null;

                TestApplication.Invoke(() =>
                {
                    void Observer(object sender, DispatcherUnhandledExceptionEventArgs e)
                    {
                        observedHandled = e.Handled;
                        e.Handled = true; // the app's own handler, keeping the shared dispatcher alive
                    }

                    Application.Current.DispatcherUnhandledException += Observer;

                    try
                    {
                        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() => throw new InvalidOperationException("app-thread boom")));
                        escaped = Record.Exception(StaThread.PumpDispatcher);
                    }
                    finally
                    {
                        Application.Current.DispatcherUnhandledException -= Observer;
                    }
                });

                Assert.Null(escaped);
                Assert.False(observedHandled); // reached the app's handler, unclaimed by the element
                Assert.DoesNotContain(vm.Calls, call => call.StartsWith("OnErrorCaptured", StringComparison.Ordinal));
            }
            finally
            {
                // Before the fix this is also what removed the stray subscription again, so a
                // failing run of this test cannot poison the tests after it.
                element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));
            }
        });
    }

    /// <summary>
    /// An element's <c>DataContext</c> is very often replaced between <c>Initialized</c> and
    /// <c>Loaded</c> - that is the whole reason the create/beforeMount hooks are attempted twice.
    /// Whichever ViewModel is actually mounted must receive the complete sequence: tracking only
    /// "did these already run" would spend them on the ViewModel that was then discarded, leaving
    /// the live one with <c>OnMounted</c> and no <c>OnCreated</c>/<c>OnBeforeMount</c> before it.
    /// </summary>
    [Fact]
    public void ReplacingDataContextBeforeMountGivesTheNewViewModelTheFullSequence()
    {
        StaThread.Run(() =>
        {
            var discarded = new FakeLifecycleViewModel();
            var element = new ContentControl { DataContext = discarded };
            Lifecycle.SetEnable(element, true);

            // Initialized runs the first, best-effort pass against the ViewModel present now.
            element.BeginInit();
            element.EndInit();

            var live = new FakeLifecycleViewModel();
            element.DataContext = live;

            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

            Assert.Equal(["OnBeforeCreate", "OnCreated", "OnBeforeMount", "OnMounted", "OnActivated"], live.Calls);
            Assert.DoesNotContain("OnMounted", discarded.Calls);

            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));
        });
    }

    /// <summary>The same ViewModel must not have its create/beforeMount hooks run twice just because the two passes both saw it.</summary>
    [Fact]
    public void AnUnchangedDataContextStillOnlyRunsTheEarlyHooksOnce()
    {
        StaThread.Run(() =>
        {
            var vm = new FakeLifecycleViewModel();
            var element = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(element, true);

            element.BeginInit();
            element.EndInit();
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

            Assert.Equal(["OnBeforeCreate", "OnCreated", "OnBeforeMount", "OnMounted", "OnActivated"], vm.Calls);

            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));
        });
    }
}
