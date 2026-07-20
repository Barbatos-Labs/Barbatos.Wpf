using System.Windows;
using System.Windows.Controls;
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
