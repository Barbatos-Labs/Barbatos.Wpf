using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.Aquarius.UnitTests;

public class DirectivesEventTests
{
    [Fact]
    public void RaisingTheNamedEventExecutesTheCommand()
    {
        StaThread.Run(() =>
        {
            object? executedWith = null;
            var command = new RelayCommand<object?>(p => executedWith = p);

            var textBox = new TextBox();
            Directives.SetEvent(textBox, nameof(TextBox.TextChanged));
            Directives.SetCommand(textBox, command);

            textBox.Text = "hi"; // raises TextChanged

            Assert.NotNull(executedWith);
            Assert.IsAssignableFrom<TextChangedEventArgs>(executedWith);
        });
    }

    [Fact]
    public void ExplicitCommandParameterOverridesTheEventArgs()
    {
        StaThread.Run(() =>
        {
            object? executedWith = null;
            var command = new RelayCommand<object?>(p => executedWith = p);

            var textBox = new TextBox();
            Directives.SetEvent(textBox, nameof(TextBox.TextChanged));
            Directives.SetCommand(textBox, command);
            Directives.SetCommandParameter(textBox, "fixed");

            textBox.Text = "hi";

            Assert.Equal("fixed", executedWith);
        });
    }

    [Fact]
    public void UnknownEventNameThrows()
    {
        StaThread.Run(() =>
        {
            var textBox = new TextBox();
            Directives.SetCommand(textBox, new RelayCommand(() => { }));

            Assert.Throws<InvalidOperationException>(() => Directives.SetEvent(textBox, "NoSuchEvent"));
        });
    }

