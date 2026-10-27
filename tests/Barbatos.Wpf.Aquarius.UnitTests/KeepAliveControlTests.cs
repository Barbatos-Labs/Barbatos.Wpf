// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.Aquarius.UnitTests;

/// <summary>
/// <c>&lt;aq:KeepAlive&gt;</c> wrapping an <c>&lt;aq:Component Is="..."/&gt;</c> - Vue's own shape.
/// Does switching the component really only produce <c>IOnDeactivated</c>/<c>IOnActivated</c>, and
/// does everything that genuinely drops a View really produce a full unmount?
/// </summary>
/// <remarks>
/// <b>Every test here shows a real <see cref="Window"/>, deliberately.</b> Lifecycle treats an
/// element with no <c>PresentationSource</c> as "assume visible" (see its own <c>OnLoaded</c>), so
/// the synthetic <c>RaiseEvent(LoadedEvent)</c> style most of this suite uses would report
/// <c>OnActivated</c> for collapsed, cached Views too - turning every assertion below green for the
/// wrong reason. <c>KeepAliveTests</c>, which pinned the hand-rolled recipe this control replaces,
/// has the same constraint for the same reason.
/// </remarks>
public class KeepAliveControlTests
{
    [Fact]
    public void FirstActivationBuildsAndMountsTheView()
    {
        StaThread.Run(() =>
        {
            var keepAlive = Build(out var component);
            using var window = Show(keepAlive);

            component.Is = typeof(KeepAliveProbeViewA);
            StaThread.PumpDispatcher();

            Assert.Equal(["OnBeforeCreate", "OnCreated", "OnBeforeMount", "OnMounted", "OnActivated"], CallsFor(keepAlive, typeof(KeepAliveProbeViewA)));
        });
    }

    [Fact]
    public void AViewThatWasNeverShownIsNeverBuilt()
    {
        // Lazy, exactly like Vue: <KeepAlive> caches a component after it first renders, it does
        // not pre-render every branch. Nothing being cached for B *is* the proof it was never
        // built - the factory runs in exactly one place, on the first switch to a type.
        StaThread.Run(() =>
        {
            var keepAlive = Build(out var component);
            using var window = Show(keepAlive);

            component.Is = typeof(KeepAliveProbeViewA);
            StaThread.PumpDispatcher();

            Assert.True(keepAlive.TryGetCached(typeof(KeepAliveProbeViewA), out _));
            Assert.False(keepAlive.TryGetCached(typeof(KeepAliveProbeViewB), out _));
            Assert.Single(keepAlive.Children);
        });
    }

    [Fact]
    public void SwitchingAwayAndBackOnlyDeactivatesAndReactivates()
    {
        // The whole point of the control - and what neither a TabControl nor Frame/Page gives you
        // (both confirmed in KeepAliveTests to genuinely destroy and recreate).
        StaThread.Run(() =>
        {
            var keepAlive = Build(out var component);
            using var window = Show(keepAlive);

            component.Is = typeof(KeepAliveProbeViewA);
            StaThread.PumpDispatcher();
            var firstA = keepAlive.ActiveView;
            ClearCalls(keepAlive, typeof(KeepAliveProbeViewA));

            component.Is = typeof(KeepAliveProbeViewB);
            StaThread.PumpDispatcher();

            Assert.Equal(["OnDeactivated"], CallsFor(keepAlive, typeof(KeepAliveProbeViewA)));
            Assert.Equal(["OnBeforeCreate", "OnCreated", "OnBeforeMount", "OnMounted", "OnActivated"], CallsFor(keepAlive, typeof(KeepAliveProbeViewB)));
            ClearCalls(keepAlive, typeof(KeepAliveProbeViewA));
            ClearCalls(keepAlive, typeof(KeepAliveProbeViewB));

            component.Is = typeof(KeepAliveProbeViewA);
            StaThread.PumpDispatcher();

            Assert.Equal(["OnActivated"], CallsFor(keepAlive, typeof(KeepAliveProbeViewA)));
            Assert.Equal(["OnDeactivated"], CallsFor(keepAlive, typeof(KeepAliveProbeViewB)));

            // Same instance, so everything it holds survived the round trip.
            Assert.Same(firstA, keepAlive.ActiveView);
        });
    }

