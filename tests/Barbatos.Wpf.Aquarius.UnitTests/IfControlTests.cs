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
    public void ElseIsShownWhenConditionIsFalse()
    {
        StaThread.Run(() =>
        {
            var child = new Border();
            var elseContent = new Border();

            var ifControl = new If { Condition = false, Child = child, Else = elseContent };

            Assert.Same(elseContent, ifControl.Content);
        });
    }

    [Fact]
    public void ChildStillWinsOverElseWhenConditionIsTrue()
    {
        StaThread.Run(() =>
        {
            var child = new Border();
            var elseContent = new Border();

            var ifControl = new If { Condition = true, Child = child, Else = elseContent };

            Assert.Same(child, ifControl.Content);
        });
    }

    [Fact]
    public void ChangingElseWhileConditionIsFalseUpdatesContentImmediately()
    {
        StaThread.Run(() =>
        {
            var ifControl = new If { Condition = false, Child = new Border(), Else = new Border() };
            var newElse = new Border();

            ifControl.Else = newElse;

            Assert.Same(newElse, ifControl.Content);
        });
    }

    [Fact]
    public void ChangingElseWhileConditionIsTrueDoesNotChangeVisibleContent()
    {
        StaThread.Run(() =>
        {
            var child = new Border();
            var ifControl = new If { Condition = true, Child = child, Else = new Border() };

            ifControl.Else = new Border();

            Assert.Same(child, ifControl.Content);
        });
    }

    [Fact]
    public void NestedIfInElseActsAsAThreeBranchElseIfChain()
    {
        StaThread.Run(() =>
        {
            var contentA = new Border();
            var contentB = new Border();
            var contentFallback = new Border();

            var innerIf = new If { Condition = false, Child = contentB, Else = contentFallback };
            var outerIf = new If { Condition = false, Child = contentA, Else = innerIf };

            void SetType(string type)
            {
                outerIf.Condition = type == "A";
                innerIf.Condition = type == "B";
            }

            SetType("A");
            Assert.Same(contentA, outerIf.Content);

            SetType("B");
            Assert.Same(innerIf, outerIf.Content);
            Assert.Same(contentB, innerIf.Content);

            SetType("C");
            Assert.Same(innerIf, outerIf.Content);
            Assert.Same(contentFallback, innerIf.Content);
        });
    }

    [Fact]
    public void SwitchingFromOneBranchToTheNextFiresUnmountThenMount()
    {
        StaThread.Run(() =>
        {
            var vmA = new FakeLifecycleViewModel();
            var vmB = new FakeLifecycleViewModel();
            var contentA = new ContentControl { DataContext = vmA };
            Lifecycle.SetEnable(contentA, true);
            var contentB = new ContentControl { DataContext = vmB };
            Lifecycle.SetEnable(contentB, true);

            var ifControl = new If { Condition = true, Child = contentA, Else = contentB };
            var window = new Window { Content = ifControl, Width = 200, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();

            Assert.Contains("OnMounted", vmA.Calls);
            vmA.Calls.Clear();

            ifControl.Condition = false;
            StaThread.PumpDispatcher();

            Assert.Contains("OnUnmounted", vmA.Calls);
            Assert.Contains("OnMounted", vmB.Calls);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void JumpingDirectlyFromTheFirstToTheLastBranchNeverMountsTheSkippedMiddleOne()
    {
        StaThread.Run(() =>
        {
            var vmA = new FakeLifecycleViewModel();
            var vmB = new FakeLifecycleViewModel();
            var vmC = new FakeLifecycleViewModel();
            var contentA = new ContentControl { DataContext = vmA };
            Lifecycle.SetEnable(contentA, true);
            var contentB = new ContentControl { DataContext = vmB };
            Lifecycle.SetEnable(contentB, true);
            var contentC = new ContentControl { DataContext = vmC };
            Lifecycle.SetEnable(contentC, true);

            // innerIf.Condition starts (and stays) false - it always shows C, never B.
            var innerIf = new If { Condition = false, Child = contentB, Else = contentC };
            var outerIf = new If { Condition = true, Child = contentA, Else = innerIf };

            var window = new Window { Content = outerIf, Width = 200, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();

            Assert.Contains("OnMounted", vmA.Calls);

            outerIf.Condition = false; // "A" -> "C" directly
            StaThread.PumpDispatcher();

            Assert.Contains("OnUnmounted", vmA.Calls);
            Assert.Contains("OnMounted", vmC.Calls);
            Assert.Empty(vmB.Calls); // B's DataContext was never mounted at all

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void ConditionTogglingFiresTheFullVueStyleMountAndUnmountSequence()
    {
        // Not just "Contains" - the exact BeforeMount->Mounted / BeforeUnmount->Unmounted
        // order Vue itself documents for v-if, since If is the primary port of that "destroy
        // and recreate a subtree" semantic (unlike Directives.Show/v-show, which never
        // unmounts at all - see DirectivesShowTests for that distinction).
        StaThread.Run(() =>
        {
            var vm = new FakeLifecycleViewModel();
            var child = new ContentControl { DataContext = vm };
            Lifecycle.SetEnable(child, true);
            var ifControl = new If { Condition = true, Child = child };

            var window = new Window { Content = ifControl, Width = 200, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();

            Assert.Equal(["OnBeforeCreate", "OnCreated", "OnBeforeMount", "OnMounted", "OnActivated"], vm.Calls);

            vm.Calls.Clear();
            ifControl.Condition = false;
            StaThread.PumpDispatcher();

            // Deactivated fires before BeforeUnmount here - unlike a synthetic RaiseEvent
            // test, a real detach from the visual tree fires the real IsVisibleChanged
            // (going false) slightly before Unloaded does, so by the time OnUnloaded's own
            // SetVisibleActivated(false) call runs it's already a no-op (dedup guard).
            Assert.Equal(["OnDeactivated", "OnBeforeUnmount", "OnUnmounted"], vm.Calls);

            vm.Calls.Clear();
            ifControl.Condition = true;
            StaThread.PumpDispatcher();

            // A fresh mount, same as the first time - If never "resumes" a torn-down child,
            // it genuinely destroys and recreates it, matching Vue's real v-if.
            Assert.Equal(["OnBeforeCreate", "OnCreated", "OnBeforeMount", "OnMounted", "OnActivated"], vm.Calls);

            window.Close();
            StaThread.PumpDispatcher();
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
