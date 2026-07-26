using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

namespace Barbatos.Wpf.AquariusRouter.UnitTests;

public class RouterLinkTests
{
    private static Router BuildRouter() => new([
        new RouteRecord
        {
            Path = "/users",
            Name = "users",
            View = typeof(RecordingViewA),
            Children =
            [
                new RouteRecord { Path = "", Name = "users-home", View = typeof(RecordingViewB) },
                new RouteRecord { Path = ":id", Name = "user-detail", View = typeof(RecordingViewC) },
            ],
        },
        new RouteRecord { Path = "/about", Name = "about", View = typeof(RecordingViewD) },
    ]);

    [Fact]
    public void ClickingNavigatesToTheConfiguredTarget()
    {
        StaThread.Run(() =>
        {
            var router = BuildRouter();
            Assert.Null(router.Start("/about").GetAwaiter().GetResult());

            var button = new Button();
            RouterLink.SetRouter(button, router);
            RouterLink.SetTo(button, "/users");

            var window = new Window { Content = button };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
                StaThread.PumpDispatcher();

                Assert.Equal("users-home", router.CurrentRoute.Value.Name);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ReplaceTrueNavigatesWithoutPushingANewHistoryEntry()
    {
        StaThread.Run(() =>
        {
            var router = BuildRouter();
            Assert.Null(router.Start("/about").GetAwaiter().GetResult());
            Assert.Null(router.Push("/users").GetAwaiter().GetResult());

            var button = new Button();
            RouterLink.SetRouter(button, router);
            RouterLink.SetTo(button, "/about");
            RouterLink.SetReplace(button, true);

            var window = new Window { Content = button };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
                StaThread.PumpDispatcher();

                Assert.Equal("about", router.CurrentRoute.Value.Name);
                // Replace means Back() should land on the ORIGINAL "/about" entry from Start,
                // not on the "/users" entry that was Push()'d in between.
                Assert.Null(router.Back().GetAwaiter().GetResult());
                Assert.Equal("about", router.CurrentRoute.Value.Name);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void HyperlinkIsSupportedTheSameWayAsAButtonBase()
    {
        StaThread.Run(() =>
        {
            var router = BuildRouter();
            Assert.Null(router.Start("/about").GetAwaiter().GetResult());

            var hyperlink = new Hyperlink(new Run("Users"));
            RouterLink.SetRouter(hyperlink, router);
            RouterLink.SetTo(hyperlink, "/users");

            var textBlock = new TextBlock();
            textBlock.Inlines.Add(hyperlink);
            var window = new Window { Content = textBlock };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                hyperlink.RaiseEvent(new RoutedEventArgs(Hyperlink.ClickEvent, hyperlink));
                StaThread.PumpDispatcher();

                Assert.Equal("users-home", router.CurrentRoute.Value.Name);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void SettingToOnAnUnsupportedElementTypeThrows()
    {
        StaThread.Run(() =>
        {
            var element = new TextBlock();
            Assert.Throws<InvalidOperationException>(() => RouterLink.SetTo(element, "/about"));
        });
    }

    [Fact]
    public void IsExactActiveIsTrueOnlyForAnExactMatchIncludingParams()
    {
        StaThread.Run(() =>
        {
            var router = BuildRouter();
            Assert.Null(router.Start(new RouteLocationRaw { Name = "user-detail", Params = new Dictionary<string, object?> { ["id"] = "42" } }).GetAwaiter().GetResult());

            var exactButton = new Button();
            RouterLink.SetRouter(exactButton, router);
            RouterLink.SetTo(exactButton, new RouteLocationRaw { Name = "user-detail", Params = new Dictionary<string, object?> { ["id"] = "42" } });

            var differentIdButton = new Button();
            RouterLink.SetRouter(differentIdButton, router);
            RouterLink.SetTo(differentIdButton, new RouteLocationRaw { Name = "user-detail", Params = new Dictionary<string, object?> { ["id"] = "99" } });

            var panel = new StackPanel();
            panel.Children.Add(exactButton);
            panel.Children.Add(differentIdButton);
            var window = new Window { Content = panel };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                Assert.True(RouterLink.GetIsExactActive(exactButton));
                Assert.True(RouterLink.GetIsActive(exactButton));

                Assert.False(RouterLink.GetIsExactActive(differentIdButton));
                Assert.False(RouterLink.GetIsActive(differentIdButton));
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void IsActiveStaysTrueForALinkToAnIndexParentWhileAnyOfItsChildrenAreActive()
    {
        StaThread.Run(() =>
        {
            var router = BuildRouter();
            // Currently on a child of "/users" (the ":id" detail route) - a link to the parent's
            // own index route ("/users" itself) should still read as active, matching Vue
            // Router's real isActive semantics for a parent/index link.
            Assert.Null(router.Start(new RouteLocationRaw { Name = "user-detail", Params = new Dictionary<string, object?> { ["id"] = "1" } }).GetAwaiter().GetResult());

            var usersLink = new Button();
            RouterLink.SetRouter(usersLink, router);
            RouterLink.SetTo(usersLink, "/users");

            var window = new Window { Content = usersLink };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                Assert.True(RouterLink.GetIsActive(usersLink));
                Assert.False(RouterLink.GetIsExactActive(usersLink)); // not the exact leaf
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void IsActiveUpdatesLiveAsTheCurrentRouteChanges()
    {
        StaThread.Run(() =>
        {
            var router = BuildRouter();
            Assert.Null(router.Start("/about").GetAwaiter().GetResult());

            var usersLink = new Button();
            RouterLink.SetRouter(usersLink, router);
            RouterLink.SetTo(usersLink, "/users");

            var window = new Window { Content = usersLink };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                Assert.False(RouterLink.GetIsActive(usersLink));

                Assert.Null(router.Push("/users").GetAwaiter().GetResult());
                StaThread.PumpDispatcher();

                Assert.True(RouterLink.GetIsActive(usersLink));
            }
            finally
            {
                window.Close();
            }
        });
    }
}
