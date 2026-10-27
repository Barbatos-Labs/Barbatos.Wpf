// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.AquariusRouter.UnitTests;

/// <summary>
/// Covers <see cref="Router.Go"/>/<see cref="Router.Back"/>/<see cref="Router.Forward"/> against
/// history entries that carry a query/hash - <see cref="IRouterHistory"/> stores a location as its
/// serialized <see cref="RouteLocation.FullPath"/>, so moving back to one means turning that
/// string back into a target, not matching it as if it were a bare path.
/// </summary>
public class RouterHistoryNavigationTests
{
    private static Router SearchRouter() => new([
        new RouteRecord { Path = "/", Name = "home", View = typeof(RecordingViewA) },
        new RouteRecord { Path = "/search", Name = "search", View = typeof(RecordingViewB) },
    ]);

    [Fact]
    public void GoingBackToAnEntryThatCarriedAQueryRestoresIt()
    {
        StaThread.Run(() =>
        {
            var router = SearchRouter();
            Assert.Null(router.Start("/").GetAwaiter().GetResult());
            Assert.Null(router.Push(new RouteLocationRaw
            {
                Path = "/search",
                Query = new Dictionary<string, string?> { ["q"] = "hello world" },
            }).GetAwaiter().GetResult());
            Assert.Null(router.Push("/").GetAwaiter().GetResult());

            Assert.Null(router.Back().GetAwaiter().GetResult());

            Assert.Equal("/search", router.CurrentRoute.Value.Path);
            Assert.Equal("hello world", router.CurrentRoute.Value.Query["q"]);
        });
    }

    [Fact]
    public void GoingBackToAnEntryThatCarriedAHashRestoresIt()
    {
        StaThread.Run(() =>
        {
            var router = SearchRouter();
            Assert.Null(router.Start("/").GetAwaiter().GetResult());
            Assert.Null(router.Push(new RouteLocationRaw { Path = "/search", Hash = "results" }).GetAwaiter().GetResult());
            Assert.Null(router.Push("/").GetAwaiter().GetResult());

            Assert.Null(router.Back().GetAwaiter().GetResult());

            Assert.Equal("results", router.CurrentRoute.Value.Hash);
        });
    }

    [Fact]
    public void ForwardReturnsToTheQueryCarryingEntryToo()
    {
        StaThread.Run(() =>
        {
            var router = SearchRouter();
            Assert.Null(router.Start("/").GetAwaiter().GetResult());
            Assert.Null(router.Push(new RouteLocationRaw
            {
                Path = "/search",
                Query = new Dictionary<string, string?> { ["q"] = "hello" },
            }).GetAwaiter().GetResult());

            Assert.Null(router.Back().GetAwaiter().GetResult());
            Assert.Equal("/", router.CurrentRoute.Value.Path);

            Assert.Null(router.Forward().GetAwaiter().GetResult());

            Assert.Equal("/search", router.CurrentRoute.Value.Path);
            Assert.Equal("hello", router.CurrentRoute.Value.Query["q"]);
        });
    }

    [Fact]
    public void ParseSplitsAFullPathIntoItsParts()
    {
        var raw = RouteLocationRaw.Parse("/search?q=hello%20world&page=2#results");

        Assert.Equal("/search", raw.Path);
        Assert.Equal("hello world", raw.Query!["q"]);
        Assert.Equal("2", raw.Query["page"]);
        Assert.Equal("results", raw.Hash);
    }

    [Fact]
    public void ParseLeavesQueryAndHashNullWhenThePathCarriesNeither()
    {
        var raw = RouteLocationRaw.Parse("/about");

        Assert.Equal("/about", raw.Path);
        Assert.Null(raw.Query);
        Assert.Null(raw.Hash);
    }

    /// <summary>A valueless query key round-trips back to <see langword="null"/>, not <c>""</c> - the two stay distinguishable.</summary>
    [Fact]
    public void ParseKeepsAValuelessQueryKeyNull()
    {
        var raw = RouteLocationRaw.Parse("/search?debug");

        Assert.True(raw.Query!.ContainsKey("debug"));
        Assert.Null(raw.Query["debug"]);
    }

    [Fact]
    public void ParseHandlesAHashWithNoQuery()
    {
        var raw = RouteLocationRaw.Parse("/docs#section-2");

        Assert.Equal("/docs", raw.Path);
        Assert.Null(raw.Query);
        Assert.Equal("section-2", raw.Hash);
    }

    [Fact]
    public void AQueryValueContainingReservedCharactersSurvivesTheRoundTrip()
    {
        StaThread.Run(() =>
        {
            var router = SearchRouter();
            Assert.Null(router.Start("/").GetAwaiter().GetResult());
            Assert.Null(router.Push(new RouteLocationRaw
            {
                Path = "/search",
                Query = new Dictionary<string, string?> { ["q"] = "a&b=c?d#e/f" },
            }).GetAwaiter().GetResult());
            Assert.Null(router.Push("/").GetAwaiter().GetResult());

            Assert.Null(router.Back().GetAwaiter().GetResult());

            Assert.Equal("a&b=c?d#e/f", router.CurrentRoute.Value.Query["q"]);
        });
    }
}
