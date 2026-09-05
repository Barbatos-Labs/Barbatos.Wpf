using System.Windows.Controls;
using Barbatos.Wpf.Aquarius.Composition;

namespace Barbatos.Wpf.AquariusRouter.UnitTests;

/// <summary>
/// A minimal <see cref="IServiceProvider"/> for tests to hand <see cref="Composition.Setup.ServiceProvider"/> -
/// registering a View TYPE against an already-constructed, already-configured instance is what
/// lets <see cref="Router"/>'s real construction path (<c>Setup.ServiceProvider?.GetService(...)
/// ?? Activator.CreateInstance(...)</c>) pick up a specific <see cref="RecordingViewModel"/>
/// (with its own label/shared call log already wired) instead of a bare, undifferentiated
/// <c>Activator.CreateInstance</c> instance.
/// </summary>
internal sealed class FakeServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _instances = [];

    public void Register(Type type, object instance) => _instances[type] = instance;

    public object? GetService(Type serviceType) => _instances.GetValueOrDefault(serviceType);
}

/// <summary>Four distinct, otherwise-empty View types - enough to give several records in the same test their own separately-registered instance via <see cref="FakeServiceProvider"/>.</summary>
internal sealed class RecordingViewA : ContentControl;
internal sealed class RecordingViewB : ContentControl;
internal sealed class RecordingViewC : ContentControl;
internal sealed class RecordingViewD : ContentControl;

/// <summary>
/// A "layout" View whose own content is a nested <see cref="RouterView"/> - the plain-C#
/// equivalent of a real View's XAML declaring <c>&lt;aqr:RouterView /&gt;</c> inside itself,
/// used to test that the nested one finds its own depth/effective Router automatically.
/// </summary>
internal sealed class LayoutView : ContentControl
{
    public RouterView NestedRouterView { get; } = new();

    public LayoutView() => Content = NestedRouterView;
}

/// <summary>
/// Implements every guard/data hook interface and appends a "<paramref name="label"/>.HookName"
/// entry to a single, test-owned shared log for every call - lets a test pin the exact
/// interleaved order guards fire in across several different records/ViewModels at once, the
/// same call-order-list style Aquarius's own <c>FakeLifecycleViewModel</c> uses.
/// </summary>
internal sealed class RecordingViewModel(List<string> log, string label) :
    IOnBeforeRouteEnter, IOnBeforeRouteUpdate, IOnBeforeRouteLeave, IOnRouteEnter, IOnRouteUpdate
{
    public string Label { get; } = label;
    public NavigationGuardResult EnterResult { get; set; } = NavigationGuardResult.Continue;
    public NavigationGuardResult UpdateResult { get; set; } = NavigationGuardResult.Continue;
    public NavigationGuardResult LeaveResult { get; set; } = NavigationGuardResult.Continue;
    public RouteLocation? LastTo { get; private set; }
    public RouteLocation? LastFrom { get; private set; }

    public Task<NavigationGuardResult> OnBeforeRouteEnter(RouteLocation to, RouteLocation from)
    {
        log.Add($"{Label}.OnBeforeRouteEnter");
        LastTo = to;
        LastFrom = from;
        return Task.FromResult(EnterResult);
    }

    public Task<NavigationGuardResult> OnBeforeRouteUpdate(RouteLocation to, RouteLocation from)
    {
        log.Add($"{Label}.OnBeforeRouteUpdate");
        LastTo = to;
        LastFrom = from;
        return Task.FromResult(UpdateResult);
    }

    public Task<NavigationGuardResult> OnBeforeRouteLeave(RouteLocation to, RouteLocation from)
    {
        log.Add($"{Label}.OnBeforeRouteLeave");
        return Task.FromResult(LeaveResult);
    }

    public void OnRouteEnter(RouteLocation to, RouteLocation from) => log.Add($"{Label}.OnRouteEnter");

    public void OnRouteUpdate(RouteLocation to, RouteLocation from) => log.Add($"{Label}.OnRouteUpdate");
}

/// <summary>
/// Implements every mount/unmount <see cref="Lifecycle"/> hook and appends a
/// "<paramref name="label"/>.HookName" entry to a shared log - the Lifecycle-hook counterpart of
/// <see cref="RecordingViewModel"/>, used to pin the exact order Aquarius's own Mounted/Unmounted
/// hooks fire in for a View that <see cref="Router"/> constructed and a <see cref="RouterView"/>
/// later attached to a live visual tree (as opposed to Aquarius's own test suite, which only ever
/// simulates <c>Loaded</c>/<c>Unloaded</c> via synthetic <c>BeginInit</c>/<c>EndInit</c> - here the
/// element is genuinely shown in a real <see cref="System.Windows.Window"/>).
/// </summary>
internal sealed class LifecycleRecordingViewModel(List<string> log, string label) :
    IOnBeforeMount, IOnMounted, IOnBeforeUnmount, IOnUnmounted
{
    public void OnBeforeMount() => log.Add($"{label}.OnBeforeMount");

    public void OnMounted() => log.Add($"{label}.OnMounted");

    public void OnBeforeUnmount() => log.Add($"{label}.OnBeforeUnmount");

    public void OnUnmounted() => log.Add($"{label}.OnUnmounted");
}

/// <summary>
/// A View that wires <see cref="Lifecycle.EnableProperty"/> and its own <see cref="LifecycleRecordingViewModel"/>
/// directly in its own constructor - deliberately NOT via <c>Setup.Enable</c> or a
/// <see cref="FakeServiceProvider"/> registration, so that <see cref="Router"/>'s real
/// <c>Activator.CreateInstance(Type)</c> fallback (used whenever nothing is registered for this
/// type) produces a genuinely independent instance, with its own independent <see cref="Calls"/>
/// log, every time one is constructed - the same "no keep-alive by default" guarantee Vue Router
/// itself has. <see cref="Calls"/> is exposed directly (rather than a test-supplied shared list)
/// so a test can compare two different instances' logs without them ever being confused for
/// each other.
/// </summary>
internal abstract class SelfWiredLifecycleView : ContentControl
{
    public List<string> Calls { get; } = [];

