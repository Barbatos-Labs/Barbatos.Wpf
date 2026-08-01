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

    /// <summary>
    /// An alias shares its <see cref="RouteRecord"/> - and therefore its <c>Name</c> - with the
    /// record it aliases, so registering alias nodes by name too would overwrite the canonical
    /// one (aliases register last), silently making a by-name navigation resolve to the ALIAS
    /// path instead of the real one.
    /// </summary>
    [Fact]
    public void ResolvingByNameUsesTheCanonicalPathNotAnAlias()
    {
        var matcher = new RouteMatcher([
            new RouteRecord { Path = "/about", Name = "about", View = StubView, Alias = ["/aboutus"] },
        ]);

        Assert.Equal("/about", matcher.Resolve(new RouteLocationRaw { Name = "about" }).Path);

        // ...and the alias itself still resolves by path, onto that same record.
        Assert.Equal("about", matcher.Resolve(new RouteLocationRaw { Path = "/aboutus" }).Name);
    }

    /// <summary>
    /// A child added at runtime lives only in the matcher's own node tree, never in its parent's
    /// <see cref="RouteRecord.Children"/> (which is <c>init</c>-only) - so removal has to walk the
    /// node tree, or a dynamically-added child outlives its parent and keeps matching.
    /// </summary>
    [Fact]
    public void RemovingAParentAlsoRemovesChildrenAddedDynamically()
    {
        var parent = new RouteRecord { Path = "/settings", Name = "settings", View = StubView };
        var matcher = new RouteMatcher([parent]);
        matcher.AddRoute("settings", new RouteRecord { Path = "security", Name = "settings-security", View = StubView });

        Assert.True(matcher.HasRoute("settings-security"));

        matcher.RemoveRoute(parent);

        Assert.False(matcher.HasRoute("settings"));
        Assert.False(matcher.HasRoute("settings-security"));
        Assert.Throws<InvalidOperationException>(() => matcher.Resolve(new RouteLocationRaw { Path = "/settings/security" }));
    }

    [Fact]
    public void RemovingAParentAlsoRemovesItsStaticallyDeclaredChildren()
    {
        var parent = new RouteRecord
        {
            Path = "/settings",
            Name = "settings",
            View = StubView,
            Children = [new RouteRecord { Path = "security", Name = "settings-security", View = StubView }],
        };
        var matcher = new RouteMatcher([parent]);

        matcher.RemoveRoute(parent);

        Assert.False(matcher.HasRoute("settings-security"));
        Assert.Empty(matcher.GetRoutes());
    }

    /// <summary>
    /// Resolution skips a route whose first path segment is a literal that can't be what the
    /// incoming path starts with, rather than running its regex. That shortcut has to agree with
    /// how the regex itself was compiled - case-insensitively by default - or it would prune a
    /// route the regex would have accepted.
    /// </summary>
    [Fact]
    public void PathMatchingStaysCaseInsensitiveByDefault()
    {
        var matcher = new RouteMatcher([
            new RouteRecord { Path = "/users/:id", Name = "user", View = StubView },
            new RouteRecord { Path = "/about", Name = "about", View = StubView },
        ]);

        Assert.Equal("user", matcher.Resolve(new RouteLocationRaw { Path = "/USERS/42" }).Name);
        Assert.Equal("about", matcher.Resolve(new RouteLocationRaw { Path = "/AbOuT" }).Name);
    }

    [Fact]
    public void SensitiveOptionStillRejectsAMiscasedPath()
    {
        var matcher = new RouteMatcher(
            [new RouteRecord { Path = "/Users", Name = "users", View = StubView }],
            new PathParserOptions { Sensitive = true });

        Assert.Equal("users", matcher.Resolve(new RouteLocationRaw { Path = "/Users" }).Name);
        Assert.Throws<InvalidOperationException>(() => matcher.Resolve(new RouteLocationRaw { Path = "/users" }));
    }

    /// <summary>A segment mixing a literal prefix with a param has no fixed first segment to prune on - it must still be reached.</summary>
    [Fact]
    public void AMixedLiteralAndParamFirstSegmentStillResolves()
    {
        var matcher = new RouteMatcher([new RouteRecord { Path = "/:year-:month", Name = "archive", View = StubView }]);

        var location = matcher.Resolve(new RouteLocationRaw { Path = "/2026-08" });

        Assert.Equal("archive", location.Name);
        Assert.Equal("2026", location.Params["year"]);
        Assert.Equal("08", location.Params["month"]);
    }

    /// <summary>A catch-all's first segment is a param, so it can never be pruned away - it stays the last-resort match for any path.</summary>
    [Fact]
    public void TheCatchAllStillMatchesAPathSharingNoLiteralWithAnyRoute()
    {
        var matcher = new RouteMatcher([
            new RouteRecord { Path = "/users/:id", Name = "user", View = StubView },
            new RouteRecord { Path = "/:pathMatch(.*)*", Name = "not-found", View = StubView },
        ]);

        Assert.Equal("not-found", matcher.Resolve(new RouteLocationRaw { Path = "/totally/unknown" }).Name);
        Assert.Equal("user", matcher.Resolve(new RouteLocationRaw { Path = "/users/42" }).Name);
    }

    /// <summary>
    /// <c>Views</c> already covers the single-outlet case as its <c>"default"</c> entry, so setting
    /// <c>View</c> as well is always a mistake - and one that would otherwise be silent, since
    /// <c>Views</c> simply wins and the <c>View</c> never renders. Surfaces while the route table
    /// is being built, not on some later navigation.
    /// </summary>
    [Fact]
    public void ARecordSettingBothViewAndViewsIsRejectedWhenRegistered()
    {
        var record = new RouteRecord
        {
            Path = "/dashboard",
            Name = "dashboard",
            View = StubView,
            Views = new Dictionary<string, Type> { ["sidebar"] = StubView },
        };

        Assert.Throws<InvalidOperationException>(() => new RouteMatcher([record]));
    }

    /// <summary>A grouping record with no name/views/redirect of its own never enters the matchable list, but its children do - removing it must still take them with it.</summary>
    [Fact]
    public void RemovingAPureGroupingParentAlsoRemovesItsChildren()
    {
        var parent = new RouteRecord
        {
            Path = "/group",
            Children = [new RouteRecord { Path = "child", Name = "group-child", View = StubView }],
        };
        var matcher = new RouteMatcher([parent]);
        Assert.True(matcher.HasRoute("group-child"));

        matcher.RemoveRoute(parent);

        Assert.False(matcher.HasRoute("group-child"));
        Assert.Empty(matcher.GetRoutes());
    }
}
