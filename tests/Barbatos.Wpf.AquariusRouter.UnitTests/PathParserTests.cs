// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.AquariusRouter.UnitTests;

public class PathParserTests
{
    private static PathParser Parser(string path, PathParserOptions? options = null) =>
        PathParser.Create(PathTokenizer.Tokenize(path), options);

    [Fact]
    public void ParsesASimpleStaticPath()
    {
        var parser = Parser("/users/list");
        Assert.NotNull(parser.Parse("/users/list"));
        Assert.Null(parser.Parse("/users/other"));
    }

    [Fact]
    public void IsCaseInsensitiveByDefault()
    {
        var parser = Parser("/Users");
        Assert.NotNull(parser.Parse("/users"));
        Assert.NotNull(parser.Parse("/USERS"));
    }

    [Fact]
    public void SensitiveOptionMakesMatchingCaseSensitive()
    {
        var parser = Parser("/Users", new PathParserOptions { Sensitive = true });
        Assert.NotNull(parser.Parse("/Users"));
        Assert.Null(parser.Parse("/users"));
    }

    [Fact]
    public void AllowsATrailingSlashByDefault()
    {
        var parser = Parser("/users");
        Assert.NotNull(parser.Parse("/users/"));
    }

    [Fact]
    public void StrictOptionDisallowsATrailingSlash()
    {
        var parser = Parser("/users", new PathParserOptions { Strict = true });
        Assert.NotNull(parser.Parse("/users"));
        Assert.Null(parser.Parse("/users/"));
    }

    [Fact]
    public void ExtractsAPlainParam()
    {
        var parser = Parser("/users/:id");
        var result = parser.Parse("/users/42");
        Assert.NotNull(result);
        Assert.Equal("42", result["id"]);
    }

    [Fact]
    public void ExtractsMultipleParams()
    {
        var parser = Parser("/users/:username/posts/:postId");
        var result = parser.Parse("/users/eduardo/posts/123");
        Assert.NotNull(result);
        Assert.Equal("eduardo", result["username"]);
        Assert.Equal("123", result["postId"]);
    }

    [Fact]
    public void OptionalParamAbsentParsesAsEmptyString()
    {
        var parser = Parser("/users/:id?");
        var result = parser.Parse("/users");
        Assert.NotNull(result);
        Assert.Equal("", result["id"]);
    }

    [Fact]
    public void OptionalParamPresentParsesNormally()
    {
        var parser = Parser("/users/:id?");
        var result = parser.Parse("/users/42");
        Assert.NotNull(result);
        Assert.Equal("42", result["id"]);
    }

    [Fact]
    public void RepeatableParamWithMultipleValuesParsesAsAnArray()
    {
        var parser = Parser("/:chapters+");
        var result = parser.Parse("/a/b/c");
        Assert.NotNull(result);
        var chapters = Assert.IsAssignableFrom<IReadOnlyList<string>>(result["chapters"]);
        Assert.Equal(["a", "b", "c"], chapters);
    }

    [Fact]
    public void RepeatableOptionalParamWithNoValuesParsesAsEmptyStringNotAnEmptyArray()
    {
        var parser = Parser("/:chapters*");
        var result = parser.Parse("/");
        Assert.NotNull(result);
        Assert.IsType<string>(result["chapters"]);
        Assert.Equal("", result["chapters"]);
    }

    [Fact]
    public void CustomRegexConstrainsWhatMatches()
    {
        var parser = Parser(@"/users/:id(\d+)");
        Assert.NotNull(parser.Parse("/users/42"));
        Assert.Null(parser.Parse("/users/abc"));
    }

    [Fact]
    public void CatchAllMatchesAnythingIncludingRoot()
    {
        var parser = Parser("/:pathMatch(.*)*");
        Assert.NotNull(parser.Parse("/"));
        Assert.NotNull(parser.Parse("/a/b/c"));
    }

    [Fact]
    public void InvalidCustomRegexThrowsAtCompileTime() =>
        Assert.Throws<FormatException>(() => Parser(@"/users/:id(unterminated["));

    // --- Stringify ---

    [Fact]
    public void StringifiesAStaticPathVerbatim() =>
        Assert.Equal("/users/list", Parser("/users/list").Stringify(new Dictionary<string, object?>()));

    [Fact]
    public void StringifiesAPlainParam() =>
        Assert.Equal("/users/42", Parser("/users/:id").Stringify(new Dictionary<string, object?> { ["id"] = "42" }));

    [Fact]
    public void StringifyThrowsWhenARequiredParamIsMissing() =>
        Assert.Throws<InvalidOperationException>(() => Parser("/users/:id").Stringify(new Dictionary<string, object?>()));

    [Fact]
    public void StringifyOmitsAMissingOptionalParamAndItsSlash() =>
        Assert.Equal("/users", Parser("/users/:id?").Stringify(new Dictionary<string, object?>()));

    [Fact]
    public void StringifyJoinsARepeatableParamWithSlashes() =>
        Assert.Equal("/a/b/c", Parser("/:chapters+").Stringify(new Dictionary<string, object?> { ["chapters"] = new[] { "a", "b", "c" } }));

    [Fact]
    public void StringifyThrowsWhenANonRepeatableParamIsGivenAnArray() =>
        Assert.Throws<InvalidOperationException>(() => Parser("/users/:id").Stringify(new Dictionary<string, object?> { ["id"] = new[] { "1", "2" } }));

    [Fact]
    public void ParseThenStringifyRoundTrips()
    {
        var parser = Parser("/users/:username/posts/:postId");
        var parsed = parser.Parse("/users/eduardo/posts/123")!;
        var stringified = parser.Stringify(parsed.ToDictionary(kv => kv.Key, kv => (object?)kv.Value));
        Assert.Equal("/users/eduardo/posts/123", stringified);
    }

    // --- Scoring / ranking ---

    [Fact]
    public void AStaticSegmentOutranksADynamicOneAtTheSamePosition()
    {
        var staticParser = Parser("/users/new");
        var dynamicParser = Parser("/users/:id");
        Assert.True(PathParser.Compare(staticParser, dynamicParser) < 0);
    }

    [Fact]
    public void ACustomRegexParamOutranksAPlainDynamicParam()
    {
        var customParser = Parser(@"/users/:id(\d+)");
        var plainParser = Parser("/users/:name");
        Assert.True(PathParser.Compare(customParser, plainParser) < 0);
    }

    [Fact]
    public void ACatchAllSortsLastAgainstAnythingElse()
    {
        var catchAll = Parser("/:pathMatch(.*)*");
        var staticParser = Parser("/a/b");
        var dynamicParser = Parser("/:id");

        Assert.True(PathParser.Compare(staticParser, catchAll) < 0);
        Assert.True(PathParser.Compare(dynamicParser, catchAll) < 0);
    }

    [Fact]
    public void AnOptionalParamRanksBelowARequiredOneOtherwiseIdentical()
    {
        var required = Parser("/users/:id");
        var optional = Parser("/users/:id?");
        Assert.True(PathParser.Compare(required, optional) < 0);
    }

    [Fact]
    public void CompareIsZeroForTwoEquivalentPaths() =>
        Assert.Equal(0, PathParser.Compare(Parser("/users/:id"), Parser("/users/:name")));
}
