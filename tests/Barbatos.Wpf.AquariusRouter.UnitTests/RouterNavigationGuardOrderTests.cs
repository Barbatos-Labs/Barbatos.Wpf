using Barbatos.Wpf.Aquarius.Composition;

namespace Barbatos.Wpf.AquariusRouter.UnitTests;

public class RouterNavigationGuardOrderTests
{
    /// <summary>
    /// The single most important test in this suite: pins the exact interleaved order of every
    /// guard/data-hook stage across a navigation that simultaneously leaves one nested record,
    /// updates its (reused) parent, and enters a different sibling record - the one navigation
    /// shape that exercises leave/update/enter all at once. Matches Vue Router's own documented
    /// "Full Navigation Resolution Flow" order (adjusted only for the construction-timing/no-
    /// callback-capture differences <see cref="IOnBeforeRouteEnter"/> documents).
    /// </summary>
    [Fact]
    public void LeaveUpdateAndEnterHooksFireInTheDocumentedOrderForANestedSiblingSwitch()
    {
        StaThread.Run(() =>
        {
            var log = new List<string>();
            var original = Setup.ServiceProvider;
            var provider = new FakeServiceProvider();
            var sectionView = new RecordingViewA { DataContext = new RecordingViewModel(log, "Section") };
            var profileView = new RecordingViewB { DataContext = new RecordingViewModel(log, "Profile") };
            var settingsView = new RecordingViewC { DataContext = new RecordingViewModel(log, "Settings") };
            provider.Register(typeof(RecordingViewA), sectionView);
            provider.Register(typeof(RecordingViewB), profileView);
            provider.Register(typeof(RecordingViewC), settingsView);
            Setup.ServiceProvider = provider;

            try
            {
                var router = new Router([
                    new RouteRecord
                    {
                        Path = "/section/:id",
                        Name = "section",
                        View = typeof(RecordingViewA),
                        Children =
                        [
                            new RouteRecord { Path = "profile", Name = "section-profile", View = typeof(RecordingViewB) },
                            new RouteRecord { Path = "settings", Name = "section-settings", View = typeof(RecordingViewC) },
                        ],
                    },
                ]);

                using var beforeEach = router.BeforeEach((_, _) =>
                {
                    log.Add("BeforeEach");
                    return Task.FromResult(NavigationGuardResult.Continue);
                });
                using var beforeResolve = router.BeforeResolve((_, _) =>
                {
                    log.Add("BeforeResolve");
                    return Task.FromResult(NavigationGuardResult.Continue);
                });
                using var afterEach = router.AfterEach((_, _, _) => log.Add("AfterEach"));

                var startFailure = router.Start("/section/1/profile").GetAwaiter().GetResult();
                Assert.Null(startFailure);
                log.Clear(); // only care about the SECOND navigation's own order from here.

                var failure = router.Push("/section/1/settings").GetAwaiter().GetResult();

                Assert.Null(failure);
                Assert.Equal(
                    [
                        "Profile.OnBeforeRouteLeave",
                        "BeforeEach",
                        "Section.OnRouteUpdate",
                        "Section.OnBeforeRouteUpdate",
                        "Settings.OnRouteEnter",
                        "Settings.OnBeforeRouteEnter",
                        "BeforeResolve",
                        "AfterEach",
                    ],
                    log);
            }
            finally
            {
                Setup.ServiceProvider = original;
            }
        });
    }

    [Fact]
    public void LeavingRecordsFireDeepestChildFirst()
    {
        StaThread.Run(() =>
        {
            var log = new List<string>();
            var original = Setup.ServiceProvider;
            var provider = new FakeServiceProvider();
            var parentView = new RecordingViewA { DataContext = new RecordingViewModel(log, "Parent") };
            var childView = new RecordingViewB { DataContext = new RecordingViewModel(log, "Child") };
            var otherView = new RecordingViewC { DataContext = new RecordingViewModel(log, "Other") };
            provider.Register(typeof(RecordingViewA), parentView);
            provider.Register(typeof(RecordingViewB), childView);
            provider.Register(typeof(RecordingViewC), otherView);
            Setup.ServiceProvider = provider;

            try
            {
                var router = new Router([
                    new RouteRecord
                    {
                        Path = "/parent",
                        Name = "parent",
                        View = typeof(RecordingViewA),
                        Children = [new RouteRecord { Path = "child", Name = "child", View = typeof(RecordingViewB) }],
                    },
                    new RouteRecord { Path = "/other", Name = "other", View = typeof(RecordingViewC) },
                ]);

                Assert.Null(router.Start("/parent/child").GetAwaiter().GetResult());
                log.Clear();

                Assert.Null(router.Push("/other").GetAwaiter().GetResult());

                // Both Parent and Child leave entirely (neither record is in the new Matched) -
                // Child (deeper) must be checked before Parent, matching Vue Router's own
                // deepest-child-first order.
                Assert.Equal("Child.OnBeforeRouteLeave", log[0]);
                Assert.Equal("Parent.OnBeforeRouteLeave", log[1]);
            }
            finally
            {
                Setup.ServiceProvider = original;
            }
        });
    }

