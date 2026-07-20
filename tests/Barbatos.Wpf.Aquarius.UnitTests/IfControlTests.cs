using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.Aquarius.UnitTests;

public class IfControlTests
{
    [Fact]
    public void ChildIsMountedWhenConditionIsInitiallyTrue()
    {
        StaThread.Run(() =>
        {
            var child = new Border();

            var ifControl = new If { Condition = true, Child = child };

            Assert.Same(child, ifControl.Content);
        });
    }

    [Fact]
    public void ConditionFalseDetachesTheChild()
    {
        StaThread.Run(() =>
        {
            var child = new Border();
            var ifControl = new If { Condition = true, Child = child };

            ifControl.Condition = false;

            Assert.Null(ifControl.Content);
            Assert.Same(child, ifControl.Child); // still remembered, ready to restore
        });
    }

    [Fact]
    public void ConditionTrueAgainRestoresTheSameChildInstance()
    {
        StaThread.Run(() =>
        {
            var child = new Border();
            var ifControl = new If { Condition = true, Child = child };
            ifControl.Condition = false;

            ifControl.Condition = true;

            Assert.Same(child, ifControl.Content);
        });
    }

    [Fact]
    public void TogglingConditionMountsAndUnmountsTheChildsDataContext()
    {
        StaThread.Run(() =>
        {
            var vm = new FakeLifecycleViewModel();
            var child = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(child, true);
            var ifControl = new If { Condition = true, Child = child };

            var window = new Window { Content = ifControl, Width = 200, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();

            Assert.Contains("OnMounted", vm.Calls);

            vm.Calls.Clear();
            ifControl.Condition = false;
            StaThread.PumpDispatcher();

            Assert.Contains("OnUnmounted", vm.Calls);

            vm.Calls.Clear();
            ifControl.Condition = true;
            StaThread.PumpDispatcher();

            Assert.Contains("OnMounted", vm.Calls);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }
}
