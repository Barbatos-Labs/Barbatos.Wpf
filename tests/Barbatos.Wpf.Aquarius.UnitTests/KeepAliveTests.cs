using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.Aquarius.UnitTests;

// Corresponds to the README's "KeepAlive" section: Vue's <KeepAlive> caches component
// instances across dynamic-component switching, only firing onActivated/onDeactivated, never
// destroying them. These tests check whether WPF's own native "tab/page switching" primitives
// (TabControl, Frame+Page) actually give you that for free, the way the docs claim, or whether
// Directives.Show is genuinely required to get real KeepAlive semantics - confirmed by running
// each one, not assumed from how the primitives are supposed to work.
public class KeepAliveTests
{
    [Fact]
    public void SwitchingAPlainTabControlGenuinelyDestroysAndRecreatesTheDeselectedTabsContent()
    {
        // Disproves the README's current claim that "a plain WPF TabControl already never
        // destroys inactive content." What actually happens: on the very first Show, *both*
        // tabs' content mount (TabControl realizes every TabItem's content upfront, only
        // toggling Visibility for the non-selected one - confirmed via IsLoaded/IsVisible,
        // not just the hooks below). But the moment you actually switch away from a tab, its
        // content is for-real removed from the tree - a genuine OnUnmounted, not a hide - and
        // switching back to it is a fresh OnBeforeMount/OnMounted, not a resume. So the
        // "never destroys" claim only ever held for the narrow initial-render window, not for
        // any realistic "user clicks between tabs" sequence.
        StaThread.Run(() =>
        {
            var vmA = new FakeLifecycleViewModel();
            var contentA = new ContentControl { DataContext = vmA };
            Lifecycle.SetEnable(contentA, true);

            var vmB = new FakeLifecycleViewModel();
            var contentB = new ContentControl { DataContext = vmB };
            Lifecycle.SetEnable(contentB, true);

            var tabControl = new TabControl
            {
                Items =
                {
                    new TabItem { Header = "A", Content = contentA },
                    new TabItem { Header = "B", Content = contentB },
                },
                SelectedIndex = 0,
            };

            var window = new Window { Content = tabControl, Width = 200, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();

            Assert.Contains("OnMounted", vmA.Calls);
            Assert.Contains("OnMounted", vmB.Calls); // both tabs' content load upfront
            vmA.Calls.Clear();
            vmB.Calls.Clear();

            tabControl.SelectedIndex = 1; // switch away from A, to B
            StaThread.PumpDispatcher();

            // The deselected tab (A) is genuinely torn down - not hidden, destroyed.
            Assert.Contains("OnUnmounted", vmA.Calls);

            tabControl.SelectedIndex = 0; // switch back to A, which deselects B
            StaThread.PumpDispatcher();

            // Reselecting A is a fresh mount, not a resume of the same instance's state.
            Assert.Contains("OnBeforeMount", vmA.Calls);
            Assert.Contains("OnMounted", vmA.Calls);

            // B, now that it has gone through a real deselect of its own, is torn down too -
            // the "both load upfront" behavior on the very first Show was not B getting a
            // permanent keep-alive exemption, just startup-window timing.
            Assert.Contains("OnUnmounted", vmB.Calls);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void NavigatingAwayAndBackWithAFrameGenuinelyDestroysAndRecreatesPageContent()
    {
        // Frame.Navigate/GoBack is the closest native WPF analogue of Vue Router's
        // <router-view> switching pages - and confirms the exact same "destroy and
        // recreate" reality as TabControl and Teleport: navigating away from a Page fully
        // unmounts its content, and navigating back is a fresh mount, not a resume - even
        // though the *Page object itself* survives in the journal (GoBack really does
        // return the same Page instance, proven below), its content still cycles through
        // real Loaded/Unloaded on every visit.
        StaThread.Run(() =>
        {
            var vm1 = new FakeLifecycleViewModel();
            var content1 = new ContentControl { DataContext = vm1 };
            Lifecycle.SetEnable(content1, true);
            var page1 = new Page { Content = content1 };

            var vm2 = new FakeLifecycleViewModel();
            var content2 = new ContentControl { DataContext = vm2 };
            Lifecycle.SetEnable(content2, true);
            var page2 = new Page { Content = content2 };

            var frame = new Frame();
            var window = new Window { Content = frame, Width = 200, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();

            frame.Navigate(page1);
            StaThread.PumpDispatcher();

            Assert.Equal(["OnBeforeCreate", "OnCreated", "OnBeforeMount", "OnMounted", "OnActivated"], vm1.Calls);
            vm1.Calls.Clear();

            frame.Navigate(page2);
            StaThread.PumpDispatcher();

            Assert.Equal(["OnDeactivated", "OnBeforeUnmount", "OnUnmounted"], vm1.Calls);
            Assert.Equal(["OnBeforeCreate", "OnCreated", "OnBeforeMount", "OnMounted", "OnActivated"], vm2.Calls);
            vm1.Calls.Clear();
            vm2.Calls.Clear();

            frame.GoBack();
            StaThread.PumpDispatcher();

            // Same Page instance, but a genuinely fresh mount for its content - not a
            // resume of whatever state it had before navigating away.
            Assert.Same(page1, frame.Content);
            Assert.Equal(["OnBeforeCreate", "OnCreated", "OnBeforeMount", "OnMounted", "OnActivated"], vm1.Calls);
            Assert.Equal(["OnDeactivated", "OnBeforeUnmount", "OnUnmounted"], vm2.Calls);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void PageKeepAliveTrueDoesNotChangeContentMountingEitherDespiteTheName()
    {
        // Page.KeepAlive sounds like exactly the switch to flip for Vue-style KeepAlive
        // semantics - but confirmed empirically (identical sequence to the KeepAlive=false
        // test above, only Page.KeepAlive itself differs) that it does not change content
        // Loaded/Unloaded behavior at all for object-instance navigation like this. It
        // governs something else (journal entry retention for URI-based navigation) - it
        // is not the tool for this job, the same way Binding.FallbackValue turned out not
        // to be the tool for SlotContent's "show X when not provided" (see SlotXamlTests).
        StaThread.Run(() =>
        {
            var vm1 = new FakeLifecycleViewModel();
            var content1 = new ContentControl { DataContext = vm1 };
            Lifecycle.SetEnable(content1, true);
            var page1 = new Page { Content = content1, KeepAlive = true };

            var vm2 = new FakeLifecycleViewModel();
            var content2 = new ContentControl { DataContext = vm2 };
            Lifecycle.SetEnable(content2, true);
            var page2 = new Page { Content = content2, KeepAlive = true };

            var frame = new Frame();
            var window = new Window { Content = frame, Width = 200, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();

            frame.Navigate(page1);
            StaThread.PumpDispatcher();
            vm1.Calls.Clear();

            frame.Navigate(page2);
            StaThread.PumpDispatcher();

            Assert.Contains("OnUnmounted", vm1.Calls); // still torn down despite KeepAlive=true
            vm1.Calls.Clear();

            frame.GoBack();
            StaThread.PumpDispatcher();

            Assert.Contains("OnBeforeMount", vm1.Calls); // still a fresh mount, not a resume
            Assert.Contains("OnMounted", vm1.Calls);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void ThreeSectionsToggledViaDirectivesShowGiveGenuineKeepAliveSemantics()
    {
        // The corrected recipe from the README's "KeepAlive" section, run for real: unlike
        // TabControl and Frame+Page above (both confirmed to genuinely destroy and recreate
        // content on every switch), keeping all sections simultaneously present in the tree
        // and toggling Directives.Show between them never unmounts anything at all - only
        // OnActivated/OnDeactivated ever fire, exactly matching Vue's actual <KeepAlive>.
        StaThread.Run(() =>
        {
            var vmA = new FakeLifecycleViewModel();
            var sectionA = new ContentControl { DataContext = vmA };
            Lifecycle.SetEnable(sectionA, true);

            var vmB = new FakeLifecycleViewModel();
            var sectionB = new ContentControl { DataContext = vmB };
            Lifecycle.SetEnable(sectionB, true);

            var vmC = new FakeLifecycleViewModel();
            var sectionC = new ContentControl { DataContext = vmC };
            Lifecycle.SetEnable(sectionC, true);

            Directives.SetShow(sectionA, true);
            Directives.SetShow(sectionB, false);
            Directives.SetShow(sectionC, false);

            var root = new Grid { Children = { sectionA, sectionB, sectionC } };
            var window = new Window { Content = root, Width = 200, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();

            Assert.Equal(["OnBeforeCreate", "OnCreated", "OnBeforeMount", "OnMounted", "OnActivated"], vmA.Calls);
            // B and C mount too (Directives.Show only toggles Visibility, it never keeps
            // content out of the tree) - but correctly do NOT activate, since they were
            // never actually shown (Lifecycle.OnLoaded checks the real IsVisible before
            // reporting activated, not just "did Loaded fire" - see its own comment).
            Assert.Equal(["OnBeforeCreate", "OnCreated", "OnBeforeMount", "OnMounted"], vmB.Calls);
            Assert.Equal(["OnBeforeCreate", "OnCreated", "OnBeforeMount", "OnMounted"], vmC.Calls);
            vmA.Calls.Clear();
            vmB.Calls.Clear();
            vmC.Calls.Clear();

            // "Switch to tab B"
            Directives.SetShow(sectionA, false);
            Directives.SetShow(sectionB, true);
            StaThread.PumpDispatcher();

            Assert.Equal(["OnDeactivated"], vmA.Calls); // hidden, not destroyed
            Assert.Equal(["OnActivated"], vmB.Calls); // revealed, not recreated
            Assert.Empty(vmC.Calls);
            vmA.Calls.Clear();
            vmB.Calls.Clear();

            // "Switch to tab C"
            Directives.SetShow(sectionB, false);
            Directives.SetShow(sectionC, true);
            StaThread.PumpDispatcher();

            Assert.Equal(["OnDeactivated"], vmB.Calls);
            Assert.Equal(["OnActivated"], vmC.Calls);
            vmB.Calls.Clear();
            vmC.Calls.Clear();

            // "Back to tab A" - the whole point: this is NOT a fresh mount.
            Directives.SetShow(sectionC, false);
            Directives.SetShow(sectionA, true);
            StaThread.PumpDispatcher();

            Assert.Equal(["OnDeactivated"], vmC.Calls);
            Assert.Equal(["OnActivated"], vmA.Calls); // just this - no BeforeMount/Mounted

            window.Close();
            StaThread.PumpDispatcher();
        });
    }
}
