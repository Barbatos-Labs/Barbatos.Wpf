// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Text.RegularExpressions;

namespace Barbatos.Wpf.AquariusValee.UnitTests;

public class RulesTests
{
    [Theory]
    [InlineData("Hello", true)]
    [InlineData("chào", true)] // Vietnamese diacritics - a single word, all Unicode letters
    [InlineData("Xin chào", false)] // a space is not a letter - that's what AlphaSpacesAttribute is for
    [InlineData("Hello123", false)]
    [InlineData("", true)] // isEmpty short-circuit
    [InlineData(null, true)]
    public void AlphaAcceptsUnicodeLettersOnly(string? value, bool expected) =>
        Assert.Equal(expected, new AlphaAttribute().IsValid(value));

    [Fact]
    public void AlphaAsciiOnlyRejectsNonAsciiLetters() =>
        Assert.False(new AlphaAttribute { AsciiOnly = true }.IsValid("chào"));

    [Theory]
    [InlineData("Hello123", true)]
    [InlineData("Hello 123", false)]
    [InlineData("", true)]
    public void AlphaNumericAcceptsLettersAndDigitsOnly(string? value, bool expected) =>
        Assert.Equal(expected, new AlphaNumericAttribute().IsValid(value));

    [Theory]
    [InlineData("hello-world_123", true)]
    [InlineData("hello world", false)]
    public void AlphaDashAcceptsLettersDigitsDashesAndUnderscores(string? value, bool expected) =>
        Assert.Equal(expected, new AlphaDashAttribute().IsValid(value));

    [Theory]
    [InlineData("John Doe", true)]
    [InlineData("John_Doe", false)]
    public void AlphaSpacesAcceptsLettersAndWhitespaceOnly(string? value, bool expected) =>
        Assert.Equal(expected, new AlphaSpacesAttribute().IsValid(value));

    [Theory]
    [InlineData("Hello123", true)]
    [InlineData("chào123", false)]
    [InlineData("", true)]
    public void AlphaNumericAsciiOnlyAcceptsAsciiLettersAndDigitsOnly(string? value, bool expected) =>
        Assert.Equal(expected, new AlphaNumericAttribute { AsciiOnly = true }.IsValid(value));

    [Theory]
    [InlineData("hello-world_123", true)]
    [InlineData("xin-chào", false)]
    public void AlphaDashAsciiOnlyAcceptsAsciiLettersDigitsDashesAndUnderscoresOnly(string? value, bool expected) =>
        Assert.Equal(expected, new AlphaDashAttribute { AsciiOnly = true }.IsValid(value));

    [Theory]
    [InlineData("John Doe\t", true)]
    [InlineData("Xin chào", false)]
    [InlineData("John Doe", false)] // a no-break space is Unicode whitespace, but not ASCII whitespace
    public void AlphaSpacesAsciiOnlyAcceptsAsciiLettersAndAsciiWhitespaceOnly(string? value, bool expected) =>
        Assert.Equal(expected, new AlphaSpacesAttribute { AsciiOnly = true }.IsValid(value));

    [Fact]
    public void AlphaSpacesAcceptsUnicodeWhitespaceWhenNotAsciiOnly() =>
        Assert.True(new AlphaSpacesAttribute().IsValid("John Doe"));

    [Fact]
    public void OneOfAcceptsAnyOfTheAllowedValues()
    {
        var attribute = new OneOfAttribute("Admin", "User", "Guest");

        Assert.True(attribute.IsValid("Admin"));
        Assert.False(attribute.IsValid("Superuser"));
        Assert.True(attribute.IsValid(null));
    }

    [Fact]
    public void OneOfMatchesLooselyAcrossTypesUnlikeTheBclsStrictAllowedValuesAttribute()
    {
        // The differentiator that keeps OneOfAttribute worth having alongside .NET 8's own
        // AllowedValuesAttribute (which is strict-equality-only, confirmed empirically: 1 (int)
        // and "1" (string) do NOT match there) - OneOfAttribute mirrors vee-validate's own real
        // `one_of` rule, which uses loose/string-form cross-type equality.
        var attribute = new OneOfAttribute(1, 2, 3);

        Assert.True(attribute.IsValid("1"));
        Assert.False(new AllowedValuesAttribute(1, 2, 3).IsValid("1"));
    }

    [Fact]
    public void NotOneOfRejectsAnyOfTheDisallowedValues()
    {
        var attribute = new NotOneOfAttribute("admin", "root");

        Assert.False(attribute.IsValid("admin"));
        Assert.True(attribute.IsValid("regularUser"));
    }

    [Theory]
    [InlineData("1234", 4, true)]
    [InlineData("123", 4, false)]
    [InlineData("12a4", 4, false)]
    [InlineData("", 4, true)]
    public void DigitsRequiresExactlyNAsciiDigits(string? value, int length, bool expected) =>
        Assert.Equal(expected, new DigitsAttribute(length).IsValid(value));

