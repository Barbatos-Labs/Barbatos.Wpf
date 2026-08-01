using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Barbatos.Wpf.AquariusValee.Sample.Views.Showcase;

/// <summary>
/// Shows an overlaid <see cref="Field{T}.Prompt"/> watermark only while the bound field's own
/// <c>Value</c> is empty - WPF's <see cref="System.Windows.Controls.TextBox"/> has no native
/// placeholder/watermark support, unlike HTML's <c>&lt;input placeholder&gt;</c>, so
/// ShowcaseView.xaml overlays a real <see cref="System.Windows.Controls.TextBlock"/> on top of
/// the input and uses this converter to hide it the moment the user types anything.
/// </summary>
/// <remarks>
/// Checks <c>value?.ToString()</c>, not a direct <c>value as string</c> cast - a <c>Field&lt;int&gt;</c>
/// like <c>Price</c> boxes a plain <see cref="int"/>, which is never itself a <see cref="string"/>
/// (the cast always silently returns <see langword="null"/>), so the direct-cast version always
/// reported "empty" and left the watermark showing behind every digit typed. An <see cref="int"/>
/// has no real "empty" state to represent anyway (even <c>0</c> is a value, not blankness), so this
/// correctly collapses the watermark as soon as the field has any value at all.
/// </remarks>
public sealed class EmptyStringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.IsNullOrEmpty(value?.ToString()) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
