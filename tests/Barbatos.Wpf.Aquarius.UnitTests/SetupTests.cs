using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.Aquarius.UnitTests;

public class SetupTests
{
    [Fact]
    public void ExplicitViewModelTypeResolvesAndAssignsDataContext()
    {
        StaThread.Run(() =>
        {
            var element = new ContentControl();
            Setup.SetViewModel(element, typeof(SetupProbeViewModel));

            ((ISupportInitialize)element).BeginInit();
            ((ISupportInitialize)element).EndInit();

            Assert.IsType<SetupProbeViewModel>(element.DataContext);
        });
    }

    [Fact]
    public void EnableAloneResolvesByNamingConventionInTheSameAssembly()
    {
        StaThread.Run(() =>
        {
            // SetupProbeView -> SetupProbeViewModel, both defined in this same test
            // assembly - the default resolver's same-assembly fast path.
            var element = new SetupProbeView();
            Setup.SetEnable(element, true);

            ((ISupportInitialize)element).BeginInit();
            ((ISupportInitialize)element).EndInit();

            Assert.IsType<SetupProbeViewModel>(element.DataContext);
        });
    }

    [Fact]
    public void ExplicitViewModelWinsOverConventionWhenBothAreSet()
    {
        StaThread.Run(() =>
        {
            // SetupProbeView's own naming convention would resolve to
            // SetupProbeViewModel - the explicit override below must win instead.
            var element = new SetupProbeView();
            Setup.SetEnable(element, true);
            Setup.SetViewModel(element, typeof(FakeLifecycleViewModel));

            ((ISupportInitialize)element).BeginInit();
            ((ISupportInitialize)element).EndInit();

            Assert.IsType<FakeLifecycleViewModel>(element.DataContext);
        });
    }

    [Fact]
    public void ServiceProviderIsConsultedBeforeTheActivatorFallback()
    {
        StaThread.Run(() =>
        {
            var original = Setup.ServiceProvider;
            var registered = new SetupProbeViewModel();
            var provider = new FakeServiceProvider();
            provider.Register(typeof(SetupProbeViewModel), registered);
            Setup.ServiceProvider = provider;
            try
            {
                var element = new SetupProbeView();
                Setup.SetEnable(element, true);

                ((ISupportInitialize)element).BeginInit();
                ((ISupportInitialize)element).EndInit();

                // Same instance, not merely the same type - proves the registered singleton
                // came back rather than a fresh Activator.CreateInstance one.
                Assert.Same(registered, element.DataContext);
            }
            finally
            {
                Setup.ServiceProvider = original;
            }
        });
    }

    [Fact]
    public void UnresolvedConventionSilentlyLeavesDataContextAloneByDefault()
    {
        StaThread.Run(() =>
        {
            Assert.False(Setup.ThrowOnUnresolved);

            // Plain ContentControl: its type name doesn't end in "View", so the default
            // convention can't even form a guess.
            var sentinel = new object();
            var element = new ContentControl { DataContext = sentinel };
            Setup.SetEnable(element, true);

            ((ISupportInitialize)element).BeginInit();
            ((ISupportInitialize)element).EndInit();

            Assert.Same(sentinel, element.DataContext);
        });
    }

    [Fact]
    public void ThrowOnUnresolvedOptInThrowsClearlyInstead()
    {
        StaThread.Run(() =>
        {
            var original = Setup.ThrowOnUnresolved;
            Setup.ThrowOnUnresolved = true;
            try
            {
                var element = new ContentControl();
                Setup.SetEnable(element, true);

                ((ISupportInitialize)element).BeginInit();
                var ex = Assert.Throws<InvalidOperationException>(() => ((ISupportInitialize)element).EndInit());
                Assert.Contains("ContentControl", ex.Message);
            }
            finally
            {
                Setup.ThrowOnUnresolved = original;
            }
        });
    }

    [Fact]
    public void CustomResolverOverridesTheDefaultConvention()
    {
        StaThread.Run(() =>
        {
            var original = Setup.Resolver;
            Setup.Resolver = _ => typeof(SetupProbeViewModel);
            try
            {
                // A plain ContentControl - the default convention would never match this (no
                // "View" suffix), so a hit here can only come from the custom Resolver.
                var element = new ContentControl();
                Setup.SetEnable(element, true);

                ((ISupportInitialize)element).BeginInit();
                ((ISupportInitialize)element).EndInit();

                Assert.IsType<SetupProbeViewModel>(element.DataContext);
            }
            finally
            {
                Setup.Resolver = original;
            }
        });
    }

