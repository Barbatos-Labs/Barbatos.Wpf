// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using Barbatos.Wpf.AquariusValee.Validation;

namespace Barbatos.Wpf.AquariusValee.Xaml;

public static partial class Valee
{
    private static readonly DependencyProperty AssociatedPasswordFieldProperty =
        DependencyProperty.RegisterAttached("AssociatedPasswordField", typeof(Field<string>), typeof(Valee), new PropertyMetadata(null));

    /// <summary>
    /// The <see cref="PasswordBox"/>-specific counterpart of <see cref="FieldProperty"/>. Wires
    /// <see cref="PasswordBox.PasswordChanged"/> to <see cref="Field{T}.HandleChange"/> and
    /// <see cref="UIElement.LostFocus"/> to <see cref="IField.HandleBlur"/> - but, unlike
    /// <see cref="FieldProperty"/>, <b>never creates a real <see cref="System.Windows.Data.Binding"/>
    /// on <see cref="PasswordBox.Password"/></b>, mirroring <c>Directives.Model</c>'s own
    /// deliberate refusal to bind <c>Password</c> (it is not a <see cref="DependencyProperty"/>,
    /// for security reasons). A direct consequence: there is no <c>BindingExpression</c> for
    /// WPF's native validation adorner to attach to here, so this field's errors need a plain
    /// bound <see cref="TextBlock"/> instead, e.g.
    /// <c>&lt;TextBlock Text="{Binding LoginForm.Password.ErrorMessage}" /&gt;</c> - the adorner
    /// trick <see cref="FieldProperty"/> gets for free does not apply to a <see cref="PasswordBox"/>.
    /// </summary>
    public static readonly DependencyProperty PasswordFieldProperty =
        DependencyProperty.RegisterAttached("PasswordField", typeof(Field<string>), typeof(Valee), new FrameworkPropertyMetadata(null, OnPasswordFieldChanged));

    /// <summary>Sets <see cref="PasswordFieldProperty"/>.</summary>
    public static void SetPasswordField(DependencyObject element, Field<string>? value) => element.SetValue(PasswordFieldProperty, value);

    /// <summary>Gets <see cref="PasswordFieldProperty"/>.</summary>
    public static Field<string>? GetPasswordField(DependencyObject element) => (Field<string>?)element.GetValue(PasswordFieldProperty);

    private static void OnPasswordFieldChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox passwordBox)
            throw new InvalidOperationException("Valee.PasswordField can only be set on a PasswordBox.");

        if (e.OldValue is not null)
        {
            passwordBox.PasswordChanged -= OnPasswordBoxPasswordChanged;
            passwordBox.LostFocus -= OnPasswordBoxLostFocus;
        }

        if (e.NewValue is null)
        {
            passwordBox.ClearValue(AssociatedPasswordFieldProperty);
            return;
        }

        var field = (Field<string>)e.NewValue;
        passwordBox.SetValue(AssociatedPasswordFieldProperty, field);
        passwordBox.PasswordChanged += OnPasswordBoxPasswordChanged;
        passwordBox.LostFocus += OnPasswordBoxLostFocus;
        ElementsByField.AddOrUpdate(field, passwordBox);
    }

    private static void OnPasswordBoxPasswordChanged(object sender, RoutedEventArgs e)
    {
        var passwordBox = (PasswordBox)sender;

        if (passwordBox.GetValue(AssociatedPasswordFieldProperty) is Field<string> field)
            field.HandleChange(passwordBox.Password);
    }

    private static void OnPasswordBoxLostFocus(object sender, RoutedEventArgs e)
    {
        var passwordBox = (PasswordBox)sender;

        if (passwordBox.GetValue(AssociatedPasswordFieldProperty) is Field<string> field)
            field.HandleBlur();
    }
}
