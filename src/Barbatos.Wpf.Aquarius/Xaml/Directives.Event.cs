// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace Barbatos.Wpf.Xaml;

public static partial class Directives
{
    /// <summary>
    /// Names the event to wire to <see cref="CommandProperty"/> - the Aquarius counterpart of
    /// Vue's generic <c>v-on:eventname="handler"</c>.
    /// </summary>
    /// <remarks>
    /// <code>
    /// &lt;Border aq:Directives.Event="MouseLeftButtonDown" aq:Directives.Command="{Binding BorderClickedCommand}" /&gt;
    /// </code>
    /// Unlike a curated list of hardcoded event/command attached properties, this wires
    /// up any public .NET event by name via reflection
    /// (<see cref="Type.GetEvent(string)"/> + <see cref="Delegate.CreateDelegate(Type, object?, MethodInfo)"/>
    /// against a single relay method) - it works for events WPF gives no built-in
    /// <c>Command</c> for (e.g. <c>Border.MouseLeftButtonDown</c>, <c>TextBox.TextChanged</c>),
    /// the same way <c>v-on</c> is not limited to a fixed event list in Vue. The command is
    /// executed with <see cref="CommandParameterProperty"/> if set, otherwise the raised
    /// <see cref="EventArgs"/> itself. Automatically unhooked on <c>Unloaded</c>. Also honors
    /// <see cref="ModifiersProperty"/> - see "Modifiers" below.
    ///
    /// <b>Modifiers</b> (<see cref="ModifiersProperty"/>, comma-separated) - a curated subset
    /// of Vue's real <c>v-on</c> modifier list, each a no-op unless the raised
    /// <see cref="EventArgs"/> is actually the matching type:
    /// <list type="bullet">
    /// <item><c>stop</c>, <c>prevent</c> - both set <see cref="RoutedEventArgs.Handled"/> on a
    /// routed event. WPF collapses the DOM's separate stopPropagation/preventDefault into one
    /// <c>Handled</c> flag, so these two are honestly the same operation here.</item>
    /// <item><c>once</c> - unhooks after this single invocation.</item>
    /// <item><c>self</c> - only invokes the command if <c>OriginalSource</c> is the element
    /// itself, not a descendant the event bubbled up from.</item>
    /// <item><c>left</c>/<c>right</c>/<c>middle</c> - only invokes for that
    /// <see cref="MouseButtonEventArgs.ChangedButton"/>.</item>
    /// <item><c>enter</c>/<c>tab</c>/<c>esc</c>/<c>space</c>/<c>up</c>/<c>down</c>/<c>left</c>/
    /// <c>right</c>/<c>delete</c> - only invokes for that <see cref="KeyEventArgs.Key"/>
    /// (<c>delete</c> matches both Delete and Backspace, matching Vue's own alias).</item>
    /// </list>
    /// <c>capture</c> is intentionally not a modifier here - WPF already has a native, more
    /// idiomatic equivalent, the <c>Preview{EventName}</c> tunneling event, so
    /// <c>Directives.Event="PreviewMouseDown"</c> *is* the capture-phase port. <c>passive</c> is
    /// skipped as not applicable (a browser scroll-performance concept with no WPF equivalent).
    /// </remarks>
    public static readonly DependencyProperty EventProperty =
        DependencyProperty.RegisterAttached(
            "Event",
            typeof(string),
            typeof(Directives),
            new PropertyMetadata(null, OnEventBindingChanged));

    /// <summary>The command executed when <see cref="EventProperty"/> is raised.</summary>
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached(
            "Command",
            typeof(ICommand),
            typeof(Directives),
            new PropertyMetadata(null, OnEventBindingChanged));

    /// <summary>
    /// The parameter passed to <see cref="CommandProperty"/>. Defaults to the raised
    /// <see cref="EventArgs"/> when unset.
    /// </summary>
    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.RegisterAttached("CommandParameter", typeof(object), typeof(Directives));

    /// <summary>Sets <see cref="EventProperty"/>.</summary>
    public static void SetEvent(DependencyObject element, string? value) => element.SetValue(EventProperty, value);

    /// <summary>Gets <see cref="EventProperty"/>.</summary>
    public static string? GetEvent(DependencyObject element) => (string?)element.GetValue(EventProperty);

    /// <summary>Sets <see cref="CommandProperty"/>.</summary>
    public static void SetCommand(DependencyObject element, ICommand? value) => element.SetValue(CommandProperty, value);

    /// <summary>Gets <see cref="CommandProperty"/>.</summary>
    public static ICommand? GetCommand(DependencyObject element) => (ICommand?)element.GetValue(CommandProperty);

