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
}