    [Fact]
    public void ViewModelStateSurvivesARoundTrip()
    {
        StaThread.Run(() =>
        {
            var keepAlive = Build(out var component);
            using var window = Show(keepAlive);

            component.Is = typeof(KeepAliveProbeViewA);
            StaThread.PumpDispatcher();
            ViewModelFor(keepAlive, typeof(KeepAliveProbeViewA)).Counter = 42;

            component.Is = typeof(KeepAliveProbeViewB);
            StaThread.PumpDispatcher();
            component.Is = typeof(KeepAliveProbeViewA);
            StaThread.PumpDispatcher();

            Assert.Equal(42, ViewModelFor(keepAlive, typeof(KeepAliveProbeViewA)).Counter);
        });
    }

    [Fact]
    public void ClearingIsHidesEverythingWithoutUnmountingIt()
    {
        StaThread.Run(() =>
        {
            var keepAlive = Build(out var component);
            using var window = Show(keepAlive);

            component.Is = typeof(KeepAliveProbeViewA);
            StaThread.PumpDispatcher();
            ClearCalls(keepAlive, typeof(KeepAliveProbeViewA));

            component.Is = null;
            StaThread.PumpDispatcher();

            Assert.Equal(["OnDeactivated"], CallsFor(keepAlive, typeof(KeepAliveProbeViewA)));
            Assert.True(keepAlive.TryGetCached(typeof(KeepAliveProbeViewA), out _));
            Assert.Null(keepAlive.ActiveView);
        });
    }

    [Fact]
    public void ExceedingMaxEvictsTheLeastRecentlyActivatedViewForReal()
    {
        StaThread.Run(() =>
        {
            var keepAlive = Build(out var component);
            keepAlive.Max = 2;
            using var window = Show(keepAlive);

            component.Is = typeof(KeepAliveProbeViewA);
            StaThread.PumpDispatcher();
            var evicted = ViewModelFor(keepAlive, typeof(KeepAliveProbeViewA));
            evicted.Calls.Clear();

            component.Is = typeof(KeepAliveProbeViewB);
            StaThread.PumpDispatcher();
            component.Is = typeof(KeepAliveProbeViewC);
            StaThread.PumpDispatcher();

            // A is the least recently activated, so it goes - and goes for real.
            Assert.False(keepAlive.TryGetCached(typeof(KeepAliveProbeViewA), out _));
            Assert.True(keepAlive.TryGetCached(typeof(KeepAliveProbeViewB), out _));
            Assert.True(keepAlive.TryGetCached(typeof(KeepAliveProbeViewC), out _));

            // Two separate stages, one hook each, in the order they actually happened: the
            // OnDeactivated came from the switch to B (collapsing A), and only the later switch to
            // C pushed the cache over Max and unmounted it for real.
            Assert.Equal(["OnDeactivated", "OnBeforeUnmount", "OnUnmounted"], evicted.Calls);

            // ...and coming back to it is a genuinely fresh instance, not a resume.
            component.Is = typeof(KeepAliveProbeViewA);
            StaThread.PumpDispatcher();
            Assert.NotSame(evicted, ViewModelFor(keepAlive, typeof(KeepAliveProbeViewA)));
        });
    }

    [Fact]
    public void MaxNeverEvictsWhateverIsCurrentlyShowing()
    {
        StaThread.Run(() =>
        {
            var keepAlive = Build(out var component);
            keepAlive.Max = 1;
            using var window = Show(keepAlive);

            component.Is = typeof(KeepAliveProbeViewA);
            StaThread.PumpDispatcher();
            component.Is = typeof(KeepAliveProbeViewB);
            StaThread.PumpDispatcher();

            Assert.False(keepAlive.TryGetCached(typeof(KeepAliveProbeViewA), out _));
            Assert.True(keepAlive.TryGetCached(typeof(KeepAliveProbeViewB), out _));
            Assert.NotNull(keepAlive.ActiveView);
        });
    }

    [Fact]
    public void AnExcludedTypeIsDroppedAsSoonAsYouSwitchAwayFromIt()
    {
        // Exclude matches the View's TYPE NAME - the WPF counterpart of Vue matching a component's
        // own `name` option.
        StaThread.Run(() =>
        {
            var keepAlive = Build(out var component);
            keepAlive.Exclude = nameof(KeepAliveProbeViewB);
            using var window = Show(keepAlive);

            component.Is = typeof(KeepAliveProbeViewB);
            StaThread.PumpDispatcher();
            var excluded = ViewModelFor(keepAlive, typeof(KeepAliveProbeViewB));
            excluded.Calls.Clear();

            component.Is = typeof(KeepAliveProbeViewA);
            StaThread.PumpDispatcher();

            Assert.False(keepAlive.TryGetCached(typeof(KeepAliveProbeViewB), out _));
            Assert.Equal(["OnDeactivated", "OnBeforeUnmount", "OnUnmounted"], excluded.Calls);
        });
    }

