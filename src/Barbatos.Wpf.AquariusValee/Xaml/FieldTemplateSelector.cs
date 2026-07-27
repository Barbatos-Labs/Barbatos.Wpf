// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using Barbatos.Wpf.AquariusValee.Validation;

namespace Barbatos.Wpf.AquariusValee.Xaml;

/// <summary>
/// Picks a <see cref="DataTemplate"/> for an <see cref="IField"/> by looking up a resource keyed
/// on <see cref="IField.UIHint"/> first, then <see cref="IField.DataTypeHint"/>'s enum name, then
/// a resource literally named <c>"Default"</c> - the WPF/C# counterpart of ASP.NET Dynamic Data
/// choosing a template from <c>[UIHint("...")]</c>.
/// </summary>
/// <remarks>
/// <code>
/// &lt;Window.Resources&gt;
///     &lt;aqv:FieldTemplateSelector x:Key="FieldTemplateSelector" /&gt;
///
///     &lt;DataTemplate x:Key="MultilineText"&gt;
///         &lt;TextBox aqv:Valee.Field="{Binding}" AcceptsReturn="True" TextWrapping="Wrap" Height="60" /&gt;
///     &lt;/DataTemplate&gt;
///     &lt;DataTemplate x:Key="Password"&gt;
///         &lt;PasswordBox aqv:Valee.PasswordField="{Binding}" /&gt;
///     &lt;/DataTemplate&gt;
///     &lt;DataTemplate x:Key="Default"&gt;
///         &lt;TextBox aqv:Valee.Field="{Binding}" /&gt;
///     &lt;/DataTemplate&gt;
/// &lt;/Window.Resources&gt;
///
/// &lt;ItemsControl ItemsSource="{Binding ProfileForm.Fields.Values}"
///               ItemTemplateSelector="{StaticResource FieldTemplateSelector}" /&gt;
/// </code>
/// Only the resource key convention is prescribed here - the actual <see cref="DataTemplate"/>s
/// (and therefore which real control each key maps to) are entirely up to the consuming app,
/// the same way ASP.NET Dynamic Data never shipped opinions about what a "MultilineText" template
/// should look like either.
/// <para>
/// The bound item's <c>DataContext</c> inside each template is the <see cref="IField"/> itself
/// (an <see cref="System.Windows.Controls.ItemsControl"/>'s own default behavior) - a plain
/// <c>{Binding}</c> (no path) is what <see cref="Xaml.Valee.FieldProperty"/>/
/// <see cref="Xaml.Valee.PasswordFieldProperty"/> bind against in the example above.
/// </para>
/// </remarks>
public sealed class FieldTemplateSelector : DataTemplateSelector
{
    /// <inheritdoc/>
    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        if (item is not IField field || container is not FrameworkElement element)
            return base.SelectTemplate(item, container);

        if (field.UIHint is { Length: > 0 } uiHint && element.TryFindResource(uiHint) is DataTemplate byUiHint)
            return byUiHint;

        if (field.DataTypeHint is { } dataType && element.TryFindResource(dataType.ToString()) is DataTemplate byDataType)
            return byDataType;

        return element.TryFindResource("Default") as DataTemplate ?? base.SelectTemplate(item, container);
    }
}
