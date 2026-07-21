// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Xaml;

/// <summary>
/// Recursive-descent parser for <see cref="Expr"/>'s grammar, lowest to highest precedence:
/// ternary <c>?:</c> -&gt; <c>||</c> -&gt; <c>&amp;&amp;</c> -&gt; <c>== !=</c> -&gt;
/// <c>&gt; &gt;= &lt; &lt;=</c> -&gt; binary <c>+ -</c> -&gt; <c>* /</c> -&gt; unary <c>! -</c>
/// -&gt; primary (number, string, <c>true</c>/<c>false</c>, dotted identifier, optionally
/// element-referenced via a leading <c>#</c>, or a parenthesized sub-expression).
/// </summary>
internal static class ExprParser
{
    public static ExprNode Parse(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new InvalidOperationException("Expr: expression is empty.");

        var tokens = ExprLexer.Tokenize(source);
        var cursor = 0;

        var node = ParseTernary(tokens, ref cursor, source);

        if (tokens[cursor].Type != TokenType.EndOfInput)
            throw new InvalidOperationException($"Expr: unexpected {Describe(tokens[cursor])} at position {tokens[cursor].Position} in \"{source}\".");

        return node;
    }

    private static ExprNode ParseTernary(List<Token> tokens, ref int cursor, string source)
    {
        var condition = ParseOr(tokens, ref cursor, source);

        if (tokens[cursor].Type != TokenType.Question)
            return condition;

        cursor++;
        // Right-associative: each branch parses a full ternary, so "a ? b : c ? d : e"
        // chains as "a ? b : (c ? d : e)", matching C#/JS.
        var whenTrue = ParseTernary(tokens, ref cursor, source);

        if (tokens[cursor].Type != TokenType.Colon)
            throw new InvalidOperationException($"Expr: expected ':' but reached {Describe(tokens[cursor])} in \"{source}\".");

        cursor++;
        var whenFalse = ParseTernary(tokens, ref cursor, source);

        return new TernaryNode(condition, whenTrue, whenFalse);
    }

    private static ExprNode ParseOr(List<Token> tokens, ref int cursor, string source)
    {
        var left = ParseAnd(tokens, ref cursor, source);

        while (tokens[cursor].Type == TokenType.Or)
        {
            cursor++;
            left = new BinaryNode(BinaryOperator.Or, left, ParseAnd(tokens, ref cursor, source));
        }

        return left;
    }

    private static ExprNode ParseAnd(List<Token> tokens, ref int cursor, string source)
    {
        var left = ParseEquality(tokens, ref cursor, source);

        while (tokens[cursor].Type == TokenType.And)
        {
            cursor++;
            left = new BinaryNode(BinaryOperator.And, left, ParseEquality(tokens, ref cursor, source));
        }

        return left;
    }

    private static ExprNode ParseEquality(List<Token> tokens, ref int cursor, string source)
    {
        var left = ParseRelational(tokens, ref cursor, source);

        while (tokens[cursor].Type is TokenType.Equal or TokenType.NotEqual)
        {
            var op = tokens[cursor].Type == TokenType.Equal ? BinaryOperator.Equal : BinaryOperator.NotEqual;
            cursor++;
            left = new BinaryNode(op, left, ParseRelational(tokens, ref cursor, source));
        }

        return left;
    }

    private static ExprNode ParseRelational(List<Token> tokens, ref int cursor, string source)
    {
        var left = ParseAdditive(tokens, ref cursor, source);

        while (tokens[cursor].Type is TokenType.Greater or TokenType.GreaterOrEqual or TokenType.Less or TokenType.LessOrEqual)
        {
            var op = tokens[cursor].Type switch
            {
                TokenType.Greater => BinaryOperator.GreaterThan,
                TokenType.GreaterOrEqual => BinaryOperator.GreaterThanOrEqual,
                TokenType.Less => BinaryOperator.LessThan,
                _ => BinaryOperator.LessThanOrEqual,
            };
            cursor++;
            left = new BinaryNode(op, left, ParseAdditive(tokens, ref cursor, source));
        }

        return left;
    }

    private static ExprNode ParseAdditive(List<Token> tokens, ref int cursor, string source)
    {
        var left = ParseMultiplicative(tokens, ref cursor, source);

        while (tokens[cursor].Type is TokenType.Plus or TokenType.Minus)
        {
            var op = tokens[cursor].Type == TokenType.Plus ? BinaryOperator.Add : BinaryOperator.Subtract;
            cursor++;
            left = new BinaryNode(op, left, ParseMultiplicative(tokens, ref cursor, source));
        }

        return left;
    }

    private static ExprNode ParseMultiplicative(List<Token> tokens, ref int cursor, string source)
    {
        var left = ParseUnary(tokens, ref cursor, source);

        while (tokens[cursor].Type is TokenType.Star or TokenType.Slash)
        {
            var op = tokens[cursor].Type == TokenType.Star ? BinaryOperator.Multiply : BinaryOperator.Divide;
            cursor++;
            left = new BinaryNode(op, left, ParseUnary(tokens, ref cursor, source));
        }

        return left;
    }

    private static ExprNode ParseUnary(List<Token> tokens, ref int cursor, string source)
    {
        if (tokens[cursor].Type == TokenType.Not)
        {
            cursor++;
            return new UnaryNode(UnaryOperator.Not, ParseUnary(tokens, ref cursor, source));
        }

        if (tokens[cursor].Type == TokenType.Minus)
        {
            cursor++;
            return new UnaryNode(UnaryOperator.Negate, ParseUnary(tokens, ref cursor, source));
        }

        return ParsePrimary(tokens, ref cursor, source);
    }

    private static ExprNode ParsePrimary(List<Token> tokens, ref int cursor, string source)
    {
        var token = tokens[cursor];

        switch (token.Type)
        {
            case TokenType.Number:
                cursor++;
                return new LiteralNode(token.Value);
            case TokenType.String:
                cursor++;
                return new LiteralNode(token.Value);
            case TokenType.True:
                cursor++;
                return new LiteralNode(true);
            case TokenType.False:
                cursor++;
                return new LiteralNode(false);
            case TokenType.Identifier:
                cursor++;
                return new IdentifierNode(token.Text);
            case TokenType.LParen:
            {
                cursor++;
                var inner = ParseTernary(tokens, ref cursor, source);

                if (tokens[cursor].Type != TokenType.RParen)
                    throw new InvalidOperationException($"Expr: expected ')' but reached {Describe(tokens[cursor])} in \"{source}\".");

                cursor++;
                return inner;
            }
            default:
                throw new InvalidOperationException($"Expr: expected an expression but found {Describe(token)} in \"{source}\".");
        }
    }

    private static string Describe(Token token) =>
        token.Type == TokenType.EndOfInput ? "end of expression" : $"'{token.Text}'";
}
