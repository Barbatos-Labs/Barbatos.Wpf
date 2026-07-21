using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.Aquarius.UnitTests;

public class SuspenseTests
{
    [Fact]
    public void DefaultsToNotPendingAndShowsChild()
    {
        StaThread.Run(() =>
        {
            var child = new Border();

            var suspense = new Suspense { Child = child };

            Assert.Same(child, suspense.Content);
        });
    }

    [Fact]
    public void PendingShowsFallbackInsteadOfChild()
    {
        StaThread.Run(() =>
        {
            var child = new Border();
            var fallback = new Border();

            var suspense = new Suspense { Child = child, Fallback = fallback, IsPending = true };

            Assert.Same(fallback, suspense.Content);
        });
    }

    [Fact]
    public void TogglingIsPendingSwapsBetweenFallbackAndChild()
    {
        StaThread.Run(() =>
        {
            var child = new Border();
            var fallback = new Border();
            var suspense = new Suspense { Child = child, Fallback = fallback };

            suspense.IsPending = true;
            Assert.Same(fallback, suspense.Content);

            suspense.IsPending = false;
            Assert.Same(child, suspense.Content);
        });
    }

    [Fact]
    public void ChangingChildWhileNotPendingUpdatesContentImmediately()
    {
        StaThread.Run(() =>
        {
            var suspense = new Suspense { Child = new Border() };
            var replacement = new Border();

            suspense.Child = replacement;

            Assert.Same(replacement, suspense.Content);
        });
    }

    [Fact]
    public void ChangingChildWhilePendingDoesNotChangeVisibleContent()
    {
        StaThread.Run(() =>
        {
            var fallback = new Border();
            var suspense = new Suspense { Child = new Border(), Fallback = fallback, IsPending = true };

            suspense.Child = new Border();

            Assert.Same(fallback, suspense.Content);
        });
    }

    [Fact]
    public void IsPendingTogglingFiresTheFullVueStyleMountAndUnmountSequenceOnTheChild()
    {
        // Suspense swaps ContentControl.Content exactly the way If does (see
        // IfControlTests.ConditionTogglingFiresTheFullVueStyleMountAndUnmountSequence) - so
        // the resolved Child is genuinely destroyed while Fallback is showing, not just
        // hidden, matching Vue's own <Suspense>: the #default slot's component is truly
        // unmounted while #fallback is displayed, not kept alive underneath it.
        StaThread.Run(() =>
        {
            var vm = new FakeLifecycleViewModel();
            var child = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(child, true);
            var suspense = new Suspense { Child = child, Fallback = new Border() };

            var window = new Window { Content = suspense, Width = 200, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();

            Assert.Equal(["OnBeforeCreate", "OnCreated", "OnBeforeMount", "OnMounted", "OnActivated"], vm.Calls);

            vm.Calls.Clear();
            suspense.IsPending = true;
            StaThread.PumpDispatcher();

            // Same ordering nuance as If's real-detach case: a real visual-tree removal
            // fires IsVisibleChanged (going false) slightly before Unloaded.
            Assert.Equal(["OnDeactivated", "OnBeforeUnmount", "OnUnmounted"], vm.Calls);

            vm.Calls.Clear();
            suspense.IsPending = false;
            StaThread.PumpDispatcher();

            // A fresh mount, same as the first time - resolving never "resumes" a torn-down
            // child, matching If's own re-mount behavior.
            Assert.Equal(["OnBeforeCreate", "OnCreated", "OnBeforeMount", "OnMounted", "OnActivated"], vm.Calls);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }
}
