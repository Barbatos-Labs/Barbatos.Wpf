namespace Barbatos.Wpf.AquariusRouter.UnitTests;

public class RouterRedirectAndFailureTests
{
    private static Router SimpleRouter() => new([
        new RouteRecord { Path = "/", Name = "home", View = typeof(RecordingViewA) },
        new RouteRecord { Path = "/other", Name = "other", View = typeof(RecordingViewB) },
    ]);

    [Fact]
    public void StaticRedirectResolvesToTheTargetBeforeAnyGuardRuns()
    {
        StaThread.Run(() =>
        {
            var beforeEachCallCount = 0;
            var router = new Router([
                new RouteRecord { Path = "/old", Redirect = _ => "/new" },
                new RouteRecord { Path = "/new", Name = "new", View = typeof(RecordingViewA) },
            ]);
            using var beforeEach = router.BeforeEach((_, _) =>
            {
                beforeEachCallCount++;
                return Task.FromResult(NavigationGuardResult.Continue);
            });

            var failure = router.Start("/old").GetAwaiter().GetResult();

            Assert.Null(failure);
            Assert.Equal("new", router.CurrentRoute.Value.Name);
            // Only one navigation's worth of guard checks - the redirecting "/old" record's
            // own (nonexistent) guards never ran, and the redirect itself doesn't double-count.
            Assert.Equal(1, beforeEachCallCount);
        });
    }

    [Fact]
    public void RedirectCanBeAFunctionOfTheTargetLocation()
    {
        StaThread.Run(() =>
        {
            var router = new Router([
                new RouteRecord
                {
                    Path = "/search/:term",
                    Redirect = to => new RouteLocationRaw
                    {
                        Path = "/search",
                        Query = new Dictionary<string, string?> { ["q"] = (string)to.Params["term"]! },
                    },
                },
                new RouteRecord { Path = "/search", Name = "search", View = typeof(RecordingViewA) },
            ]);

            var failure = router.Start("/search/hello").GetAwaiter().GetResult();

            Assert.Null(failure);
            Assert.Equal("search", router.CurrentRoute.Value.Name);
            Assert.Equal("hello", router.CurrentRoute.Value.Query["q"]);
        });
    }

    [Fact]
    public void AGuardReturnedRedirectStartsAFreshNavigationAtTheNewTarget()
    {
        StaThread.Run(() =>
        {
            var router = SimpleRouter();
            using var beforeEach = router.BeforeEach((to, _) =>
                Task.FromResult(to.Name == "other" ? NavigationGuardResult.Redirect("/") : NavigationGuardResult.Continue));

            var failure = router.Start("/other").GetAwaiter().GetResult();

            Assert.Null(failure);
            Assert.Equal("home", router.CurrentRoute.Value.Name);
        });
    }

    [Fact]
    public void AGuardRedirectLoopThrowsAfterTooManyHops()
    {
        StaThread.Run(() =>
        {
            var router = SimpleRouter();
            using var beforeEach = router.BeforeEach((to, _) =>
                Task.FromResult(NavigationGuardResult.Redirect(to.Name == "home" ? "/other" : "/")));

            Assert.Throws<InvalidOperationException>(() => router.Start("/").GetAwaiter().GetResult());
        });
    }

    /// <summary>
    /// A static <see cref="RouteRecord.Redirect"/> cycle recurses synchronously - nothing awaits
    /// between hops - so without a hop limit it ends in a stack overflow, which .NET cannot catch:
    /// the whole process dies. It must fail the way a guard-redirect loop does instead. The cycle
    /// below breaks itself after 200 hops, so that without the limit this test fails on its
    /// assertion rather than taking the test host down with it.
    /// </summary>
    [Fact]
    public void AStaticRedirectLoopThrowsAfterTooManyHopsInsteadOfOverflowingTheStack()
    {
        StaThread.Run(() =>
        {
            var hops = 0;
            var router = new Router([
                new RouteRecord { Path = "/a", Redirect = _ => ++hops < 200 ? "/b" : "/end" },
                new RouteRecord { Path = "/b", Redirect = _ => ++hops < 200 ? "/a" : "/end" },
                new RouteRecord { Path = "/end", Name = "end", View = typeof(RecordingViewA) },
            ]);
            var afterEachCalled = false;
            using var afterEach = router.AfterEach((_, _, _) => afterEachCalled = true);

            var exception = Assert.Throws<InvalidOperationException>(() => router.Start("/a").GetAwaiter().GetResult());

            Assert.StartsWith("Infinite redirect loop detected", exception.Message);
            Assert.Equal(30, hops);
            Assert.Same(RouteLocation.Start, router.CurrentRoute.Value);
            Assert.False(afterEachCalled);
        });
    }

