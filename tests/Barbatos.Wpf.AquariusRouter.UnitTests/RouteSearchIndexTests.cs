namespace Barbatos.Wpf.AquariusRouter.UnitTests;

public class RouteSearchIndexTests
{
    private static readonly Type StubView = typeof(object);

    [Fact]
    public void MatchesByTitleSubstring()
    {
        var router = new Router([
            new RouteRecord
            {
                Path = "/settings/security", Name = "settings-security", View = StubView,
                Search = [new RouteSearchEntry { Title = "Security settings" }],
            },
        ]);

        var results = new RouteSearchIndex(router).Query("security");
        Assert.Single(results);
        Assert.Equal("settings-security", results[0].Record.Name);
    }

    [Fact]
    public void MatchesByKeywordsEvenWhenNotInTitle()
    {
        var router = new Router([
            new RouteRecord
            {
                Path = "/settings/security", Name = "settings-security", View = StubView,
                Search = [new RouteSearchEntry { Title = "Security settings", Keywords = "change password" }],
            },
        ]);

        var results = new RouteSearchIndex(router).Query("password");
        Assert.Single(results);
    }

    [Fact]
    public void MatchIsCaseInsensitive()
    {
        var router = new Router([
            new RouteRecord { Path = "/about", Name = "about", View = StubView, Search = [new RouteSearchEntry { Title = "About" }] },
        ]);

        Assert.Single(new RouteSearchIndex(router).Query("ABOUT"));
    }

    [Fact]
    public void MatchIsDiacriticInsensitive()
    {
        var router = new Router([
            new RouteRecord { Path = "/transfer", Name = "transfer", View = StubView, Search = [new RouteSearchEntry { Title = "Chuyển tiền" }] },
        ]);

        Assert.Single(new RouteSearchIndex(router).Query("chuyen tien"));
    }

    [Fact]
    public void MatchFoldsTheVietnameseDStroke()
    {
        var router = new Router([
            new RouteRecord { Path = "/login", Name = "login", View = StubView, Search = [new RouteSearchEntry { Title = "Đăng nhập" }] },
        ]);

        Assert.Single(new RouteSearchIndex(router).Query("dang nhap"));
    }

    [Fact]
    public void MultiTermQueryRequiresEveryTermToMatch()
    {
        var router = new Router([
            new RouteRecord { Path = "/transfer", Name = "transfer", View = StubView, Search = [new RouteSearchEntry { Title = "Chuyển tiền nhanh" }] },
        ]);
        var index = new RouteSearchIndex(router);

        Assert.Single(index.Query("tien nhanh"));
        Assert.Empty(index.Query("tien xyz"));
    }

    [Fact]
    public void MultiTermQueryCanSpanTitleAndKeywordsCombined()
    {
        var router = new Router([
            new RouteRecord
            {
                Path = "/settings/security", Name = "settings-security", View = StubView,
                Search = [new RouteSearchEntry { Title = "Bảo mật", Keywords = "đổi mật khẩu" }],
            },
        ]);

        // "bao" only appears in the title, "doi" only in the keywords - neither field alone has both.
        Assert.Single(new RouteSearchIndex(router).Query("bao doi"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankOrNullQueryReturnsNoResults(string? query)
    {
        var router = new Router([
            new RouteRecord { Path = "/about", Name = "about", View = StubView, Search = [new RouteSearchEntry { Title = "About" }] },
        ]);

        Assert.Empty(new RouteSearchIndex(router).Query(query));
    }

    [Fact]
    public void ExactTitleMatchRanksAboveAPartialOne()
    {
        var router = new Router([
            new RouteRecord { Path = "/about", Name = "about", View = StubView, Search = [new RouteSearchEntry { Title = "About us" }] },
            new RouteRecord { Path = "/", Name = "home", View = StubView, Search = [new RouteSearchEntry { Title = "About" }] },
        ]);

        var results = new RouteSearchIndex(router).Query("about");
        Assert.Equal(2, results.Count);
        Assert.Equal("home", results[0].Record.Name);
    }

    [Fact]
    public void TitleMatchRanksAboveAKeywordsOnlyMatch()
    {
        var router = new Router([
            new RouteRecord
            {
                Path = "/settings/notifications", Name = "settings-notifications", View = StubView,
                Search = [new RouteSearchEntry { Title = "Alerts", Keywords = "notifications" }],
            },
            new RouteRecord
            {
                Path = "/settings/inbox", Name = "settings-inbox", View = StubView,
                Search = [new RouteSearchEntry { Title = "Notifications" }],
            },
        ]);

        var results = new RouteSearchIndex(router).Query("notifications");
        Assert.Equal(2, results.Count);
        Assert.Equal("settings-inbox", results[0].Record.Name);
    }

    [Fact]
    public void NestedChildRoutesAreSearchable()
    {
        var router = new Router([
            new RouteRecord
            {
                Path = "/settings", Name = "settings", View = StubView,
                Children = [
                    new RouteRecord { Path = "security", Name = "settings-security", View = StubView, Search = [new RouteSearchEntry { Title = "Security" }] },
                ],
            },
        ]);

        var results = new RouteSearchIndex(router).Query("security");
        Assert.Single(results);
        Assert.Equal("settings-security", results[0].Record.Name);
    }

    [Fact]
    public void RoutesWithNoSearchEntriesAreNeverReturned()
    {
        var router = new Router([
            new RouteRecord { Path = "/users/:id", Name = "user", View = StubView },
        ]);

        Assert.Empty(new RouteSearchIndex(router).Query("user"));
    }

    [Fact]
    public void MultipleEntriesOnOneRecordCoverDifferentPhrasings()
    {
        var router = new Router([
            new RouteRecord
            {
                Path = "/transfer", Name = "transfer", View = StubView,
                Search =
                [
                    new RouteSearchEntry { Title = "Chuyển tiền nhanh" },
                    new RouteSearchEntry { Title = "Chuyển khoản 24/7" },
                ],
            },
        ]);
        var index = new RouteSearchIndex(router);

        Assert.Single(index.Query("nhanh"));
        Assert.Single(index.Query("khoan"));
        Assert.Equal(2, index.Query("chuyen").Count);
    }

    [Fact]
    public void ResultToNavigatesByRecordName()
    {
        var router = new Router([
            new RouteRecord { Path = "/about", Name = "about", View = StubView, Search = [new RouteSearchEntry { Title = "About" }] },
        ]);

        var result = new RouteSearchIndex(router).Query("about").Single();
        Assert.Equal("about", result.To.Name);
    }

    [Fact]
    public void ResultToThrowsWhenTheRecordHasNoName()
    {
        var router = new Router([
            new RouteRecord { Path = "/about", View = StubView, Search = [new RouteSearchEntry { Title = "About" }] },
        ]);

        var result = new RouteSearchIndex(router).Query("about").Single();
        Assert.Throws<InvalidOperationException>(() => result.To);
    }

    [Fact]
    public void QueryReflectsRoutesAddedAfterConstruction()
    {
        var router = new Router([
            new RouteRecord { Path = "/", Name = "home", View = StubView },
        ]);
        var index = new RouteSearchIndex(router);
        Assert.Empty(index.Query("about"));

        router.AddRoute(new RouteRecord { Path = "/about", Name = "about", View = StubView, Search = [new RouteSearchEntry { Title = "About" }] });

        Assert.Single(index.Query("about"));
    }
}
