using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.AquariusValee.Sample.Views.Showcase;

/// <summary>
/// A hand-rolled mask for the one field on this page that a positional mask cannot express.
/// </summary>
/// <remarks>
/// Every other masked field here uses <c>aqv:Valee.Mask</c> from the library instead - a card
/// number is <c>"0000 0000 0000 0000"</c>, a country code is <c>"&gt;AA"</c>, a SKU is
/// <c>"&gt;AAA-0000"</c>. A phone number has no such fixed layout: length and grouping differ by
/// country, so pinning one shape (US-style <c>"(000) 000-0000"</c>, say) would fight every other
/// country's format. What is fixed is E.164's worldwide maximum of 15 digits, excluding the
/// leading <c>+</c> - a real standard, not a guess - so this accepts digits plus one optional
/// leading <c>+</c>, caps at 15 digits, and groups in 3s purely to keep a long run of digits
/// readable.
/// <para>
/// Same shape as any WPF input mask, since <see cref="TextBox"/> has no WinForms
/// <c>MaskedTextBox</c> equivalent: <see cref="UIElement.PreviewTextInput"/> rejects characters as
/// they are typed, and a <c>TextChanged</c> pass reformats and repositions the caret - reassigning
/// <see cref="TextBox.Text"/> resets the caret unless corrected by hand.
/// </para>
/// </remarks>
public static class InputMask
{
    private const int PhoneDigitCount = 15;

    private static bool _isReformatting;

    public static readonly DependencyProperty PhoneProperty =
        DependencyProperty.RegisterAttached("Phone", typeof(bool), typeof(InputMask), new PropertyMetadata(false, OnPhoneChanged));

    public static void SetPhone(DependencyObject element, bool value) => element.SetValue(PhoneProperty, value);
    public static bool GetPhone(DependencyObject element) => (bool)element.GetValue(PhoneProperty);

    private static void OnPhoneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox) return;

        textBox.PreviewTextInput -= OnPhonePreviewTextInput;
        textBox.TextChanged -= OnPhoneTextChanged;

        if ((bool)e.NewValue)
        {
            textBox.PreviewTextInput += OnPhonePreviewTextInput;
            textBox.TextChanged += OnPhoneTextChanged;
        }
    }

    private static void OnPhonePreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        var textBox = (TextBox)sender;

        if (e.Text == "+")
        {
            // Only a single leading '+' is accepted, only at the very start.
            e.Handled = textBox.CaretIndex != 0 || textBox.Text.Contains('+');
            return;
        }

        // Reject anything else that isn't a digit outright for a clean typing feel - TextChanged
        // below would strip it right back out anyway (it re-derives the formatted text from '+'
        // and digits alone regardless of how they got there, so a paste with stray letters/dashes
        // still comes out clean), so this is purely to avoid a character flashing then vanishing.
        e.Handled = !e.Text.All(char.IsDigit);
    }

    private static void OnPhoneTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isReformatting) return;
        var textBox = (TextBox)sender;

        var hasPlus = textBox.Text.StartsWith('+');
        var digitsBeforeCaret = textBox.Text[..Math.Min(textBox.CaretIndex, textBox.Text.Length)].Count(char.IsDigit);

        var digits = new string(textBox.Text.Where(char.IsDigit).ToArray());
        if (digits.Length > PhoneDigitCount)
            digits = digits[..PhoneDigitCount];

        var groupedDigits = string.Join(' ', Chunk(digits, 3));
        var formatted = hasPlus ? "+" + groupedDigits : groupedDigits;
        var newCaretIndex = CaretIndexAfterNDigits(formatted, digitsBeforeCaret);

        _isReformatting = true;
        textBox.Text = formatted;
        _isReformatting = false;

        // Reassigning Text resets CaretIndex/SelectionStart to 0 - but WPF applies that reset as
        // part of finishing the SAME Text-property-changed pipeline that raised this TextChanged,
        // which runs AFTER this handler returns. Setting CaretIndex synchronously here gets
        // silently overwritten by that later reset (confirmed empirically - this was the actual
        // bug, not a test artifact). Dispatcher.BeginInvoke at Input priority defers the fix to a
        // separate dispatcher operation, after the current one (and WPF's own reset) has finished.
        textBox.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, () =>
        {
            textBox.SelectionStart = newCaretIndex;
            textBox.SelectionLength = 0;
        });
    }

    private static IEnumerable<string> Chunk(string digits, int size)
    {
        for (var i = 0; i < digits.Length; i += size)
            yield return digits.Substring(i, Math.Min(size, digits.Length - i));
    }

    private static int CaretIndexAfterNDigits(string formatted, int digitCount)
    {
        if (digitCount <= 0)
        {
            // Not always position 0 - the optional leading '+' is a non-digit prefix that must
            // always precede every digit, so "0 digits before the caret" belongs right after it,
            // not before it (landing before it let the next typed digit insert ahead of the '+',
            // which made hasPlus's own "does Text start with '+'" check false on the very next
            // keystroke and silently drop the '+' forever - confirmed empirically).
            var i = 0;
            while (i < formatted.Length && !char.IsDigit(formatted[i]))
                i++;
            return i;
        }

        var seen = 0;
        for (var i = 0; i < formatted.Length; i++)
        {
            if (!char.IsDigit(formatted[i])) continue;
            seen++;
            if (seen == digitCount) return i + 1;
        }
        return formatted.Length;
    }
}