    [Fact]
    public void ACancellingLeaveGuardAbortsBeforeAnyLaterStageRuns()
    {
        StaThread.Run(() =>
        {
            var log = new List<string>();
            var original = Setup.ServiceProvider;
            var provider = new FakeServiceProvider();
            var homeVm = new RecordingViewModel(log, "Home") { LeaveResult = NavigationGuardResult.Cancel };
            var homeView = new RecordingViewA { DataContext = homeVm };
            var otherView = new RecordingViewB { DataContext = new RecordingViewModel(log, "Other") };
            provider.Register(typeof(RecordingViewA), homeView);
            provider.Register(typeof(RecordingViewB), otherView);
            Setup.ServiceProvider = provider;

            try
            {
                var router = new Router([
                    new RouteRecord { Path = "/", Name = "home", View = typeof(RecordingViewA) },
                    new RouteRecord { Path = "/other", Name = "other", View = typeof(RecordingViewB) },
                ]);

                Assert.Null(router.Start("/").GetAwaiter().GetResult());
                log.Clear();

                // Registered only AFTER Start - so a true reading of beforeEachRan below can only
                // mean the SECOND navigation reached BeforeEach, which is exactly what must not
                // happen once the leave guard has already cancelled it.
                var beforeEachRan = false;
                using var beforeEach = router.BeforeEach((_, _) =>
                {
                    beforeEachRan = true;
                    return Task.FromResult(NavigationGuardResult.Continue);
                });

                var failure = router.Push("/other").GetAwaiter().GetResult();

                Assert.NotNull(failure);
                Assert.Equal(NavigationFailureType.Aborted, failure.Type);
                Assert.False(beforeEachRan, "BeforeEach must not run once a leave guard has already cancelled.");
                Assert.Equal(["Home.OnBeforeRouteLeave"], log);
                Assert.Equal("home", router.CurrentRoute.Value.Name); // never committed
            }
            finally
            {
                Setup.ServiceProvider = original;
            }
        });
    }

    [Fact]
    public void AGuardReturningFalseIsShorthandForCancel()
    {
        StaThread.Run(() =>
        {
            var router = new Router([
                new RouteRecord { Path = "/", Name = "home", View = typeof(RecordingViewA) },
                new RouteRecord { Path = "/other", Name = "other", View = typeof(RecordingViewB) },
            ]);

            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            using var beforeEach = router.BeforeEach((_, _) => Task.FromResult<NavigationGuardResult>(false));
            var failure = router.Push("/other").GetAwaiter().GetResult();

            Assert.NotNull(failure);
            Assert.Equal(NavigationFailureType.Aborted, failure.Type);
        });
    }

    [Fact]
    public void PerRouteBeforeEnterOnlyFiresForGenuinelyNewEnteringRecords()
    {
        StaThread.Run(() =>
        {
            var beforeEnterCallCount = 0;

            var router = new Router([
                new RouteRecord
                {
                    Path = "/section/:id",
                    Name = "section",
                    View = typeof(RecordingViewA),
                    BeforeEnter = (_, _) =>
                    {
                        beforeEnterCallCount++;
                        return Task.FromResult(NavigationGuardResult.Continue);
                    },
                },
            ]);

            Assert.Null(router.Start(new RouteLocationRaw { Name = "section", Params = new Dictionary<string, object?> { ["id"] = "1" } }).GetAwaiter().GetResult());
            Assert.Equal(1, beforeEnterCallCount);

            // Same record matches again (id changes from 1 to 2) - this is an UPDATE, not a
            // fresh entry, so BeforeEnter must not fire a second time.
            Assert.Null(router.Push(new RouteLocationRaw { Name = "section", Params = new Dictionary<string, object?> { ["id"] = "2" } }).GetAwaiter().GetResult());
            Assert.Equal(1, beforeEnterCallCount);
        });
    }
}
