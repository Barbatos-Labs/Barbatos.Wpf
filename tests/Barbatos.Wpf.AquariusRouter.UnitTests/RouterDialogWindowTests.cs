using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.AquariusRouter.UnitTests;

/// <summary>
/// Navigating to a component that lives inside a child dialog <see cref="Window"/>.
/// </summary>
/// <remarks>
/// This works, and these tests pin exactly how. The important structural fact is that <b>a dialog
/// is its own visual-tree root</b>: a <see cref="RouterView"/> inside one is always at depth 0
/// however deeply nested the route that opened it was, and property inheritance does not cross
/// the window boundary. So a dialog gets its own <see cref="Router"/> - declared once with
/// <see cref="RouterScope.RouterProperty"/> rather than repeated on every link inside it - and
/// navigating in the dialog leaves the main window's route untouched, which is what a dialog
/// almost always wants.
/// </remarks>
public class RouterDialogWindowTests
{
    [Fact]
    public void ADialogWithItsOwnRouterRendersAndNavigatesIndependentlyOfTheMainWindow()
    {
        StaThread.Run(() =>
        {
            var mainRouter = new Router([Record("/", "main-home", typeof(RecordingViewA))]);
            var dialogRouter = new Router([
                Record("/summary", "summary", typeof(RecordingViewB)),
                Record("/details", "details", typeof(RecordingViewC)),
            ]);

            Assert.Null(mainRouter.Start("/").GetAwaiter().GetResult());
            Assert.Null(dialogRouter.Start("/summary").GetAwaiter().GetResult());

            var mainOutlet = new RouterView { Router = mainRouter };
            var main = new Window { Content = mainOutlet, Width = 200, Height = 200 };

            var dialogOutlet = new RouterView();
            var dialog = new Window { Content = dialogOutlet, Width = 200, Height = 200 };

            // One declaration at the dialog root, inherited by everything inside it.
            RouterScope.SetRouter(dialog, dialogRouter);

            try
            {
                main.Show();
                dialog.Owner = main; // WPF requires the owner to have been shown first
                dialog.Show();
                StaThread.PumpDispatcher();

                Assert.IsType<RecordingViewA>(mainOutlet.Content);
                Assert.IsType<RecordingViewB>(dialogOutlet.Content);

                Assert.Null(dialogRouter.Push("/details").GetAwaiter().GetResult());
                StaThread.PumpDispatcher();

                Assert.IsType<RecordingViewC>(dialogOutlet.Content);
                Assert.IsType<RecordingViewA>(mainOutlet.Content); // untouched
                Assert.Equal("/", mainRouter.CurrentRoute.Value.Path);
            }
            finally
            {
                dialog.Close();
                main.Close();
                StaThread.PumpDispatcher();
            }
        });
    }

