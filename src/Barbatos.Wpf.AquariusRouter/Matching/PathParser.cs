// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Text;
using System.Text.RegularExpressions;

namespace Barbatos.Wpf.AquariusRouter.Matching;

/// <summary>A named param key extracted while compiling a <see cref="PathParser"/>.</summary>
public sealed record PathParserParamKey(string Name, bool Repeatable, bool Optional);

/// <summary>
/// Options controlling how <see cref="PathParser.Create"/> compiles a path's regex.
/// </summary>
/// <remarks>
/// Vue Router's own equivalent options object also has <c>start</c>/<c>end</c> flags - <c>start</c>
/// is internal-only (always <see langword="true"/> in practice) and <c>end</c> is deprecated
/// there in favor of always being <see langword="true"/> too, so neither is exposed here; every
/// <see cref="PathParser"/> anchors both ends of its regex.
/// </remarks>
public sealed record PathParserOptions
{
    /// <summary>Makes the compiled regex case-sensitive. Default <see langword="false"/>.</summary>
    public bool Sensitive { get; init; }

    /// <summary>Disallows a trailing slash from also matching. Default <see langword="false"/>.</summary>
    public bool Strict { get; init; }
}

/// <summary>
/// A compiled matcher for one route path - the regex, extracted param keys, and a ranking
/// <see cref="Score"/> used to order overlapping routes, plus <see cref="Parse"/>/<see cref="Stringify"/>
/// to convert between a real path string and a params dictionary. The WPF/C# counterpart of Vue
/// Router's own <c>tokensToParser</c>/<c>PathParser</c>.
/// </summary>
/// <remarks>
/// Built from <see cref="PathTokenizer.Tokenize"/>'s output via <see cref="Create"/>. A faithful
/// port of Vue Router's real scoring algorithm (same constants, same tie-break rules) - this is
/// what lets a route table declare overlapping paths like <c>/users/:id</c> and
/// <c>/users/new</c> and always resolve <c>/users/new</c> to the more specific (static) one,
/// and what sorts a catch-all like <c>/:pathMatch(.*)*</c> to be tried dead last.
/// <para>
/// Param values in <see cref="Parse"/>'s result (and expected by <see cref="Stringify"/>) are
/// either a plain <see cref="string"/>, or - for a <see cref="PathToken.Repeatable"/> param with
/// at least one occurrence - an <see cref="IReadOnlyList{T}"/> of <see cref="string"/>. An
/// optional param with zero occurrences is a plain empty string either way, never an empty
/// array - matching Vue Router's own real behavior exactly (confirmed by reading its source, not
/// assumed), even though "empty repeatable param" might otherwise seem like it should be `[]`.
/// </para>
/// </remarks>
public sealed class PathParser
{
    private const double Root = 90;
    private const double Segment = 40;
    private const double StaticScore = 40;
    private const double Dynamic = 20;
    private const double BonusCustomRegExp = 10;
    private const double BonusWildcard = -50;
    private const double BonusRepeatable = -20;
    private const double BonusOptional = -8;
    private const double BonusStrict = 0.7;
    private const double BonusCaseSensitive = 0.25;

    private const string BaseParamPattern = "[^/]+?";

    private PathParser(Regex regex, IReadOnlyList<double[]> score, IReadOnlyList<PathParserParamKey> keys, IReadOnlyList<IReadOnlyList<PathToken>> segments)
    {
        Regex = regex;
        Score = score;
        Keys = keys;
        Segments = segments;
    }

    /// <summary>The compiled regex used to test/match a path against this parser.</summary>
    public Regex Regex { get; }

    /// <summary>
    /// One <see langword="double"/> array per path segment, each entry scoring one token within
    /// that segment - higher sorts first. Compared segment-by-segment, most-significant segment
    /// first, via <see cref="Compare"/>.
    /// </summary>
    public IReadOnlyList<double[]> Score { get; }

    /// <summary>Every <see cref="PathTokenKind.Param"/> token's key, in the order they appear (and capture) in <see cref="Regex"/>.</summary>
    public IReadOnlyList<PathParserParamKey> Keys { get; }