    [Fact]
    public void IncludeActsAsAnAllowListAndEverythingElseIsDropped()
    {
        StaThread.Run(() =>
        {
            var keepAlive = Build(out var component);
            keepAlive.Include = nameof(KeepAliveProbeViewA);
            using var window = Show(keepAlive);

            component.Is = typeof(KeepAliveProbeViewB);
            StaThread.PumpDispatcher();
            component.Is = typeof(KeepAliveProbeViewA);
            StaThread.PumpDispatcher();
            component.Is = typeof(KeepAliveProbeViewB);
            StaThread.PumpDispatcher();

            Assert.True(keepAlive.TryGetCached(typeof(KeepAliveProbeViewA), out _));  // on the allow-list
            Assert.True(keepAlive.TryGetCached(typeof(KeepAliveProbeViewB), out _));  // not on it, but showing

            component.Is = typeof(KeepAliveProbeViewA);
            StaThread.PumpDispatcher();

            Assert.False(keepAlive.TryGetCached(typeof(KeepAliveProbeViewB), out _)); // ...and dropped once it isn't
        });
    }

    [Fact]
    public void ExcludingATypeAtRuntimeRetroactivelyDropsItFromTheCache()
    {
        // Vue watches include/exclude and prunes the existing cache when they change. Applying the
        // filter only at switch time would leave an already-cached View mounted indefinitely after
        // it was excluded.
        StaThread.Run(() =>
        {
            var keepAlive = Build(out var component);
            using var window = Show(keepAlive);

            component.Is = typeof(KeepAliveProbeViewA);
            StaThread.PumpDispatcher();
            component.Is = typeof(KeepAliveProbeViewB);
            StaThread.PumpDispatcher();
            Assert.True(keepAlive.TryGetCached(typeof(KeepAliveProbeViewA), out _));

            var dropped = ViewModelFor(keepAlive, typeof(KeepAliveProbeViewA));
            dropped.Calls.Clear();

            keepAlive.Exclude = nameof(KeepAliveProbeViewA);
            StaThread.PumpDispatcher();

            Assert.False(keepAlive.TryGetCached(typeof(KeepAliveProbeViewA), out _));
            Assert.Equal(["OnBeforeUnmount", "OnUnmounted"], dropped.Calls);
        });
    }

    [Fact]
    public void ExcludingTheShowingTypeKeepsItUntilYouSwitchAway()
    {
        // The one entry a retroactive exclude must not release outright - dropping the View that
        // is currently on screen would blank the control.
        StaThread.Run(() =>
        {
            var keepAlive = Build(out var component);
            using var window = Show(keepAlive);

            component.Is = typeof(KeepAliveProbeViewA);
            StaThread.PumpDispatcher();

            keepAlive.Exclude = nameof(KeepAliveProbeViewA);
            StaThread.PumpDispatcher();

            Assert.True(keepAlive.TryGetCached(typeof(KeepAliveProbeViewA), out _));
            Assert.NotNull(keepAlive.ActiveView);

            component.Is = typeof(KeepAliveProbeViewB);
            StaThread.PumpDispatcher();

            Assert.False(keepAlive.TryGetCached(typeof(KeepAliveProbeViewA), out _));
        });
    }

    [Fact]
    public void AnIsSetBeforeTheControlLoadsStillResolves()
    {
        // What the XAML parser does: attributes are applied before the element is ever connected,
        // so the first meaningful read of a bound Is only happens at Loaded.
        StaThread.Run(() =>
        {
            var component = new Component { Is = typeof(KeepAliveProbeViewA) };
            var keepAlive = new KeepAlive { Child = component };

            using var window = Show(keepAlive);

            Assert.True(keepAlive.TryGetCached(typeof(KeepAliveProbeViewA), out _));
            Assert.NotNull(keepAlive.ActiveView);
        });
    }

    [Fact]
    public void ReleasingTheShowingViewDeactivatesThenUnmountsIt()
    {
        StaThread.Run(() =>
        {
            var keepAlive = Build(out var component);
            using var window = Show(keepAlive);

            component.Is = typeof(KeepAliveProbeViewA);
            StaThread.PumpDispatcher();
            var released = ViewModelFor(keepAlive, typeof(KeepAliveProbeViewA));
            released.Calls.Clear();

            Assert.True(keepAlive.Release(typeof(KeepAliveProbeViewA)));
            StaThread.PumpDispatcher();

            Assert.Equal(["OnDeactivated", "OnBeforeUnmount", "OnUnmounted"], released.Calls);
            Assert.Null(keepAlive.ActiveView);
        });
    }