    /// <summary>The hop limit counts a loop, not ordinary use: a short chain of static redirects still lands on its target.</summary>
    [Fact]
    public void AChainOfStaticRedirectsWithinTheLimitStillResolves()
    {
        StaThread.Run(() =>
        {
            var router = new Router([
                new RouteRecord { Path = "/a", Redirect = _ => "/b" },
                new RouteRecord { Path = "/b", Redirect = _ => "/c" },
                new RouteRecord { Path = "/c", Redirect = _ => "/end" },
                new RouteRecord { Path = "/end", Name = "end", View = typeof(RecordingViewA) },
            ]);

            Assert.Null(router.Start("/a").GetAwaiter().GetResult());

            Assert.Equal("end", router.CurrentRoute.Value.Name);
        });
    }

    [Fact]
    public void NavigatingToTheCurrentLocationAgainReturnsDuplicatedWithoutRunningGuards()
    {
        StaThread.Run(() =>
        {
            var router = SimpleRouter();
            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            var guardRan = false;
            using var beforeEach = router.BeforeEach((_, _) =>
            {
                guardRan = true;
                return Task.FromResult(NavigationGuardResult.Continue);
            });

            var failure = router.Push("/").GetAwaiter().GetResult();

            Assert.NotNull(failure);
            Assert.Equal(NavigationFailureType.Duplicated, failure.Type);
            Assert.False(guardRan);
        });
    }

    [Fact]
    public void ForceBypassesTheDuplicateCheck()
    {
        StaThread.Run(() =>
        {
            var router = SimpleRouter();
            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            var guardRan = false;
            using var beforeEach = router.BeforeEach((_, _) =>
            {
                guardRan = true;
                return Task.FromResult(NavigationGuardResult.Continue);
            });

            var failure = router.Push(new RouteLocationRaw { Path = "/", Force = true }).GetAwaiter().GetResult();

            Assert.Null(failure);
            Assert.True(guardRan);
        });
    }

    [Fact]
    public void GoBackAndForwardNavigateThroughHistory()
    {
        StaThread.Run(() =>
        {
            var router = SimpleRouter();
            Assert.Null(router.Start("/").GetAwaiter().GetResult());
            Assert.Null(router.Push("/other").GetAwaiter().GetResult());

            Assert.Null(router.Back().GetAwaiter().GetResult());
            Assert.Equal("home", router.CurrentRoute.Value.Name);

            Assert.Null(router.Forward().GetAwaiter().GetResult());
            Assert.Equal("other", router.CurrentRoute.Value.Name);
        });
    }

    [Fact]
    public void GoOutOfRangeIsASilentNoOp()
    {
        StaThread.Run(() =>
        {
            var router = SimpleRouter();
            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            var failure = router.Go(-100).GetAwaiter().GetResult();

            Assert.Null(failure);
            Assert.Equal("home", router.CurrentRoute.Value.Name);
        });
    }

    [Fact]
    public void AfterEachReceivesTheFailureOnAnAbortedNavigationToo()
    {
        StaThread.Run(() =>
        {
            var router = SimpleRouter();
            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            using var beforeEach = router.BeforeEach((_, _) => Task.FromResult<NavigationGuardResult>(false));

            NavigationFailure? observed = null;
            var afterEachCalled = false;
            using var afterEach = router.AfterEach((_, _, failure) =>
            {
                afterEachCalled = true;
                observed = failure;
            });

            router.Push("/other").GetAwaiter().GetResult();

            // AfterEach fires "regardless of outcome" (matching Vue Router's own real behavior -
            // its docs show detecting a failure INSIDE an AfterEach hook as the canonical global
            // idiom), not only on a successfully committed navigation.
            Assert.True(afterEachCalled);
            Assert.NotNull(observed);
            Assert.Equal(NavigationFailureType.Aborted, observed.Type);
        });
    }

