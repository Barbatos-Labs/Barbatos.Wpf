// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

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
    public void ClosingTheWindowIsRefusedWhileItsLeaveGuardSaysNo()
    {
        // The [X] is a request to navigate away, not a fait accompli. Closing the window first and
        // navigating afterwards left a refused navigation on the modal record with no window -
        // and pushing that route again was Duplicated, so nothing could ever reopen it.
        TestApplication.Invoke(() =>
        {
            var routes = GuardedInspectorRoutes(out var inspector);

            WithHost(routes, (router, _, _) =>
            {
                Assert.Null(router.Push("/").GetAwaiter().GetResult());
                Assert.Null(router.Push("/inspector").GetAwaiter().GetResult());
                StaThread.PumpDispatcher();
                var dialog = OpenDialog()!;
                var guard = (SwitchableLeaveGuardViewModel)inspector.Instances["inspector"].DataContext;
                guard.LeaveResult = NavigationGuardResult.Cancel;

                dialog.Close();
                StaThread.PumpDispatcher();

                Assert.Equal(1, guard.LeaveCalls);
                Assert.Same(dialog, OpenDialog());
                Assert.True(dialog.IsVisible);
                Assert.Equal("/inspector", router.CurrentRoute.Value.Path);

                // Once the guard allows it, the very same [X] closes the window onto the parent.
                guard.LeaveResult = NavigationGuardResult.Continue;
                dialog.Close();
                StaThread.PumpDispatcher();

                Assert.Null(OpenDialog());
                Assert.Equal("/", router.CurrentRoute.Value.Path);
            });
        });
    }

    [Fact]
    public void ALeaveGuardThatThrowsOnTheWindowsOwnCloseReachesTheDispatcher()
    {
        // Nothing awaits the navigation an [X] starts. A guard that threw used to vanish with its
        // task - and since the route still claims the window, the window stays open: a window that
        // would not close, with no trace of why. It is rethrown the way a RouterLink click's is.
        // The rethrow is posted at Normal priority, so the Background-priority pump below runs it
        // on this very dispatcher - no waiting needed.
        var failure = new InvalidOperationException("Leave guard failed.");
        var observed = new List<Exception>();

        void OnUnhandled(object? sender, DispatcherUnhandledExceptionEventArgs e)
        {
            observed.Add(e.Exception);
            e.Handled = true;
        }

        TestApplication.Invoke(() =>
        {
            var routes = GuardedInspectorRoutes(out var inspector);
            Application.Current.DispatcherUnhandledException += OnUnhandled;

            try
            {
                WithHost(routes, (router, _, _) =>
                {
                    Assert.Null(router.Push("/").GetAwaiter().GetResult());
                    Assert.Null(router.Push("/inspector").GetAwaiter().GetResult());
                    StaThread.PumpDispatcher();
                    var dialog = OpenDialog()!;
                    var guard = (SwitchableLeaveGuardViewModel)inspector.Instances["inspector"].DataContext;
                    guard.LeaveException = failure;

                    dialog.Close();
                    StaThread.PumpDispatcher();

                    Assert.Same(failure, Assert.Single(observed));
                    Assert.Same(dialog, OpenDialog());
                    Assert.Equal("/inspector", router.CurrentRoute.Value.Path);

                    // Fixed, the same [X] goes through.
                    guard.LeaveException = null;
                    dialog.Close();
                    StaThread.PumpDispatcher();

                    Assert.Null(OpenDialog());
                    Assert.Equal("/", router.CurrentRoute.Value.Path);
                });
            }
            finally
            {
                Application.Current.DispatcherUnhandledException -= OnUnhandled;
            }
        });
    }

    [Fact]
    public void ClosingTheWindowWhileAGuardIsStillDecidingKeepsItOpenUntilTheNavigationCommits()
    {
        // A guard that awaits something (a prompt, a save, the sample's own BeforeResolve delay)
        // has not decided by the time Closing must be answered, so the window stays until the
        // navigation commits - and then closes like any other navigation away.
        TestApplication.Invoke(() => WithHost((router, _, _) =>
        {
            Assert.Null(router.Push("/").GetAwaiter().GetResult());
            Assert.Null(router.Push("/inspector").GetAwaiter().GetResult());
            StaThread.PumpDispatcher();
            var dialog = OpenDialog()!;

            var gate = new TaskCompletionSource<NavigationGuardResult>();
            using var guard = router.BeforeEach((_, _) => gate.Task);

            dialog.Close();
            StaThread.PumpDispatcher();

            Assert.Same(dialog, OpenDialog());
            Assert.Equal("/inspector", router.CurrentRoute.Value.Path);

            gate.SetResult(NavigationGuardResult.Continue);
            StaThread.PumpDispatcher();

            Assert.Null(OpenDialog());
            Assert.Equal("/", router.CurrentRoute.Value.Path);
        }));
    }

    [Fact]
    public void ClosingTheHostClosesTheWindowEvenWhenItsLeaveGuardSaysNo()
    {
        // WPF does not let an owned window veto its owner's close, so the dialog goes regardless -
        // and its leave guard (an unsaved-changes prompt, in a real app) is asked at most once,
        // never once from Closing and again from Closed.
        TestApplication.Invoke(() =>
        {
            var routes = GuardedInspectorRoutes(out var inspector);

            WithHost(routes, (router, host, _) =>
            {
                Assert.Null(router.Push("/").GetAwaiter().GetResult());
                Assert.Null(router.Push("/inspector").GetAwaiter().GetResult());
                StaThread.PumpDispatcher();
                var guard = (SwitchableLeaveGuardViewModel)inspector.Instances["inspector"].DataContext;
                guard.LeaveResult = NavigationGuardResult.Cancel;

                host.Close();
                StaThread.PumpDispatcher();

                Assert.Null(OpenDialog());
                Assert.InRange(guard.LeaveCalls, 0, 1);
            });
        });
    }

    [Fact]
    public void ClosingTheWindowUnderAnUnnamedParentGoesBack()
    {
        // No name to push, so the [X] is a Back - which lands on the parent, since arriving at the
        // dialog is what put it on the history stack.
        TestApplication.Invoke(() => WithHost(UnnamedParentRoutes(), (router, _, _) =>
        {
            Assert.Null(router.Push("/").GetAwaiter().GetResult());
            Assert.Null(router.Push("/inspector").GetAwaiter().GetResult());
            StaThread.PumpDispatcher();

            OpenDialog()!.Close();
            StaThread.PumpDispatcher();

            Assert.Null(OpenDialog());
            Assert.Equal("/", router.CurrentRoute.Value.Path);
        }));
    }

    [Fact]
    public void ClosingTheWindowWithNowhereToGoBackToStillClosesIt()
    {
        // Started straight on the modal route, under an unnamed parent: Back has no earlier entry
        // and is a silent no-op. Treating that like a refusal would leave a window no [X] can ever
        // close; it closes instead, and the route stays where it was - the documented reason to
        // name a modal route's parent.
        TestApplication.Invoke(() => WithHost(UnnamedParentRoutes(), (router, _, _) =>
        {
            Assert.Null(router.Start("/inspector").GetAwaiter().GetResult());
            StaThread.PumpDispatcher();
            var dialog = OpenDialog()!;

            dialog.Close();
            StaThread.PumpDispatcher();

            Assert.Null(OpenDialog());
            Assert.False(dialog.IsVisible);
            Assert.Equal("/inspector", router.CurrentRoute.Value.Path);
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
    /// The default table (a parent with two modal children sharing one window type), a host
    /// window with <c>RouterWindows.Enable</c> and a default outlet, torn down after.
    /// </summary>
    private static void WithHost(Action<Router, Window, RouterView> body) =>
        WithHost(
        [
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
        ],
        body);

    /// <summary>
    /// A parent with one modal child whose View is paired with a <see cref="SwitchableLeaveGuardViewModel"/>,
    /// handing back that child record so a test can reach the guard through its <see cref="RouteRecord.Instances"/>.
    /// </summary>
    private static IReadOnlyList<RouteRecord> GuardedInspectorRoutes(out RouteRecord inspector)
    {
        inspector = new RouteRecord
        {
            Path = "inspector",
            Name = "inspector",
            Window = typeof(ProbeDialogWindow),
            Views = new Dictionary<string, Type> { ["inspector"] = typeof(RecordingViewB) },
            ViewModels = new Dictionary<string, Type> { ["inspector"] = typeof(SwitchableLeaveGuardViewModel) },
        };

        return [new RouteRecord { Path = "/", Name = "home", View = typeof(RecordingViewA), Children = [inspector] }];
    }

    /// <summary>A modal child under a parent with no <see cref="RouteRecord.Name"/>, so closing its window can only fall back to <c>Back</c>.</summary>
    private static IReadOnlyList<RouteRecord> UnnamedParentRoutes() =>
    [
        new RouteRecord
        {
            Path = "/",
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
            ],
        },
    ];

    /// <summary>
    /// Builds a router over <paramref name="routes"/>, a host window with <c>RouterWindows.Enable</c>
    /// and a default outlet, and tears it all down after.
    /// </summary>
    private static void WithHost(IEnumerable<RouteRecord> routes, Action<Router, Window, RouterView> body)
    {
        var router = new Router(routes);

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
