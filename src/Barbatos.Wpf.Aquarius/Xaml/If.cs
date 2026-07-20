// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Barbatos.Wpf.Xaml;

/// <summary>
/// Conditionally renders its content - the Aquarius counterpart of Vue's <c>v-if</c>.
/// </summary>
/// <remarks>
/// <code>
/// &lt;aq:If Condition="{Binding ShowPanel}"&gt;
///     &lt;TextBlock Text="Only in the tree while ShowPanel is true" /&gt;
/// &lt;/aq:If&gt;
/// </code>
/// Unlike <see cref="Directives.ShowProperty"/> (v-show, which only toggles
/// <see cref="UIElement.Visibility"/>), <see cref="Condition"/> going <c>false</c>
/// genuinely detaches the content from the visual tree - the same "destroy" Vue performs
/// for <c>v-if</c> - rather than just hiding it. The child is kept in
/// <see cref="Child"/> (the XAML content property) so it can be reattached to
/// <see cref="ContentControl.Content"/> unchanged once <see cref="Condition"/> is
/// <c>true</c> again.
///
/// Detaching a live <see cref="FrameworkElement"/> fires WPF's own <c>Unloaded</c> event
/// on it (and reattaching fires <c>Loaded</c>), so a child under
/// <see cref="Composition.Lifecycle.EnableProperty"/> genuinely receives <c>IOnUnmounted</c>/
/// <c>IOnMounted</c> calls as <see cref="Condition"/> toggles - the same way Vue actually
/// destroys and recreates a <c>v-if</c> subtree.
/// </remarks>
[ContentProperty(nameof(Child))]
public class If : ContentControl
{
    /// <summary>
    /// The content to conditionally render. This - not the inherited
    /// <see cref="ContentControl.Content"/> - is the XAML content property, since
    /// <see cref="ContentControl.Content"/> itself is reserved for whatever is currently
    /// actually displayed (<c>null</c> while <see cref="Condition"/> is <c>false</c>).
    /// </summary>
    public static readonly DependencyProperty ChildProperty =
        DependencyProperty.Register(
            nameof(Child),
            typeof(object),
            typeof(If),
            new PropertyMetadata(null, OnChildOrConditionChanged));

    /// <summary>
    /// Whether <see cref="Child"/> should currently be mounted.
    /// </summary>
    public static readonly DependencyProperty ConditionProperty =
        DependencyProperty.Register(
            nameof(Condition),
            typeof(bool),
            typeof(If),
            new PropertyMetadata(true, OnChildOrConditionChanged));

    /// <inheritdoc cref="ChildProperty"/>
    public object? Child
    {
        get => GetValue(ChildProperty);
        set => SetValue(ChildProperty, value);
    }

    /// <inheritdoc cref="ConditionProperty"/>
    public bool Condition
    {
        get => (bool)GetValue(ConditionProperty);
        set => SetValue(ConditionProperty, value);
    }

    private static void OnChildOrConditionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var element = (If)d;
        element.Content = element.Condition ? element.Child : null;
    }
}
