// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Barbatos.Wpf.AquariusValee.UnitTests;

/// <summary>
/// Drives a real <see cref="TextBox"/> through <see cref="TextCompositionManager"/> so every case
/// goes through the genuine <c>PreviewTextInput</c> pipeline, the same approach
/// <see cref="ValeeMaskTests"/> uses.
/// </summary>
public class ValeeNumericTests
{
    private static TextBox NewBox(NumericInput kind, double? minimum = null, double? maximum = null, string text = "")
    {
        var box = new TextBox { Text = text };

        if (minimum is not null)
            Valee.SetMinimum(box, minimum);

        if (maximum is not null)
            Valee.SetMaximum(box, maximum);

        Valee.SetNumeric(box, kind);
        box.CaretIndex = box.Text.Length;
        return box;
    }

    private static void Press(TextBox box, char key) =>
        TextCompositionManager.StartComposition(new TextComposition(InputManager.Current, box, key.ToString()));

    private static string Type(NumericInput kind, string keystrokes, double? minimum = null, double? maximum = null) =>
        StaThread.Run(() =>
        {
            var box = NewBox(kind, minimum, maximum);

            foreach (var key in keystrokes)
            {
                Press(box, key);
                box.CaretIndex = box.Text.Length;
            }

            return box.Text;
        });

