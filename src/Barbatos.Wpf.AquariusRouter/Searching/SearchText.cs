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
    /// Folds <paramref name="value"/> for accent-insensitive comparison. <c>đ</c>/<c>Đ</c> is
    /// handled explicitly before decomposition: Unicode encodes the stroke as part of a distinct
    /// base letter rather than as a combining mark, so - unlike every other Vietnamese diacritic -
    /// it survives an ordinary decompose-and-strip pass untouched.
    /// </summary>
    public static string Fold(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            builder.Append(c switch
            {
                'đ' => 'd',
                'Đ' => 'D',
                _ => c,
            });
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