    private IReadOnlyList<IReadOnlyList<PathToken>> Segments { get; }

    /// <summary>
    /// Compiles <paramref name="segments"/> (as produced by <see cref="PathTokenizer.Tokenize"/>)
    /// into a <see cref="PathParser"/>.
    /// </summary>
    public static PathParser Create(IReadOnlyList<IReadOnlyList<PathToken>> segments, PathParserOptions? options = null)
    {
        options ??= new PathParserOptions();

        var pattern = new StringBuilder("^");
        var score = new List<double[]>();
        var keys = new List<PathParserParamKey>();

        foreach (var segment in segments)
        {
            var segmentScores = segment.Count == 0 ? new List<double> { Root } : new List<double>();

            // Allows a trailing slash to be required (strict mode) even for an otherwise-empty
            // final segment - the tokenizer produces one of these for a path ending in "/".
            if (options.Strict && segment.Count == 0)
                pattern.Append('/');

            for (var tokenIndex = 0; tokenIndex < segment.Count; tokenIndex++)
            {
                var token = segment[tokenIndex];
                var subSegmentScore = Segment + (options.Sensitive ? BonusCaseSensitive : 0);

                if (token.Kind == PathTokenKind.Static)
                {
                    if (tokenIndex == 0)
                        pattern.Append('/');
                    pattern.Append(Regex.Escape(token.Value));
                    subSegmentScore += StaticScore;
                }
                else
                {
                    keys.Add(new PathParserParamKey(token.Value, token.Repeatable, token.Optional));

                    var re = token.Regex.Length > 0 ? token.Regex : BaseParamPattern;
                    if (re != BaseParamPattern)
                    {
                        subSegmentScore += BonusCustomRegExp;
                        try
                        {
                            _ = new Regex($"({re})");
                        }
                        catch (Exception ex)
                        {
                            throw new FormatException($"Invalid custom RegExp for param \"{token.Value}\" ({re}): {ex.Message}", ex);
                        }
                    }

                    var subPattern = token.Repeatable ? $"((?:{re})(?:/(?:{re}))*)" : $"({re})";

                    if (tokenIndex == 0)
                    {
                        // Avoid an optional "/" if there are more tokens in this segment, e.g.
                        // /:p?-static or /:p?-:p2 - only a lone optional param can swallow its
                        // own leading slash.
                        subPattern = token.Optional && segment.Count < 2 ? $"(?:/{subPattern})" : "/" + subPattern;
                    }
                    if (token.Optional)
                        subPattern += "?";

                    pattern.Append(subPattern);

                    subSegmentScore += Dynamic;
                    if (token.Optional)
                        subSegmentScore += BonusOptional;
                    if (token.Repeatable)
                        subSegmentScore += BonusRepeatable;
                    if (re == ".*")
                        subSegmentScore += BonusWildcard;
                }

                segmentScores.Add(subSegmentScore);
            }

            score.Add(segmentScores.ToArray());
        }

        // Only the very last token's score gets the strict bonus.
        if (options.Strict)
        {
            var lastSegment = score[^1];
            lastSegment[^1] += BonusStrict;
        }

        if (!options.Strict)
            pattern.Append("/?");
        pattern.Append('$');

        var regex = new Regex(pattern.ToString(), options.Sensitive ? RegexOptions.Compiled : RegexOptions.Compiled | RegexOptions.IgnoreCase);

        return new PathParser(regex, score, keys, segments);
    }

    /// <summary>
    /// Matches <paramref name="path"/> against <see cref="Regex"/> and extracts params, or
    /// returns <see langword="null"/> if it doesn't match.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Parse(string path)
    {
        var match = Regex.Match(path);
        if (!match.Success)
            return null;

        var parameters = new Dictionary<string, object>();
        for (var i = 0; i < Keys.Count; i++)
        {
            var group = match.Groups[i + 1];
            var value = group.Success ? group.Value : "";
            var key = Keys[i];
            parameters[key.Name] = value.Length > 0 && key.Repeatable
                ? (IReadOnlyList<string>)value.Split('/')
                : value;
        }

        return parameters;
    }

