// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Barbatos.Wpf.AquariusValee.Xaml;

public static partial class Valee
{
    /// <summary>
    /// Formats a <see cref="TextBox"/> as the user types, from a positional mask such as
    /// <c>"AAA-0000"</c> - separators appear on their own, characters that cannot go in the next
    /// slot are ignored, and the caret always lands where the next character will actually be
    /// typed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Mask characters</b> - everything not in this table is a literal, inserted automatically
    /// as soon as the slots before it are filled:
    /// </para>
    /// <list type="table">
    /// <item><term><c>0</c></term><description>a required digit.</description></item>
    /// <item><term><c>A</c></term><description>a required letter.</description></item>
    /// <item><term><c>*</c></term><description>a required letter or digit.</description></item>
    /// <item><term><c>&gt;</c></term><description>upper-case everything that follows.</description></item>
    /// <item><term><c>&lt;</c></term><description>lower-case everything that follows.</description></item>
    /// <item><term><c>\</c></term><description>treat the next character as a literal (use <c>\0</c> for a literal zero).</description></item>
    /// </list>
    /// <code>
    /// &lt;TextBox aqv:Valee.Field="{Binding Sku}" aqv:Valee.Mask="&gt;AAA-0000" /&gt;
    /// </code>
    /// <para>
    /// <b>A mask guides typing; it does not validate.</b> Keep the real rule on the model - a mask
    /// is deliberately permissive (a half-typed <c>"AB"</c> is a perfectly good intermediate state,
    /// and the user can always leave the field early), so pair it with
    /// <see cref="Rules.PatternAttribute"/> or another attribute for the actual verdict. The two
    /// are separate on purpose, and cannot be collapsed into one: a regular expression is not
    /// positional, so it cannot answer "what may be typed at index 3?", and running the full
    /// validation regex per keystroke rejects every prefix - typing <c>ABC-1234</c> against
    /// <c>^[A-Z]{3}-\d{4}$</c> would block all eight keystrokes, since no prefix matches until the
    /// last one. This is the same split WinForms' <c>MaskedTextBox</c>, imask.js and Cleave.js all
    /// make.
    /// </para>
    /// <para>
    /// <b>Not annoying, specifically</b>: literals are auto-inserted rather than demanded; a
    /// character that would be discarded is swallowed before it can flash on screen; deleting a
    /// separator deletes the character before it instead of silently reappearing; and pasted text
    /// is filtered through the mask rather than rejected. The formatted text is what reaches the
    /// bound <see cref="Validation.Field{T}"/>, so the model sees <c>"ABC-1234"</c>, not raw
    /// keystrokes.
    /// </para>
    /// </remarks>
    public static readonly DependencyProperty MaskProperty =
        DependencyProperty.RegisterAttached("Mask", typeof(string), typeof(Valee), new PropertyMetadata(null, OnMaskChanged));

    /// <summary>Sets <see cref="MaskProperty"/>.</summary>
    public static void SetMask(DependencyObject element, string? value) => element.SetValue(MaskProperty, value);

    /// <summary>Gets <see cref="MaskProperty"/>.</summary>
    public static string? GetMask(DependencyObject element) => (string?)element.GetValue(MaskProperty);

    // Re-entrancy guard. Attached per element rather than a single static flag, so two masked
    // boxes reformatting in the same dispatcher turn cannot suppress each other's pass.
    private static readonly DependencyProperty IsApplyingMaskProperty =
        DependencyProperty.RegisterAttached("IsApplyingMask", typeof(bool), typeof(Valee), new PropertyMetadata(false));

    private static void OnMaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Throw rather than no-op, matching every sibling attached property here: a mask that
        // silently does nothing because it landed on the wrong control is the kind of bug that
        // only shows up in manual testing.
        if (d is not TextBox textBox)
            throw new InvalidOperationException($"Valee.Mask can only be set on a TextBox, not '{d.GetType().Name}'.");

        textBox.PreviewTextInput -= OnMaskPreviewTextInput;
        textBox.TextChanged -= OnMaskTextChanged;
        DataObject.RemovePastingHandler(textBox, OnMaskPaste);

