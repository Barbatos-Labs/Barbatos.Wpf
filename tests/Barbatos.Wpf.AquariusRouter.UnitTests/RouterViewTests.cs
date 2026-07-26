using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.AquariusRouter.UnitTests;

public class RouterViewTests
{
    [Fact]
    public void ARealCompiledXamlViewResolvesItsOwnDataContextViaSetupBeforeRouterChecksGuards()
    {
        StaThread.Run(() =>
        {
            var router = new Router([new RouteRecord { Path = "/", Name = "home", View = typeof(RoutedProbeView) }]);

            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            var view = Assert.IsType<RoutedProbeView>(router.CurrentRoute.Value.Matched[0].Instances["default"]);
            var vm = Assert.IsType<RoutedProbeViewModel>(view.DataContext);
            Assert.Equal(["OnBeforeRouteEnter"], vm.Calls);
            Assert.Equal("Hello from a real compiled View", vm.Greeting);
        });
    }

    [Fact]
    public void DisplaysTheMatchedViewAtRootDepth()
    {
        StaThread.Run(() =>
        {
            var router = new Router([new RouteRecord { Path = "/", Name = "home", View = typeof(RecordingViewA) }]);
            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            var window = new Window { Content = new RouterView { Router = router } };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                var routerView = (RouterView)window.Content;
                Assert.IsType<RecordingViewA>(routerView.Content);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void UpdatesContentWhenNavigatingToADifferentRecord()
    {
        StaThread.Run(() =>
        {
            var router = new Router([
                new RouteRecord { Path = "/", Name = "home", View = typeof(RecordingViewA) },
                new RouteRecord { Path = "/other", Name = "other", View = typeof(RecordingViewB) },
            ]);
            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            var window = new Window { Content = new RouterView { Router = router } };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                var routerView = (RouterView)window.Content;
                Assert.IsType<RecordingViewA>(routerView.Content);

                Assert.Null(router.Push("/other").GetAwaiter().GetResult());
                Assert.IsType<RecordingViewB>(routerView.Content);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ANestedRouterViewFindsItsOwnDepthAndInheritsTheEffectiveRouterAutomatically()
    {
        StaThread.Run(() =>
        {
            var router = new Router([
                new RouteRecord
                {
                    Path = "/parent",
                    Name = "parent",
                    View = typeof(LayoutView),
                    Children = [new RouteRecord { Path = "child", Name = "child", View = typeof(RecordingViewA) }],
                },
            ]);
            Assert.Null(router.Start("/parent/child").GetAwaiter().GetResult());

            // The explicit Router is set ONLY on the root RouterView - the nested one (inside
            // LayoutView's own content) deliberately has no Router of its own, proving it
            // inherits its ancestor's effective Router rather than needing Router.Current.
            var rootRouterView = new RouterView { Router = router };
            var window = new Window { Content = rootRouterView };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                var layoutView = Assert.IsType<LayoutView>(rootRouterView.Content);
                Assert.IsType<RecordingViewA>(layoutView.NestedRouterView.Content);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void NamedOutletsAtTheSameDepthEachShowTheirOwnView()
    {
        StaThread.Run(() =>
        {
            var router = new Router([
                new RouteRecord
                {
                    Path = "/",
                    Name = "home",
                    Views = new Dictionary<string, Type> { ["default"] = typeof(RecordingViewA), ["sidebar"] = typeof(RecordingViewB) },
                },
            ]);
            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            var defaultOutlet = new RouterView { Router = router };
            var sidebarOutlet = new RouterView { Router = router, OutletName = "sidebar" };
            var panel = new StackPanel();
            panel.Children.Add(sidebarOutlet);
            panel.Children.Add(defaultOutlet);
            var window = new Window { Content = panel };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                Assert.IsType<RecordingViewA>(defaultOutlet.Content);
                Assert.IsType<RecordingViewB>(sidebarOutlet.Content);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void SkipsPastAParentRecordWithNoViewsOfItsOwn()
    {
        StaThread.Run(() =>
        {
            var router = new Router([
                new RouteRecord
                {
                    Path = "/admin", // pure grouping record - no View/Views
                    Children = [new RouteRecord { Path = "users", Name = "admin-users", View = typeof(RecordingViewA) }],
                },
            ]);
            Assert.Null(router.Start("/admin/users").GetAwaiter().GetResult());

            var window = new Window { Content = new RouterView { Router = router } };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                // A single, un-nested RouterView at depth 0 still finds "admin-users" directly,
                // skipping the pass-through "/admin" record automatically.
                Assert.IsType<RecordingViewA>(((RouterView)window.Content).Content);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ShowingWithNoRouterAvailableSurfacesAsADispatcherExceptionInsteadOfCrashingTheProcess()
    {
        // Regression test for a real, empirically-found bug: RouterView.OnLoaded is reached from
        // deep inside WPF's own render pipeline (MediaContext.FireLoadedPendingCallbacks) the
        // first time a Window showing it is displayed - a direct, synchronous throw from there
        // propagated straight through WPF's internal Dispatcher.Invoke chain and crashed the
        // whole test host process outright, never reaching an ordinary try/catch around Show(),
        // and never reaching Application.DispatcherUnhandledException either. The fix defers the
        // failure via Dispatcher.BeginInvoke instead (see RouterView's own remarks), which is
        // what this test actually proves: the exception surfaces through the normal, catchable
        // DispatcherUnhandledException route instead.
        Exception? observed = null;
        using var signal = new ManualResetEventSlim();

        void OnUnhandled(object? sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            observed = e.Exception;
            e.Handled = true;
            signal.Set();
        }

        TestApplication.Invoke(() =>
        {
            Application.Current.DispatcherUnhandledException += OnUnhandled;
            var window = new Window { Content = new RouterView() };
            window.Show();
            window.Close();
        });

        try
        {
            // Waited OUTSIDE TestApplication.Invoke deliberately - waiting on the same shared
            // dispatcher thread that still needs to process the queued BeginInvoke'd throw would
            // deadlock instead of ever observing it.
            Assert.True(signal.Wait(TimeSpan.FromSeconds(5)), "Expected the deferred failure to reach DispatcherUnhandledException.");
            Assert.IsType<InvalidOperationException>(observed);
        }
        finally
        {
            TestApplication.Invoke(() => Application.Current.DispatcherUnhandledException -= OnUnhandled);
        }
    }
}