    [Fact]
    public void ExactLengthIsExpressedNativelyAsLengthAttributeWithEqualMinAndMax()
    {
        // No Barbatos-authored "ExactLengthAttribute" exists - confirmed empirically that .NET
        // 8's own LengthAttribute(n, n) already covers this exactly, for strings and collections
        // alike, so a custom one would be pure redundancy.
        var attribute = new LengthAttribute(5, 5);

        Assert.True(attribute.IsValid("hello"));
        Assert.False(attribute.IsValid("hell"));
        Assert.False(attribute.IsValid("helloo"));
        Assert.True(attribute.IsValid(new List<int> { 1, 2, 3, 4, 5 }));
        Assert.False(attribute.IsValid(new List<int> { 1, 2 }));
    }

    [Theory]
    [InlineData("12345", true)]
    [InlineData("123.45", false)]
    [InlineData("-123", false)]
    public void NumericStringRequiresDigitsOnlyNoSign(string? value, bool expected) =>
        Assert.Equal(expected, new NumericStringAttribute().IsValid(value));

    [Theory]
    [InlineData("12345", true)]
    [InlineData("-12345", true)]
    [InlineData("-", false)]
    [InlineData("12.45", false)]
    public void IntegerStringAllowsAnOptionalLeadingMinusSign(string? value, bool expected) =>
        Assert.Equal(expected, new IntegerStringAttribute().IsValid(value));

    [Theory]
    [InlineData("abc", true)]
    [InlineData("abc123", false)]  // MustMatchEntireValue defaults to true - a partial match is not enough
    [InlineData("", true)]
    [InlineData(null, true)]
    public void PatternMatchesTheWholeValueByDefault(string? value, bool expected) =>
        Assert.Equal(expected, new PatternAttribute("[a-z]+").IsValid(value));

    [Fact]
    public void PatternExposesRegexOptionsWhichIsExactlyWhatTheBclAttributeCannotDo()
    {
        // The whole reason this attribute exists alongside RegularExpressionAttribute: the BCL one
        // has no RegexOptions property at all (confirmed empirically), so case-insensitivity is
        // only reachable by smuggling an inline (?i) into the pattern string.
        Assert.Null(typeof(RegularExpressionAttribute).GetProperty("RegexOptions"));
        Assert.False(new RegularExpressionAttribute("^[a-z]+$").IsValid("ABC"));

        Assert.True(new PatternAttribute("[a-z]+") { Options = RegexOptions.IgnoreCase }.IsValid("ABC"));
        Assert.False(new PatternAttribute("[a-z]+").IsValid("ABC"));
    }

    [Fact]
    public void PatternCanBeRelaxedToASubstringSearch()
    {
        var anywhere = new PatternAttribute(@"\d{4}") { MustMatchEntireValue = false };

        Assert.True(anywhere.IsValid("order 1234 shipped"));
        Assert.False(anywhere.IsValid("order 123 shipped"));
        Assert.False(new PatternAttribute(@"\d{4}").IsValid("order 1234 shipped"));
    }

    [Fact]
    public void PatternTreatsACatastrophicallyBacktrackingMatchAsInvalidRatherThanThrowing()
    {
        // A pathological pattern/input pair must fail the field, never tear down the UI thread.
        var evil = new PatternAttribute("(a+)+$") { MatchTimeoutInMilliseconds = 1 };

        var result = evil.IsValid(new string('a', 50) + "!");

        Assert.False(result);
    }

    [Fact]
    public void PatternRejectsANullOrEmptyPatternUpFront()
    {
        Assert.Throws<ArgumentNullException>(() => new PatternAttribute(null!));
        Assert.Throws<ArgumentException>(() => new PatternAttribute(""));
    }

    private sealed class PriceModel
    {
        public int MinPrice { get; set; } = 10;
        public int MaxPrice { get; set; } = 100;
        public int Price { get; set; } = 50;
    }

    // PropertyRangeAttribute overrides the ValidationContext-aware IsValid(object?, ValidationContext)
    // overload, not the simple IsValid(object?) every other attribute in this file uses - the base
    // ValidationAttribute.IsValid(object?) throws NotImplementedException unless overridden, so a
    // real ValidationContext wrapping a real model instance (via GetValidationResult) is required,
    // exactly like CompareAttribute itself needs.
    [Fact]
    public void PropertyRangeReflectsSiblingPropertiesCurrentValuesNotFixedConstants()
    {
        var model = new PriceModel();
        var attribute = new PropertyRangeAttribute(nameof(PriceModel.MinPrice), nameof(PriceModel.MaxPrice));
        var context = new ValidationContext(model) { MemberName = nameof(PriceModel.Price) };

        Assert.Equal(ValidationResult.Success, attribute.GetValidationResult(50, context));
        Assert.NotEqual(ValidationResult.Success, attribute.GetValidationResult(5, context));
        Assert.NotEqual(ValidationResult.Success, attribute.GetValidationResult(500, context));

        // Same value (5), but the bound itself moved - proves it reads MinPrice live, not a
        // snapshot taken when the attribute was constructed.
        model.MinPrice = 0;
        Assert.Equal(ValidationResult.Success, attribute.GetValidationResult(5, context));
    }