    [Fact]
    public void AfterEachReceivesNullFailureOnASuccessfulNavigation()
    {
        StaThread.Run(() =>
        {
            var router = SimpleRouter();
            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            // Seeded with a non-null sentinel so the assertion below actually proves AfterEach
            // was invoked with null, rather than just never being invoked at all.
            NavigationFailure? observed = new() { Type = NavigationFailureType.Aborted, To = router.CurrentRoute.Value, From = router.CurrentRoute.Value };
            using var afterEach = router.AfterEach((_, _, failure) => observed = failure);

            Assert.Null(router.Push("/other").GetAwaiter().GetResult());

            Assert.Null(observed);
        });
    }

    [Fact]
    public void AfterEachReceivesTheDuplicatedFailure()
    {
        StaThread.Run(() =>
        {
            var router = SimpleRouter();
            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            NavigationFailure? observed = null;
            using var afterEach = router.AfterEach((_, _, failure) => observed = failure);

            var failure = router.Push("/").GetAwaiter().GetResult();

            Assert.NotNull(failure);
            Assert.Same(failure, observed);
        });
    }

    /// <summary>
    /// Entering Views are constructed <i>before</i> <see cref="IOnBeforeRouteEnter"/>/
    /// <c>BeforeResolve</c> run (see <see cref="Router"/>'s own remarks on why), so a guard that
    /// then cancels must not leave those never-mounted instances behind in
    /// <see cref="RouteRecord.Instances"/> - Vue Router never creates the component at all in
    /// this case, and a leftover instance here would be silently reused by the next attempt.
    /// </summary>
    [Fact]
    public void ACancelledNavigationLeavesNoConstructedViewInstancesBehind()
    {
        StaThread.Run(() =>
        {
            var dashboard = new RouteRecord { Path = "/dashboard", Name = "dashboard", View = typeof(RecordingViewA) };
            var router = new Router([new RouteRecord { Path = "/", Name = "home", View = typeof(RecordingViewB) }, dashboard]);
            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            using var guard = router.BeforeResolve((_, _) => Task.FromResult(NavigationGuardResult.Cancel));

            var failure = router.Push("/dashboard").GetAwaiter().GetResult();

            Assert.NotNull(failure);
            Assert.Equal(NavigationFailureType.Aborted, failure.Type);
            Assert.Empty(dashboard.Instances);
        });
    }

    [Fact]
    public void ARedirectedNavigationLeavesNoConstructedViewInstancesBehind()
    {
        StaThread.Run(() =>
        {
            var dashboard = new RouteRecord { Path = "/dashboard", Name = "dashboard", View = typeof(RecordingViewA) };
            var router = new Router([
                new RouteRecord { Path = "/", Name = "home", View = typeof(RecordingViewB) },
                new RouteRecord { Path = "/login", Name = "login", View = typeof(RecordingViewC) },
                dashboard,
            ]);
            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            using var guard = router.BeforeResolve((to, _) =>
                Task.FromResult(to.Name == "dashboard" ? NavigationGuardResult.Redirect("/login") : NavigationGuardResult.Continue));

            Assert.Null(router.Push("/dashboard").GetAwaiter().GetResult());

            Assert.Equal("/login", router.CurrentRoute.Value.Path);
            Assert.Empty(dashboard.Instances);
        });
    }

    /// <summary>The observable consequence of the leak above: a retried navigation must build a genuinely fresh View, not silently reuse the aborted attempt's instance.</summary>
    [Fact]
    public void RetryingAfterACancelledNavigationConstructsAFreshView()
    {
        StaThread.Run(() =>
        {
            var dashboard = new RouteRecord { Path = "/dashboard", Name = "dashboard", View = typeof(RecordingViewA) };
            var router = new Router([new RouteRecord { Path = "/", Name = "home", View = typeof(RecordingViewB) }, dashboard]);
            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            object? abortedAttemptView = null;
            var shouldCancel = true;
            using var guard = router.BeforeResolve((_, _) =>
            {
                if (shouldCancel && dashboard.Instances.TryGetValue("default", out var pending))
                    abortedAttemptView = pending;
                return Task.FromResult(shouldCancel ? NavigationGuardResult.Cancel : NavigationGuardResult.Continue);
            });

            Assert.NotNull(router.Push("/dashboard").GetAwaiter().GetResult());
            Assert.NotNull(abortedAttemptView);

            shouldCancel = false;
            Assert.Null(router.Push("/dashboard").GetAwaiter().GetResult());

            Assert.NotSame(abortedAttemptView, dashboard.Instances["default"]);
        });
    }

