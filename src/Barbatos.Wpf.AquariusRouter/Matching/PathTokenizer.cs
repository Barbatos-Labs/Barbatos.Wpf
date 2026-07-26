// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Text;

namespace Barbatos.Wpf.AquariusRouter.Matching;

/// <summary>
/// Splits a route path string like <c>/users/:id</c> into per-segment lists of
/// <see cref="PathToken"/>s - the first stage of turning a path into a matchable
/// <see cref="PathParser"/>, the WPF/C# counterpart of Vue Router's own path tokenizer.
/// </summary>
/// <remarks>
/// A deliberately literal, line-by-line port of Vue Router's own hand-written character state
/// machine (no regex used to parse the grammar itself - only a single-character class check).
/// Kept this faithful on purpose: this exact algorithm, including a couple of non-obvious edge
/// cases (see below), already has years of real-world route strings behind it.
/// <para>
/// Grammar: a <b>segment</b> is whatever sits between two <c>/</c> characters. Each segment is
/// a list of tokens - almost always one, but <c>/:year-:month</c> is a single segment
/// containing two <b>Param</b> tokens either side of a literal <c>-</c>. A token is:
/// </para>
/// <list type="bullet">
/// <item><description><b>Static</b> - literal text.</description></item>
/// <item><description><b>Param</b> - <c>:name</c>, with optional modifiers read off whichever
/// character terminates it: <c>?</c> (optional), <c>+</c> (repeatable, 1+), <c>*</c> (repeatable
/// and optional, 0+), and/or a custom regex in parentheses immediately after the name,
/// <c>:id(\d+)</c>.</description></item>
/// </list>
/// <c>\</c> escapes the next character literally, so a literal <c>:</c>/<c>/</c>/etc. can appear
/// inside a static token. A repeatable param must be alone in its segment (<c>/:ids+</c> is
/// fine, <c>/:a-:ids+</c> throws) - optional has no such restriction.
/// </remarks>
public static class PathTokenizer
{
    /// <summary>
    /// Tokenizes <paramref name="path"/> into its per-segment token lists. Throws
    /// <see cref="ArgumentException"/> if <paramref name="path"/> doesn't start with <c>/</c>
    /// (unless empty - see below), and <see cref="FormatException"/> for a malformed grammar
    /// (an unterminated custom regex, or a repeatable param sharing a segment with another token).
    /// </summary>
    /// <remarks>
    /// An empty <paramref name="path"/> tokenizes to one empty segment (used for a route record
    /// with no path contribution of its own, e.g. a nested route's parent-joining base case) and
    /// <c>"/"</c> tokenizes to one segment containing a single empty <see cref="PathTokenKind.Static"/>
    /// token - both special-cased up front, matching Vue Router's own two early-return cases.
    /// </remarks>
    public static IReadOnlyList<IReadOnlyList<PathToken>> Tokenize(string path)
    {
        if (path.Length == 0)
            return [Array.Empty<PathToken>()];

        if (path == "/")
            return [[PathToken.Static("")]];

        if (path[0] != '/')
            throw new ArgumentException($"Route paths should start with a \"/\": \"{path}\" should be \"/{path}\".", nameof(path));

        var state = TokenizerState.Static;
        var previousState = state;
        var segments = new List<List<PathToken>>();
        List<PathToken>? segment = null;

        var buffer = new StringBuilder();
        var customRegex = new StringBuilder();
        var currentChar = '\0';

        void FinalizeSegment()
        {
            if (segment is not null)
                segments.Add(segment);
            segment = [];
        }

        void ConsumeBuffer()
        {
            if (buffer.Length == 0)
                return;

            var value = buffer.ToString();

            if (state == TokenizerState.Static)
            {
                segment!.Add(PathToken.Static(value));
            }
            else
            {
                var repeatable = currentChar is '*' or '+';
                var optional = currentChar is '*' or '?';

                // Mirrors Vue Router's own check exactly: `segment.length > 1`, i.e. this
                // repeatable param would be the THIRD-OR-LATER token added to its segment.
                if (segment!.Count > 1 && repeatable)
                    throw new FormatException($"A repeatable param ({value}) must be alone in its segment. eg: '/:{value}+'.");

                segment.Add(PathToken.Param(value, customRegex.ToString(), optional, repeatable));
            }

            buffer.Clear();
        }

        var i = 0;
        while (i < path.Length)
        {
            currentChar = path[i];
            i++;

            switch (state)
            {
                case TokenizerState.Static:
                    if (currentChar == '\\')
                    {
                        previousState = state;
                        state = TokenizerState.EscapeNext;
                    }
                    else if (currentChar == '/')
                    {
                        ConsumeBuffer();
                        FinalizeSegment();
                    }
                    else if (currentChar == ':')
                    {
                        ConsumeBuffer();
                        state = TokenizerState.Param;
                    }
                    else
                    {
                        buffer.Append(currentChar);
                    }
                    break;

                case TokenizerState.EscapeNext:
                    buffer.Append(currentChar);
                    state = previousState;
                    break;

                case TokenizerState.Param:
                    if (currentChar == '(')
                    {
                        state = TokenizerState.ParamRegExp;
                    }
                    else if (IsValidParamChar(currentChar))
                    {
                        buffer.Append(currentChar);
                    }
                    else
                    {
                        ConsumeBuffer();
                        state = TokenizerState.Static;
                        // Go back one character if we were not consuming a modifier - it needs
                        // to be reprocessed by the Static state (e.g. the '-' in /:a-:b).
                        if (currentChar is not ('*' or '?' or '+'))
                            i--;
                    }
                    break;

                case TokenizerState.ParamRegExp:
                    if (currentChar == ')')
                    {
                        if (customRegex.Length > 0 && customRegex[^1] == '\\')
                        {
                            // Un-escape a literal \) inside the custom regex text itself.
                            customRegex.Length--;
                            customRegex.Append(currentChar);
                        }
                        else
                        {
                            state = TokenizerState.ParamRegExpEnd;
                        }
                    }
                    else
                    {
                        customRegex.Append(currentChar);
                    }
                    break;

                case TokenizerState.ParamRegExpEnd:
                    // Same finalize-as-Param logic as the Param state's own else-branch above.
                    ConsumeBuffer();
                    state = TokenizerState.Static;
                    if (currentChar is not ('*' or '?' or '+'))
                        i--;
                    customRegex.Clear();
                    break;
            }
        }

        if (state == TokenizerState.ParamRegExp)
            throw new FormatException($"Unfinished custom RegExp for param \"{buffer}\".");

        ConsumeBuffer();
        FinalizeSegment();

        return segments;
    }

    private static bool IsValidParamChar(char c) =>
        (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';

    private enum TokenizerState
    {
        Static,
        Param,
        ParamRegExp,
        ParamRegExpEnd,
        EscapeNext,
    }
}
