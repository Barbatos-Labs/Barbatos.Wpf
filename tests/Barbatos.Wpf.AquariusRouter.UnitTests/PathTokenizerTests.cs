// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.AquariusRouter.UnitTests;

public class PathTokenizerTests
{
    [Fact]
    public void EmptyPathTokenizesToOneEmptySegment()
    {
        var segments = PathTokenizer.Tokenize("");
        Assert.Single(segments);
        Assert.Empty(segments[0]);
    }

    [Fact]
    public void RootPathTokenizesToOneEmptyStaticToken()
    {
        var segments = PathTokenizer.Tokenize("/");
        Assert.Single(segments);
        var token = Assert.Single(segments[0]);
        Assert.Equal(PathTokenKind.Static, token.Kind);
        Assert.Equal("", token.Value);
    }

    [Fact]
    public void PathNotStartingWithSlashThrows() =>
        Assert.Throws<ArgumentException>(() => PathTokenizer.Tokenize("users"));

    [Fact]
    public void PlainStaticSegmentsTokenizeOnePerSegment()
    {
        var segments = PathTokenizer.Tokenize("/users/list");

        Assert.Equal(2, segments.Count);
        Assert.Equal("users", Assert.Single(segments[0]).Value);
        Assert.Equal("list", Assert.Single(segments[1]).Value);
    }

    [Fact]
    public void PlainParamTokenizesAsNonOptionalNonRepeatable()
    {
        var segments = PathTokenizer.Tokenize("/users/:id");

        var token = Assert.Single(segments[1]);
        Assert.Equal(PathTokenKind.Param, token.Kind);
        Assert.Equal("id", token.Value);
        Assert.False(token.Optional);
        Assert.False(token.Repeatable);
        Assert.Equal("", token.Regex);
    }

    [Theory]
    [InlineData("/users/:id?", false, true)]
    [InlineData("/users/:id+", true, false)]
    [InlineData("/users/:id*", true, true)]
    public void ModifierSuffixesSetRepeatableAndOptionalCorrectly(string path, bool repeatable, bool optional)
    {
        var token = Assert.Single(PathTokenizer.Tokenize(path)[1]);
        Assert.Equal(repeatable, token.Repeatable);
        Assert.Equal(optional, token.Optional);
    }

    [Fact]
    public void CustomRegexIsCapturedVerbatim()
    {
        var token = Assert.Single(PathTokenizer.Tokenize(@"/users/:id(\d+)")[1]);
        Assert.Equal(@"\d+", token.Regex);
        Assert.False(token.Optional);
        Assert.False(token.Repeatable);
    }

    [Fact]
    public void CustomRegexCombinedWithRepeatModifierAfterTheParen()
    {
        var token = Assert.Single(PathTokenizer.Tokenize(@"/:chapters(\d+)+")[0]);
        Assert.Equal(@"\d+", token.Regex);
        Assert.True(token.Repeatable);
        Assert.False(token.Optional);
    }

    [Fact]
    public void CatchAllPatternTokenizesAsWildcardOptionalRepeatable()
    {
        var token = Assert.Single(PathTokenizer.Tokenize("/:pathMatch(.*)*")[0]);
        Assert.Equal(".*", token.Regex);
        Assert.True(token.Repeatable);
        Assert.True(token.Optional);
    }

    [Fact]
    public void EscapedCharacterInStaticTextIsLiteral()
    {
        var token = Assert.Single(PathTokenizer.Tokenize(@"/a\:b")[0]);
        Assert.Equal(PathTokenKind.Static, token.Kind);
        Assert.Equal("a:b", token.Value);
    }

    [Fact]
    public void EscapedClosingParenInsideCustomRegexIsUnescapedIntoTheRegexText()
    {
        var token = Assert.Single(PathTokenizer.Tokenize(@"/:id(a\)b)")[0]);
        Assert.Equal("a)b", token.Regex);
    }

    [Fact]
    public void MultipleParamsSharingOneSegmentTokenizeAsSiblingsWithAStaticSeparator()
    {
        var segment = PathTokenizer.Tokenize("/:year-:month")[0];

        Assert.Equal(3, segment.Count);
        Assert.Equal(PathTokenKind.Param, segment[0].Kind);
        Assert.Equal("year", segment[0].Value);
        Assert.Equal(PathTokenKind.Static, segment[1].Kind);
        Assert.Equal("-", segment[1].Value);
        Assert.Equal(PathTokenKind.Param, segment[2].Kind);
        Assert.Equal("month", segment[2].Value);
    }

    [Fact]
    public void RepeatableParamNotAloneInItsSegmentThrows() =>
        Assert.Throws<FormatException>(() => PathTokenizer.Tokenize("/:a-:ids+"));

    [Fact]
    public void OptionalParamNotAloneInItsSegmentIsAllowed()
    {
        var segment = PathTokenizer.Tokenize("/:a?-:b")[0];
        Assert.Equal(3, segment.Count);
        Assert.True(segment[0].Optional);
    }

    [Fact]
    public void UnterminatedCustomRegexThrows() =>
        Assert.Throws<FormatException>(() => PathTokenizer.Tokenize("/:id(abc"));

    [Fact]
    public void TrailingSlashProducesATrailingEmptySegment()
    {
        var segments = PathTokenizer.Tokenize("/home/");
        Assert.Equal(2, segments.Count);
        Assert.Equal("home", Assert.Single(segments[0]).Value);
        Assert.Empty(segments[1]);
    }
}
