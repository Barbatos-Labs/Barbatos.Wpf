// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.AquariusRouter.UnitTests;

/// <summary>
/// Does anything the router touches keep a View, a ViewModel or a whole window alive after it
/// should have been let go?
/// </summary>
/// <remarks>
/// A <see cref="Router"/> is normally an application-lifetime singleton, which makes it the most
/// dangerous thing in this library to be referenced by: anything it holds, or anything subscribed
/// to its <see cref="Router.CurrentRoute"/>, is held for as long as the app runs. Every test here
/// builds its subject inside a separate non-inlined method so the calling frame never roots it -
/// see <see cref="GcProbe"/>.
/// </remarks>
public class RouterLeakTests
{
    [Fact]
    public void ARouterViewThatSawASecondLoadedIsStillCollectableAfterUnloading()
    {
        // The leak: RouterView assigned a fresh Watch.On handle over the previous one without
        // disposing it, and WPF raises Loaded a second time with no Unloaded in between (a
        // TabControl realizes every tab's content up front, then raises Loaded again on whichever
        // one later becomes selected). The orphaned watch stays subscribed to Router.CurrentRoute
        // - an app-lifetime object - and pins the RouterView plus its entire routed subtree.
        // RouterLink already guarded its own watch this way, with a comment explaining why;
        // RouterView simply did not.
        //
        // The Router is deliberately rooted here for the whole assertion. Without that, the
        // Router, its CurrentRoute, the orphaned watch and the RouterView form one unreachable
        // island that the GC collects as a group - so the test would pass whether or not the leak
        // was fixed. An app-lifetime Router (Router.Current, or a DI singleton) is what makes the
        // difference between "held" and "held forever", and it is the real-world case.
        StaThread.Run(() =>
        {
            var router = RootedRouter();
            GcProbe.AssertCollected(RunOutlet(router, loadedCount: 2), "the RouterView");
            GC.KeepAlive(router);
        });
    }

    [Fact]
    public void ARouterViewIsCollectableAfterUnloadingNormally()
    {
        // The baseline the test above is measured against.
        StaThread.Run(() =>
        {
            var router = RootedRouter();
            GcProbe.AssertCollected(RunOutlet(router, loadedCount: 1), "the RouterView");
            GC.KeepAlive(router);
        });
    }

    [Fact]
    public void ALoadedRouterViewIsDeliberatelyNotCollectable()
    {
        // The control case, proving the assertions above can actually fail: while a RouterView is
        // loaded it is watching a live Router's CurrentRoute and is supposed to be held by it.
        StaThread.Run(() =>
        {
            var router = RootedRouter();
            GcProbe.AssertAlive(RunOutlet(router, loadedCount: 1, unload: false), "a still-loaded RouterView");
            GC.KeepAlive(router);
        });
    }

    [Fact]
    public void LeavingARouteReleasesItsViewAndItsViewModel()
    {
        // The router's contract: no accidental keep-alive, ever. Leaving clears the record's
        // Instances and nothing else is holding on - including for the ViewModel, which is the
        // half that actually owns subscriptions, timers and loaded data. Keep-alive is a template
        // concern here (Aquarius's own KeepAlive control), exactly as in Vue Router; the router
        // itself always destroys.
        StaThread.Run(() =>
        {
            var home = new RouteRecord { Path = "/", Name = "home", View = typeof(SelfWiredLifecycleViewA) };
            var other = new RouteRecord { Path = "/other", Name = "other", View = typeof(SelfWiredLifecycleViewB) };
            var router = new Router([home, other]);

            var (view, viewModel) = NavigateAwayFrom(router, home);

            Assert.Empty(home.Instances);
            GcProbe.AssertCollected(view, "the left route's View");
            GcProbe.AssertCollected(viewModel, "the left route's ViewModel");
        });
    }

    [Fact]
    public void AnUnloadedRouterLinkIsCollectable()
    {
        StaThread.Run(() =>
        {
            var router = new Router([new RouteRecord { Path = "/", Name = "home", View = typeof(RecordingViewA) }]);
            Assert.Null(router.Start("/").GetAwaiter().GetResult());

            GcProbe.AssertCollected(RunLink(router), "the RouterLink's button");
        });
    }

    /// <summary>A router the caller keeps alive for the whole test, standing in for an app-lifetime one.</summary>
    private static Router RootedRouter()
    {
        var router = new Router([
            new RouteRecord { Path = "/", Name = "home", View = typeof(RecordingViewA) },
            new RouteRecord { Path = "/other", Name = "other", View = typeof(RecordingViewB) },
        ]);
        Assert.Null(router.Start("/").GetAwaiter().GetResult());
        return router;
    }

    /// <summary>
    /// Loads an outlet <paramref name="loadedCount"/> times, optionally unloads it, then navigates
    /// the router elsewhere.
    /// </summary>
    /// <remarks>
    /// That last navigation is what makes the assertion mean anything. A <see cref="Router"/> holds
    /// the <i>current</i> route's View in <see cref="RouteRecord.Instances"/>, and WPF gives that
    /// View a logical-parent back-pointer to the outlet showing it - so while the route is still
    /// current the outlet is legitimately reachable, leak or no leak. Navigating away clears that
    /// instance, leaving an orphaned <c>CurrentRoute</c> watch as the only thing that could still
    /// be holding the outlet.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RunOutlet(Router router, int loadedCount, bool unload = true)
    {
        var outlet = new RouterView { Router = router };

        for (var i = 0; i < loadedCount; i++)
            outlet.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, outlet));

        if (unload)
        {
            outlet.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, outlet));
            outlet.Content = null;
        }

        Assert.Null(router.Push("/other").GetAwaiter().GetResult());

        return new WeakReference(outlet);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RunLink(Router router)
    {
        var button = new Button();
        RouterLink.SetRouter(button, router);
        RouterLink.SetTo(button, "/");

        button.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, button));
        button.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, button));

        return new WeakReference(button);
    }

    /// <summary>
    /// Shows the route at <c>/</c> in a real window, then navigates away and tears the window
    /// down - returning only <see cref="WeakReference"/>s to the View that was left and its
    /// ViewModel, so the caller's frame never roots either (nor, through the View's <c>Parent</c>
    /// chain, the window).
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference View, WeakReference ViewModel) NavigateAwayFrom(Router router, RouteRecord record)
    {
        Assert.Null(router.Start("/").GetAwaiter().GetResult());

        var window = new Window { Content = new RouterView { Router = router }, Width = 200, Height = 200 };
        window.Show();
        StaThread.PumpDispatcher();

        var left = record.Instances["default"];
        var weak = (new WeakReference(left), new WeakReference(left.DataContext));

        Assert.Null(router.Push("/other").GetAwaiter().GetResult());
        StaThread.PumpDispatcher();

        window.Close();
        StaThread.PumpDispatcher();

        return weak;
    }
}
