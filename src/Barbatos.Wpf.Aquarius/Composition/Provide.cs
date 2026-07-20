// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;

namespace Barbatos.Wpf.Composition;

/// <summary>
/// Registers a value for <see cref="Inject"/> to find anywhere in this subtree - the
/// Aquarius counterpart of Vue's <c>provide(key, value)</c>.
/// </summary>
/// <remarks>
/// <code>
/// &lt;Grid aq:Provide.Key="ThemeColor" aq:Provide.Value="{Binding AccentBrush}"&gt;
///     ... &lt;TextBlock Foreground="{aq:Inject ThemeColor}" /&gt; anywhere inside ...
/// &lt;/Grid&gt;
/// </code>
/// Vue's <c>provide</c>/<c>inject</c> run in component <c>setup()</c> (i.e. in Aquarius,
/// the ViewModel) and walk the *component* tree; ViewModels intentionally don't hold a
/// reference to their View, so a literal port isn't honest here. This leans on WPF's own
/// closest analog instead: <see cref="FrameworkElement.Resources"/> plus
/// <see cref="FrameworkElement.FindResource"/>, which already walks up the logical tree
/// and merges resource dictionaries at each level - <see cref="ValueProperty"/> is just stored
/// into <see cref="FrameworkElement.Resources"/> under <see cref="KeyProperty"/>, so it composes
/// with WPF's existing lookup/override-by-nesting semantics for free.
///
/// Like a DOM <c>id</c> (or the plain string keys Vue itself warns about for large
/// apps/libraries), a string <see cref="KeyProperty"/> can collide with an unrelated resource
/// that happens to share the same name. For collision-proofing - Vue's own recommendation
/// is a <c>Symbol</c> - use a dedicated <c>object</c> sentinel instead of a string:
/// <c>public static readonly object ThemeColorKey = new();</c>, referenced from XAML via
/// <c>{x:Static local:Keys.ThemeColorKey}</c>.
///
/// Reactivity crosses the boundary for free: if <see cref="ValueProperty"/> is a
/// <see cref="Reactivity.Ref{T}"/> or an <c>ObservableObject</c>, nothing extra is needed
/// - bindings against it (or code reading <c>.Value</c>) already see live updates after
/// injection, the same guarantee Vue gives an injected ref.
/// </remarks>
public static class Provide
{
    /// <summary>The key <see cref="Inject"/> looks up. A plain <see cref="string"/> or, for
    /// collision-proofing, a dedicated <see cref="object"/> sentinel - see the type remarks.</summary>
    public static readonly DependencyProperty KeyProperty =
        DependencyProperty.RegisterAttached(
            "Key",
            typeof(object),
            typeof(Provide),
            new PropertyMetadata(null, OnProvideChanged));

    /// <summary>The value registered under <see cref="KeyProperty"/>.</summary>
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.RegisterAttached(
            "Value",
            typeof(object),
            typeof(Provide),
            new PropertyMetadata(null, OnProvideChanged));

    /// <summary>Sets <see cref="KeyProperty"/>.</summary>
    public static void SetKey(DependencyObject element, object? value) => element.SetValue(KeyProperty, value);

    /// <summary>Gets <see cref="KeyProperty"/>.</summary>
    public static object? GetKey(DependencyObject element) => element.GetValue(KeyProperty);

    /// <summary>Sets <see cref="ValueProperty"/>.</summary>
    public static void SetValue(DependencyObject element, object? value) => element.SetValue(ValueProperty, value);

    /// <summary>Gets <see cref="ValueProperty"/>.</summary>
    public static object? GetValue(DependencyObject element) => element.GetValue(ValueProperty);

    private static void OnProvideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            throw new InvalidOperationException("Provide.Key/Provide.Value can only be set on a FrameworkElement.");

        // Key itself changing (not just Value) means the old registration must move, not
        // just be overwritten in place.
        if (e.Property == KeyProperty && e.OldValue is { } oldKey)
            element.Resources.Remove(oldKey);

        if (GetKey(element) is { } key)
            element.Resources[key] = GetValue(element);
    }
}
