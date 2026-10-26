// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Text;

namespace Barbatos.Wpf.Aquarius.Xaml;

internal enum TokenType
{
    Number,
    String,
    Identifier,
    True,
    False,
    Null,
    And,
    Or,
    Equal,
    NotEqual,
    Greater,
    GreaterOrEqual,
    Less,
    LessOrEqual,
    Plus,
    Minus,
    Star,
    Slash,
    Not,
    LParen,
    RParen,
    Question,
    Colon,
    EndOfInput,
}

internal readonly struct Token(TokenType type, string text, int position, object? value = null)
{
    public TokenType Type { get; } = type;
    public string Text { get; } = text;
    public int Position { get; } = position;

    /// <summary><see langword="double"/> for <see cref="TokenType.Number"/>, <see langword="string"/> for <see cref="TokenType.String"/>.</summary>
    public object? Value { get; } = value;
}

internal static class ExprLexer
{
    public static List<Token> Tokenize(string source)
    {
        var tokens = new List<Token>();
        var i = 0;

        while (i < source.Length)
        {
            var c = source[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            tokens.Add(c switch
            {
                _ when char.IsAsciiDigit(c) => ReadNumber(source, ref i),
                '"' => ReadString(source, ref i),
                _ when IsIdentifierStart(c) => ReadWord(source, ref i),
                '#' => ReadElementReference(source, ref i),
                _ => ReadOperator(source, ref i),
            });
        }

        tokens.Add(new Token(TokenType.EndOfInput, "", i));
        return tokens;
    }

    private static Token ReadNumber(string source, ref int i)
    {
        var start = i;
        SkipDigits(source, ref i);

        if (i < source.Length && source[i] == '.' && i + 1 < source.Length && char.IsAsciiDigit(source[i + 1]))
        {
            i++;
            SkipDigits(source, ref i);
        }

        var text = source[start..i];
        return new Token(TokenType.Number, text, start, double.Parse(text, CultureInfo.InvariantCulture));
    }

    private static Token ReadString(string source, ref int i)
    {
        var start = i;
        i++;
        var builder = new StringBuilder();

        while (true)
        {
            if (i >= source.Length)
                throw new InvalidOperationException($"Expr: unterminated string literal starting at position {start} in \"{source}\".");

            var ch = source[i];

            if (ch == '"')
            {
                i++;
                break;
            }

            if (ch == '\\')
            {
                if (i + 1 >= source.Length || source[i + 1] is not ('"' or '\\'))
                    throw new InvalidOperationException($"Expr: unsupported escape sequence at position {i} in \"{source}\" - only \\\" and \\\\ are supported.");

                builder.Append(source[i + 1]);
                i += 2;
                continue;
            }

            builder.Append(ch);
            i++;
        }

        return new Token(TokenType.String, source[start..i], start, builder.ToString());
    }

    /// <summary>An identifier, or one of the <c>true</c>/<c>false</c>/<c>null</c> keywords.</summary>
    private static Token ReadWord(string source, ref int i)
    {
        var start = i;
        SkipIdentifierParts(source, ref i);

        var text = source[start..i];
        var type = text switch
        {
            "true" => TokenType.True,
            "false" => TokenType.False,
            "null" => TokenType.Null,
            _ => TokenType.Identifier,
        };
        return new Token(type, text, start);
    }

    /// <summary>
    /// "#ElementName.Path" - an element-reference identifier (resolved via
    /// Binding.ElementName instead of DataContext). The token text keeps the
    /// leading '#' - Expr.cs checks for it when building each Binding, and it also
    /// naturally keeps "#Foo" and a plain "Foo" from colliding as the same
    /// ExprEvaluator.BindIdentifiers slot, since they're genuinely different sources.
    /// </summary>
    private static Token ReadElementReference(string source, ref int i)
    {
        var start = i;
        i++;
        if (i >= source.Length || !IsIdentifierStart(source[i]))
            throw new InvalidOperationException($"Expr: expected an element name after '#' at position {start} in \"{source}\".");

        SkipIdentifierParts(source, ref i);

        return new Token(TokenType.Identifier, source[start..i], start);
    }

    /// <summary>
    /// The two-character operators are listed before the one-character ones they start with, so
    /// <c>!=</c> never lexes as <c>!</c> followed by a stray <c>=</c>.
    /// </summary>
    private static Token ReadOperator(string source, ref int i)
    {
        var start = i;
        var (type, text) = (source[i], Peek(source, i + 1)) switch
        {
            ('&', '&') => (TokenType.And, "&&"),
            ('|', '|') => (TokenType.Or, "||"),
            ('=', '=') => (TokenType.Equal, "=="),
            ('!', '=') => (TokenType.NotEqual, "!="),
            ('>', '=') => (TokenType.GreaterOrEqual, ">="),
            ('<', '=') => (TokenType.LessOrEqual, "<="),
            ('>', _) => (TokenType.Greater, ">"),
            ('<', _) => (TokenType.Less, "<"),
            ('!', _) => (TokenType.Not, "!"),
            ('+', _) => (TokenType.Plus, "+"),
            ('-', _) => (TokenType.Minus, "-"),
            ('*', _) => (TokenType.Star, "*"),
            ('/', _) => (TokenType.Slash, "/"),
            ('(', _) => (TokenType.LParen, "("),
            (')', _) => (TokenType.RParen, ")"),
            ('?', _) => (TokenType.Question, "?"),
            (':', _) => (TokenType.Colon, ":"),
            _ => throw new InvalidOperationException($"Expr: unexpected character '{source[i]}' at position {start} in \"{source}\"."),
        };

        i += text.Length;
        return new Token(type, text, start);
    }

    private static void SkipDigits(string source, ref int i)
    {
        while (i < source.Length && char.IsAsciiDigit(source[i]))
            i++;
    }

    private static void SkipIdentifierParts(string source, ref int i)
    {
        while (i < source.Length && IsIdentifierPart(source[i]))
            i++;
    }

    private static bool IsIdentifierStart(char c) => char.IsAsciiLetter(c) || c == '_';

    private static bool IsIdentifierPart(char c) => char.IsAsciiLetterOrDigit(c) || c is '_' or '.';

    private static char? Peek(string source, int index) => index < source.Length ? source[index] : null;
}
