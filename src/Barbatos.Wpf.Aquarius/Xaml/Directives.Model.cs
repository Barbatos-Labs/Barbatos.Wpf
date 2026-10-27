// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Barbatos.Wpf.Aquarius.Xaml;

public static partial class Directives
{
    // ModelProperty's default - see its remarks. Declared above it on purpose: static
    // initializers run in textual order, and ModelProperty's metadata reads this one.
    private static readonly object ModelUnset = new();

    /// <summary>
    /// Two-way-binding sugar for common input controls - the Aquarius counterpart of
    /// Vue's <c>v-model</c>.
    /// </summary>
    /// <remarks>
    /// <code>
    /// &lt;TextBox aq:Directives.Model="{Binding Name}" /&gt;
    /// &lt;!-- instead of --&gt;
    /// &lt;TextBox Text="{Binding Name, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" /&gt;
    /// </code>
    /// Must be set with a <see cref="Binding"/> (not a literal value): the
    /// <see cref="PropertyChangedCallback"/> reads the <see cref="Binding"/> back off the
    /// attached property via <see cref="BindingExpression.ParentBinding"/>, clones it
    /// with <see cref="BindingMode.TwoWay"/> and
    /// <see cref="System.Windows.Data.UpdateSourceTrigger.PropertyChanged"/>, and
    /// re-applies it to the real property for the element's type
    /// (<see cref="TextBox.TextProperty"/>, <see cref="ToggleButton.IsCheckedProperty"/>,
    /// <see cref="Selector.SelectedItemProperty"/>, <see cref="RangeBase.ValueProperty"/>).
    /// An unsupported element type throws, matching Vue's own dev-time warning for an
    /// invalid <c>v-model</c> target rather than silently doing nothing.
    /// <para>
    /// The two-way binding is created the first time the bound value resolves, <b>including when
    /// it resolves to <see langword="null"/></b> - a <c>SelectedItem</c> nothing is selected in
    /// yet, a <c>string?</c> not filled in yet. WPF calls a property-changed callback only when
    /// the effective value changes, so with a <see langword="null"/> default a binding whose first
    /// value was also <see langword="null"/> never reached this callback: the control showed the
    /// value, but nothing the user selected or typed was written back until the source became
    /// non-null some other way. The property's default is therefore a private "unset" marker
    /// rather than <see langword="null"/>; <see cref="GetModel"/> still reports an unset property
    /// as <see langword="null"/>. Setting <see langword="null"/> as a plain value now throws like
    /// any other plain value, while clearing the property (<c>ClearValue</c>,
    /// <see cref="BindingOperations.ClearBinding"/>) is accepted and leaves the two-way binding
    /// already applied in place.
    /// </para>
    /// </remarks>
    public static readonly DependencyProperty ModelProperty =
        DependencyProperty.RegisterAttached(
            "Model",
            typeof(object),
            typeof(Directives),
            new FrameworkPropertyMetadata(ModelUnset, OnModelChanged));

    /// <summary>Sets <see cref="ModelProperty"/>.</summary>
    public static void SetModel(DependencyObject element, object? value) => element.SetValue(ModelProperty, value);

    /// <summary>Gets <see cref="ModelProperty"/>; <see langword="null"/> while it is unset.</summary>
    public static object? GetModel(DependencyObject element)
    {
        var value = element.GetValue(ModelProperty);
        return ReferenceEquals(value, ModelUnset) ? null : value;
    }

    private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var sourceBinding = BindingOperations.GetBindingExpression(d, ModelProperty)?.ParentBinding;

        // Back to unset with no binding left means the property was cleared (ClearValue,
        // ClearBinding), not given a plain value - nothing to apply and nothing to reject.
        if (sourceBinding is null && ReferenceEquals(e.NewValue, ModelUnset))
            return;

        var targetProperty = ResolveModelTargetProperty(d);

        if (sourceBinding is null)
        {
            throw new InvalidOperationException(
                "Directives.Model must be set with a Binding, e.g. aq:Directives.Model=\"{Binding Path}\" " +
                "- not a plain value.");
        }

        var binding = new Binding
        {
            Path = sourceBinding.Path,
            Converter = sourceBinding.Converter,
            ConverterParameter = sourceBinding.ConverterParameter,
            ConverterCulture = sourceBinding.ConverterCulture,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };

        // Binding.Source/RelativeSource/ElementName are mutually exclusive, and merely
        // *assigning* one - even a value that looks like the unset default - marks it as
        // "set" and trips that check. Forward at most whichever one the original binding
        // actually used; a plain `{Binding Path}` (relying on DataContext, the overwhelmingly
        // common case here) sets none of them, so none should be copied.
        if (sourceBinding.Source is not null)
            binding.Source = sourceBinding.Source;
        else if (sourceBinding.RelativeSource is not null)
            binding.RelativeSource = sourceBinding.RelativeSource;
        else if (!string.IsNullOrEmpty(sourceBinding.ElementName))
            binding.ElementName = sourceBinding.ElementName;

        BindingOperations.SetBinding(d, targetProperty, binding);
    }

    private static DependencyProperty ResolveModelTargetProperty(DependencyObject element) => element switch
    {
        TextBox => TextBox.TextProperty,
        PasswordBox => throw new InvalidOperationException(
            "Directives.Model does not support PasswordBox: Password is deliberately not a " +
            "DependencyProperty for security reasons, the same limitation Vue's own v-model has " +
            "for <input type=\"password\">. Handle PasswordChanged manually instead."),
        ToggleButton => ToggleButton.IsCheckedProperty,
        Selector => Selector.SelectedItemProperty,
        RangeBase => RangeBase.ValueProperty,
        _ => throw new InvalidOperationException(
            $"Directives.Model does not support '{element.GetType().Name}'. Supported elements: " +
            "TextBox, CheckBox/RadioButton (ToggleButton), ComboBox/ListBox (Selector), Slider (RangeBase)."),
    };
}
