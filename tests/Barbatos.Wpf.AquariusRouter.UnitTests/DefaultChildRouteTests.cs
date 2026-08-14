namespace Barbatos.Wpf.AquariusRouter.UnitTests;

/// <summary>
/// A parent's default child is the one whose own <see cref="RouteRecord.Path"/> adds nothing
/// (<c>""</c>) - it resolves to exactly the parent's path. These pin that targeting the parent
/// lands on it the same way whichever form of navigation is used, since resolving by path can
/// never produce the bare parent and an outlet showing nothing would be the only alternative.
/// </summary>
public class DefaultChildRouteTests
{
    private static readonly Type StubView = typeof(object);

    private static RouteMatcher SettingsMatcher() => new([
        new RouteRecord
        {
            Path = "/settings", Name = "settings", View = StubView,
            Children =
            [
                new RouteRecord { Path = "", Name = "settings-profile", View = StubView },
                new RouteRecord { Path = "security", Name = "settings-security", View = StubView },
            ],
        },
    ]);

    [Fact]
    public void ResolvingTheParentsPathLandsOnTheDefaultChild()
    {
        var location = SettingsMatcher().Resolve(new RouteLocationRaw { Path = "/settings" });

        Assert.Equal("settings-profile", location.Name);
        Assert.Equal(2, location.Matched.Count);
    }

    [Fact]
    public void ResolvingTheParentsNameAlsoLandsOnTheDefaultChild()
    {
        var location = SettingsMatcher().Resolve(new RouteLocationRaw { Name = "settings" });

        Assert.Equal("settings-profile", location.Name);
        Assert.Equal(2, location.Matched.Count);
        Assert.Equal("/settings", location.Path);
    }

    [Fact]
    public void ANonDefaultChildIsStillReachableByItsOwnName()
    {
        var location = SettingsMatcher().Resolve(new RouteLocationRaw { Name = "settings-security" });

        Assert.Equal("settings-security", location.Name);
        Assert.Equal("/settings/security", location.Path);
    }

    [Fact]
    public void AParentWithNoDefaultChildStillResolvesToItself()
    {
        var matcher = new RouteMatcher([
            new RouteRecord
            {
                Path = "/settings", Name = "settings", View = StubView,
                Children = [new RouteRecord { Path = "security", Name = "settings-security", View = StubView }],
            },
        ]);

        var location = matcher.Resolve(new RouteLocationRaw { Name = "settings" });

        Assert.Equal("settings", location.Name);
        Assert.Single(location.Matched);
    }

    [Fact]
    public void DescendsThroughAChainOfDefaultChildren()
    {
        var matcher = new RouteMatcher([
            new RouteRecord
            {
                Path = "/shell", Name = "shell", View = StubView,
                Children =
                [
                    new RouteRecord
                    {
                        Path = "", Name = "shell-inner", View = StubView,
                        Children = [new RouteRecord { Path = "", Name = "shell-leaf", View = StubView }],
                    },
                ],
            },
        ]);

        var byName = matcher.Resolve(new RouteLocationRaw { Name = "shell" });
        var byPath = matcher.Resolve(new RouteLocationRaw { Path = "/shell" });

        Assert.Equal("shell-leaf", byName.Name);
        Assert.Equal("shell-leaf", byPath.Name);
        Assert.Equal(3, byName.Matched.Count);
    }

    [Fact]
    public void ParamsSurviveDescendingToTheDefaultChild()
    {
        var matcher = new RouteMatcher([
            new RouteRecord
            {
                Path = "/users/:id", Name = "user", View = StubView,
                Children = [new RouteRecord { Path = "", Name = "user-overview", View = StubView }],
            },
        ]);

        var location = matcher.Resolve(new RouteLocationRaw
        {
            Name = "user",
            Params = new Dictionary<string, object?> { ["id"] = "42" },
        });

        Assert.Equal("user-overview", location.Name);
        Assert.Equal("/users/42", location.Path);
        Assert.Equal("42", location.Params["id"]);
    }

    /// <summary>An alias node shares its record with the canonical one and is not a child of anything - it must never be mistaken for a default child.</summary>
    [Fact]
    public void AnAliasIsNotTreatedAsADefaultChild()
    {
        var matcher = new RouteMatcher([
            new RouteRecord
            {
                Path = "/settings", Name = "settings", View = StubView, Alias = ["/prefs"],
                Children = [new RouteRecord { Path = "security", Name = "settings-security", View = StubView }],
            },
        ]);

        var location = matcher.Resolve(new RouteLocationRaw { Name = "settings" });

        Assert.Equal("settings", location.Name);
        Assert.Single(location.Matched);
    }

    /// <summary>The two navigation forms must agree end to end, not just at the matcher.</summary>
    [Fact]
    public void PushingByNameAndByPathReachTheSameDefaultChild()
    {
        StaThread.Run(() =>
        {
            var router = new Router([
                new RouteRecord { Path = "/", Name = "home", View = typeof(RecordingViewA) },
                new RouteRecord
                {
                    Path = "/settings", Name = "settings", View = typeof(RecordingViewB),
                    Children = [new RouteRecord { Path = "", Name = "settings-profile", View = typeof(RecordingViewC) }],
                },
            ]);

            Assert.Null(router.Start("/").GetAwaiter().GetResult());
            Assert.Null(router.Push("/settings").GetAwaiter().GetResult());
            Assert.Equal("settings-profile", router.CurrentRoute.Value.Name);

            Assert.Null(router.Push("/").GetAwaiter().GetResult());
            Assert.Null(router.Push(new RouteLocationRaw { Name = "settings" }).GetAwaiter().GetResult());
            Assert.Equal("settings-profile", router.CurrentRoute.Value.Name);
        });
    }

    /// <summary>
    /// The other way to give a parent a default: redirect it at the record. Unlike an index child
    /// this changes the resolved path, and runs before any guard - see <see cref="RouteRecord.Redirect"/>.
    /// </summary>
    [Fact]
    public void AParentCanInsteadRedirectToAChild()
    {
        StaThread.Run(() =>
        {
            var router = new Router([
                new RouteRecord { Path = "/", Name = "home", View = typeof(RecordingViewA) },
                new RouteRecord
                {
                    Path = "/settings", Name = "settings", Redirect = _ => "/settings/security",
                    Children = [new RouteRecord { Path = "security", Name = "settings-security", View = typeof(RecordingViewB) }],
                },
            ]);

            Assert.Null(router.Start("/").GetAwaiter().GetResult());
            Assert.Null(router.Push("/settings").GetAwaiter().GetResult());

            Assert.Equal("settings-security", router.CurrentRoute.Value.Name);
            Assert.Equal("/settings/security", router.CurrentRoute.Value.Path);
        });
    }
}
