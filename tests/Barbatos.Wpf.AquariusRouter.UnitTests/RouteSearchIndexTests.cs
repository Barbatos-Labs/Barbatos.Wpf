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

    /// <summary>
    /// Japanese dakuten/handakuten are combining marks, so blanket mark-stripping silently rewrites
    /// <c>だいがく</c> ("university") into <c>たいかく</c> ("physique") - and since the query is
    /// folded the same way, that never surfaces as "nothing found", only as unrelated results
    /// matching each other.
    /// </summary>
    [Fact]
    public void JapaneseVoicedKanaStayDistinctFromTheirUnvoicedForm()
    {
        var router = new Router([
            new RouteRecord { Path = "/university", Name = "university", View = StubView, Search = [new RouteSearchEntry { Title = "だいがく" }] },
            new RouteRecord { Path = "/physique", Name = "physique", View = StubView, Search = [new RouteSearchEntry { Title = "たいかく" }] },
        ]);
        var index = new RouteSearchIndex(router);

        Assert.Equal("university", index.Query("だいがく").Single().Record.Name);
        Assert.Equal("physique", index.Query("たいかく").Single().Record.Name);
    }

    [Fact]
    public void ThaiVowelSignsSurviveFolding()
    {
        var router = new Router([
            new RouteRecord { Path = "/transfer", Name = "transfer", View = StubView, Search = [new RouteSearchEntry { Title = "โอนเงิน" }] },
        ]);
        var index = new RouteSearchIndex(router);

        Assert.Single(index.Query("เงิน"));
        // The vowel-less spelling is a different string and must not match.
        Assert.Empty(index.Query("เงน"));
    }

    /// <summary>Cyrillic <c>й</c> decomposes to <c>и</c> + breve, but they are distinct letters.</summary>
    [Fact]
    public void CyrillicShortIStaysDistinctFromI()
    {
        var router = new Router([
            new RouteRecord { Path = "/yogurt", Name = "yogurt", View = StubView, Search = [new RouteSearchEntry { Title = "йогурт" }] },
        ]);
        var index = new RouteSearchIndex(router);

        Assert.Single(index.Query("йогурт"));
        Assert.Empty(index.Query("иогурт"));
    }

    [Fact]
    public void NonLatinScriptsWithoutCombiningMarksAreUnaffected()
    {
        var router = new Router([
            new RouteRecord { Path = "/ko", Name = "ko", View = StubView, Search = [new RouteSearchEntry { Title = "계좌이체" }] },
            new RouteRecord { Path = "/zh", Name = "zh", View = StubView, Search = [new RouteSearchEntry { Title = "转账" }] },
        ]);
        var index = new RouteSearchIndex(router);

        Assert.Equal("ko", index.Query("계좌").Single().Record.Name);
        Assert.Equal("zh", index.Query("转账").Single().Record.Name);
    }

    [Fact]
    public void TextResolverLocalizesTheSearchableTitle()
    {
        var router = new Router([
            new RouteRecord { Path = "/transfer", Name = "transfer", View = StubView, Search = [new RouteSearchEntry { Title = "nav.transfer" }] },
        ]);
        var vietnamese = new Dictionary<string, string> { ["nav.transfer"] = "Chuyển tiền" };
        var index = new RouteSearchIndex(router) { TextResolver = key => vietnamese.GetValueOrDefault(key, key) };

        var result = index.Query("chuyen tien").Single();

        Assert.Equal("transfer", result.Record.Name);
        Assert.Equal("Chuyển tiền", result.Title);
        Assert.Equal("nav.transfer", result.Entry.Title);
    }

    /// <summary>A route table is built once at startup, so the language a query searches in has to come from the resolver, live - not from whatever was current when the table was built.</summary>
    [Fact]
    public void SwitchingLanguageTakesEffectOnTheVeryNextQuery()
    {
        var router = new Router([
            new RouteRecord { Path = "/transfer", Name = "transfer", View = StubView, Search = [new RouteSearchEntry { Title = "nav.transfer" }] },
        ]);

        var language = "vi";
        var strings = new Dictionary<string, Dictionary<string, string>>
        {
            ["vi"] = new() { ["nav.transfer"] = "Chuyển tiền" },
            ["en"] = new() { ["nav.transfer"] = "Transfer money" },
        };
        var index = new RouteSearchIndex(router) { TextResolver = key => strings[language].GetValueOrDefault(key, key) };

        Assert.Single(index.Query("chuyen tien"));
        Assert.Empty(index.Query("transfer"));

        language = "en";

        Assert.Empty(index.Query("chuyen tien"));
        Assert.Equal("Transfer money", index.Query("transfer").Single().Title);

        // ...and switching back still works (the earlier language is simply still cached).
        language = "vi";
        Assert.Equal("Chuyển tiền", index.Query("chuyen").Single().Title);
    }

    [Fact]
    public void TextResolverAlsoAppliesToKeywords()
    {
        var router = new Router([
            new RouteRecord
            {
                Path = "/security", Name = "security", View = StubView,
                Search = [new RouteSearchEntry { Title = "nav.security", Keywords = "nav.security.keywords" }],
            },
        ]);
        var strings = new Dictionary<string, string>
        {
            ["nav.security"] = "Bảo mật",
            ["nav.security.keywords"] = "đổi mật khẩu",
        };
        var index = new RouteSearchIndex(router) { TextResolver = key => strings.GetValueOrDefault(key, key) };

        Assert.Single(index.Query("doi mat khau"));
    }

    [Fact]
    public void ResultTitleIsTheDeclaredTitleWhenNoResolverIsSet()
    {
        var router = new Router([
            new RouteRecord { Path = "/about", Name = "about", View = StubView, Search = [new RouteSearchEntry { Title = "About" }] },
        ]);

        Assert.Equal("About", new RouteSearchIndex(router).Query("about").Single().Title);
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
