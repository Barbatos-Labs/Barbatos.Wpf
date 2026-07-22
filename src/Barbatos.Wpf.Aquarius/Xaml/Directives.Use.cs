// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;

namespace Barbatos.Wpf.Aquarius.Xaml;

public static partial class Directives
{
    /// <summary>
    /// Attaches a custom <see cref="Directive"/> instance to an element, mirroring Vue's
    /// <c>v-directivename</c> usage.
    /// </summary>
    /// <remarks>
    /// <code>
    /// &lt;TextBox aq:Directives.Use="{StaticResource AutoFocus}"
    ///          aq:Directives.UseValue="{Binding SomeValue}"
    ///          aq:Directives.Argument="foo"
    ///          aq:Directives.Modifiers="bar,baz" /&gt;
    /// </code>
    /// v1 supports one directive per element through this property - composing several
    /// is possible by writing a directive that itself dispatches to others. A
    /// collection-valued version is real future work, not built speculatively here.
    /// </remarks>
    public static readonly DependencyProperty UseProperty =
        DependencyProperty.RegisterAttached(
            "Use",
            typeof(Directive),
            typeof(Directives),
            new PropertyMetadata(null, OnUseChanged));

    /// <summary>
    /// The bound value passed to the directive's hooks as <see cref="DirectiveBinding.Value"/>
    /// (and the previous value as <see cref="DirectiveBinding.OldValue"/>) - mirrors a Vue
    /// custom directive's own binding value, <c>v-my-directive="value"</c>. Changing this
    /// while mounted calls <see cref="Directive.Updated"/>.
    /// </summary>
    public static readonly DependencyProperty UseValueProperty =
        DependencyProperty.RegisterAttached(
            "UseValue",
            typeof(object),
            typeof(Directives),
            new PropertyMetadata(null, OnDirectiveBindingChanged));

    /// <summary>
    /// Passed to the directive's hooks as <see cref="DirectiveBinding.Argument"/> - mirrors
    /// the <c>arg</c> in Vue's <c>v-my-directive:arg="value"</c>.
    /// </summary>
    public static readonly DependencyProperty ArgumentProperty =
        DependencyProperty.RegisterAttached(
            "Argument",
            typeof(string),
            typeof(Directives),
            new PropertyMetadata(null, OnDirectiveBindingChanged));

    /// <summary>
    /// A comma-separated modifier list (e.g. <c>"stop,prevent"</c>), parsed into
    /// <see cref="DirectiveBinding.Modifiers"/> - mirrors Vue's dot-chained
    /// <c>v-my-directive.mod1.mod2="value"</c>. Also read by
    /// <see cref="EventProperty"/>'s modifier handling.
    /// </summary>
    public static readonly DependencyProperty ModifiersProperty =
        DependencyProperty.RegisterAttached(
            "Modifiers",
            typeof(string),
            typeof(Directives),
            new PropertyMetadata(null, OnDirectiveBindingChanged));

    /// <summary>Sets <see cref="UseProperty"/>.</summary>
    public static void SetUse(DependencyObject element, Directive? value) => element.SetValue(UseProperty, value);

    /// <summary>Gets <see cref="UseProperty"/>.</summary>
    public static Directive? GetUse(DependencyObject element) => (Directive?)element.GetValue(UseProperty);

    /// <summary>Sets <see cref="UseValueProperty"/>.</summary>
    public static void SetUseValue(DependencyObject element, object? value) => element.SetValue(UseValueProperty, value);

    /// <summary>Gets <see cref="UseValueProperty"/>.</summary>
    public static object? GetUseValue(DependencyObject element) => element.GetValue(UseValueProperty);

    /// <summary>Sets <see cref="ArgumentProperty"/>.</summary>
    public static void SetArgument(DependencyObject element, string? value) => element.SetValue(ArgumentProperty, value);

    /// <summary>Gets <see cref="ArgumentProperty"/>.</summary>
    public static string? GetArgument(DependencyObject element) => (string?)element.GetValue(ArgumentProperty);

    /// <summary>Sets <see cref="ModifiersProperty"/>.</summary>
    public static void SetModifiers(DependencyObject element, string? value) => element.SetValue(ModifiersProperty, value);

    /// <summary>Gets <see cref="ModifiersProperty"/>.</summary>
    public static string? GetModifiers(DependencyObject element) => (string?)element.GetValue(ModifiersProperty);

    /// <summary>Parses <see cref="ModifiersProperty"/>'s comma-separated form into a set.</summary>
    internal static IReadOnlySet<string> ParseModifiers(string? modifiers) =>
        string.IsNullOrEmpty(modifiers)
            ? EmptyModifiers
            : new HashSet<string>(modifiers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static readonly IReadOnlySet<string> EmptyModifiers = new HashSet<string>();

    private static readonly DependencyProperty UseStateProperty =
        DependencyProperty.RegisterAttached("UseState", typeof(DirectiveState), typeof(Directives));

    private static void OnUseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;

        if (element.GetValue(UseStateProperty) is DirectiveState existing)
        {
            existing.Detach();
            element.ClearValue(UseStateProperty);
        }

        if (e.NewValue is Directive directive)
        {
            var state = new DirectiveState(element, directive);
            element.SetValue(UseStateProperty, state);
            state.Attach();
        }
    }

    private static void OnDirectiveBindingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d.GetValue(UseStateProperty) is DirectiveState state)
            state.NotifyBindingChanged();
    }

    private sealed class DirectiveState(FrameworkElement element, Directive directive)
    {
        private bool _mounted;
        private object? _lastValue;

        public void Attach()
        {
            element.Loaded += OnLoaded;
            element.Unloaded += OnUnloaded;
        }

        public void Detach()
        {
            element.Loaded -= OnLoaded;
            element.Unloaded -= OnUnloaded;

            if (_mounted)
            {
                _mounted = false;
                directive.Unmounted(element, CurrentBinding(_lastValue));
            }
        }

        public void NotifyBindingChanged()
        {
            if (!_mounted)
                return;

            var binding = CurrentBinding(_lastValue);
            _lastValue = GetUseValue(element);
            directive.Updated(element, binding);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _mounted = true;
            _lastValue = GetUseValue(element);
            directive.Mounted(element, CurrentBinding(oldValue: null));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _mounted = false;
            directive.Unmounted(element, CurrentBinding(_lastValue));
        }

        private DirectiveBinding CurrentBinding(object? oldValue) => new()
        {
            Value = GetUseValue(element),
            OldValue = oldValue,
            Argument = GetArgument(element),
            Modifiers = ParseModifiers(GetModifiers(element)),
        };
    }
}
