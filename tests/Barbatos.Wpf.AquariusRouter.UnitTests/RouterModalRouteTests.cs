using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.AquariusRouter.UnitTests;

/// <summary>
/// Modal routes: a record carrying <see cref="RouteRecord.Window"/> renders in its own
/// <see cref="Window"/>, which <see cref="RouterWindows"/> opens and closes purely from the current
/// route.
/// </summary>
/// <remarks>
/// The point of putting this on the record rather than on a link is that <b>every</b> way of
/// navigating reaches it - a <c>RouterLink</c>, a <c>router.Push</c> from a ViewModel, a keyboard
/// shortcut, in-app search, a URI activation. These tests therefore drive it mostly through plain
/// <c>Push</c>/<c>Back</c> calls rather than clicks, precisely because that is the case a
/// link-level property could never have covered.
/// <para>
/// They run on <see cref="TestApplication"/> rather than <c>StaThread</c>: real windows are shown,
/// and <c>Application.Current.Windows</c> is what the assertions inspect.
/// </para>
/// </remarks>
public class RouterModalRouteTests
{
    [Fact]
    public void NavigatingToAModalRecordOpensItsWindowAndLeavesTheParentShowing()
    {
        TestApplication.Invoke(() => WithHost((router, host, outlet) =>
        {
            Assert.Null(router.Push("/").GetAwaiter().GetResult());
            StaThread.PumpDispatcher();

            Assert.IsType<RecordingViewA>(outlet.Content);
            Assert.Null(OpenDialog());

            // A plain Push - no link involved anywhere.
            Assert.Null(router.Push("/inspector").GetAwaiter().GetResult());
            StaThread.PumpDispatcher();

            var dialog = OpenDialog();
            Assert.NotNull(dialog);
            Assert.Same(host, dialog!.Owner);

            // The dialog shows the child record's own named outlet...
            Assert.IsType<RecordingViewB>(dialog.Outlet.Content);

            // ...and the main window still shows the parent behind it. That is what makes this a
            // modal route rather than a navigation away from the current screen.
            Assert.IsType<RecordingViewA>(outlet.Content);
        }));
    }

    [Fact]
    public void NavigatingAwayClosesTheWindow()
    {
        TestApplication.Invoke(() => WithHost((router, _, _) =>
        {
            Assert.Null(router.Push("/inspector").GetAwaiter().GetResult());
            StaThread.PumpDispatcher();
            Assert.NotNull(OpenDialog());

            Assert.Null(router.Push("/").GetAwaiter().GetResult());
            StaThread.PumpDispatcher();

            Assert.Null(OpenDialog());
        }));
    }

    [Fact]
    public void GoingBackClosesTheWindow()
    {
        // One router, one history - so the dialog sits on the history stack like anything else,
        // and Back is the natural way out of it.
        TestApplication.Invoke(() => WithHost((router, _, _) =>
        {
            Assert.Null(router.Push("/").GetAwaiter().GetResult());
            Assert.Null(router.Push("/inspector").GetAwaiter().GetResult());
            StaThread.PumpDispatcher();
            Assert.NotNull(OpenDialog());

            Assert.Null(router.Back().GetAwaiter().GetResult());
            StaThread.PumpDispatcher();

            Assert.Null(OpenDialog());
            Assert.Equal("/", router.CurrentRoute.Value.Path);
        }));
    }

    [Fact]
    public void ClosingTheWindowNavigatesBackToTheParent()
    {
        // The other half of the symmetry. Without it the route would go on claiming a window that
        // is no longer on screen, and navigating to it again would look like a no-op.
        TestApplication.Invoke(() => WithHost((router, _, _) =>
        {
            Assert.Null(router.Push("/").GetAwaiter().GetResult());
            Assert.Null(router.Push("/inspector").GetAwaiter().GetResult());
            StaThread.PumpDispatcher();

            OpenDialog()!.Close();
            StaThread.PumpDispatcher();

            Assert.Equal("/", router.CurrentRoute.Value.Path);
            Assert.Null(OpenDialog());
        }));
    }

