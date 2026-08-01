// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Barbatos.Wpf.AquariusValee.Validation;

namespace Barbatos.Wpf.AquariusValee.Xaml;

public static partial class Valee
{
    /// <summary>
    /// Restricts a <see cref="TextBox"/> to numeric input, and - when a bound is known - snaps an
    /// out-of-range value back into range once the user leaves the field.
    /// </summary>
    /// <remarks>
    /// <code>
    /// &lt;TextBox aqv:Valee.Field="{Binding Age}" aqv:Valee.Numeric="Integer" /&gt;
    /// &lt;TextBox aqv:Valee.Field="{Binding Price}" aqv:Valee.Numeric="Decimal" aqv:Valee.Minimum="0" /&gt;
    /// </code>
    /// <para>
    /// <b>Typing</b> is filtered per keystroke, and pasted text is filtered the same way. Anything
    /// that could not begin a valid number is dropped before it appears: letters, punctuation, a
    /// second decimal separator, a minus sign anywhere but the front. Half-finished input is
    /// deliberately still allowed - <c>"-"</c>, <c>"."</c> and <c>"-."</c> are all legal states on
    /// the way to a real number, and rejecting them would make the field impossible to fill.
    /// Unlike a regular expression, a number's grammar is simple enough to decide that
    /// prefix-by-prefix, which is exactly why this can filter typing when
    /// <see cref="Rules.PatternAttribute"/> cannot.
    /// </para>
    /// <para>
    /// <b>Bounds</b> come from <see cref="MinimumProperty"/>/<see cref="MaximumProperty"/> when set,
    /// and otherwise from the bound field's own <see cref="IField.Minimum"/>/
    /// <see cref="IField.Maximum"/> - so a model property already carrying <c>[Range(13, 120)]</c>
    /// needs nothing repeated in XAML. Each side is independent: set only a minimum for "0 or
    /// more", only a maximum for "up to 100", or neither to just filter characters. A minimum of
    /// zero or above also removes the minus sign from the accepted characters, since no value in
    /// range could use it.
    /// </para>
    /// <para>
    /// <b>Clamping happens on <see cref="UIElement.LostFocus"/>, never while typing.</b> Snapping
    /// mid-keystroke is actively hostile: with a 10-100 range, someone typing <c>50</c> would see
    /// their first <c>5</c> rewritten to <c>10</c> and end up with <c>100</c>. Waiting for the
    /// field to be left is what every numeric editor does, and it leaves the value the user
    /// actually meant intact while they type.
    /// </para>
    /// <para>
    /// Clamping <b>corrects</b>, it does not validate - it silently rewrites what was typed, so
    /// keep <c>[Range]</c> (or an equivalent rule) on the model regardless. That is what reports a
    /// genuinely bad value, and what still runs when a value arrives from anywhere other than this
    /// text box.
    /// </para>
    /// </remarks>
    public static readonly DependencyProperty NumericProperty =
        DependencyProperty.RegisterAttached("Numeric", typeof(NumericInput), typeof(Valee), new PropertyMetadata(NumericInput.None, OnNumericChanged));

    /// <summary>Sets <see cref="NumericProperty"/>.</summary>
    public static void SetNumeric(DependencyObject element, NumericInput value) => element.SetValue(NumericProperty, value);

    /// <summary>Gets <see cref="NumericProperty"/>.</summary>
    public static NumericInput GetNumeric(DependencyObject element) => (NumericInput)element.GetValue(NumericProperty);

    /// <summary>
    /// The lowest value a <see cref="NumericProperty"/> box accepts, overriding the bound field's
    /// own <see cref="IField.Minimum"/>. Leave unset for "no lower bound".
    /// </summary>
    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.RegisterAttached("Minimum", typeof(double?), typeof(Valee), new PropertyMetadata(null));

    /// <summary>Sets <see cref="MinimumProperty"/>.</summary>
    public static void SetMinimum(DependencyObject element, double? value) => element.SetValue(MinimumProperty, value);

    /// <summary>Gets <see cref="MinimumProperty"/>.</summary>
    public static double? GetMinimum(DependencyObject element) => (double?)element.GetValue(MinimumProperty);

    /// <summary>
    /// The highest value a <see cref="NumericProperty"/> box accepts, overriding the bound field's
    /// own <see cref="IField.Maximum"/>. Leave unset for "no upper bound".
    /// </summary>
    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.RegisterAttached("Maximum", typeof(double?), typeof(Valee), new PropertyMetadata(null));

    /// <summary>Sets <see cref="MaximumProperty"/>.</summary>
    public static void SetMaximum(DependencyObject element, double? value) => element.SetValue(MaximumProperty, value);

    /// <summary>Gets <see cref="MaximumProperty"/>.</summary>
    public static double? GetMaximum(DependencyObject element) => (double?)element.GetValue(MaximumProperty);

