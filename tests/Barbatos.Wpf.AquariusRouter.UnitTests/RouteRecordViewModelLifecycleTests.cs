using System.Windows;
using System.Windows.Controls;
using Barbatos.Wpf.Aquarius.Composition;

namespace Barbatos.Wpf.AquariusRouter.UnitTests;

/// <summary>
/// A Lifecycle-enabled View that supplies NO DataContext of its own - the shape a library View
/// takes when the application is expected to provide the ViewModel.
/// </summary>
internal sealed class LifecycleOnlyView : ContentControl
{
    public LifecycleOnlyView() => Lifecycle.SetEnable(this, true);
}

/// <summary>
/// A Lifecycle-enabled View that resolves its OWN ViewModel through <c>Setup</c> - the case where
/// a route then overrides that choice with <see cref="RouteRecord.ViewModel"/>.
/// </summary>
internal sealed class SelfSetupLifecycleView : ContentControl
{
    public SelfSetupLifecycleView()
    {
        Lifecycle.SetEnable(this, true);
        Setup.SetViewModel(this, typeof(OwnPresenter));
        BeginInit();
        EndInit();
    }
}

/// <summary>
/// The same View with the two attached properties applied in the opposite order - which is all it
/// takes in real XAML (<c>aq:Setup.ViewModel</c> written above <c>aq:Lifecycle.Enable</c>), since
/// each property's changed-callback subscribes to <c>Initialized</c> as it is applied, and handlers
/// run in subscription order.
/// </summary>
internal sealed class SetupFirstLifecycleView : ContentControl
{
    public SetupFirstLifecycleView()
    {
        Setup.SetViewModel(this, typeof(OwnPresenter));
        Lifecycle.SetEnable(this, true);
        BeginInit();
        EndInit();
    }
}

internal sealed class OwnPresenter : FullLifecycleRecorder
{
    public OwnPresenter() : base("Own") { }
}

internal sealed class RoutePairedPresenter : FullLifecycleRecorder
{
    public RoutePairedPresenter() : base("Paired") { }
}

/// <summary>Records every lifecycle hook Aquarius offers, so a test can see exactly which ViewModel received which.</summary>
internal abstract class FullLifecycleRecorder(string label) :
    IOnBeforeCreate, IOnCreated, IOnBeforeMount, IOnMounted, IOnBeforeUnmount, IOnUnmounted
{
    public List<string> Calls { get; } = [];

    public void OnBeforeCreate() => Calls.Add($"{label}.OnBeforeCreate");

    public void OnCreated() => Calls.Add($"{label}.OnCreated");

    public void OnBeforeMount() => Calls.Add($"{label}.OnBeforeMount");

    public void OnMounted() => Calls.Add($"{label}.OnMounted");

    public void OnBeforeUnmount() => Calls.Add($"{label}.OnBeforeUnmount");

    public void OnUnmounted() => Calls.Add($"{label}.OnUnmounted");
}

/// <summary>
/// Pins how Aquarius's <c>Lifecycle</c> behaves for a ViewModel supplied by the route table rather
/// than by the View itself - the DataContext is assigned after the View's own <c>Initialized</c>
/// has already passed, which is precisely where the create/beforeMount hooks are first attempted.
/// </summary>
public class RouteRecordViewModelLifecycleTests
{
    private static (Window Window, Router Router) ShowRoute(RouteRecord record)
    {
        var router = new Router([record]);
        Assert.Null(router.Start(record.Path).GetAwaiter().GetResult());

        var window = new Window { Content = new RouterView { Router = router } };
        window.Show();
        StaThread.PumpDispatcher();
        return (window, router);
    }

