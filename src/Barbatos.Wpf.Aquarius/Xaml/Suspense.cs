// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Barbatos.Wpf.Xaml;

/// <summary>
/// Shows fallback content while something is loading - the Aquarius counterpart of Vue's
/// <c>&lt;Suspense&gt;</c>.
/// </summary>
/// <remarks>
/// <code>
/// &lt;aq:Suspense IsPending="{Binding IsLoading}"&gt;
///     &lt;local:DashboardView /&gt;
///     &lt;aq:Suspense.Fallback&gt;
///         &lt;TextBlock Text="Loading..." /&gt;
///     &lt;/aq:Suspense.Fallback&gt;
/// &lt;/aq:Suspense&gt;
/// </code>
/// Vue's real <c>&lt;Suspense&gt;</c> auto-detects async <c>setup()</c>/async components
/// anywhere down the tree it wraps, aggregating them into one pending/resolved state with
/// its own <c>#default</c>/<c>#fallback</c> slots and <c>pending</c>/<c>resolve</c>/
/// <c>fallback</c> events. C# has no equivalent to hook into "this ViewModel's setup is
/// async" automatically, so this is a deliberately narrower, explicit port: the ViewModel
/// already knows when it's loading (an <c>IsLoading</c>/<see cref="Reactivity.Ref{T}"/>
/// property it sets around an async call) - <see cref="IsPending"/> just wires that
/// straight to which content shows, the same <see cref="Child"/>-vs-
/// <see cref="ContentControl.Content"/> split <see cref="If"/>/<see cref="Animation.Transition"/>
/// already establish. No nested-Suspense boundary handling, no automatic dependency
/// aggregation - one explicit boolean in, one of two contents out.
/// </remarks>
[ContentProperty(nameof(Child))]
public class Suspense : ContentControl
{
    /// <summary>The resolved content, shown while <see cref="IsPending"/> is <c>false</c>.
    /// This - not the inherited <see cref="ContentControl.Content"/> - is the XAML content
    /// property, the same reason <see cref="If.Child"/> exists.</summary>
    public static readonly DependencyProperty ChildProperty =
        DependencyProperty.Register(
            nameof(Child),
            typeof(object),
            typeof(Suspense),
            new PropertyMetadata(null, OnStateChanged));

    /// <summary>The loading-state content, shown while <see cref="IsPending"/> is <c>true</c>.</summary>
    public static readonly DependencyProperty FallbackProperty =
        DependencyProperty.Register(
            nameof(Fallback),
            typeof(object),
            typeof(Suspense),
            new PropertyMetadata(null, OnStateChanged));

    /// <summary>Whether <see cref="Fallback"/> (<c>true</c>) or <see cref="Child"/> (<c>false</c>, default) is shown.</summary>
    public static readonly DependencyProperty IsPendingProperty =
        DependencyProperty.Register(
            nameof(IsPending),
            typeof(bool),
            typeof(Suspense),
            new PropertyMetadata(false, OnStateChanged));

    /// <inheritdoc cref="ChildProperty"/>
    public object? Child
    {
        get => GetValue(ChildProperty);
        set => SetValue(ChildProperty, value);
    }

    /// <inheritdoc cref="FallbackProperty"/>
    public object? Fallback
    {
        get => GetValue(FallbackProperty);
        set => SetValue(FallbackProperty, value);
    }

    /// <inheritdoc cref="IsPendingProperty"/>
    public bool IsPending
    {
        get => (bool)GetValue(IsPendingProperty);
        set => SetValue(IsPendingProperty, value);
    }

    private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var suspense = (Suspense)d;
        suspense.Content = suspense.IsPending ? suspense.Fallback : suspense.Child;
    }
}