    private static string Blur(NumericInput kind, string text, double? minimum = null, double? maximum = null) =>
        StaThread.Run(() =>
        {
            var box = NewBox(kind, minimum, maximum, text);
            box.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent, box));
            return box.Text;
        });

    [Theory]
    [InlineData("1234", "1234")]
    [InlineData("12a34", "1234")]          // letters dropped
    [InlineData("12 34", "1234")]          // spaces dropped
    [InlineData("1.234", "1234")]          // no separator in Integer mode
    [InlineData("1,234", "1234")]
    [InlineData("12$34!", "1234")]
    public void IntegerModeKeepsDigitsAndDropsEverythingElse(string keystrokes, string expected) =>
        Assert.Equal(expected, Type(NumericInput.Integer, keystrokes));

    [Theory]
    [InlineData("12.34", "12.34")]
    [InlineData("12.3.4", "12.34")]        // only the first separator is accepted
    [InlineData("12.34a", "12.34")]
    public void DecimalModeAcceptsExactlyOneSeparator(string keystrokes, string expected) =>
        Assert.Equal(expected, Type(NumericInput.Decimal, keystrokes));

    [Theory]
    [InlineData("-")]                      // a lone sign is a legal state on the way to a number
    [InlineData("-5")]
    [InlineData("-12")]
    public void AHalfTypedNegativeIsAllowedThrough(string keystrokes) =>
        Assert.Equal(keystrokes, Type(NumericInput.Integer, keystrokes));

    [Fact]
    public void ALoneDecimalSeparatorIsAllowedThroughToo() =>
        // Rejecting "." would make ".5" impossible to type at all.
        Assert.Equal(".5", Type(NumericInput.Decimal, ".5"));

    [Fact]
    public void TheSignIsOnlyAcceptedAtTheFront() =>
        Assert.Equal("12", Type(NumericInput.Integer, "1-2"));

    [Fact]
    public void ANonNegativeLowerBoundRemovesTheSignEntirely() =>
        // No value satisfying "0 or more" is negative, so the key is pure noise.
        Assert.Equal("5", Type(NumericInput.Integer, "-5", minimum: 0));

    [Fact]
    public void ANegativeLowerBoundStillAcceptsTheSign() =>
        Assert.Equal("-5", Type(NumericInput.Integer, "-5", minimum: -100));

    [Fact]
    public void NothingIsClampedWhileTyping() =>
        // The whole reason clamping waits for blur: snapping "5" to "10" mid-word would leave the
        // user typing "100" when they meant "50".
        Assert.Equal("5", Type(NumericInput.Integer, "5", minimum: 10, maximum: 100));

    [Theory]
    [InlineData("5", 10, 100, "10")]       // below the floor
    [InlineData("500", 10, 100, "100")]    // above the ceiling
    [InlineData("50", 10, 100, "50")]      // already in range, left alone
    public void LeavingTheFieldSnapsAnOutOfRangeValueBackIn(string text, double min, double max, string expected) =>
        Assert.Equal(expected, Blur(NumericInput.Integer, text, min, max));

    [Theory]
    [InlineData("-5", "0")]                // raised to the floor
    [InlineData("999999", "999999")]       // no ceiling, so nothing to snap down to
    public void AMinimumOnBothItsOwnBoundsOnlyTheBottom(string text, string expected) =>
        Assert.Equal(expected, Blur(NumericInput.Decimal, text, minimum: 0));

    [Theory]
    [InlineData("500", "100")]             // lowered to the ceiling
    [InlineData("-999999", "-999999")]     // no floor, so nothing to snap up to
    public void AMaximumOnItsOwnBoundsOnlyTheTop(string text, string expected) =>
        Assert.Equal(expected, Blur(NumericInput.Decimal, text, maximum: 100));

    [Fact]
    public void WithNoBoundsAtAllLeavingTheFieldChangesNothing() =>
        Assert.Equal("999999", Blur(NumericInput.Integer, "999999"));

    [Theory]
    [InlineData("")]
    [InlineData("-")]
    [InlineData(".")]
    public void AnUnparseableIntermediateValueIsLeftForValidationToJudge(string text) =>
        // Rewriting a half-typed value on blur would destroy what the user was in the middle of;
        // the field's own rules report it instead.
        Assert.Equal(text, Blur(NumericInput.Decimal, text, minimum: 10, maximum: 100));

    [Fact]
    public void BoundsAreDiscoveredFromTheBoundFieldsRangeAttribute()
    {
        // [Range(13, 120)] on the model is the single source of truth - nothing repeated in XAML.
        var result = StaThread.Run(() =>
        {
            var field = new Field<int>("Age", 0, new RangeAttribute(13, 120));
            var box = new TextBox();
            Valee.SetField(box, field);
            Valee.SetNumeric(box, NumericInput.Integer);

            box.Text = "150";
            box.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent, box));
            return box.Text;
        });

        Assert.Equal("120", result);
    }

    [Fact]
    public void AnExplicitBoundOnTheElementOverridesTheFieldsOwn()
    {
        var result = StaThread.Run(() =>
        {
            var field = new Field<int>("Age", 0, new RangeAttribute(13, 120));
            var box = new TextBox();
            Valee.SetField(box, field);
            Valee.SetMaximum(box, 60);
            Valee.SetNumeric(box, NumericInput.Integer);

            box.Text = "150";
            box.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent, box));
            return box.Text;
        });

        Assert.Equal("60", result);
    }

    [Fact]
    public void ANonNumericRangeAttributeContributesNoBounds()
    {
        // [Range(typeof(DateTime), ...)] would otherwise convert to a tick count and clamp a text
        // box to something meaningless.
        var field = new Field<string>("StartsOn", "", new RangeAttribute(typeof(DateTime), "2020-01-01", "2030-01-01"));

        Assert.Null(field.Minimum);
        Assert.Null(field.Maximum);
    }

    [Fact]
    public void FromModelPicksUpRangeBoundsToo()
    {
        var form = Form.FromModel(new AgedModel());

        Assert.Equal(13d, form.Fields["Age"].Minimum);
        Assert.Equal(120d, form.Fields["Age"].Maximum);
        Assert.Null(form.Fields["Nickname"].Minimum);
    }

    private sealed class AgedModel
    {
        [Range(13, 120)]
        public int Age { get; set; }

        public string Nickname { get; set; } = "";
    }

    [Fact]
    public void ClearingNumericDetachesItAndLeavesTheBoxAlone()
    {
        var result = StaThread.Run(() =>
        {
            var box = NewBox(NumericInput.Integer);
            Valee.SetNumeric(box, NumericInput.None);

            foreach (var key in "a1b2")
            {
                Press(box, key);
                box.CaretIndex = box.Text.Length;
            }

            return box.Text;
        });

        Assert.Equal("a1b2", result);
    }

    [Fact]
    public void NumericRefusesANonTextBoxInsteadOfSilentlyDoingNothing() =>
        StaThread.Run(() =>
            Assert.Throws<InvalidOperationException>(() => Valee.SetNumeric(new CheckBox(), NumericInput.Integer)));
}