    [Fact]
    public void CombinedWithLifecycleEnableTheResolvedViewModelStillReceivesHooksEvenWhenLifecycleWiresFirst()
    {
        StaThread.Run(() =>
        {
            var element = new ContentControl();

            // Deliberately attach Lifecycle's Initialized handler before Setup's - XAML
            // gives no guarantee which of two attached properties on the same element has
            // its changed-callback (and therefore its Initialized subscription) run first.
            // This ordering means Lifecycle's own best-effort-at-Initialized hook check runs
            // against a DataContext Setup hasn't resolved yet - only Lifecycle's guaranteed
            // fallback at Loaded can save it, which proves Setup resolving at Initialized
            // (not Loaded) is early enough regardless of which attached property happens to
            // apply first.
            Lifecycle.SetEnable(element, true);
            Setup.SetViewModel(element, typeof(FakeLifecycleViewModel));

            ((ISupportInitialize)element).BeginInit();
            ((ISupportInitialize)element).EndInit();

            Assert.IsType<FakeLifecycleViewModel>(element.DataContext);

            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

            var vm = (FakeLifecycleViewModel)element.DataContext;
            Assert.Equal(["OnBeforeCreate", "OnCreated", "OnBeforeMount", "OnMounted", "OnActivated"], vm.Calls);

            // See the cleanup note in LifecycleTests.MountedFiresAfterBeforeMountOnLoaded.
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));
        });
    }

    [Fact]
    public void CombinedWithLifecycleEnableWiredAfterSetupTheCreateAndBeforeMountHooksAlreadyFireAtInitialized()
    {
        StaThread.Run(() =>
        {
            var element = new ContentControl();

            // Opposite order from the test above - Setup wired first here, so its
            // Initialized handler runs before Lifecycle's. This is the "easy" ordering:
            // Lifecycle's own best-effort-at-Initialized check should succeed immediately
            // (not merely via the guaranteed-at-Loaded fallback), which this asserts
            // directly by checking state before Loaded is ever raised.
            Setup.SetViewModel(element, typeof(FakeLifecycleViewModel));
            Lifecycle.SetEnable(element, true);

            ((ISupportInitialize)element).BeginInit();
            ((ISupportInitialize)element).EndInit();

            var vm = (FakeLifecycleViewModel)element.DataContext;
            Assert.Equal(["OnBeforeCreate", "OnCreated", "OnBeforeMount"], vm.Calls);

            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

            Assert.Equal(["OnBeforeCreate", "OnCreated", "OnBeforeMount", "OnMounted", "OnActivated"], vm.Calls);

            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));
        });
    }

    [Fact]
    public void CombinedWithLifecycleEnableTheFullUnmountSequenceFiresCorrectlyToo()
    {
        StaThread.Run(() =>
        {
            var element = new ContentControl();
            Setup.SetViewModel(element, typeof(FakeLifecycleViewModel));
            Lifecycle.SetEnable(element, true);

            ((ISupportInitialize)element).BeginInit();
            ((ISupportInitialize)element).EndInit();
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

            var vm = (FakeLifecycleViewModel)element.DataContext;
            vm.Calls.Clear();

            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));

            Assert.Equal(["OnBeforeUnmount", "OnDeactivated", "OnUnmounted"], vm.Calls);
        });
    }

    [Fact]
    public void CombinedWithLifecycleEnableThePropertyChangeUpdateHookFiresCorrectly()
    {
        StaThread.Run(() =>
        {
            var element = new ContentControl();
            Setup.SetViewModel(element, typeof(FakeLifecycleViewModel));
            Lifecycle.SetEnable(element, true);

            ((ISupportInitialize)element).BeginInit();
            ((ISupportInitialize)element).EndInit();
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

            var vm = (FakeLifecycleViewModel)element.DataContext;
            vm.Calls.Clear();

            // Proves Lifecycle.StartMountedTracking's WatchDataContext correctly subscribed
            // to *this* Setup-resolved instance's PropertyChanged, not a stale/null
            // reference captured before Setup ever assigned DataContext.
            vm.Counter++;
            Assert.Equal(["OnBeforeUpdate"], vm.Calls);

            StaThread.PumpDispatcher();
            Assert.Equal(["OnBeforeUpdate", "OnUpdated"], vm.Calls);

            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));
        });
    }

    [Fact]
    public void CombinedWithLifecycleEnableAsyncHooksFireCorrectlyToo()
    {
        StaThread.Run(() =>
        {
            var element = new ContentControl();
            Setup.SetViewModel(element, typeof(FakeAsyncLifecycleViewModel));
            Lifecycle.SetEnable(element, true);

            ((ISupportInitialize)element).BeginInit();
            ((ISupportInitialize)element).EndInit();
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

            var vm = (FakeAsyncLifecycleViewModel)element.DataContext;
            // OnActivatedAsync also fires on mount, mirroring Vue's "onActivated is also
            // called on mount" note that the sync hooks already demonstrate elsewhere.
            Assert.Equal(["OnBeforeCreateAsync", "OnCreatedAsync", "OnBeforeMountAsync", "OnMountedAsync:started", "OnActivatedAsync"], vm.Calls);

            vm.MountedGate.SetResult();
            StaThread.PumpDispatcher();

            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));
            StaThread.PumpDispatcher();

            Assert.Contains("OnBeforeUnmountAsync", vm.Calls);
            Assert.Contains("OnUnmountedAsync", vm.Calls);
        });
    }
}
