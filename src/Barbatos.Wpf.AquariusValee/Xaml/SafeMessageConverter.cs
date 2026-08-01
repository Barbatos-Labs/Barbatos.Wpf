// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Barbatos.Wpf.AquariusValee.Xaml;

/// <summary>
/// Wraps a localization <see cref="IValueConverter"/> so that a string it fails to translate is
/// shown <b>as written</b> instead of vanishing - the safety net that keeps plain-text
/// <c>ErrorMessage</c>/<c>[Display(Name/Prompt)]</c> values working even while a localization
/// system is active.
/// </summary>
/// <remarks>
/// <para>
/// AquariusValee has no dependency on any localization library, and a plain literal
/// <c>ErrorMessage = "Enter a valid email address."</c> is a fully supported, ordinary way to use
/// it. The problem this solves appears only once an application adds localization on top: a
/// typical localizing converter resolves a lookup key, and returns <see langword="null"/> or empty
/// for anything it does not recognise. Bound to a <c>TextBlock.Text</c>, that renders as
/// <i>nothing at all</i> - so one unregistered key, one typo, or one message the author
/// deliberately wrote as literal prose silently becomes an invisible error message, which is far
/// worse than an untranslated one.
/// </para>
/// <para>
/// Set <see cref="Inner"/> to the localizing converter (it is this class's XAML content property,
/// so it can simply be nested inside the element) and bind through this instead:
/// </para>
/// <code>
/// &lt;aqv:SafeMessageConverter x:Key="Message"&gt;
///     &lt;i18n:LocalizeConverter /&gt;
/// &lt;/aqv:SafeMessageConverter&gt;
/// ...
/// &lt;TextBlock Text="{Binding ErrorMessage, Converter={StaticResource Message}}" /&gt;
/// </code>
/// <para>
/// Nothing here is specific to any one localization package - <see cref="Inner"/> is an ordinary
/// <see cref="IValueConverter"/>, so this works with <c>Barbatos.i18n</c>,
/// <c>IStringLocalizer</c>-based converters, ResX-backed ones, or a hand-written lambda converter
/// equally. With <see cref="Inner"/> left unset the converter is a pass-through, which is exactly
/// the right behavior for an application that does not localize at all.
/// </para>
/// </remarks>
[ContentProperty(nameof(Inner))]
public sealed class SafeMessageConverter : IValueConverter
{
    /// <summary>
    /// The localizing converter to try first. When unset, this converter passes values through
    /// unchanged.
    /// </summary>
    public IValueConverter? Inner { get; set; }

    /// <summary>
    /// Runs <see cref="Inner"/> and returns its result only if it produced a non-empty string;
    /// otherwise returns <paramref name="value"/> unchanged, so the original text is displayed.
    /// A converter that throws (an unparseable format placeholder, for instance) is treated the
    /// same as one that produced nothing - a display-time formatting problem must not take the
    /// message down with it.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Inner is null)
            return value ?? string.Empty;

        try
        {
            if (Inner.Convert(value, targetType, parameter, culture) is string { Length: > 0 } translated)
                return translated;
        }
        catch (Exception)
        {
            // Fall through to the untranslated value below.
        }

        return value ?? string.Empty;
    }

    /// <summary>Not supported - these bindings are one-way, from a validation message to a display element.</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException($"{nameof(SafeMessageConverter)} is one-way only.");
}
