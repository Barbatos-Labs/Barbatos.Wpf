// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.Aquarius.Xaml;

/// <summary>
/// Renders whichever View type <see cref="Is"/> currently names - the Aquarius counterpart of
/// Vue's <c>&lt;component :is="..."&gt;</c>.
/// </summary>
/// <remarks>
/// <code>
/// &lt;aq:Component Is="{Binding CurrentView.Value}" /&gt;
/// </code>
/// The View is resolved through <see cref="Composition.Setup.Resolve"/> - the same
/// DI-first-else-<c>Activator</c> path every other View in this library family comes out of - so a
/// constructor-injected View just works, and its own <c>aq:Setup.Enable</c> resolves its
/// <c>DataContext</c> exactly as usual.
/// <para>
/// <b>Changing <see cref="Is"/> destroys the previous View.</b> It is dropped outright, so it and
/// its <c>DataContext</c> become collectable, and a ViewModel holding a subscription or a timer
/// stops being reachable the moment its View goes. That is Vue's behavior too, and it is precisely
/// what <see cref="KeepAlive"/> exists to opt out of: wrap this control in one to cache the Views
/// it switches between instead of rebuilding them.
/// </para>
/// <para>
/// <b>The name.</b> Deliberately <c>Component</c>, matching Vue's own tag, at the cost of colliding
/// with <see cref="System.ComponentModel.Component"/> for C# code that has both namespaces
/// imported - a qualified name resolves that on the rare occasion it comes up. This type is
/// written in XAML, where <c>&lt;aq:Component&gt;</c> is unambiguous and reads exactly like the
/// Vue it ports.
/// </para>
/// </remarks>
public class Component : ContentControl
{
    /// <summary>
    /// The View type to render, or <see langword="null"/> to render nothing. Bindable, so it is
    /// normally driven from a <see cref="Reactivity.Ref{T}"/> of <see cref="Type"/>.
    /// </summary>
    public static readonly DependencyProperty IsProperty =
        DependencyProperty.Register(nameof(Is), typeof(Type), typeof(Component), new PropertyMetadata(null, OnIsChanged));

    /// <inheritdoc cref="IsProperty"/>
    public Type? Is
    {
        get => (Type?)GetValue(IsProperty);
        set => SetValue(IsProperty, value);
    }

    /// <summary>
    /// Set by an owning <see cref="KeepAlive"/> while it is managing this control's content, so
    /// that switching <see cref="Is"/> caches rather than rebuilds.
    /// </summary>
    /// <remarks>
    /// The owner hosts the resolved Views in its own panel and leaves <see cref="ContentControl.Content"/>
    /// alone - which is why this control renders nothing at all in that mode, the same way Vue's
    /// <c>&lt;component&gt;</c> is itself replaced by whatever it resolves to.
    /// </remarks>
    internal KeepAlive? Owner { get; set; }

    /// <summary>Raised when <see cref="Is"/> changes, so an owning <see cref="KeepAlive"/> can react.</summary>
    internal event EventHandler? IsChanged;

    /// <summary>Builds the element for the current <see cref="Is"/> - the factory a <see cref="KeepAlive"/> caches behind.</summary>
    internal FrameworkElement Resolve()
    {
        var type = Is ?? throw new InvalidOperationException("Component.Is is not set.");

        return Composition.Setup.Resolve(type) as FrameworkElement
            ?? throw new InvalidOperationException($"Component.Is must name a FrameworkElement type, not '{type}'.");
    }

    private static void OnIsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var component = (Component)d;
        component.IsChanged?.Invoke(component, EventArgs.Empty);

        // In KeepAlive's hands, the owner decides what is shown and where - this control must not
        // also render a copy of its own.
        if (component.Owner is null)
            component.Content = component.Is is null ? null : component.Resolve();
    }
}
