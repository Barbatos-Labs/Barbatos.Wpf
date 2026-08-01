// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Text;

namespace Barbatos.Wpf.AquariusRouter.Searching;

/// <summary>
/// Normalizes text into the form <see cref="RouteSearchIndex"/> actually compares - lowercased
/// and stripped of diacritics, so a query typed without accents still finds an accented title.
/// </summary>
internal static class SearchText
{
    /// <summary>
    /// Folds <paramref name="value"/> for accent-insensitive comparison, <b>stripping a combining
    /// mark only when it sits on a Latin letter</b>. <c>đ</c>/<c>Đ</c> is mapped explicitly:
    /// Unicode encodes that stroke as part of a distinct base letter rather than as a combining
    /// mark, so - unlike every other Vietnamese diacritic - it survives decomposition untouched.
    /// </summary>
    /// <remarks>
    /// The Latin-only restriction is what keeps this safe outside Latin script. Stripping every
    /// combining mark is the obvious implementation and is actively wrong for several writing
    /// systems, because plenty of scripts encode <i>letters</i> - not decorations - that way:
    /// <list type="bullet">
    /// <item><description>Japanese dakuten/handakuten are combining marks, so <c>だ</c> decomposes
    /// to <c>た</c> + a mark - blanket stripping turns <c>だいがく</c> ("university") into
    /// <c>たいかく</c> ("physique").</description></item>
    /// <item><description>Thai vowel signs are combining marks: <c>เงิน</c> ("money") loses its
    /// vowel and becomes the non-word <c>เงน</c>.</description></item>
    /// <item><description>Cyrillic <c>й</c> decomposes to <c>и</c> + breve, conflating two
    /// distinct letters (and Ukrainian <c>ї</c> with <c>і</c>).</description></item>
    /// </list>
    /// Because both the query and the indexed text are folded identically, that damage never shows
    /// up as "nothing is found" - it shows up as unrelated results quietly matching each other,
    /// which is worse. Restricting the strip to Latin bases keeps Vietnamese (the case this exists
    /// for), French, German, Nordic, Polish, Turkish and friends behaving exactly as before, while
    /// leaving every other script byte-for-byte intact.
    /// <para>
    /// The deliberate trade-off: Arabic harakat and Hebrew niqqud are combining marks too, and they
    /// <i>are</i> decorations a searcher usually omits - those are no longer stripped either. Route
    /// titles rarely carry them, so a bare query still matches a bare title; supply the unvowelled
    /// spelling in <see cref="RouteSearchEntry.Keywords"/> if a title does carry them.
    /// </para>
    /// </remarks>
    public static string Fold(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(decomposed.Length);
        var onLatinBase = false;

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                // Keep the mark unless it is decorating a Latin letter, where dropping it is the
                // whole point (and where NFD reliably leaves an ASCII letter as the base).
                if (!onLatinBase)
                    builder.Append(c);
                continue;
            }

            var folded = c switch
            {
                'đ' => 'd',
                'Đ' => 'D',
                _ => c,
            };

            onLatinBase = IsLatinLetter(folded);
            builder.Append(folded);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    /// <summary>
    /// Whether <paramref name="c"/> is a Latin letter, i.e. one whose combining marks are accents
    /// worth ignoring. Only ASCII needs testing: <see cref="NormalizationForm.FormD"/> already
    /// reduces every accented Latin letter this cares about - including the whole Vietnamese set in
    /// Latin Extended Additional - to an ASCII base plus separate marks.
    /// </summary>
    private static bool IsLatinLetter(char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z';
}