    [Fact]
    public void ARouterLinkInsideTheDialogResolvesTheDialogsOwnRouter()
    {
        // The ergonomic half: without an inherited scope, every single link in a dialog would
        // need RouterLink.Router set individually - and missing one would silently navigate the
        // main window instead, which is exactly the kind of bug nobody notices until a user does.
        StaThread.Run(() =>
        {
            var mainRouter = new Router([Record("/", "main-home", typeof(RecordingViewA))]);
            var dialogRouter = new Router([
                Record("/summary", "summary", typeof(RecordingViewB)),
                Record("/details", "details", typeof(RecordingViewC)),
            ]);

            Assert.Null(mainRouter.Start("/").GetAwaiter().GetResult());
            Assert.Null(dialogRouter.Start("/summary").GetAwaiter().GetResult());

            var original = Router.Current;
            try
            {
                // Deliberately point the ambient default at the MAIN router, so a link that
                // failed to pick up the scope would visibly navigate the wrong one.
                Router.Current = mainRouter;

                var main = new Window { Content = new RouterView { Router = mainRouter }, Width = 200, Height = 200 };

                var link = new Button();
                RouterLink.SetTo(link, "/details");
                var dialogOutlet = new RouterView();
                var dialog = new Window
                {
                    Content = new StackPanel { Children = { link, dialogOutlet } },
                    Width = 200,
                    Height = 200,
                };
                RouterScope.SetRouter(dialog, dialogRouter);

                try
                {
                    main.Show();
                    dialog.Owner = main; // WPF requires the owner to have been shown first
                    dialog.Show();
                    StaThread.PumpDispatcher();

                    link.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, link));
                    StaThread.PumpDispatcher();

                    Assert.Equal("/details", dialogRouter.CurrentRoute.Value.Path);
                    Assert.Equal("/", mainRouter.CurrentRoute.Value.Path);
                    Assert.IsType<RecordingViewC>(dialogOutlet.Content);

                    // ...and IsActive tracks the dialog's router too, not the ambient one.
                    Assert.True(RouterLink.GetIsExactActive(link));
                }
                finally
                {
                    dialog.Close();
                    main.Close();
                    StaThread.PumpDispatcher();
                }
            }
            finally
            {
                Router.Current = original;
            }
        });
    }

    [Fact]
    public void AModalShowDialogHostsARoutedViewJustTheSame()
    {
        // ShowDialog blocks, so the assertions run from inside the dialog's own Loaded handler -
        // the point being that a modal dialog is not a special case for the router.
        StaThread.Run(() =>
        {
            var dialogRouter = new Router([Record("/summary", "summary", typeof(RecordingViewB))]);
            Assert.Null(dialogRouter.Start("/summary").GetAwaiter().GetResult());

            var outlet = new RouterView();
            var dialog = new Window { Content = outlet, Width = 200, Height = 200 };
            RouterScope.SetRouter(dialog, dialogRouter);

            // The outlet's own Loaded, not the Window's: WPF's Loaded broadcast reaches the root
            // first, so at Window.Loaded time the RouterView has not resolved anything yet.
            object? shown = null;
            outlet.Loaded += (_, _) =>
            {
                shown = outlet.Content;
                dialog.Close();
            };

            dialog.ShowDialog();
            StaThread.PumpDispatcher();

            Assert.IsType<RecordingViewB>(shown);
        });
    }

    [Fact]
    public void ARouterViewInADialogIsAlwaysAtDepthZeroBecauseTheDialogIsItsOwnTreeRoot()
    {
        // Pinned deliberately, because it is the reason a dialog wants its own Router. Sharing
        // the app's router here would show the ROOT record's View in the dialog, not the nested
        // one that opened it - a silently wrong result rather than an error.
        StaThread.Run(() =>
        {
            var child = new RouteRecord { Path = "child", Name = "child", View = typeof(RecordingViewB) };
            var parent = new RouteRecord { Path = "/parent", Name = "parent", View = typeof(LayoutView), Children = [child] };
            var router = new Router([parent]);
            Assert.Null(router.Start("/parent/child").GetAwaiter().GetResult());

            var main = new Window { Content = new RouterView { Router = router }, Width = 200, Height = 200 };
            var dialogOutlet = new RouterView { Router = router };
            var dialog = new Window { Content = dialogOutlet, Width = 200, Height = 200 };

            try
            {
                main.Show();
                dialog.Owner = main; // WPF requires the owner to have been shown first
                dialog.Show();
                StaThread.PumpDispatcher();

                // Depth 0 - the parent's own View, not the nested child's.
                Assert.IsType<LayoutView>(dialogOutlet.Content);
            }
            finally
            {
                dialog.Close();
                main.Close();
                StaThread.PumpDispatcher();
            }
        });
    }

    [Fact]
    public void ClosingTheDialogUnmountsItsRoutedViewAndReleasesIt()
    {
        // The memory question for the dialog case: with no keep-alive in play, closing the
        // dialog must let the whole thing go.
        StaThread.Run(() =>
        {
            var record = Record("/summary", "summary", typeof(SelfWiredLifecycleViewA));
            var dialogRouter = new Router([record, Record("/details", "details", typeof(SelfWiredLifecycleViewB))]);
            Assert.Null(dialogRouter.Start("/summary").GetAwaiter().GetResult());

            var (weakDialog, weakView, calls) = OpenAndCloseDialog(dialogRouter, record);

            Assert.Equal(["A.OnBeforeMount", "A.OnMounted", "A.OnBeforeUnmount", "A.OnUnmounted"], calls);

            // Navigating on is what lets the Router drop the instance - the last strong reference
            // to the routed View, and through its Parent chain to the dialog itself.
            Assert.Null(dialogRouter.Push("/details").GetAwaiter().GetResult());
            Assert.Empty(record.Instances);

            GcProbe.AssertCollected(weakView, "the dialog's routed View");
            GcProbe.AssertCollected(weakDialog, "the closed dialog");
        });
    }

    /// <summary>
    /// Opens and closes the dialog without ever letting the caller's own stack frame hold the
    /// dialog or its View - the hook log comes back instead, which is a plain
    /// <see cref="List{T}"/> of strings and references nothing.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Dialog, WeakReference View, List<string> Calls) OpenAndCloseDialog(Router router, RouteRecord record)
    {
        var dialog = new Window { Content = new RouterView(), Width = 200, Height = 200 };
        RouterScope.SetRouter(dialog, router);

        dialog.Show();
        StaThread.PumpDispatcher();
        dialog.Close();
        StaThread.PumpDispatcher();

        var view = (SelfWiredLifecycleViewA)record.Instances["default"];
        return (new WeakReference(dialog), new WeakReference(view), view.Calls);
    }

    private static void Click(Button link) =>
        link.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, link));

    private static TWindow? OpenWindowOfType<TWindow>()
        where TWindow : Window
    {
        foreach (Window window in Application.Current.Windows)
        {
            if (window is TWindow match)
                return match;
        }

        return null;
    }

    private static RouteRecord Record(string path, string name, Type view) => new() { Path = path, Name = name, View = view };
}
