using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Barbatos.Wpf.Aquarius.UnitTests;

/// <summary>
/// Several <see cref="Teleport"/>s rendering into one host - a toolbar assembled from widgets
/// declared all over the app, an overlay stack, a docking edge.
/// </summary>
/// <remarks>
/// The thing under test throughout is that the resulting child order is <see cref="Teleport.Order"/>
/// and nothing else: not declaration order, not which host registered first, not which
/// <see cref="Teleport"/> happened to load first. Every test here deliberately loads its
/// <see cref="Teleport"/>s in an order that disagrees with their <see cref="Teleport.Order"/>,
/// so a regression to plain <c>Children.Add</c> append order fails rather than passing by luck.
/// </remarks>
public class TeleportMultiHostTests
{
    [Fact]
    public void ThreeTeleportsSharingAHostLandInOrderNotInLoadOrder()
    {
        StaThread.Run(() =>
        {
            var hostName = NewHostName();
            var host = new StackPanel();
            TeleportHost.SetRegisterHost(host, hostName);

            // Declared last-first-middle; expected to render first-middle-last.
            var last = Widget("last", hostName, order: 20);
            var first = Widget("first", hostName, order: 0);
            var middle = Widget("middle", hostName, order: 10);

            using var window = Show(host, last, first, middle);

            Assert.Equal(["first", "middle", "last"], NamesIn(host));
        });
    }

    [Fact]
    public void AHostRegisteringAfterEveryTeleportHasLoadedStillGetsThemInOrder()
    {
        // The late-host path goes through TeleportHost.HostRegistered instead of each
        // Teleport's own Loaded, and the notification order there is subscription order -
        // i.e. load order - so it is the case most likely to come out backwards.
        StaThread.Run(() =>
        {
            var hostName = NewHostName();
            var host = new StackPanel();

            var third = Widget("third", hostName, order: 3);
            var firstWidget = Widget("first", hostName, order: 1);
            var second = Widget("second", hostName, order: 2);

            using var window = Show(host, third, firstWidget, second);
            Assert.Empty(NamesIn(host)); // no host registered yet

            TeleportHost.SetRegisterHost(host, hostName);
            StaThread.PumpDispatcher();

            Assert.Equal(["first", "second", "third"], NamesIn(host));
        });
    }

    [Fact]
    public void EqualOrdersFallBackToCreationOrderRatherThanSomethingArbitrary()
    {
        StaThread.Run(() =>
        {
            var hostName = NewHostName();
            var host = new StackPanel();
            TeleportHost.SetRegisterHost(host, hostName);

            // All at the default Order - created a/b/c, loaded c/b/a.
            var a = Widget("a", hostName);
            var b = Widget("b", hostName);
            var c = Widget("c", hostName);

            using var window = Show(host, c, b, a);

            Assert.Equal(["a", "b", "c"], NamesIn(host));
        });
    }

    [Fact]
    public void TheHostsOwnStaticChildrenAreKeptAndTeleportedContentGoesAfterThem()
    {
        // A host is allowed to have permanent content of its own - a StackPanel's fixed
        // header, a DockPanel's status bar. Teleported children are inserted relative to each
        // other only, never relative to those.
        StaThread.Run(() =>
        {
            var hostName = NewHostName();
            var host = new StackPanel { Children = { new Border { Name = "header" } } };
            TeleportHost.SetRegisterHost(host, hostName);

            var second = Widget("second", hostName, order: 2);
            var first = Widget("first", hostName, order: 1);

            using var window = Show(host, second, first);

            Assert.Equal(["header", "first", "second"], NamesIn(host));
        });
    }

    [Theory]
    [InlineData(typeof(UniformGrid))]
    [InlineData(typeof(StackPanel))]
    [InlineData(typeof(WrapPanel))]
    [InlineData(typeof(DockPanel))]
    [InlineData(typeof(Grid))]
    [InlineData(typeof(Canvas))]
    public void AnyPanelTypeWorksAsAHostForSeveralTeleports(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type panelType)
    {
        // TeleportHost has always accepted any Panel, but every example only ever showed a
        // Grid - which is the one panel where "several children" means "stacked on top of each
        // other" rather than laid out side by side. These are the layouts that actually make
        // multi-Teleport hosting useful, pinned so a future change can't quietly narrow it.
        StaThread.Run(() =>
        {
            var hostName = NewHostName();
            var host = (Panel)Activator.CreateInstance(panelType)!;
            TeleportHost.SetRegisterHost(host, hostName);

            var b = Widget("b", hostName, order: 2);
            var a = Widget("a", hostName, order: 1);

            using var window = Show(host, b, a);

            Assert.Equal(["a", "b"], NamesIn(host));
        });
    }