    [Fact]
    public void PropertyRangeThrowsWhenItReferencesAPropertyThatDoesNotExist()
    {
        var model = new PriceModel();
        var attribute = new PropertyRangeAttribute("NoSuchMinProperty", nameof(PriceModel.MaxPrice));
        var context = new ValidationContext(model) { MemberName = nameof(PriceModel.Price) };

        Assert.Throws<InvalidOperationException>(() => attribute.GetValidationResult(50, context));
    }

    [Fact]
    public void PropertyRangeDeclaresBothBoundsAsDependenciesViaIDependsOnProperties()
    {
        IDependsOnProperties attribute = new PropertyRangeAttribute(nameof(PriceModel.MinPrice), nameof(PriceModel.MaxPrice));

        Assert.Equal([nameof(PriceModel.MinPrice), nameof(PriceModel.MaxPrice)], attribute.DependsOnPropertyNames);
    }

    // Stands in for a .resx-generated resource class: ValidationAttribute looks the named static
    // string property up by reflection, exactly as it does on a generated Resources class.
    public static class RuleMessages
    {
        public static string LettersOnly => "Chỉ được nhập chữ cái vào {0}.";

        public static string PriceOutOfRange => "{0} phải nằm trong khoảng {1} đến {2}.";
    }

    [Fact]
    public void AValeeRuleTakesItsMessageFromAResourceWhenOneIsNamed()
    {
        // Every Valee rule used to assign its default text to ErrorMessage in its constructor, and
        // the BCL throws InvalidOperationException ("Either ErrorMessageString or
        // ErrorMessageResourceName must be set, but not both") the moment a message is formatted
        // with both set - so the resx route DataAnnotations offers for localization was closed.
        var attribute = new AlphaAttribute
        {
            ErrorMessageResourceType = typeof(RuleMessages),
            ErrorMessageResourceName = nameof(RuleMessages.LettersOnly),
        };

        Assert.Equal("Chỉ được nhập chữ cái vào Họ tên.", attribute.FormatErrorMessage("Họ tên"));
    }

    [Fact]
    public async Task AResourceMessageReachesTheFieldsErrors()
    {
        var field = new Field<string>("FullName", "abc123", new AlphaAttribute
        {
            ErrorMessageResourceType = typeof(RuleMessages),
            ErrorMessageResourceName = nameof(RuleMessages.LettersOnly),
        })
        {
            Label = "Họ tên",
        };

        await field.ValidateAsync();

        Assert.Equal("Chỉ được nhập chữ cái vào Họ tên.", field.ErrorMessage);
    }

    private sealed class LocalizedPriceModel
    {
        public int MinPrice { get; set; } = 10;

        public int MaxPrice { get; set; } = 100;

        [Display(Name = "Giá")]
        [PropertyRange(nameof(MinPrice), nameof(MaxPrice),
            ErrorMessageResourceType = typeof(RuleMessages),
            ErrorMessageResourceName = nameof(RuleMessages.PriceOutOfRange))]
        public int Price { get; set; } = 500;
    }

    [Fact]
    public async Task PropertyRangeFormatsAResourceMessageWithTheLiveBounds()
    {
        // The one localization route a PropertyRange message has - an attribute cannot reach a DI
        // localizer - and it keeps the {1}/{2} placeholders a lookup-key message has to give up.
        var form = Form.FromModel(new LocalizedPriceModel());

        await form.Fields["Price"].ValidateAsync();

        Assert.Equal("Giá phải nằm trong khoảng 10 đến 100.", form.Fields["Price"].ErrorMessage);
    }

    [Fact]
    public void AValeeRuleStillFallsBackToItsDefaultMessageAndANamedErrorMessageStillWins()
    {
        // The default now travels through the base constructor, so ErrorMessage itself reads null
        // until set. That is ValidationAttribute's own getter on .NET 8 and 10 alike - it returns
        // the explicitly set message or an internal default only EmailAddress/Phone/Url-style BCL
        // attributes can fill - and RequiredAttribute, which passes its default the same way,
        // reads null too; the docs promise that parity, so it is pinned here.
        var byDefault = new DigitsAttribute(6);
        var custom = new DigitsAttribute(6) { ErrorMessage = "Enter the {0} from the text message." };

        Assert.Null(byDefault.ErrorMessage);
        Assert.Null(new RequiredAttribute().ErrorMessage);
        Assert.Equal("The PIN field must be exactly 6 digits.", byDefault.FormatErrorMessage("PIN"));
        Assert.Equal("Enter the PIN from the text message.", custom.FormatErrorMessage("PIN"));
    }
}
