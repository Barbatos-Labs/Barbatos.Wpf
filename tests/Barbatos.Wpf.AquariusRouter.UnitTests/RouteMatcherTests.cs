namespace Barbatos.Wpf.AquariusRouter.UnitTests;

public class RouteMatcherTests
{
    private static readonly Type StubView = typeof(object);

    [Fact]
    public void ResolvesAPlainStaticPath()
    {
        var matcher = new RouteMatcher([
            new RouteRecord { Path = "/", Name = "home", View = StubView },
            new RouteRecord { Path = "/about", Name = "about", View = StubView },
        ]);

        var location = matcher.Resolve(new RouteLocationRaw { Path = "/about" });
        Assert.Equal("about", location.Name);
        Assert.Single(location.Matched);
    }

    [Fact]
    public void ResolvingAnUnknownPathThrows()
    {
        var matcher = new RouteMatcher([new RouteRecord { Path = "/", View = StubView }]);
        Assert.Throws<InvalidOperationException>(() => matcher.Resolve(new RouteLocationRaw { Path = "/nowhere" }));
    }

    [Fact]
    public void ResolvesByNameAndSubstitutesParams()
    {
        var matcher = new RouteMatcher([new RouteRecord { Path = "/users/:id", Name = "user", View = StubView }]);

        var location = matcher.Resolve(new RouteLocationRaw
        {
            Name = "user",
            Params = new Dictionary<string, object?> { ["id"] = "42" },
        });

        Assert.Equal("/users/42", location.Path);
        Assert.Equal("42", location.Params["id"]);
    }

    [Fact]
    public void ResolvingAnUnknownNameThrows()
    {
        var matcher = new RouteMatcher([new RouteRecord { Path = "/", View = StubView }]);
        Assert.Throws<InvalidOperationException>(() => matcher.Resolve(new RouteLocationRaw { Name = "nowhere" }));
    }

    [Fact]
    public void RelativeNavigationReResolvesTheCurrentPathWithMergedParams()
    {
        var matcher = new RouteMatcher([new RouteRecord { Path = "/search", Name = "search", View = StubView }]);

        var current = matcher.Resolve(new RouteLocationRaw { Path = "/search" }) with
        {
            Query = new Dictionary<string, string?> { ["q"] = "old" },
        };

        var location = matcher.Resolve(new RouteLocationRaw(), current);
        Assert.Equal("/search", location.Path);
    }

    [Fact]
    public void NestedChildPathJoinsOntoTheParentsPath()
    {
        var matcher = new RouteMatcher([
            new RouteRecord
            {
                Path = "/users/:id",
                Name = "user",
                View = StubView,
                Children =
                [
                    new RouteRecord { Path = "profile", Name = "user-profile", View = StubView },
                ],
            },
        ]);

        var location = matcher.Resolve(new RouteLocationRaw { Path = "/users/42/profile" });
        Assert.Equal("user-profile", location.Name);
        Assert.Equal(2, location.Matched.Count);
        Assert.Equal("42", location.Params["id"]);
    }

    [Fact]
    public void AbsoluteChildPathIgnoresParentNesting()
    {
        var matcher = new RouteMatcher([
            new RouteRecord
            {
                Path = "/users",
                View = StubView,
                Children = [new RouteRecord { Path = "/top-level", Name = "top-level", View = StubView }],
            },
        ]);

        var location = matcher.Resolve(new RouteLocationRaw { Path = "/top-level" });
        Assert.Equal("top-level", location.Name);
    }

    [Fact]
    public void EmptyPathChildResolvesAtExactlyTheParentsPath()
    {
        var matcher = new RouteMatcher([
            new RouteRecord
            {
                Path = "/users/:id",
                View = StubView,
                Children = [new RouteRecord { Path = "", Name = "user-home", View = StubView }],
            },
        ]);

        var location = matcher.Resolve(new RouteLocationRaw { Path = "/users/42" });
        Assert.Equal("user-home", location.Name);
        Assert.Equal(2, location.Matched.Count);
    }

    [Fact]
    public void AParentWithNoViewIsAPureGroupingRecordNotDirectlyMatchable()
    {
        var matcher = new RouteMatcher([
            new RouteRecord
            {
                Path = "/admin",
                Children = [new RouteRecord { Path = "users", Name = "admin-users", View = StubView }],
            },
        ]);

        Assert.Throws<InvalidOperationException>(() => matcher.Resolve(new RouteLocationRaw { Path = "/admin" }));

        var location = matcher.Resolve(new RouteLocationRaw { Path = "/admin/users" });
        Assert.Equal(2, location.Matched.Count);
        Assert.Null(location.Matched[0].Name);
        Assert.Equal("admin-users", location.Matched[1].Name);
    }

    [Fact]
    public void NamedViewsPopulateTheOutletMap()
    {
        var record = new RouteRecord
        {
            Path = "/",
            Views = new Dictionary<string, Type> { ["default"] = StubView, ["sidebar"] = StubView },
        };
        var matcher = new RouteMatcher([record]);

        var location = matcher.Resolve(new RouteLocationRaw { Path = "/" });
        Assert.Equal(2, location.Matched[0].Views!.Count);
    }