    private static void OnNumericChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox)
            throw new InvalidOperationException($"Valee.Numeric can only be set on a TextBox, not '{d.GetType().Name}'.");

        textBox.PreviewTextInput -= OnNumericPreviewTextInput;
        textBox.LostFocus -= OnNumericLostFocus;
        DataObject.RemovePastingHandler(textBox, OnNumericPaste);

        if ((NumericInput)e.NewValue == NumericInput.None)
            return;

        textBox.PreviewTextInput += OnNumericPreviewTextInput;
        textBox.LostFocus += OnNumericLostFocus;
        DataObject.AddPastingHandler(textBox, OnNumericPaste);
    }

    private static void OnNumericPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var textBox = (TextBox)sender;
        var candidate = textBox.Text
            .Remove(textBox.SelectionStart, textBox.SelectionLength)
            .Insert(textBox.SelectionStart, e.Text);

        e.Handled = !IsPartialNumber(candidate, textBox);
    }

    private static void OnNumericPaste(object sender, DataObjectPastingEventArgs e)
    {
        var textBox = (TextBox)sender;

        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        var pasted = (string)e.DataObject.GetData(DataFormats.Text)!;
        var candidate = textBox.Text
            .Remove(textBox.SelectionStart, textBox.SelectionLength)
            .Insert(textBox.SelectionStart, pasted);

        // All-or-nothing rather than stripping characters out: silently turning a pasted
        // "1.234,56" into "1.23456" would be a wrong number presented as if it were the one the
        // user pasted, which is worse than nothing happening.
        if (!IsPartialNumber(candidate, textBox))
            e.CancelCommand();
    }

    private static void OnNumericLostFocus(object sender, RoutedEventArgs e)
    {
        var textBox = (TextBox)sender;
        var (minimum, maximum) = ResolveBounds(textBox);

        if (minimum is null && maximum is null)
            return;

        var culture = CultureOf(textBox);

        if (!double.TryParse(textBox.Text, NumberStyles.Float, culture, out var value))
            return;

        var clamped = value;

        if (minimum is { } min && clamped < min)
            clamped = min;

        if (maximum is { } max && clamped > max)
            clamped = max;

        if (clamped.Equals(value))
            return;

        // Round-trips through the same culture the binding will read it back with, so the
        // corrected text parses to exactly the value that was clamped to.
        textBox.Text = clamped.ToString("R", culture);
        textBox.CaretIndex = textBox.Text.Length;
    }

    /// <summary>
    /// Bounds set directly on the element win; otherwise the bound field's own bounds are used, so
    /// a <c>[Range]</c> already on the model does not have to be repeated in XAML.
    /// </summary>
    private static (double? Minimum, double? Maximum) ResolveBounds(TextBox textBox)
    {
        var field = textBox.GetValue(AssociatedFieldProperty) as IField;

        return (GetMinimum(textBox) ?? field?.Minimum, GetMaximum(textBox) ?? field?.Maximum);
    }

    /// <summary>
    /// Whether <paramref name="text"/> is a number or could still become one with more typing -
    /// so a lone sign or separator passes, but anything that can never be numeric does not.
    /// </summary>
    private static bool IsPartialNumber(string text, TextBox textBox)
    {
        if (text.Length == 0)
            return true;

        var culture = CultureOf(textBox);
        var separator = culture.NumberFormat.NumberDecimalSeparator;
        var negativeSign = culture.NumberFormat.NegativeSign;
        var allowsDecimals = GetNumeric(textBox) == NumericInput.Decimal;

        // A lower bound of zero or more rules the sign out entirely - no value that could satisfy
        // it is negative, so the key is noise.
        var (minimum, _) = ResolveBounds(textBox);
        var allowsNegative = minimum is not { } min || min < 0;

        var index = 0;

        if (allowsNegative && text.StartsWith(negativeSign, StringComparison.Ordinal))
            index = negativeSign.Length;

        var seenSeparator = false;

        while (index < text.Length)
        {
            if (allowsDecimals && !seenSeparator && string.CompareOrdinal(text, index, separator, 0, separator.Length) == 0)
            {
                seenSeparator = true;
                index += separator.Length;
                continue;
            }

            if (!char.IsAsciiDigit(text[index]))
                return false;

            index++;
        }

        return true;
    }

    /// <summary>
    /// The culture the element's own bindings will use, not the thread's.
    /// </summary>
    /// <remarks>
    /// WPF converts a bound value using the culture from <see cref="FrameworkElement.Language"/>
    /// (which defaults to <c>en-US</c> regardless of the thread's culture), so reading the
    /// separator from anywhere else would let the box accept a character the binding then refuses
    /// to parse - the value would simply stop updating, with nothing on screen to explain why. Set
    /// <see cref="FrameworkElement.Language"/> to change both together.
    /// </remarks>
    private static CultureInfo CultureOf(FrameworkElement element)
    {
        try
        {
            return element.Language.GetSpecificCulture();
        }
        catch (InvalidOperationException)
        {
            // An XmlLanguage with no matching CultureInfo - fall back rather than tear down input.
            return CultureInfo.CurrentCulture;
        }
    }
}
