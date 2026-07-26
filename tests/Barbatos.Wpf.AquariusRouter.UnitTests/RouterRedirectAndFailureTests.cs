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
}