    /// <summary>
    /// Builds a real path string from <paramref name="parameters"/> (each value a
    /// <see cref="string"/> or <see cref="IReadOnlyList{T}"/> of <see cref="string"/> for a
    /// repeatable param). Throws if a required param is missing, or a non-repeatable param is
    /// given a list.
    /// </summary>
    public string Stringify(IReadOnlyDictionary<string, object?> parameters)
    {
        var path = new StringBuilder();
        var avoidDuplicatedSlash = false;

        foreach (var segment in Segments)
        {
            if (!avoidDuplicatedSlash || path.Length == 0 || path[^1] != '/')
                path.Append('/');
            avoidDuplicatedSlash = false;

            foreach (var token in segment)
            {
                if (token.Kind == PathTokenKind.Static)
                {
                    path.Append(token.Value);
                    continue;
                }

                parameters.TryGetValue(token.Value, out var param);

                if (param is IReadOnlyList<string> && !token.Repeatable)
                    throw new InvalidOperationException($"Provided param \"{token.Value}\" is an array but it is not repeatable (* or + modifiers).");

                var text = param switch
                {
                    IReadOnlyList<string> list => string.Join('/', list),
                    string s => s,
                    null => "",
                    _ => param.ToString() ?? "",
                };

                if (text.Length == 0)
                {
                    if (token.Optional)
                    {
                        // Multiple optional params sharing a segment (/:a?-:b?) don't need this
                        // slash bookkeeping - only a lone optional param can swallow its slash.
                        if (segment.Count < 2)
                        {
                            if (path.Length > 0 && path[^1] == '/')
                                path.Length--;
                            else
                                avoidDuplicatedSlash = true;
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Missing required param \"{token.Value}\".");
                    }
                }

                path.Append(text);
            }
        }

        return path.Length > 0 ? path.ToString() : "/";
    }

    /// <summary>
    /// Compares two parsers' <see cref="Score"/>s so the more specific one sorts first (used to
    /// keep a <see cref="RouteMatcher"/>'s matcher list ordered most-to-least specific).
    /// Returns a negative number if <paramref name="a"/> should sort before <paramref name="b"/>,
    /// positive if after, zero if equal.
    /// </summary>
    public static int Compare(PathParser a, PathParser b)
    {
        var aScore = a.Score;
        var bScore = b.Score;

        var i = 0;
        while (i < aScore.Count && i < bScore.Count)
        {
            var comparison = CompareScoreArray(aScore[i], bScore[i]);
            if (comparison != 0)
                return comparison;
            i++;
        }

        if (Math.Abs(bScore.Count - aScore.Count) == 1)
        {
            if (IsLastScoreNegative(aScore))
                return 1;
            if (IsLastScoreNegative(bScore))
                return -1;
        }

        return bScore.Count - aScore.Count;
    }

    private static int CompareScoreArray(double[] a, double[] b)
    {
        var i = 0;
        while (i < a.Length && i < b.Length)
        {
            var diff = b[i] - a[i];
            if (diff != 0)
                return Math.Sign(diff);
            i++;
        }

        // If the last subsegment was Static, the shorter segment should be sorted first;
        // otherwise the longer (more specific) one wins.
        if (a.Length < b.Length)
            return a is [var onlyA] && onlyA == StaticScore + Segment ? -1 : 1;
        if (a.Length > b.Length)
            return b is [var onlyB] && onlyB == StaticScore + Segment ? 1 : -1;

        return 0;
    }

    /// <summary>Detects a trailing wildcard/optional-repeatable "splat" like <c>/home/:id(.*)*</c>.</summary>
    private static bool IsLastScoreNegative(IReadOnlyList<double[]> score)
    {
        if (score.Count == 0)
            return false;

        var last = score[^1];
        return last.Length > 0 && last[^1] < 0;
    }
}