        if (e.NewValue is not string mask || mask.Length == 0)
            return;

        textBox.PreviewTextInput += OnMaskPreviewTextInput;
        textBox.TextChanged += OnMaskTextChanged;
        DataObject.AddPastingHandler(textBox, OnMaskPaste);

        // An initial value arriving from the bound field (an edit form, not a blank one) should
        // already be laid out correctly, without waiting for the user to type.
        if (textBox.Text.Length > 0)
            ApplyMask(textBox, MaskTokens.For(mask), isDeletion: false);
    }

    private static void OnMaskPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var textBox = (TextBox)sender;
        if (GetMask(textBox) is not { Length: > 0 } mask)
            return;

        var tokens = MaskTokens.For(mask);
        var current = textBox.Text;
        var candidate = current
            .Remove(textBox.SelectionStart, textBox.SelectionLength)
            .Insert(textBox.SelectionStart, e.Text);

        // If running the keystroke through the mask changes nothing, it had no effect - a letter
        // aimed at a digit slot, or a character past the end of the mask. Swallow it here rather
        // than letting TextChanged strip it back out, so it never flashes on screen.
        e.Handled = Format(candidate, tokens) == Format(current, tokens);
    }

    private static void OnMaskPaste(object sender, DataObjectPastingEventArgs e)
    {
        // Paste bypasses PreviewTextInput entirely. Nothing to filter here by hand: the text
        // lands, then TextChanged re-derives the whole value through the mask, which discards
        // whatever did not fit. Cancel only a paste carrying no text at all.
        if (!e.DataObject.GetDataPresent(DataFormats.Text))
            e.CancelCommand();
    }

    private static void OnMaskTextChanged(object sender, TextChangedEventArgs e)
    {
        var textBox = (TextBox)sender;

        if ((bool)textBox.GetValue(IsApplyingMaskProperty))
            return;

        if (GetMask(textBox) is not { Length: > 0 } mask)
            return;

        // "Net shrink", not "a change that removed and added nothing" - a Backspace keystroke does
        // report removed:1/added:0, but replacing Text wholesale (a ViewModel push, or a
        // select-all-and-retype) reports one change that both removes and adds, and the narrower
        // test would read that as an insertion and re-add the separator the user just deleted.
        var removed = e.Changes.Sum(change => change.RemovedLength);
        var added = e.Changes.Sum(change => change.AddedLength);

        ApplyMask(textBox, MaskTokens.For(mask), isDeletion: removed > added);
    }

    private static void ApplyMask(TextBox textBox, MaskToken[] tokens, bool isDeletion)
    {
        var original = textBox.Text;
        var formatted = Format(original, tokens);

        // Backspacing a separator would otherwise re-add it instantly and look like the key did
        // nothing. Dropping trailing literals after a deletion lets the next backspace reach the
        // character before the separator, which is what the user was aiming at.
        if (isDeletion)
            formatted = TrimTrailingLiterals(formatted, tokens);

        var caret = Math.Clamp(textBox.CaretIndex, 0, original.Length);
        var slotsBeforeCaret = CountSlotsFilled(original[..caret], tokens);
        var caretIndex = CaretIndexAfterSlots(formatted, tokens, slotsBeforeCaret);

        if (formatted != original)
        {
            textBox.SetValue(IsApplyingMaskProperty, true);
            textBox.Text = formatted;
            textBox.SetValue(IsApplyingMaskProperty, false);
        }

        // Assigning Text resets the caret to 0, but WPF applies that reset as a later step of the
        // same property-changed pipeline that raised this event - after this handler returns - so
        // setting the caret synchronously here is silently overwritten. Deferring to a separate
        // dispatcher operation at Input priority is what makes it stick.
        textBox.Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            textBox.SelectionStart = Math.Min(caretIndex, textBox.Text.Length);
            textBox.SelectionLength = 0;
        });
    }

    /// <summary>
    /// Walks the mask and the raw text together, filling each slot with the next character that
    /// fits it and skipping the ones that do not. Idempotent - re-running it on its own output is
    /// a no-op, which is what makes it safe to call on every keystroke.
    /// </summary>
    private static string Format(string text, MaskToken[] tokens)
    {
        if (text.Length == 0)
            return "";

        var result = new System.Text.StringBuilder(tokens.Length);
        var index = 0;

        foreach (var token in tokens)
        {
            if (token.Literal is { } literal)
            {
                // Consume a separator the user typed themselves, so they are not forced to skip it.
                if (index < text.Length && text[index] == literal)
                    index++;

                result.Append(literal);
                continue;
            }

            while (index < text.Length && !token.Accepts(text[index]))
                index++;

            if (index >= text.Length)
                break;

            result.Append(token.Transform(text[index]));
            index++;
        }

        return result.ToString();
    }

    private static string TrimTrailingLiterals(string formatted, MaskToken[] tokens)
    {
        var end = formatted.Length;

        while (end > 0 && end <= tokens.Length && tokens[end - 1].Literal is not null)
            end--;

        return formatted[..end];
    }

    /// <summary>
    /// Counts how many slots <paramref name="text"/> fills, by running the same walk
    /// <see cref="Format"/> does. Deliberately not "count the non-literal mask positions up to
    /// this index" - mid-edit text is not yet aligned to the mask (having just typed <c>5</c> into
    /// <c>"(000) 000"</c>, the digit sits at index 0 where the mask has a literal), so an
    /// index-based count reads a character as a separator and puts the caret in front of what was
    /// just typed.
    /// </summary>
    private static int CountSlotsFilled(string text, MaskToken[] tokens)
    {
        var index = 0;
        var slots = 0;

        foreach (var token in tokens)
        {
            if (token.Literal is { } literal)
            {
                if (index < text.Length && text[index] == literal)
                    index++;

                continue;
            }

            while (index < text.Length && !token.Accepts(text[index]))
                index++;

            if (index >= text.Length)
                break;

            slots++;
            index++;
        }

        return slots;
    }

    private static int CaretIndexAfterSlots(string formatted, MaskToken[] tokens, int slots)
    {
        var index = 0;

        if (slots > 0)
        {
            var seen = 0;

            for (; index < formatted.Length && index < tokens.Length; index++)
            {
                if (tokens[index].Literal is not null)
                    continue;

                if (++seen == slots)
                {
                    index++;
                    break;
                }
            }
        }

        // Park the caret past any separator that has already been auto-inserted, so the next
        // character typed goes into the slot after it rather than in front of it.
        while (index < formatted.Length && index < tokens.Length && tokens[index].Literal is not null)
            index++;

        return index;
    }

    private readonly record struct MaskToken(char? Literal, char Kind, char Casing)
    {
        public bool Accepts(char c) => Kind switch
        {
            '0' => char.IsDigit(c),
            'A' => char.IsLetter(c),
            _ => char.IsLetterOrDigit(c),
        };

        public char Transform(char c) => Casing switch
        {
            '>' => char.ToUpperInvariant(c),
            '<' => char.ToLowerInvariant(c),
            _ => c,
        };
    }

    private static class MaskTokens
    {
        // Masks are XAML literals, so the set of distinct strings is small, fixed, and worth
        // parsing only once per application run.
        private static readonly ConcurrentDictionary<string, MaskToken[]> Cache = new();

        public static MaskToken[] For(string mask) => Cache.GetOrAdd(mask, Parse);

        private static MaskToken[] Parse(string mask)
        {
            var tokens = new List<MaskToken>(mask.Length);
            var casing = '\0';

            for (var i = 0; i < mask.Length; i++)
            {
                var c = mask[i];

                switch (c)
                {
                    case '>' or '<':
                        casing = c;
                        continue;

                    case '\\' when i + 1 < mask.Length:
                        tokens.Add(new MaskToken(mask[++i], '\0', casing));
                        continue;

                    case '0' or 'A' or '*':
                        tokens.Add(new MaskToken(null, c, casing));
                        continue;

                    default:
                        tokens.Add(new MaskToken(c, '\0', casing));
                        continue;
                }
            }

            return [.. tokens];
        }
    }
}