    [Fact]
    public void NavigatingBetweenTwoModalRecordsReusesTheSameWindow()
    {
        // Sibling modal routes sharing one Window type: the window stays put and only its outlet
        // swaps, which is what a dialog with its own internal navigation should look like.
        TestApplication.Invoke(() => WithHost((router, _, outlet) =>
        {
            Assert.Null(router.Push("/inspector").GetAwaiter().GetResult());
            StaThread.PumpDispatcher();
            var dialog = OpenDialog();
            Assert.IsType<RecordingViewB>(dialog!.Outlet.Content);

            Assert.Null(router.Push("/inspector/raw").GetAwaiter().GetResult());
            StaThread.PumpDispatcher();

            Assert.Same(dialog, OpenDialog());
            Assert.IsType<RecordingViewC>(dialog.Outlet.Content);

            // ...and the main window never moved throughout.
            Assert.IsType<RecordingViewA>(outlet.Content);
        }));
    }

    [Fact]
    public void ARouterLinkReachesAModalRouteWithNoExtraWiring()
    {
        // The link needs nothing special at all now - that is the whole improvement over a
        // link-level Target property, which only ever worked for links.
        TestApplication.Invoke(() => WithHost((router, host, _) =>
        {
            Assert.Null(router.Push("/").GetAwaiter().GetResult());
            StaThread.PumpDispatcher();

            var link = new Button();
            RouterLink.SetTo(link, "/inspector");
            ((StackPanel)host.Content).Children.Add(link);
            StaThread.PumpDispatcher();

            link.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, link));
            StaThread.PumpDispatcher();

            Assert.NotNull(OpenDialog());
            Assert.True(RouterLink.GetIsExactActive(link));
        }));
    }

    [Fact]
    public void UnloadingTheHostClosesEveryWindowItOpened()
    {
        TestApplication.Invoke(() => WithHost((router, host, _) =>
        {
            Assert.Null(router.Push("/inspector").GetAwaiter().GetResult());
            StaThread.PumpDispatcher();
            Assert.NotNull(OpenDialog());

            host.Close();
            StaThread.PumpDispatcher();

            Assert.Null(OpenDialog());
        }));
    }

    /// <summary>
    /// Builds the route table (a parent with two modal children sharing one window type), a host
    /// window with <c>RouterWindows.Enable</c> and a default outlet, and tears it all down after.
    /// </summary>
    private static void WithHost(Action<Router, Window, RouterView> body)
    {
        var router = new Router([
            new RouteRecord
            {
                Path = "/",
                Name = "home",
                View = typeof(RecordingViewA),
                Children =
                [
                    new RouteRecord
                    {
                        Path = "inspector",
                        Name = "inspector",
                        Window = typeof(ProbeDialogWindow),
                        Views = new Dictionary<string, Type> { ["inspector"] = typeof(RecordingViewB) },
                    },
                    new RouteRecord
                    {
                        Path = "inspector/raw",
                        Name = "inspector-raw",
                        Window = typeof(ProbeDialogWindow),
                        Views = new Dictionary<string, Type> { ["inspector"] = typeof(RecordingViewC) },
                    },
                ],
            },
        ]);

        var outlet = new RouterView { Router = router };
        var host = new Window { Content = new StackPanel { Children = { outlet } }, Width = 200, Height = 200 };
        RouterScope.SetRouter(host, router);
        RouterWindows.SetEnable(host, true);

        try
        {
            host.Show();
            StaThread.PumpDispatcher();
            body(router, host, outlet);
        }
        finally
        {
            OpenDialog()?.Close();
            host.Close();
            StaThread.PumpDispatcher();
        }
    }

    private static ProbeDialogWindow? OpenDialog()
    {
        foreach (Window window in Application.Current.Windows)
        {
            if (window is ProbeDialogWindow match)
                return match;
        }

        return null;
    }
}
