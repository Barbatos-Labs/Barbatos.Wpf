using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.Aquarius.UnitTests;

public class DirectiveExtensibilityTests
{
    private sealed class RecordingDirective : Directive
    {
        public List<string> Calls { get; } = [];

        public List<DirectiveBinding> Updates { get; } = [];

        public override void Mounted(FrameworkElement element, DirectiveBinding binding) => Calls.Add($"Mounted:{element.GetType().Name}");

        public override void Updated(FrameworkElement element, DirectiveBinding binding)
        {
            Calls.Add($"Updated:{element.GetType().Name}");
            Updates.Add(binding);
        }

        public override void Unmounted(FrameworkElement element, DirectiveBinding binding) => Calls.Add($"Unmounted:{element.GetType().Name}");
    }

    [Fact]
    public void MountedFiresOnLoaded()
    {
        StaThread.Run(() =>
        {
            var directive = new RecordingDirective();
            var element = new Border();
            Directives.SetUse(element, directive);

            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

            Assert.Equal(["Mounted:Border"], directive.Calls);
        });
    }

    [Fact]
    public void UnmountedFiresOnUnloaded()
    {
        StaThread.Run(() =>
        {
            var directive = new RecordingDirective();
            var element = new Border();
            Directives.SetUse(element, directive);
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));
            directive.Calls.Clear();

            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));

            Assert.Equal(["Unmounted:Border"], directive.Calls);
        });
    }

    [Fact]
    public void ReplacingTheDirectiveUnmountsTheOldOne()
    {
        StaThread.Run(() =>
        {
            var first = new RecordingDirective();
            var second = new RecordingDirective();
            var element = new Border();
            Directives.SetUse(element, first);
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

            Directives.SetUse(element, second);

            Assert.Contains("Unmounted:Border", first.Calls);
            Assert.DoesNotContain("Mounted:Border", second.Calls); // Loaded hasn't been (re-)raised yet
        });
    }

    [Fact]
    public void MountedReceivesTheInitialValueArgumentAndModifiers()
    {
        StaThread.Run(() =>
        {
            var directive = new RecordingDirective();
            var element = new Border();
            Directives.SetUseValue(element, "hello");
            Directives.SetArgument(element, "foo");
            Directives.SetModifiers(element, "bar,baz");
            Directives.SetUse(element, directive);

            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

            Assert.Equal(["Mounted:Border"], directive.Calls);
        });
    }

    [Fact]
    public void ChangingUseValueFiresUpdatedWithValueAndOldValue()
    {
        StaThread.Run(() =>
        {
            var directive = new RecordingDirective();
            var element = new Border();
            Directives.SetUseValue(element, "first");
            Directives.SetUse(element, directive);
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

            Directives.SetUseValue(element, "second");

            var update = Assert.Single(directive.Updates);
            Assert.Equal("second", update.Value);
            Assert.Equal("first", update.OldValue);
        });
    }

    [Fact]
    public void ChangingArgumentOrModifiersFiresUpdatedWithCurrentBindingState()
    {
        StaThread.Run(() =>
        {
            var directive = new RecordingDirective();
            var element = new Border();
            Directives.SetUseValue(element, "value");
            Directives.SetUse(element, directive);
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

            Directives.SetArgument(element, "foo");
            Directives.SetModifiers(element, "bar,baz");

            Assert.Equal(2, directive.Updates.Count);
            var last = directive.Updates[^1];
            Assert.Equal("value", last.Value);
            Assert.Equal("foo", last.Argument);
            Assert.Equal(new HashSet<string> { "bar", "baz" }, last.Modifiers);
        });
    }

    [Fact]
    public void UpdatedDoesNotFireWhileUnmounted()
    {
        StaThread.Run(() =>
        {
            var directive = new RecordingDirective();
            var element = new Border();
            Directives.SetUse(element, directive);

            Directives.SetUseValue(element, "not mounted yet");

            Assert.Empty(directive.Updates);
        });
    }

    [Fact]
    public void ASecondLoadedWithNoInterveningUnloadDoesNotMountTwice()
    {
        // Same guard Lifecycle's own OnLoaded already has, and for the same documented
        // reason: WPF raises Loaded a second time with no Unloaded in between (a TabControl
        // realizes every tab's content up front, then raises Loaded again on whichever one
        // later becomes selected). Without it a custom directive sees two Mounted calls for
        // one real mount - and the second arrives with oldValue: null, silently discarding
        // whatever state the first one set up.
        StaThread.Run(() =>
        {
            var directive = new RecordingDirective();
            var element = new Border();
            Directives.SetUse(element, directive);

            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));
            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));

            Assert.Equal(["Mounted:Border"], directive.Calls);
        });
    }

    [Fact]
    public void AnUnloadedThatNeverMountedDoesNotFireUnmounted()
    {
        // The symmetric half: WPF can raise Unloaded for an element that never completed a
        // Loaded, which used to produce an Unmounted with no matching Mounted before it.
        StaThread.Run(() =>
        {
            var directive = new RecordingDirective();
            var element = new Border();
            Directives.SetUse(element, directive);

            element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));

            Assert.Empty(directive.Calls);
        });
    }
}
