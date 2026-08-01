// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.AquariusValee.Xaml;

/// <summary>Which shape of number a <see cref="Valee.NumericProperty"/> text box accepts.</summary>
public enum NumericInput
{
    /// <summary>Not a numeric box - the default, leaving typing completely unrestricted.</summary>
    None,

    /// <summary>Whole numbers only: digits, plus a leading negative sign where the bounds allow one.</summary>
    Integer,

    /// <summary>Whole or fractional numbers: as <see cref="Integer"/>, plus a single decimal separator.</summary>
    Decimal,
}