    protected SelfWiredLifecycleView(string label)
    {
        Lifecycle.SetEnable(this, true);
        DataContext = new LifecycleRecordingViewModel(Calls, label);
    }
}

/// <summary>Two distinct, independently-<c>Activator.CreateInstance</c>-able <see cref="SelfWiredLifecycleView"/>s - enough for a test to navigate between two different routed Views.</summary>
internal sealed class SelfWiredLifecycleViewA() : SelfWiredLifecycleView("A");

/// <inheritdoc cref="SelfWiredLifecycleViewA"/>
internal sealed class SelfWiredLifecycleViewB() : SelfWiredLifecycleView("B");

/// <summary>
/// The <see cref="SelfWiredLifecycleView"/> counterpart of <see cref="LayoutView"/> - a "layout"
/// View with its own Lifecycle-enabled ViewModel AND a nested <see cref="RouterView"/>, used to
/// prove a parent layout and its nested child both mount correctly (independently, each via its
/// own real <c>Loaded</c>) when a deep nested path is entered directly - e.g. a cold-start deep
/// link, exactly what an AppActions Jump List shortcut produces.
/// </summary>
internal sealed class SelfWiredLifecycleLayoutView : ContentControl
{
    public List<string> Calls { get; } = [];
    public RouterView NestedRouterView { get; } = new();

    public SelfWiredLifecycleLayoutView()
    {
        Lifecycle.SetEnable(this, true);
        DataContext = new LifecycleRecordingViewModel(Calls, "Layout");
        Content = NestedRouterView;
    }
}

/// <summary>
/// A stand-in for an app's modal-route dialog: a plain <see cref="System.Windows.Window"/> hosting
/// a named outlet, and nothing else.
/// </summary>
/// <remarks>
/// Deliberately has no router wiring of its own - that is the whole point of
/// <see cref="RouteRecord.Window"/>. <see cref="RouterWindows"/> resolves this type through
/// <see cref="Setup.Resolve"/> (so a parameterless constructor is what it needs), sets the router
/// scope on it, and pins the record the <see cref="RouterView"/> inside should render.
/// </remarks>
internal sealed class ProbeDialogWindow : System.Windows.Window
{
    public ProbeDialogWindow()
    {
        Width = 200;
        Height = 200;
        Outlet = new RouterView { OutletName = "inspector" };
        Content = Outlet;
    }

    public RouterView Outlet { get; }
}

/// <summary>
/// Records the full mount/unmount <b>and</b> activate/deactivate hook set, plus a scratch
/// <see cref="State"/> value - what keep-alive tests need that
/// <see cref="LifecycleRecordingViewModel"/> does not provide.
/// </summary>
/// <remarks>
/// The activate/deactivate pair is the whole point: keep-alive's promise is that leaving a route
/// produces <c>OnDeactivated</c> instead of <c>OnUnmounted</c>. <see cref="State"/> proves the
/// second half of that promise - that the instance is genuinely the same one, still holding
/// whatever it held before.
/// </remarks>
internal sealed class KeepAliveRecordingViewModel(string label) :
    IOnBeforeMount, IOnMounted, IOnBeforeUnmount, IOnUnmounted, IOnActivated, IOnDeactivated
{
    public List<string> Calls { get; } = [];

    public string? State { get; set; }

    public void OnBeforeMount() => Calls.Add($"{label}.OnBeforeMount");

    public void OnMounted() => Calls.Add($"{label}.OnMounted");

    public void OnBeforeUnmount() => Calls.Add($"{label}.OnBeforeUnmount");

    public void OnUnmounted() => Calls.Add($"{label}.OnUnmounted");

    public void OnActivated() => Calls.Add($"{label}.OnActivated");

    public void OnDeactivated() => Calls.Add($"{label}.OnDeactivated");
}

/// <summary>
/// A routed View that wires <see cref="Lifecycle.EnableProperty"/> and its own
/// <see cref="KeepAliveRecordingViewModel"/> in its constructor, so <see cref="Router"/>'s
/// <see cref="Activator.CreateInstance(Type)"/> path produces a fully-wired, independently
/// observable instance - the keep-alive counterpart of <see cref="SelfWiredLifecycleView"/>.
/// </summary>
internal abstract class KeepAliveProbeView : ContentControl
{
    protected KeepAliveProbeView(string label)
    {
        Lifecycle.SetEnable(this, true);
        ViewModel = new KeepAliveRecordingViewModel(label);
        DataContext = ViewModel;
    }

    public KeepAliveRecordingViewModel ViewModel { get; }
}

/// <summary>Four distinct probe View types, so several records in one test each get their own.</summary>
internal sealed class KeepAliveProbeViewA() : KeepAliveProbeView("A");

/// <inheritdoc cref="KeepAliveProbeViewA"/>
internal sealed class KeepAliveProbeViewB() : KeepAliveProbeView("B");

/// <inheritdoc cref="KeepAliveProbeViewA"/>
internal sealed class KeepAliveProbeViewC() : KeepAliveProbeView("C");

/// <summary>A layout probe with a nested <see cref="RouterView"/>, for keep-alive across a nested route chain.</summary>
internal sealed class KeepAliveProbeLayoutView : KeepAliveProbeView
{
    public KeepAliveProbeLayoutView() : base("Layout")
    {
        NestedRouterView = new RouterView();
        Content = NestedRouterView;
    }

    public RouterView NestedRouterView { get; }
}