    /// <summary>Sets <see cref="CommandParameterProperty"/>.</summary>
    public static void SetCommandParameter(DependencyObject element, object? value) => element.SetValue(CommandParameterProperty, value);

    /// <summary>Gets <see cref="CommandParameterProperty"/>.</summary>
    public static object? GetCommandParameter(DependencyObject element) => element.GetValue(CommandParameterProperty);

    private static readonly DependencyProperty EventBindingStateProperty =
        DependencyProperty.RegisterAttached("EventBindingState", typeof(EventBindingState), typeof(Directives));

    private static void OnEventBindingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d.GetValue(EventBindingStateProperty) is EventBindingState existing)
        {
            existing.Unhook();
            d.ClearValue(EventBindingStateProperty);
        }

        var eventName = GetEvent(d);
        var command = GetCommand(d);

        if (string.IsNullOrEmpty(eventName) || command is null)
            return;

        var state = new EventBindingState(d, eventName);
        d.SetValue(EventBindingStateProperty, state);
        state.Hook();
    }

    private static bool MatchesKeyAlias(string alias, Key key) => alias switch
    {
        "enter" => key == Key.Enter,
        "tab" => key == Key.Tab,
        "esc" => key == Key.Escape,
        "space" => key == Key.Space,
        "up" => key == Key.Up,
        "down" => key == Key.Down,
        "left" => key == Key.Left,
        "right" => key == Key.Right,
        "delete" => key is Key.Delete or Key.Back,
        _ => false,
    };

    private static readonly string[] KeyAliasNames = ["enter", "tab", "esc", "space", "up", "down", "left", "right", "delete"];

    private static bool MatchesMouseButtonAlias(string alias, MouseButton button) => alias switch
    {
        "left" => button == MouseButton.Left,
        "right" => button == MouseButton.Right,
        "middle" => button == MouseButton.Middle,
        _ => false,
    };

    private static readonly string[] MouseButtonAliasNames = ["left", "right", "middle"];

    private static bool PassesModifierGate(EventArgs e, object? sender, IReadOnlySet<string> modifiers)
    {
        if (modifiers.Contains("self") && e is RoutedEventArgs selfArgs && !ReferenceEquals(selfArgs.OriginalSource, sender))
            return false;

        if (e is MouseButtonEventArgs mouse)
        {
            var present = Array.FindAll(MouseButtonAliasNames, modifiers.Contains);

            if (present.Length > 0 && Array.TrueForAll(present, alias => !MatchesMouseButtonAlias(alias, mouse.ChangedButton)))
                return false;
        }

        if (e is KeyEventArgs key)
        {
            var present = Array.FindAll(KeyAliasNames, modifiers.Contains);

            if (present.Length > 0 && Array.TrueForAll(present, alias => !MatchesKeyAlias(alias, key.Key)))
                return false;
        }

        return true;
    }

    private sealed class EventBindingState
    {
        private readonly DependencyObject _target;
        private readonly EventInfo _eventInfo;
        private readonly Delegate _handler;
        private bool _hooked;

        public EventBindingState(DependencyObject target, string eventName)
        {
            _target = target;

            _eventInfo = target.GetType().GetEvent(eventName, BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException(
                    $"Directives.Event: '{target.GetType().Name}' has no public event named '{eventName}'.");

            var relayMethod = typeof(EventBindingState).GetMethod(nameof(Relay), BindingFlags.NonPublic | BindingFlags.Instance)!;
            _handler = Delegate.CreateDelegate(_eventInfo.EventHandlerType!, this, relayMethod);

            if (target is FrameworkElement element)
                element.Unloaded += OnUnloaded;
        }

        public void Hook()
        {
            _eventInfo.AddEventHandler(_target, _handler);
            _hooked = true;
        }

        public void Unhook()
        {
            if (!_hooked)
                return;

            _eventInfo.RemoveEventHandler(_target, _handler);
            _hooked = false;

            if (_target is FrameworkElement element)
                element.Unloaded -= OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) => Unhook();

        private void Relay(object? sender, EventArgs e)
        {
            var modifiers = ParseModifiers(GetModifiers(_target));

            if (modifiers.Count > 0)
            {
                if ((modifiers.Contains("stop") || modifiers.Contains("prevent")) && e is RoutedEventArgs stopArgs)
                    stopArgs.Handled = true;

                if (modifiers.Contains("once"))
                    Unhook();

                if (!PassesModifierGate(e, sender, modifiers))
                    return;
            }

            var command = GetCommand(_target);
            var parameter = GetCommandParameter(_target) ?? e;

            if (command?.CanExecute(parameter) == true)
                command.Execute(parameter);
        }
    }
}
