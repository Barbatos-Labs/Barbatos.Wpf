// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Windows.Data;

namespace Barbatos.Wpf.AquariusValee.UnitTests;

public class SafeMessageConverterTests
{
    /// <summary>
    /// Stands in for a real localizing converter: translates the handful of keys it knows and -
    /// crucially, the behavior this whole class exists to defend against - returns null for
    /// everything else rather than echoing the input back.
    /// </summary>
    private sealed class FakeLocalizingConverter : IValueConverter
    {
        private readonly Dictionary<string, string> _table = new()
        {
            ["Validation.Required"] = "This field is required.",
        };

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is string key && _table.TryGetValue(key, out var translated) ? translated : null;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    private sealed class ThrowingConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new FormatException("simulated bad format placeholder");

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    private static object? Run(IValueConverter? inner, object? value) =>
        new SafeMessageConverter { Inner = inner }.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

    [Fact]
    public void ARegisteredKeyIsStillTranslatedNormally() =>
        Assert.Equal("This field is required.", Run(new FakeLocalizingConverter(), "Validation.Required"));

    [Theory]
    [InlineData("Enter a valid email address.")]       // a plain literal message, never meant as a key
    [InlineData("Max length is {0}")]                  // literal text that happens to contain a placeholder
    [InlineData("Validation.KeyNobodyRegistered")]     // a real typo in a key
    public void AnythingTheInnerConverterCannotTranslateIsShownAsWrittenNotBlanked(string message) =>
        Assert.Equal(message, Run(new FakeLocalizingConverter(), message));

    [Fact]
    public void AnInnerConverterThatThrowsIsTreatedTheSameAsOneThatTranslatedNothing() =>
        Assert.Equal("Some message.", Run(new ThrowingConverter(), "Some message."));

    [Fact]
    public void WithNoInnerConverterItIsAPassThroughWhichIsCorrectForAnAppThatDoesNotLocalize() =>
        Assert.Equal("Some message.", Run(inner: null, "Some message."));

    [Fact]
    public void ANullValueBecomesEmptyStringRatherThanNullSoBindingsStayWellDefined()
    {
        Assert.Equal(string.Empty, Run(new FakeLocalizingConverter(), null));
        Assert.Equal(string.Empty, Run(inner: null, null));
    }

    [Fact]
    public void ConvertBackIsNotSupportedBecauseTheseBindingsAreOneWay() =>
        Assert.Throws<NotSupportedException>(() =>
            new SafeMessageConverter().ConvertBack("x", typeof(string), null, CultureInfo.InvariantCulture));
}