    [Fact]
    public void ResolutionRecoversAfterTheShowingViewIsReleasedOutFromUnderIt()
    {
        // Release/ReleaseAll are public, so the cache can be emptied while Component.Is still
        // names what was just released. Resolution therefore re-derives what should be showing
        // every time it runs, rather than short-circuiting on "the type hasn't changed since I
        // last looked" - that bookkeeping goes stale the moment the cache is touched from outside,
        // and would leave the control blank for good.
        StaThread.Run(() =>
        {
            var keepAlive = Build(out var component);
            using var window = Show(keepAlive);

            component.Is = typeof(KeepAliveProbeViewA);
            StaThread.PumpDispatcher();
            var first = keepAlive.ActiveView;

            // Release means release: blank until something asks again.
            Assert.True(keepAlive.Release(typeof(KeepAliveProbeViewA)));
            StaThread.PumpDispatcher();
            Assert.Null(keepAlive.ActiveView);

            // Any resolution trigger recovers it - here a switch away and back.
            component.Is = typeof(KeepAliveProbeViewB);
            StaThread.PumpDispatcher();
            component.Is = typeof(KeepAliveProbeViewA);
            StaThread.PumpDispatcher();

            Assert.NotNull(keepAlive.ActiveView);
            Assert.NotSame(first, keepAlive.ActiveView); // genuinely rebuilt, not resurrected
        });
    }

    [Fact]
    public void CachedTypesIsALeastRecentlyActivatedFirstSnapshot()
    {
        StaThread.Run(() =>
        {
            var keepAlive = Build(out var component);
            using var window = Show(keepAlive);

            foreach (var viewType in (Type[])[typeof(KeepAliveProbeViewA), typeof(KeepAliveProbeViewB), typeof(KeepAliveProbeViewC), typeof(KeepAliveProbeViewA)])
            {
                component.Is = viewType;
                StaThread.PumpDispatcher();
            }

            Assert.Equal([typeof(KeepAliveProbeViewB), typeof(KeepAliveProbeViewC), typeof(KeepAliveProbeViewA)], keepAlive.CachedTypes);

            // A snapshot, so releasing while iterating it is safe - which is what ReleaseAll and
            // the Include/Exclude pruning both do.
            foreach (var viewType in keepAlive.CachedTypes)
                keepAlive.Release(viewType);

            Assert.Empty(keepAlive.CachedTypes);
        });
    }

    [Fact]
    public void AViewTypeIsResolvedThroughSetupsServiceProvider()
    {
        // The DI path: the View comes out of the app's own container when there is one - the same
        // Setup.Resolve every other View in this library family goes through.
        StaThread.Run(() =>
        {
            var original = Setup.ServiceProvider;
            var registered = new KeepAliveProbeViewA();

            try
            {
                var provider = new FakeServiceProvider();
                provider.Register(typeof(KeepAliveProbeViewA), registered);
                Setup.ServiceProvider = provider;

                var keepAlive = Build(out var component);
                using var window = Show(keepAlive);

                component.Is = typeof(KeepAliveProbeViewA);
                StaThread.PumpDispatcher();

                Assert.Same(registered, keepAlive.ActiveView);
            }
            finally
            {
                Setup.ServiceProvider = original;
            }
        });
    }

    [Fact]
    public void AddingAChildDirectlyThrowsRatherThanSilentlyOverlayingTheShowingView()
    {
        StaThread.Run(() => Assert.Throws<InvalidOperationException>(() => new KeepAlive().Children.Add(new Border())));
    }

    private static KeepAlive Build(out Component component)
    {
        component = new Component();
        return new KeepAlive { Child = component };
    }

    private static IEnumerable<string> CallsFor(KeepAlive keepAlive, Type viewType) => ViewModelFor(keepAlive, viewType).Calls;

    private static void ClearCalls(KeepAlive keepAlive, Type viewType) => ViewModelFor(keepAlive, viewType).Calls.Clear();

    private static FakeLifecycleViewModel ViewModelFor(KeepAlive keepAlive, Type viewType)
    {
        Assert.True(keepAlive.TryGetCached(viewType, out var element), $"nothing is cached for '{viewType.Name}'");
        return (FakeLifecycleViewModel)element.DataContext;
    }

    private static WindowScope Show(KeepAlive keepAlive)
    {
        var window = new Window { Content = keepAlive, Width = 200, Height = 200 };
        window.Show();
        StaThread.PumpDispatcher();
        return new WindowScope(window);
    }

    private sealed class WindowScope(Window window) : IDisposable
    {
        public void Dispose()
        {
            window.Close();
            StaThread.PumpDispatcher();
        }
    }
}