    [Fact]
    public void StaticSiblingOutranksADynamicOneAtTheSamePosition()
    {
        var matcher = new RouteMatcher([
            new RouteRecord { Path = "/users/:id", Name = "user", View = StubView },
            new RouteRecord { Path = "/users/new", Name = "new-user", View = StubView },
        ]);

        var location = matcher.Resolve(new RouteLocationRaw { Path = "/users/new" });
        Assert.Equal("new-user", location.Name);
    }

    [Fact]
    public void CatchAllOnlyMatchesWhenNothingElseDoes()
    {
        var matcher = new RouteMatcher([
            new RouteRecord { Path = "/", Name = "home", View = StubView },
            new RouteRecord { Path = "/:pathMatch(.*)*", Name = "not-found", View = StubView },
        ]);

        Assert.Equal("home", matcher.Resolve(new RouteLocationRaw { Path = "/" }).Name);
        Assert.Equal("not-found", matcher.Resolve(new RouteLocationRaw { Path = "/anything/else" }).Name);
    }

    [Fact]
    public void StaticRedirectRecordIsExcludedFromMatchedButStillMatchable()
    {
        var matcher = new RouteMatcher([
            new RouteRecord { Path = "/old", Redirect = _ => "/new" },
            new RouteRecord { Path = "/new", Name = "new", View = StubView },
        ]);

        // The matcher itself doesn't follow redirects (that's Router's job) - it just needs to
        // resolve the redirecting record itself as a real, matchable location.
        var location = matcher.Resolve(new RouteLocationRaw { Path = "/old" });
        Assert.NotNull(location.Matched[^1].Redirect);
    }

    [Fact]
    public void AliasResolvesToTheSameRecordAsTheOriginal()
    {
        var record = new RouteRecord { Path = "/", Name = "home", View = StubView, Alias = ["/home", "/start"] };
        var matcher = new RouteMatcher([record]);

        var viaOriginal = matcher.Resolve(new RouteLocationRaw { Path = "/" });
        var viaAlias1 = matcher.Resolve(new RouteLocationRaw { Path = "/home" });
        var viaAlias2 = matcher.Resolve(new RouteLocationRaw { Path = "/start" });

        Assert.Same(viaOriginal.Matched[0], viaAlias1.Matched[0]);
        Assert.Same(viaOriginal.Matched[0], viaAlias2.Matched[0]);
        // Confirms they really do share runtime Instances state (this port's own deliberate
        // simplification vs. Vue Router's independent-per-alias clone - see RouteMatcher's docs).
        Assert.Same(record.Instances, viaAlias1.Matched[0].Instances);
    }

    [Fact]
    public void MetaMergesRootToLeafWithChildKeysWinning()
    {
        var matcher = new RouteMatcher([
            new RouteRecord
            {
                Path = "/admin",
                View = StubView,
                Meta = new Dictionary<string, object?> { ["requiresAuth"] = true, ["section"] = "parent" },
                Children =
                [
                    new RouteRecord
                    {
                        Path = "users",
                        Name = "admin-users",
                        View = StubView,
                        Meta = new Dictionary<string, object?> { ["section"] = "child" },
                    },
                ],
            },
        ]);

        var location = matcher.Resolve(new RouteLocationRaw { Path = "/admin/users" });
        Assert.Equal(true, location.Meta["requiresAuth"]);
        Assert.Equal("child", location.Meta["section"]);
    }

    [Fact]
    public void AddRouteWithParentNameAttachesADynamicChild()
    {
        var matcher = new RouteMatcher([new RouteRecord { Path = "/users", Name = "users", View = StubView }]);
        matcher.AddRoute("users", new RouteRecord { Path = ":id", Name = "user-detail", View = StubView });

        var location = matcher.Resolve(new RouteLocationRaw { Path = "/users/42" });
        Assert.Equal("user-detail", location.Name);
        Assert.Equal(2, location.Matched.Count);
    }

    [Fact]
    public void RemoveRouteCascadesToItsChildren()
    {
        var matcher = new RouteMatcher([
            new RouteRecord
            {
                Path = "/users",
                Name = "users",
                View = StubView,
                Children = [new RouteRecord { Path = ":id", Name = "user-detail", View = StubView }],
            },
        ]);

        matcher.RemoveRoute("users");

        Assert.False(matcher.HasRoute("users"));
        Assert.False(matcher.HasRoute("user-detail"));
        Assert.Throws<InvalidOperationException>(() => matcher.Resolve(new RouteLocationRaw { Path = "/users/42" }));
    }

    [Fact]
    public void RegisteringTheSameNameTwiceKeepsOnlyTheLatestOne()
    {
        var matcher = new RouteMatcher();
        matcher.AddRoute(new RouteRecord { Path = "/first", Name = "dup", View = StubView });
        matcher.AddRoute(new RouteRecord { Path = "/second", Name = "dup", View = StubView });

        var location = matcher.Resolve(new RouteLocationRaw { Name = "dup" });
        Assert.Equal("/second", location.Path);
    }

    [Fact]
    public void GetRoutesReturnsEveryDistinctRegisteredRecord()
    {
        var matcher = new RouteMatcher([
            new RouteRecord { Path = "/a", Name = "a", View = StubView },
            new RouteRecord { Path = "/b", Name = "b", View = StubView, Alias = ["/b-alias"] },
        ]);

        Assert.Equal(2, matcher.GetRoutes().Count);
    }
}
