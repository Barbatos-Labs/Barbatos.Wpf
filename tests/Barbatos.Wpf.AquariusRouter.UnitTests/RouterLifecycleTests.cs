using System.Windows;
using Barbatos.Wpf.Aquarius.Composition;

namespace Barbatos.Wpf.AquariusRouter.UnitTests;

/// <summary>
/// Confirms Aquarius's own <see cref="Lifecycle"/> (<c>aq:Lifecycle.Enable</c> -
/// <see cref="IOnBeforeMount"/>/<see cref="IOnMounted"/>/<see cref="IOnBeforeUnmount"/>/
/// <see cref="IOnUnmounted"/>) composes correctly end to end with <see cref="Router"/>'s own
/// construction/guard/attach pipeline - a real gap in the original test suite, since every other
/// test file here only ever calls <see cref="Router.Start"/>/<see cref="Router.Push"/> without
/// ever showing a real <see cref="Window"/>, so <c>Loaded</c>/<c>Unloaded</c> (what
/// <see cref="Lifecycle"/> is actually built on) never had a chance to fire in any of them.
/// </summary>
public class RouterLifecycleTests
{
    [Fact]
    public void MountedFiresOnlyAfterTheViewIsActuallyAttachedToALiveVisualTree()
    {
        StaThread.Run(() =>
        {
            var router = new Router([new RouteRecord { Path = "/", Name = "home", View = typeof(SelfWiredLifecycleViewA) }]);

            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            // Router.Start already constructed the View and ran every guard against it - but it
            // was never attached to a visual tree, so Mounted must not have fired yet. This is
            // what proves construction and mounting are genuinely two different moments here,
            // exactly like Vue's own onBeforeMount/onMounted split.
            var view = (SelfWiredLifecycleViewA)router.CurrentRoute.Value.Matched[0].Instances["default"];
            Assert.Empty(view.Calls);

            var window = new Window { Content = new RouterView { Router = router } };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                Assert.Equal(["A.OnBeforeMount", "A.OnMounted"], view.Calls);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void LeavingViewUnmountsAndEnteringViewMountsOnNavigation()
    {
        StaThread.Run(() =>
        {
            var router = new Router([
                new RouteRecord { Path = "/", Name = "home", View = typeof(SelfWiredLifecycleViewA) },
                new RouteRecord { Path = "/other", Name = "other", View = typeof(SelfWiredLifecycleViewB) },
            ]);

            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            var window = new Window { Content = new RouterView { Router = router } };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                var homeView = (SelfWiredLifecycleViewA)router.CurrentRoute.Value.Matched[0].Instances["default"];
                Assert.Equal(["A.OnBeforeMount", "A.OnMounted"], homeView.Calls);

                Assert.Null(router.Push("/other").GetAwaiter().GetResult());
                StaThread.PumpDispatcher();

                var otherView = (SelfWiredLifecycleViewB)router.CurrentRoute.Value.Matched[0].Instances["default"];
                Assert.Equal(["A.OnBeforeMount", "A.OnMounted", "A.OnBeforeUnmount", "A.OnUnmounted"], homeView.Calls);
                Assert.Equal(["B.OnBeforeMount", "B.OnMounted"], otherView.Calls);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void UpdatingARecordDoesNotRemountTheReusedInstance()
    {
        StaThread.Run(() =>
        {
            var router = new Router([new RouteRecord { Path = "/section/:id", Name = "section", View = typeof(SelfWiredLifecycleViewA) }]);

            Assert.Null(router.Start("/section/1").GetAwaiter().GetResult());

            var window = new Window { Content = new RouterView { Router = router } };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                var view = (SelfWiredLifecycleViewA)router.CurrentRoute.Value.Matched[0].Instances["default"];
                Assert.Equal(["A.OnBeforeMount", "A.OnMounted"], view.Calls);

                // Same RouteRecord matches again (id changes 1 -> 2) - an update, not a fresh
                // leave+enter, so the same instance is reused and must NOT remount.
                Assert.Null(router.Push("/section/2").GetAwaiter().GetResult());
                StaThread.PumpDispatcher();

                Assert.Same(view, router.CurrentRoute.Value.Matched[0].Instances["default"]);
                Assert.Equal(["A.OnBeforeMount", "A.OnMounted"], view.Calls);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ReEnteringALeftRouteConstructsAFreshInstanceThatMountsIndependently()
    {
        StaThread.Run(() =>
        {
            var router = new Router([
                new RouteRecord { Path = "/", Name = "home", View = typeof(SelfWiredLifecycleViewA) },
                new RouteRecord { Path = "/other", Name = "other", View = typeof(SelfWiredLifecycleViewB) },
            ]);

            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            var window = new Window { Content = new RouterView { Router = router } };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                var firstHomeView = (SelfWiredLifecycleViewA)router.CurrentRoute.Value.Matched[0].Instances["default"];

                Assert.Null(router.Push("/other").GetAwaiter().GetResult());
                StaThread.PumpDispatcher();
                Assert.Null(router.Push("/").GetAwaiter().GetResult());
                StaThread.PumpDispatcher();

                var secondHomeView = (SelfWiredLifecycleViewA)router.CurrentRoute.Value.Matched[0].Instances["default"];

                // No accidental keep-alive: leaving cleared RouteRecord.Instances (see Router's
                // own remarks), so re-entering built a genuinely new instance with its own fresh
                // mount sequence - not the same one silently reattached.
                Assert.NotSame(firstHomeView, secondHomeView);
                Assert.Equal(["A.OnBeforeMount", "A.OnMounted", "A.OnBeforeUnmount", "A.OnUnmounted"], firstHomeView.Calls);
                Assert.Equal(["A.OnBeforeMount", "A.OnMounted"], secondHomeView.Calls);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ParentAndNestedChildBothMountWhenADeepNestedPathIsEnteredDirectlyOnColdStart()
    {
        StaThread.Run(() =>
        {
            var router = new Router([
                new RouteRecord
                {
                    Path = "/parent",
                    Name = "parent",
                    View = typeof(SelfWiredLifecycleLayoutView),
                    Children = [new RouteRecord { Path = "child", Name = "child", View = typeof(SelfWiredLifecycleViewA) }],
                },
            ]);

            // A direct deep-link "cold start" straight to the nested child - never visited
            // "/parent" alone first. This is exactly the shape an AppActions Jump List shortcut
            // (or any startup deep link) produces: Router.Start's own "from" is RouteLocation.Start
            // (empty Matched), so every record in the chain - parent layout AND nested child alike
            // - counts as freshly entering, and both must be constructed and mounted correctly in
            // that single pass.
            Assert.Null(router.Start("/parent/child").GetAwaiter().GetResult());

            var rootRouterView = new RouterView { Router = router };
            var window = new Window { Content = rootRouterView };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                var layoutView = Assert.IsType<SelfWiredLifecycleLayoutView>(rootRouterView.Content);
                var childView = Assert.IsType<SelfWiredLifecycleViewA>(layoutView.NestedRouterView.Content);

                Assert.Equal(["Layout.OnBeforeMount", "Layout.OnMounted"], layoutView.Calls);
                Assert.Equal(["A.OnBeforeMount", "A.OnMounted"], childView.Calls);
            }
            finally
            {
                window.Close();
            }
        });
    }
}
