// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Text;

namespace Barbatos.Wpf.Xaml;

internal enum TokenType
{
    Number,
    String,
    Identifier,
    True,
    False,
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

            var start = i;

            if (char.IsAsciiDigit(c))
            {
                i++;
                while (i < source.Length && char.IsAsciiDigit(source[i]))
                    i++;

                if (i < source.Length && source[i] == '.' && i + 1 < source.Length && char.IsAsciiDigit(source[i + 1]))
                {
                    i++;
                    while (i < source.Length && char.IsAsciiDigit(source[i]))
                        i++;
                }

                var text = source[start..i];
                tokens.Add(new Token(TokenType.Number, text, start, double.Parse(text, CultureInfo.InvariantCulture)));
                continue;
            }

            if (c == '"')
            {
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

                tokens.Add(new Token(TokenType.String, source[start..i], start, builder.ToString()));
                continue;
            }

            if (char.IsAsciiLetter(c) || c == '_')
            {
                i++;
                while (i < source.Length && (char.IsAsciiLetterOrDigit(source[i]) || source[i] is '_' or '.'))
                    i++;

                var text = source[start..i];
                var type = text switch
                {
                    "true" => TokenType.True,
                    "false" => TokenType.False,
                    _ => TokenType.Identifier,
                };
                tokens.Add(new Token(type, text, start));
                continue;
            }

            // "#ElementName.Path" - an element-reference identifier (resolved via
            // Binding.ElementName instead of DataContext). The token text keeps the
            // leading '#' - Expr.cs checks for it when building each Binding, and it also
            // naturally keeps "#Foo" and a plain "Foo" from colliding as the same
            // ExprEvaluator.BindIdentifiers slot, since they're genuinely different sources.
            if (c == '#')
            {
                i++;
                if (i >= source.Length || !(char.IsAsciiLetter(source[i]) || source[i] == '_'))
                    throw new InvalidOperationException($"Expr: expected an element name after '#' at position {start} in \"{source}\".");

                while (i < source.Length && (char.IsAsciiLetterOrDigit(source[i]) || source[i] is '_' or '.'))
                    i++;

                tokens.Add(new Token(TokenType.Identifier, source[start..i], start));
                continue;
            }

            switch (c)
            {
                case '&' when Peek(source, i + 1) == '&':
                    tokens.Add(new Token(TokenType.And, "&&", start));
                    i += 2;
                    continue;
                case '|' when Peek(source, i + 1) == '|':
                    tokens.Add(new Token(TokenType.Or, "||", start));
                    i += 2;
                    continue;
                case '=' when Peek(source, i + 1) == '=':
                    tokens.Add(new Token(TokenType.Equal, "==", start));
                    i += 2;
                    continue;
                case '!' when Peek(source, i + 1) == '=':
                    tokens.Add(new Token(TokenType.NotEqual, "!=", start));
                    i += 2;
                    continue;
                case '>' when Peek(source, i + 1) == '=':
                    tokens.Add(new Token(TokenType.GreaterOrEqual, ">=", start));
                    i += 2;
                    continue;
                case '<' when Peek(source, i + 1) == '=':
                    tokens.Add(new Token(TokenType.LessOrEqual, "<=", start));
                    i += 2;
                    continue;
                case '>':
                    tokens.Add(new Token(TokenType.Greater, ">", start));
                    i++;
                    continue;
                case '<':
                    tokens.Add(new Token(TokenType.Less, "<", start));
                    i++;
                    continue;
                case '!':
                    tokens.Add(new Token(TokenType.Not, "!", start));
                    i++;
                    continue;
                case '+':
                    tokens.Add(new Token(TokenType.Plus, "+", start));
                    i++;
                    continue;
                case '-':
                    tokens.Add(new Token(TokenType.Minus, "-", start));
                    i++;
                    continue;
                case '*':
                    tokens.Add(new Token(TokenType.Star, "*", start));
                    i++;
                    continue;
                case '/':
                    tokens.Add(new Token(TokenType.Slash, "/", start));
                    i++;
                    continue;
                case '(':
                    tokens.Add(new Token(TokenType.LParen, "(", start));
                    i++;
                    continue;
                case ')':
                    tokens.Add(new Token(TokenType.RParen, ")", start));
                    i++;
                    continue;
                case '?':
                    tokens.Add(new Token(TokenType.Question, "?", start));
                    i++;
                    continue;
                case ':':
                    tokens.Add(new Token(TokenType.Colon, ":", start));
                    i++;
                    continue;
                default:
                    throw new InvalidOperationException($"Expr: unexpected character '{c}' at position {start} in \"{source}\".");
            }
        }

        tokens.Add(new Token(TokenType.EndOfInput, "", i));
        return tokens;
    }

    private static char? Peek(string source, int index) => index < source.Length ? source[index] : null;
}
