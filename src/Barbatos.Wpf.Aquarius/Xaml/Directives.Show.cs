// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;

namespace Barbatos.Wpf.Aquarius.Xaml;

public static partial class Directives
{
    /// <summary>
    /// Toggles <see cref="UIElement.Visibility"/> - the Aquarius counterpart of Vue's
    /// <c>v-show</c>.
    /// </summary>
    /// <remarks>
    /// <c>false</c> maps to <see cref="Visibility.Collapsed"/> (no layout space, state
    /// preserved), matching the <c>display: none</c> semantics <c>v-show</c> relies on.
    /// Unlike <see cref="If.Condition"/> (v-if), the element is never actually removed
    /// from the visual tree - it stays mounted and keeps running.
    /// </remarks>
    public static readonly DependencyProperty ShowProperty =
        DependencyProperty.RegisterAttached(
            "Show",
            typeof(bool),
            typeof(Directives),
            new PropertyMetadata(true, OnShowChanged));

    /// <summary>Sets <see cref="ShowProperty"/>.</summary>
    public static void SetShow(DependencyObject element, bool value) => element.SetValue(ShowProperty, value);

    /// <summary>Gets <see cref="ShowProperty"/>.</summary>
    public static bool GetShow(DependencyObject element) => (bool)element.GetValue(ShowProperty);

    private static void OnShowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
            element.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
    }
}