    [Fact]
    public void LayoutAttachedPropertiesSetOnTheContentSurviveTheTeleport()
    {
        // Teleport re-parents the content element itself, so DockPanel.Dock (or Grid.Row, or
        // any other layout attached property) declared on that element keeps working in the
        // host - which is what lets each teleported widget pick its own side of a DockPanel.
        StaThread.Run(() =>
        {
            var hostName = NewHostName();
            var host = new DockPanel();
            TeleportHost.SetRegisterHost(host, hostName);

            var content = new Border { Name = "docked" };
            DockPanel.SetDock(content, Dock.Bottom);
            var teleport = new Teleport { To = hostName, Content = content };

            using var window = Show(host, teleport);

            Assert.Same(content, host.Children[0]);
            Assert.Equal(Dock.Bottom, DockPanel.GetDock(content));
        });
    }

    [Fact]
    public void UnloadingOneTeleportLeavesTheOthersInOrder()
    {
        StaThread.Run(() =>
        {
            var hostName = NewHostName();
            var host = new StackPanel();
            TeleportHost.SetRegisterHost(host, hostName);

            var a = Widget("a", hostName, order: 1);
            var b = Widget("b", hostName, order: 2);
            var c = Widget("c", hostName, order: 3);

            var root = new StackPanel { Children = { host, a, b, c } };
            var window = new Window { Content = root, Width = 200, Height = 200 };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();
                Assert.Equal(["a", "b", "c"], NamesIn(host));

                root.Children.Remove(b);
                StaThread.PumpDispatcher();

                Assert.Equal(["a", "c"], NamesIn(host));
            }
            finally
            {
                window.Close();
                StaThread.PumpDispatcher();
            }
        });
    }

    [Fact]
    public void ChangingOrderAtRuntimeRepositionsOnlyThatTeleportsContent()
    {
        StaThread.Run(() =>
        {
            var hostName = NewHostName();
            var host = new StackPanel();
            TeleportHost.SetRegisterHost(host, hostName);

            var a = Widget("a", hostName, order: 1);
            var b = Widget("b", hostName, order: 2);
            var c = Widget("c", hostName, order: 3);

            using var window = Show(host, a, b, c);
            Assert.Equal(["a", "b", "c"], NamesIn(host));

            var untouched = host.Children[0];

            c.Order = 0;
            StaThread.PumpDispatcher();

            Assert.Equal(["c", "a", "b"], NamesIn(host));

            // "Only that Teleport's content" - a and b were never removed and re-added, so
            // they keep their identity (and, with it, their own mount state).
            Assert.Same(untouched, host.Children[1]);
        });
    }

    [Fact]
    public void ContentComesHomeInOrderAfterTheHostDisappearsAndComesBack()
    {
        StaThread.Run(() =>
        {
            var hostName = NewHostName();
            var host = new StackPanel();
            TeleportHost.SetRegisterHost(host, hostName);

            var a = Widget("a", hostName, order: 1);
            var b = Widget("b", hostName, order: 2);

            var root = new StackPanel { Children = { host, a, b } };
            var window = new Window { Content = root, Width = 200, Height = 200 };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();
                Assert.Equal(["a", "b"], NamesIn(host));

                // The host goes away (e.g. its owning dialog closes) - both Teleports bring
                // their content home rather than leaving it in a torn-down tree.
                root.Children.Remove(host);
                StaThread.PumpDispatcher();
                Assert.Empty(NamesIn(host));
                Assert.NotNull(a.Content);
                Assert.NotNull(b.Content);

                // ...and re-registering re-floats both, still in Order.
                root.Children.Insert(0, host);
                StaThread.PumpDispatcher();
                Assert.Equal(["a", "b"], NamesIn(host));
            }
            finally
            {
                window.Close();
                StaThread.PumpDispatcher();
            }
        });
    }

    private static string NewHostName() => $"Host-{Guid.NewGuid()}";

    /// <summary>A <see cref="Teleport"/> whose content is a single named <see cref="Border"/>, so order is readable as a list of strings.</summary>
    private static Teleport Widget(string name, string hostName, int order = 0) =>
        new() { To = hostName, Order = order, Content = new Border { Name = name } };

    private static IEnumerable<string> NamesIn(Panel host) => host.Children.OfType<FrameworkElement>().Select(child => child.Name);

    /// <summary>Shows <paramref name="children"/> in a real window, in the order given - which is the order their <c>Loaded</c> fires in.</summary>
    private static WindowScope Show(Panel host, params Teleport[] children)
    {
        var root = new StackPanel { Children = { host } };

        foreach (var child in children)
            root.Children.Add(child);

        var window = new Window { Content = root, Width = 200, Height = 200 };
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
