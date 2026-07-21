using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.Aquarius.UnitTests;

public class DirectivesShowTests
{
    [Fact]
    public void DefaultsToVisible()
    {
        StaThread.Run(() =>
        {
            var element = new Border();

            Assert.Equal(Visibility.Visible, element.Visibility);
        });
    }

    [Fact]
    public void FalseCollapsesTheElement()
    {
        StaThread.Run(() =>
        {
            var element = new Border();

            Directives.SetShow(element, false);

            Assert.Equal(Visibility.Collapsed, element.Visibility);
        });
    }

    [Fact]
    public void TrueRestoresVisible()
    {
        StaThread.Run(() =>
        {
            var element = new Border();
            Directives.SetShow(element, false);

            Directives.SetShow(element, true);

            Assert.Equal(Visibility.Visible, element.Visibility);
        });
    }

    [Fact]
    public void TogglingShowFiresActivatedAndDeactivatedButNeverUnmountsOrRemounts()
    {
        // This is the "KeepAlive" port in practice: unlike If (which genuinely destroys and
        // recreates its child - see IfControlTests), Directives.Show never removes the
        // element from the tree at all, so OnMounted/OnUnmounted only ever fire once each,
        // for the element's real attach/detach - toggling Show only ever fires
        // OnActivated/OnDeactivated, mirroring Vue's onActivated/onDeactivated for a
        // <KeepAlive>-cached component switching in and out of view.
        StaThread.Run(() =>
        {
            var vm = new FakeLifecycleViewModel();
            var element = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(element, true);

            var window = new Window { Content = element, Width = 200, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();

            Assert.Equal(["OnBeforeCreate", "OnCreated", "OnBeforeMount", "OnMounted", "OnActivated"], vm.Calls);
            vm.Calls.Clear();

            Directives.SetShow(element, false);
            StaThread.PumpDispatcher();

            Assert.Equal(["OnDeactivated"], vm.Calls); // not unmounted - still in the tree
            vm.Calls.Clear();

            Directives.SetShow(element, true);
            StaThread.PumpDispatcher();

            Assert.Equal(["OnActivated"], vm.Calls); // not mounted again - never left

            window.Close();
            StaThread.PumpDispatcher();
        });
    }
}
