// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Media;

namespace Barbatos.Wpf.AquariusValee.Xaml;

public static partial class Valee
{
    /// <summary>
    /// Names a field on the nearest ancestor <see cref="FormProperty"/> - sugar over
    /// <see cref="FieldProperty"/> for the common case where an ancestor container already sets
    /// <see cref="FormProperty"/>. See <see cref="FormProperty"/>'s remarks for a full example
    /// and the "must be non-null by load time" constraint.
    /// </summary>
    public static readonly DependencyProperty ForProperty =
        DependencyProperty.RegisterAttached("For", typeof(string), typeof(Valee), new PropertyMetadata(null, OnForChanged));

    /// <summary>Sets <see cref="ForProperty"/>.</summary>
    public static void SetFor(DependencyObject element, string? value) => element.SetValue(ForProperty, value);

    /// <summary>Gets <see cref="ForProperty"/>.</summary>
    public static string? GetFor(DependencyObject element) => (string?)element.GetValue(ForProperty);

    private static void OnForChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            throw new InvalidOperationException("Valee.For can only be set on a FrameworkElement.");

        element.Loaded -= OnForElementLoaded;

        if (e.NewValue is not null)
            element.Loaded += OnForElementLoaded;
    }

    private static void OnForElementLoaded(object sender, RoutedEventArgs e)
    {
        var element = (FrameworkElement)sender;
        var fieldName = GetFor(element);

        if (string.IsNullOrEmpty(fieldName))
            return;

        var form = FindAmbientForm(element);

        if (form is null)
        {
            // Confirmed (the same way RouterView.OnLoaded's own identical comment documents):
            // Loaded is raised from deep inside WPF's own render pipeline, and a synchronous
            // throw from within it crashes the whole process rather than reaching any caller's
            // try/catch. Deferring via BeginInvoke lets the current render pass finish first, so
            // this surfaces as a normal, catchable unhandled dispatcher exception instead.
            element.Dispatcher.BeginInvoke((Action)(() => throw new InvalidOperationException(
                $"Valee.For=\"{fieldName}\" could not find an ancestor with Valee.Form set to a non-null " +
                "Form. Valee.Form must be assigned - and already non-null - by the time this element " +
                "loads; construct the Form synchronously in the ViewModel's constructor rather than " +
                "populating it later/asynchronously.")));
            return;
        }

        if (!form.Fields.TryGetValue(fieldName, out var field))
        {
            element.Dispatcher.BeginInvoke((Action)(() => throw new InvalidOperationException(
                $"Valee.For=\"{fieldName}\" - no field named '{fieldName}' exists on the ambient Form.")));
            return;
        }

        ApplyFieldBinding(element, field);
    }

    /// <summary>
    /// Walks up the visual tree for the nearest ancestor with <see cref="FormProperty"/> set,
    /// mirroring <c>Barbatos.Wpf.AquariusRouter</c>'s own <c>RouterView.FindAncestor</c> - safe to
    /// do at <see cref="FrameworkElement.Loaded"/> specifically because an element is only ever
    /// connected to a live, rooted visual tree once <c>Loaded</c> has fired, and (per
    /// <c>Barbatos.Wpf.Aquarius</c>'s own <c>Setup</c>, which resolves <c>DataContext</c> at
    /// <see cref="FrameworkElement.Initialized"/>, strictly before <c>Loaded</c>) any
    /// <see cref="FormProperty"/> binding sourced from a ViewModel's <c>DataContext</c> has
    /// already resolved by the time this runs.
    /// </summary>
    private static Validation.Form? FindAmbientForm(DependencyObject element)
    {
        for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (GetForm(current) is { } form)
                return form;
        }

        return null;
    }
}