    [Fact]
    public void UnloadedUnhooksTheEventHandler()
    {
        StaThread.Run(() =>
        {
            var executions = 0;
            var command = new RelayCommand<object?>(_ => executions++);

            var textBox = new TextBox();
            Directives.SetEvent(textBox, nameof(TextBox.TextChanged));
            Directives.SetCommand(textBox, command);

            textBox.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, textBox));

            textBox.Text = "hi"; // the handler should no longer be attached

            Assert.Equal(0, executions);
        });
    }

    [Fact]
    public void ReloadingAfterAnUnloadRehooksTheEventHandler()
    {
        // The other half of UnloadedUnhooksTheEventHandler above, and the half that was
        // missing: unhooking on Unloaded is only correct if Loaded hooks back up. Every
        // element in this library that gets detached and reattached - an `If` branch
        // toggling, a `Teleport` moving between hosts, a `KeepAlive` eviction, a TabItem
        // being switched away from and back, a routed View re-entered - would otherwise
        // lose its `Directives.Event` wiring permanently on the first unload, while still
        // looking correctly wired from the outside. RouterLink is built directly on this
        // (Directives.SetEvent/SetCommand), so that bug turned any RouterLink inside such
        // a container into a dead button that still highlighted itself as active.
        StaThread.Run(() =>
        {
            var executions = 0;
            var command = new RelayCommand<object?>(_ => executions++);

            var textBox = new TextBox();
            Directives.SetEvent(textBox, nameof(TextBox.TextChanged));
            Directives.SetCommand(textBox, command);

            textBox.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, textBox));
            textBox.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, textBox));

            textBox.Text = "hi";

            Assert.Equal(1, executions);
        });
    }

    [Fact]
    public void ASecondLoadedWithNoInterveningUnloadDoesNotDoubleHook()
    {
        // WPF raises Loaded twice with no Unloaded in between often enough that this
        // library documents it in two places (Lifecycle's own OnLoaded, and the sample's
        // TeleportDemoView) - a TabControl realizes every tab's content when it loads and
        // raises Loaded again on the one that later becomes selected. Re-hooking on every
        // Loaded therefore has to be idempotent, or the command fires once per Loaded.
        StaThread.Run(() =>
        {
            var executions = 0;
            var textBox = new TextBox();
            Directives.SetEvent(textBox, nameof(TextBox.TextChanged));
            Directives.SetCommand(textBox, new RelayCommand<object?>(_ => executions++));

            textBox.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, textBox));
            textBox.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, textBox));

            textBox.Text = "hi";

            Assert.Equal(1, executions);
        });
    }

    [Fact]
    public void TheOnceModifierStaysUnhookedAcrossAReload()
    {
        // `once` is a permanent detach, not the temporary "this element is off-screen"
        // unhook Unloaded performs - so the Loaded re-hook must not resurrect it.
        StaThread.Run(() =>
        {
            var executions = 0;
            var border = new Border();
            Directives.SetEvent(border, nameof(Border.MouseLeftButtonDown));
            Directives.SetCommand(border, new RelayCommand<object?>(_ => executions++));
            Directives.SetModifiers(border, "once");

            Raise();
            Assert.Equal(1, executions);

            border.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, border));
            border.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, border));

            Raise();
            Assert.Equal(1, executions);

            void Raise() => border.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonDownEvent,
            });
        });
    }

    [Fact]
    public void AHyperlinkUnhooksOnUnloadAndRehooksOnLoadJustLikeAFrameworkElement()
    {
        // A Hyperlink is a FrameworkContentElement, not a FrameworkElement - and the
        // Unloaded wiring used to be gated on `target is FrameworkElement`, so a Hyperlink
        // was never unhooked at all. RouterLink explicitly supports both ButtonBase and
        // Hyperlink, so the two supported link kinds had opposite bugs: one lost its
        // handler forever, the other never released it.
        StaThread.Run(() =>
        {
            var executions = 0;
            var hyperlink = new Hyperlink(new Run("link"));
            Directives.SetEvent(hyperlink, nameof(Hyperlink.Click));
            Directives.SetCommand(hyperlink, new RelayCommand<object?>(_ => executions++));

            Click();
            Assert.Equal(1, executions);

            hyperlink.RaiseEvent(new RoutedEventArgs(FrameworkContentElement.UnloadedEvent, hyperlink));
            Click();
            Assert.Equal(1, executions);

            hyperlink.RaiseEvent(new RoutedEventArgs(FrameworkContentElement.LoadedEvent, hyperlink));
            Click();
            Assert.Equal(2, executions);

            void Click() => hyperlink.RaiseEvent(new RoutedEventArgs(Hyperlink.ClickEvent, hyperlink));
        });
    }

    [Fact]
    public void StopModifierMarksTheRoutedEventArgsHandled()
    {
        StaThread.Run(() =>
        {
            var border = new Border();
            Directives.SetEvent(border, nameof(Border.MouseLeftButtonDown));
            Directives.SetCommand(border, new RelayCommand<object?>(_ => { }));
            Directives.SetModifiers(border, "stop");

            var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonDownEvent,
            };
            border.RaiseEvent(args);

            Assert.True(args.Handled);
        });
    }

    [Fact]
    public void PreventModifierAlsoMarksTheRoutedEventArgsHandled()
    {
        StaThread.Run(() =>
        {
            var border = new Border();
            Directives.SetEvent(border, nameof(Border.MouseLeftButtonDown));
            Directives.SetCommand(border, new RelayCommand<object?>(_ => { }));
            Directives.SetModifiers(border, "prevent");

            var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonDownEvent,
            };
            border.RaiseEvent(args);

            Assert.True(args.Handled);
        });
    }

    [Fact]
    public void OnceModifierUnhooksAfterTheFirstInvocation()
    {
        StaThread.Run(() =>
        {
            var executions = 0;
            var border = new Border();
            Directives.SetEvent(border, nameof(Border.MouseLeftButtonDown));
            Directives.SetCommand(border, new RelayCommand<object?>(_ => executions++));
            Directives.SetModifiers(border, "once");

            Raise();
            Raise();

            Assert.Equal(1, executions);

            void Raise() => border.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonDownEvent,
            });
        });
    }

    [Fact]
    public void SelfModifierOnlyInvokesWhenOriginalSourceIsTheElementItself()
    {
        StaThread.Run(() =>
        {
            var executions = 0;
            var child = new TextBlock();
            var border = new Border { Child = child };
            Directives.SetEvent(border, nameof(Border.MouseLeftButtonDown));
            Directives.SetCommand(border, new RelayCommand<object?>(_ => executions++));
            Directives.SetModifiers(border, "self");

            // Simulate the event bubbling up from the child (OriginalSource = child).
            border.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonDownEvent,
                Source = child,
            });
            Assert.Equal(0, executions);

            // Raised directly on the border itself: Source/OriginalSource default to it.
            border.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonDownEvent,
            });
            Assert.Equal(1, executions);
        });
    }

    [Fact]
    public void MouseButtonAliasOnlyInvokesForTheMatchingButton()
    {
        StaThread.Run(() =>
        {
            var executions = 0;
            var border = new Border();
            Directives.SetEvent(border, nameof(Border.MouseDown));
            Directives.SetCommand(border, new RelayCommand<object?>(_ => executions++));
            Directives.SetModifiers(border, "right");

            border.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseDownEvent,
            });
            Assert.Equal(0, executions);

            border.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right)
            {
                RoutedEvent = UIElement.MouseDownEvent,
            });
            Assert.Equal(1, executions);
        });
    }

    [Fact]
    public void KeyAliasOnlyInvokesForTheMatchingKey()
    {
        StaThread.Run(() =>
        {
            var executions = 0;
            var textBox = new TextBox();
            Directives.SetEvent(textBox, nameof(TextBox.KeyDown));
            Directives.SetCommand(textBox, new RelayCommand<object?>(_ => executions++));
            Directives.SetModifiers(textBox, "enter");

            var window = new Window { Content = textBox, Width = 50, Height = 50 };
            window.Show();
            StaThread.PumpDispatcher();

            var source = PresentationSource.FromVisual(textBox);
            Assert.NotNull(source);

            Raise(Key.Tab);
            Assert.Equal(0, executions);

            Raise(Key.Enter);
            Assert.Equal(1, executions);

            window.Close();
            StaThread.PumpDispatcher();

            void Raise(Key key) => textBox.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key)
            {
                RoutedEvent = UIElement.KeyDownEvent,
            });
        });
    }
}
