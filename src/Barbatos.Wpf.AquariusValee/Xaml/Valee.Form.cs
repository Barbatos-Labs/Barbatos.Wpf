// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using Barbatos.Wpf.AquariusValee.Validation;

namespace Barbatos.Wpf.AquariusValee.Xaml;

public static partial class Valee
{
    /// <summary>
    /// Set once on an ancestor container so every descendant can use the shorter
    /// <see cref="ForProperty"/> ("<c>aqv:Valee.For="Email"</c>") instead of repeating
    /// <c>aqv:Valee.Field="{Binding LoginForm.Fields[Email]}"</c> on every control. Also wires
    /// automatic "focus the first invalid field" - see this member's own remarks.
    /// </summary>
    /// <remarks>
    /// <code>
    /// &lt;StackPanel aqv:Valee.Form="{Binding LoginForm}"&gt;
    ///     &lt;TextBox aqv:Valee.For="Email" /&gt;
    ///     &lt;PasswordBox aqv:Valee.PasswordField="{Binding LoginForm.Fields[Password]}" /&gt;
    /// &lt;/StackPanel&gt;
    /// </code>
    /// <b>The bound <see cref="Form"/> must be non-null and stable by the time the View
    /// loads</b> - construct it synchronously in the ViewModel's constructor, not populated
    /// later/asynchronously. <see cref="ForProperty"/> resolves it via a one-shot
    /// <see cref="FrameworkElement.Loaded"/>-time ancestor walk (mirroring
    /// <c>Barbatos.Wpf.AquariusRouter</c>'s own <c>RouterView</c> ambient-resolution pattern),
    /// with no retry if it is still <see langword="null"/> at that moment - the same documented
    /// constraint <c>Router.Current</c> carries ("must be set before first navigation").
    /// <para>
    /// <b>Focus-first-invalid-on-submit</b>: setting this property also subscribes to the
    /// <see cref="Form"/>'s own headless <see cref="Form.SubmitInvalid"/> event and, when it
    /// fires, calls <see cref="UIElement.Focus"/> + <see cref="FrameworkElement.BringIntoView()"/>
    /// on whichever real control is currently bound (via <see cref="FieldProperty"/>/
    /// <see cref="ForProperty"/>/<see cref="PasswordFieldProperty"/>) to that field - entirely
    /// automatic, no code needed in the ViewModel or the View beyond setting this one property
    /// somewhere in the visual tree. A form with no <c>Valee.Form</c> anywhere simply doesn't get
    /// this behavior - <see cref="Form"/> itself has no WPF-control dependency at all, by design,
    /// so it cannot focus anything on its own.
    /// </para>
    /// </remarks>
    public static readonly DependencyProperty FormProperty =
        DependencyProperty.RegisterAttached("Form", typeof(Form), typeof(Valee), new PropertyMetadata(null, OnFormChanged));

    /// <summary>Sets <see cref="FormProperty"/>.</summary>
    public static void SetForm(DependencyObject element, Form? value) => element.SetValue(FormProperty, value);

    /// <summary>Gets <see cref="FormProperty"/>.</summary>
    public static Form? GetForm(DependencyObject element) => (Form?)element.GetValue(FormProperty);

    private static void OnFormChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is Form oldForm)
            oldForm.SubmitInvalid -= FocusField;

        if (e.NewValue is Form newForm)
            newForm.SubmitInvalid += FocusField;

        // Form implements INotifyDataErrorInfo (Form.HasErrors aggregates every field's own
        // HasErrors) and this property's value arrives via a real WPF Binding - ValidatesOnNotifyDataErrors
        // defaults to true for any Binding, not just ones on editing controls - so without this,
        // WPF's own native validation adorner (a red border) would wrap the WHOLE element
        // Valee.Form is set on (typically the form's root panel) the instant any single field
        // anywhere in the form is invalid, not just the offending control. An empty ErrorTemplate
        // suppresses only the ADORNER; Validation.HasError/GetErrors on this element still
        // correctly reflect Form.HasErrors for anyone who wants that (e.g. a form-level "there are
        // errors" banner).
        if (d is FrameworkElement element)
            System.Windows.Controls.Validation.SetErrorTemplate(element, NoAdornerTemplate);
    }

    private static readonly System.Windows.Controls.ControlTemplate NoAdornerTemplate = new();

    private static void FocusField(IField field)
    {
        if (!ElementsByField.TryGetValue(field, out var reference) || !reference.TryGetTarget(out var element))
            return;

        element.Focus();
        element.BringIntoView();
    }
}
