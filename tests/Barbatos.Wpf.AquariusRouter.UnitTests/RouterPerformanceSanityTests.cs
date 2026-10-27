// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Diagnostics;

namespace Barbatos.Wpf.AquariusRouter.UnitTests;

/// <summary>
/// Not a micro-benchmark - exact timing assertions are flaky across Debug/Release and different
/// CI hardware. A coarse sanity check that resolving/navigating against a realistically large
/// route table stays comfortably fast, so a regression that accidentally turned <see cref="RouteMatcher.Resolve"/>
/// or the guard pipeline quadratic (e.g. re-sorting the matcher list per <c>Resolve</c> instead of
/// once at <c>AddRoute</c> time, or recompiling a path's <see cref="System.Text.RegularExpressions.Regex"/>
/// on every resolve instead of once when the route is registered) would fail this test outright
/// instead of only showing up later as "navigation feels slow".
/// </summary>
public class RouterPerformanceSanityTests
{
    [Fact]
    public void ResolvingAgainstFiveHundredRoutesStaysFast()
    {
        var routes = new List<RouteRecord>();
        for (var i = 0; i < 500; i++)
            routes.Add(new RouteRecord { Path = $"/section-{i}/:id", Name = $"section-{i}" });

        var matcher = new RouteMatcher(routes);

        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < 200; i++)
            // The worst case for a linear score-sorted scan: the LAST-registered route, so every
            // resolve has to walk past all 499 others first.
            matcher.Resolve(new RouteLocationRaw { Path = "/section-499/abc" });
        stopwatch.Stop();

        Assert.True(
            stopwatch.ElapsedMilliseconds < 3000,
            $"200 resolves against a 500-route table took {stopwatch.ElapsedMilliseconds}ms - expected well under 3000ms.");
    }

    [Fact]
    public void NavigatingBackAndForthAThousandTimesThroughAFullGuardPipelineStaysFast()
    {
        StaThread.Run(() =>
        {
            var router = new Router([
                new RouteRecord { Path = "/", Name = "home", View = typeof(RecordingViewA) },
                new RouteRecord { Path = "/other/:id", Name = "other", View = typeof(RecordingViewB) },
            ]);

            using var beforeEach = router.BeforeEach((_, _) => Task.FromResult(NavigationGuardResult.Continue));
            using var beforeResolve = router.BeforeResolve((_, _) => Task.FromResult(NavigationGuardResult.Continue));
            using var afterEach = router.AfterEach((_, _, _) => { });

            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            var stopwatch = Stopwatch.StartNew();
            for (var i = 0; i < 500; i++)
            {
                router.Push($"/other/{i}").GetAwaiter().GetResult();
                router.Push("/").GetAwaiter().GetResult();
            }
            stopwatch.Stop();

            Assert.True(
                stopwatch.ElapsedMilliseconds < 3000,
                $"1000 full navigations (leave+beforeEach+enter+beforeResolve+afterEach each) took {stopwatch.ElapsedMilliseconds}ms - expected well under 3000ms.");
        });
    }
}