    [Fact]
    public void ARoutePairedViewModelReceivesTheFullMountLifecycle()
    {
        StaThread.Run(() =>
        {
            var record = new RouteRecord
            {
                Path = "/paired",
                Name = "paired",
                View = typeof(LifecycleOnlyView),
                ViewModel = typeof(RoutePairedPresenter),
            };

            var (window, _) = ShowRoute(record);
            try
            {
                var presenter = (RoutePairedPresenter)record.Instances["default"].DataContext;
                Assert.Equal(
                    ["Paired.OnBeforeCreate", "Paired.OnCreated", "Paired.OnBeforeMount", "Paired.OnMounted"],
                    presenter.Calls);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ARoutePairedViewModelAlsoReceivesTheUnmountLifecycle()
    {
        StaThread.Run(() =>
        {
            var paired = new RouteRecord
            {
                Path = "/paired",
                Name = "paired",
                View = typeof(LifecycleOnlyView),
                ViewModel = typeof(RoutePairedPresenter),
            };
            var other = new RouteRecord { Path = "/other", Name = "other", View = typeof(RecordingViewA) };

            var router = new Router([paired, other]);
            Assert.Null(router.Start("/paired").GetAwaiter().GetResult());

            var window = new Window { Content = new RouterView { Router = router } };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                var presenter = (RoutePairedPresenter)paired.Instances["default"].DataContext;
                Assert.Null(router.Push("/other").GetAwaiter().GetResult());
                StaThread.PumpDispatcher();

                Assert.Equal(
                    [
                        "Paired.OnBeforeCreate", "Paired.OnCreated", "Paired.OnBeforeMount", "Paired.OnMounted",
                        "Paired.OnBeforeUnmount", "Paired.OnUnmounted",
                    ],
                    presenter.Calls);
            }
            finally
            {
                window.Close();
            }
        });
    }

    /// <summary>
    /// The overriding case: the View resolved its own ViewModel during construction (so that one
    /// may already have seen the early hooks), and the route then replaces it. The ViewModel that
    /// is actually live must receive the complete lifecycle - a half-initialized one that got
    /// <c>OnMounted</c> without <c>OnCreated</c> would be a genuinely broken state.
    /// </summary>
    [Fact]
    public void AnOverriddenViewModelStillReceivesTheCompleteLifecycle()
    {
        StaThread.Run(() =>
        {
            var record = new RouteRecord
            {
                Path = "/override",
                Name = "override",
                View = typeof(SelfSetupLifecycleView),
                ViewModel = typeof(RoutePairedPresenter),
            };

            var (window, _) = ShowRoute(record);
            try
            {
                var live = (RoutePairedPresenter)record.Instances["default"].DataContext;

                Assert.Equal(
                    ["Paired.OnBeforeCreate", "Paired.OnCreated", "Paired.OnBeforeMount", "Paired.OnMounted"],
                    live.Calls);
            }
            finally
            {
                window.Close();
            }
        });
    }

    /// <summary>
    /// Identical to the test above but with the View's two attached properties applied in the
    /// other order, which is enough to make its own <c>Setup</c> resolve a DataContext <i>before</i>
    /// Lifecycle's first hook attempt. The live ViewModel's lifecycle must not depend on that.
    /// </summary>
    [Fact]
    public void AnOverriddenViewModelReceivesTheCompleteLifecycleRegardlessOfAttributeOrder()
    {
        StaThread.Run(() =>
        {
            var record = new RouteRecord
            {
                Path = "/override-reordered",
                Name = "override-reordered",
                View = typeof(SetupFirstLifecycleView),
                ViewModel = typeof(RoutePairedPresenter),
            };

            var (window, _) = ShowRoute(record);
            try
            {
                var live = (RoutePairedPresenter)record.Instances["default"].DataContext;

                Assert.Equal(
                    ["Paired.OnBeforeCreate", "Paired.OnCreated", "Paired.OnBeforeMount", "Paired.OnMounted"],
                    live.Calls);
            }
            finally
            {
                window.Close();
            }
        });
    }

    /// <summary>
    /// Pins a real limit worth knowing: <c>Lifecycle</c> is opt-in <i>per View</i>
    /// (<c>aq:Lifecycle.Enable</c>), and pairing a ViewModel from the route table does not change
    /// that. A View that never opted in dispatches no hooks to anyone - so a ViewModel paired onto
    /// a completely unwired library View gets its route hooks (<c>IOnRouteEnter</c> and the guard
    /// interfaces, which <see cref="Router"/> dispatches itself) but not Aquarius's mount hooks.
    /// </summary>
    [Fact]
    public void AViewThatNeverEnabledLifecycleDispatchesNoMountHooksEvenWhenPaired()
    {
        StaThread.Run(() =>
        {
            var record = new RouteRecord
            {
                Path = "/pristine",
                Name = "pristine",
                View = typeof(global::Sealed.Library.PristineView),
                ViewModel = typeof(RoutePairedPresenter),
            };

            var (window, _) = ShowRoute(record);
            try
            {
                var presenter = (RoutePairedPresenter)record.Instances["default"].DataContext;
                Assert.Empty(presenter.Calls);
            }
            finally
            {
                window.Close();
            }
        });
    }

    /// <summary>The discarded ViewModel must not go on receiving mount/unmount hooks for a View it is no longer attached to.</summary>
    [Fact]
    public void TheReplacedViewModelDoesNotReceiveMountHooks()
    {
        StaThread.Run(() =>
        {
            var record = new RouteRecord
            {
                Path = "/override",
                Name = "override",
                View = typeof(SelfSetupLifecycleView),
                ViewModel = typeof(RoutePairedPresenter),
            };

            var router = new Router([record]);
            Assert.Null(router.Start("/override").GetAwaiter().GetResult());

            // Capture the discarded ViewModel before the window ever shows.
            var view = record.Instances["default"];
            var replaced = Setup.GetViewModel(view) is null ? null : new OwnPresenter();

            var window = new Window { Content = new RouterView { Router = router } };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                Assert.IsType<RoutePairedPresenter>(view.DataContext);
                Assert.DoesNotContain("Own.OnMounted", replaced?.Calls ?? []);
            }
            finally
            {
                window.Close();
            }
        });
    }
}
