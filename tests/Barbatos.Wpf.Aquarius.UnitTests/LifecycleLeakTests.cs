using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.Aquarius.UnitTests;

/// <summary>
/// Proves that the pieces of this library which subscribe to something longer-lived than
/// themselves actually let go again.
/// </summary>
/// <remarks>
/// Every hazard here has the same shape: a control wires itself up on <c>Loaded</c> and tears
/// down on <c>Unloaded</c>, which is only balanced if <c>Loaded</c> never fires twice. It does -
/// WPF raises it a second time with no <c>Unloaded</c> in between whenever a <c>TabControl</c>
/// realizes every tab's content up front and then raises <c>Loaded</c> again on whichever one
/// later becomes selected (documented in <see cref="Lifecycle"/>'s own <c>OnLoaded</c>, and in
/// the sample's <c>TeleportDemoView</c>). One extra subscription against a <see langword="static"/>
/// event is a process-lifetime root, so the control, its content subtree and - through
/// <c>DataContext</c> - the whole ViewModel graph never become collectable.
/// <para>
/// These are this project's GC-based assertions (AquariusRouter's <c>RouterLeakTests</c> and
/// <c>RouterDialogWindowTests</c> use that project's own copy of the same probe); see <see cref="GcProbe"/> for why they are written the way they
/// are, and why the object under test is always built inside a separate non-inlined method.
/// </para>
/// </remarks>
public class LifecycleLeakTests
{
    [Fact]
    public void ATeleportIsCollectableAfterUnloading()
    {
        // The baseline the test below is measured against: one Loaded, one Unloaded, nothing
        // left holding on.
        StaThread.Run(() => GcProbe.AssertCollected(RunTeleport(loadedCount: 1), "the Teleport"));
    }

    [Fact]
    public void ATeleportThatSawASecondLoadedIsStillCollectableAfterUnloading()
    {
        // The leak: Teleport subscribed TeleportHost's two *static* events on every Loaded,
        // and `-=` only ever removes one subscription per Unloaded. Two Loadeds then one
        // Unloaded therefore left a live handler on a static event forever - pinning this
        // Teleport, its Content subtree and its whole DataContext graph for the life of the
        // process. Nothing about it is observable through Teleport's own API (UpdateTeleport
        // is gated on _loaded, so the orphaned handler is inert), which is exactly why it
        // needs a reachability assertion rather than a behavioral one.
        StaThread.Run(() => GcProbe.AssertCollected(RunTeleport(loadedCount: 2), "the Teleport"));
    }

    [Fact]
    public void ATeleportStillLoadedIsDeliberatelyNotCollectable()
    {
        // The control case proving the assertion above can actually fail: while a Teleport is
        // genuinely loaded it *is* subscribed to those static events, and is supposed to be
        // held alive by them.
        StaThread.Run(() => GcProbe.AssertAlive(RunTeleport(loadedCount: 1, unload: false), "a still-loaded Teleport"));
    }

    [Fact]
    public void AnElementWithLifecycleEnabledIsCollectableAfterUnmounting()
    {
        // Lifecycle's own LifecycleState subscribes its dispatcher's UnhandledException while
        // mounted - the one subscription in this library that outlives the element's own tree (a
        // dispatcher lives as long as its thread, the Application's for the whole process) - so an
        // unbalanced mount would pin the element and its ViewModel to that dispatcher.
        TestApplication.Invoke(() => GcProbe.AssertCollected(RunLifecycleElement(loadedCount: 2), "the Lifecycle-enabled element"));
    }

    [Fact]
    public void SwitchingAComponentReleasesTheOldViewAndItsViewModel()
    {
        // A View outside a KeepAlive must not outlive being switched away from - and neither must
        // its ViewModel, which is the half that actually holds subscriptions, timers and loaded
        // data. Vue destroys a swapped-out <component :is> the same way; keeping it alive is what
        // <KeepAlive> is *for*, and therefore has to be opt-in.
        StaThread.Run(() =>
        {
            var (window, view, viewModel) = RunComponentSwitch(insideKeepAlive: false);

            GcProbe.AssertCollected(view, "the switched-away View");
            GcProbe.AssertCollected(viewModel, "the switched-away View's ViewModel");

            Close(window);
        });
    }

    [Fact]
    public void SwitchingInsideAKeepAliveDeliberatelyKeepsBoth()
    {
        // The control case for the test above - it proves the probe can actually fail, and pins
        // the opt-in: the very same switch inside a KeepAlive keeps View and ViewModel alive.
        StaThread.Run(() =>
        {
            var (window, view, viewModel) = RunComponentSwitch(insideKeepAlive: true);

            GcProbe.AssertAlive(view, "a cached View");
            GcProbe.AssertAlive(viewModel, "a cached View's ViewModel");

            Close(window);
        });
    }

    /// <summary>
    /// Shows one View through a <see cref="Component"/>, switches to another, and returns weak
    /// references to the first View and its ViewModel - never letting the caller's own frame hold
    /// either of them.
    /// </summary>
    /// <remarks>
    /// The still-open <see cref="Window"/> comes back so the caller keeps the whole live tree
    /// rooted across the assertion. Without that the two tests would not be comparable: an
    /// unrooted <see cref="KeepAlive"/> is collected along with everything it caches, so the
    /// control case would "pass" for a reason that has nothing to do with caching.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (Window Window, WeakReference View, WeakReference ViewModel) RunComponentSwitch(bool insideKeepAlive)
    {
        var component = new Component();
        var root = insideKeepAlive ? new KeepAlive { Child = component } : (FrameworkElement)component;

        var window = new Window { Content = root, Width = 200, Height = 200 };
        window.Show();
        StaThread.PumpDispatcher();

        component.Is = typeof(KeepAliveProbeViewA);
        StaThread.PumpDispatcher();

        var first = insideKeepAlive ? ((KeepAlive)root).ActiveView! : (FrameworkElement)component.Content;
        var weak = (new WeakReference(first), new WeakReference(first.DataContext));

        component.Is = typeof(KeepAliveProbeViewB);
        StaThread.PumpDispatcher();

        return (window, weak.Item1, weak.Item2);
    }

    private static void Close(Window window)
    {
        window.Close();
        StaThread.PumpDispatcher();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RunTeleport(int loadedCount, bool unload = true)
    {
        var hostName = $"Host-{Guid.NewGuid()}";
        var teleport = new Teleport { To = hostName, Content = new Border() };

        for (var i = 0; i < loadedCount; i++)
            teleport.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, teleport));

        if (unload)
            teleport.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, teleport));

        return new WeakReference(teleport);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RunLifecycleElement(int loadedCount)
    {
        var element = new ContentControl { DataContext = new FakeLifecycleViewModel() };
        Lifecycle.SetEnable(element, true);

        for (var i = 0; i < loadedCount; i++)
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

        element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));

        return new WeakReference(element);
    }
}
