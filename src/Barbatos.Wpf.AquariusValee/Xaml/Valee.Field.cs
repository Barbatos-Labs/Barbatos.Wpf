// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Barbatos.Wpf.AquariusValee.Validation;

namespace Barbatos.Wpf.AquariusValee.Xaml;

/// <summary>
/// Attached properties connecting a <see cref="Field{T}"/>/<see cref="Form"/> to real WPF
/// controls - the WPF/C# counterpart of vee-validate's <c>&lt;Field&gt;</c> component.
/// </summary>
public static partial class Valee
{
    private static readonly DependencyProperty AssociatedFieldProperty =
        DependencyProperty.RegisterAttached("AssociatedField", typeof(IField), typeof(Valee), new PropertyMetadata(null));

    /// <summary>
    /// The reverse lookup "focus the first invalid field on submit" (see
    /// <see cref="FormProperty"/>) needs: populated by <see cref="ApplyFieldBinding"/> (used by
    /// both <see cref="FieldProperty"/> and <see cref="ForProperty"/>) and by
    /// <see cref="PasswordFieldProperty"/>'s own changed callback, regardless of whether
    /// <see cref="FormProperty"/> is used anywhere at all - a <see cref="ConditionalWeakTable{TKey,TValue}"/>
    /// so a field with no more live-bound controls doesn't keep an element rooted.
    /// </summary>
    private static readonly ConditionalWeakTable<IField, FrameworkElement> ElementsByField = new();

    /// <summary>
    /// Binds a control's natural value property (<see cref="TextBox.Text"/>,
    /// <see cref="ToggleButton.IsChecked"/>, <see cref="Selector.SelectedItem"/>,
    /// <see cref="RangeBase.Value"/>) to an <see cref="IField"/>, and wires
    /// <see cref="UIElement.LostFocus"/> to <see cref="IField.HandleBlur"/> - something a plain
    /// <see cref="Binding"/> cannot express at all, since WPF has no built-in "touched" concept.
    /// </summary>
    /// <remarks>
    /// <code>
    /// &lt;TextBox aqv:Valee.Field="{Binding LoginForm.Email}" /&gt;
    /// </code>
    /// Constructs a real, new <see cref="Binding"/> with <see cref="Binding.Source"/> set to the
    /// resolved field and <see cref="Binding.Path"/> set to <see cref="Field{T}.Value"/> (not a
    /// clone of the source binding the way <c>Directives.Model</c> reuses its
    /// <see cref="BindingExpression.ParentBinding"/> - here the bound object is the field itself,
    /// one hop above <c>.Value</c>), with <see cref="BindingMode.TwoWay"/> and
    /// <see cref="UpdateSourceTrigger.PropertyChanged"/>. That last part matters beyond
    /// convenience: WPF's own default <see cref="UpdateSourceTrigger"/> for
    /// <see cref="TextBox.Text"/> is <see cref="UpdateSourceTrigger.LostFocus"/>, so a plain
    /// <c>{Binding Field.Value}</c> without this attached property would only push a value (and
    /// therefore only trigger <see cref="ValidateTrigger.OnChange"/> validation and live
    /// <see cref="FieldMeta.Dirty"/> updates) on blur, not per keystroke.
    /// <para>
    /// Because <see cref="Field{T}"/> implements <see cref="System.ComponentModel.INotifyDataErrorInfo"/>
    /// directly, this binding alone already gets WPF's native red-border validation adorner for
    /// free (<see cref="Binding.ValidatesOnNotifyDataErrors"/> defaults to <see langword="true"/>)
    /// - no converter or custom control needed for error display.
    /// </para>
    /// <para>Throws for <see cref="PasswordBox"/> - use <see cref="PasswordFieldProperty"/> instead.</para>
    /// </remarks>
    public static readonly DependencyProperty FieldProperty =
        DependencyProperty.RegisterAttached("Field", typeof(object), typeof(Valee), new FrameworkPropertyMetadata(null, OnFieldChanged));

    /// <summary>Sets <see cref="FieldProperty"/>.</summary>
    public static void SetField(DependencyObject element, object? value) => element.SetValue(FieldProperty, value);

    /// <summary>Gets <see cref="FieldProperty"/>.</summary>
    public static object? GetField(DependencyObject element) => element.GetValue(FieldProperty);

    private static void OnFieldChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            throw new InvalidOperationException("Valee.Field can only be set on a FrameworkElement.");

        if (e.OldValue is not null)
            element.LostFocus -= OnFieldControlLostFocus;

        if (e.NewValue is null)
        {
            element.ClearValue(AssociatedFieldProperty);
            return;
        }

        if (e.NewValue is not IField field)
        {
            throw new InvalidOperationException(
                $"Valee.Field must be set to an IField (e.g. a Field<T>), not '{e.NewValue.GetType()}'.");
        }

        ApplyFieldBinding(element, field);
    }

    private static void OnFieldControlLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is DependencyObject d && d.GetValue(AssociatedFieldProperty) is IField field)
            field.HandleBlur();
    }

    /// <summary>
    /// Shared by <see cref="FieldProperty"/> and <see cref="ForProperty"/> - binds
    /// <paramref name="element"/>'s natural value property to <paramref name="field"/> and wires
    /// <see cref="UIElement.LostFocus"/> to <see cref="IField.HandleBlur"/>.
    /// </summary>
    private static void ApplyFieldBinding(FrameworkElement element, IField field)
    {
        var targetProperty = ResolveFieldTargetProperty(element);

        BindingOperations.SetBinding(element, targetProperty, new Binding(nameof(Field<object>.Value))
        {
            Source = field,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        });

        element.SetValue(AssociatedFieldProperty, field);
        element.LostFocus += OnFieldControlLostFocus;
        ElementsByField.AddOrUpdate(field, element);
    }

    private static DependencyProperty ResolveFieldTargetProperty(FrameworkElement element) => element switch
    {
        TextBox => TextBox.TextProperty,
        PasswordBox => throw new InvalidOperationException(
            "Valee.Field does not support PasswordBox - use Valee.PasswordField instead, which never " +
            "creates a real Binding on Password, for security reasons (the same limitation " +
            "Barbatos.Wpf.Aquarius's Directives.Model has)."),
        ToggleButton => ToggleButton.IsCheckedProperty,
        Selector => Selector.SelectedItemProperty,
        RangeBase => RangeBase.ValueProperty,
        _ => throw new InvalidOperationException(
            $"Valee.Field does not support '{element.GetType().Name}'. Supported elements: " +
            "TextBox, CheckBox/RadioButton (ToggleButton), ComboBox/ListBox (Selector), Slider (RangeBase)."),
    };
}