    /// <summary>
    /// The throwing counterpart of the cancel/redirect tests above. A guard that throws is not a
    /// <see cref="NavigationFailure"/> - the exception leaves <c>Push</c> as-is - but the
    /// navigation never committed all the same, so the Views it had already built must go too.
    /// </summary>
    [Fact]
    public void ANavigationWhoseGuardThrowsLeavesNoConstructedViewInstancesBehind()
    {
        StaThread.Run(() =>
        {
            var dashboard = new RouteRecord { Path = "/dashboard", Name = "dashboard", View = typeof(RecordingViewA) };
            var router = new Router([new RouteRecord { Path = "/", Name = "home", View = typeof(RecordingViewB) }, dashboard]);
            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            using var guard = router.BeforeResolve((_, _) => throw new InvalidOperationException("Guard failed."));

            var exception = Assert.Throws<InvalidOperationException>(() => router.Push("/dashboard").GetAwaiter().GetResult());

            Assert.Equal("Guard failed.", exception.Message);
            Assert.Empty(dashboard.Instances);
            Assert.Equal("home", router.CurrentRoute.Value.Name);
        });
    }

    /// <summary>The observable consequence: after a throwing guard is fixed, the retry gets a fresh View, not the failed attempt's.</summary>
    [Fact]
    public void RetryingAfterAThrowingGuardConstructsAFreshView()
    {
        StaThread.Run(() =>
        {
            var dashboard = new RouteRecord { Path = "/dashboard", Name = "dashboard", View = typeof(RecordingViewA) };
            var router = new Router([new RouteRecord { Path = "/", Name = "home", View = typeof(RecordingViewB) }, dashboard]);
            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            object? failedAttemptView = null;
            var shouldThrow = true;
            using var guard = router.BeforeResolve((_, _) =>
            {
                if (!shouldThrow)
                    return Task.FromResult(NavigationGuardResult.Continue);

                failedAttemptView = dashboard.Instances["default"];
                throw new InvalidOperationException("Guard failed.");
            });

            Assert.Throws<InvalidOperationException>(() => router.Push("/dashboard").GetAwaiter().GetResult());
            Assert.NotNull(failedAttemptView);

            shouldThrow = false;
            Assert.Null(router.Push("/dashboard").GetAwaiter().GetResult());

            Assert.NotSame(failedAttemptView, dashboard.Instances["default"]);
        });
    }

    /// <summary>An in-component enter guard runs after its own View was built, so its throw has a View to clean up after too.</summary>
    [Fact]
    public void AnEnterGuardThatThrowsLeavesNoConstructedViewInstancesBehind()
    {
        StaThread.Run(() =>
        {
            var guarded = new RouteRecord
            {
                Path = "/guarded", Name = "guarded",
                View = typeof(RecordingViewA),
                ViewModel = typeof(ThrowingEnterGuardViewModel),
            };
            var router = new Router([new RouteRecord { Path = "/", Name = "home", View = typeof(RecordingViewB) }, guarded]);
            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            var exception = Assert.Throws<InvalidOperationException>(() => router.Push("/guarded").GetAwaiter().GetResult());

            Assert.Equal("Enter guard failed.", exception.Message);
            Assert.Empty(guarded.Instances);
        });
    }

    /// <summary>
    /// Entering Views are built root first, so a child View whose constructor throws fails the
    /// navigation after its parent's View already exists - that one has to be released as well.
    /// </summary>
    [Fact]
    public void AViewConstructorThatThrowsReleasesTheViewsBuiltBeforeIt()
    {
        StaThread.Run(() =>
        {
            var parent = new RouteRecord
            {
                Path = "/parent", Name = "parent", View = typeof(RecordingViewA),
                Children = [new RouteRecord { Path = "child", Name = "child", View = typeof(ThrowingConstructorView) }],
            };
            var router = new Router([new RouteRecord { Path = "/", Name = "home", View = typeof(RecordingViewB) }, parent]);
            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            // Activator.CreateInstance wraps a constructor's exception, and a container may wrap
            // it differently - which exception arrives is not the point here.
            Assert.ThrowsAny<Exception>(() => router.Push("/parent/child").GetAwaiter().GetResult());

            Assert.Empty(parent.Instances);
            Assert.Equal("home", router.CurrentRoute.Value.Name);
        });
    }
}
