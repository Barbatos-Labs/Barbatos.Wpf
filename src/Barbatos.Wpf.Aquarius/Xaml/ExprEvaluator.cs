// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Globalization;

namespace Barbatos.Wpf.Xaml;

/// <summary>
/// Resolves the distinct set of identifiers an <see cref="ExprNode"/> tree references, and
/// evaluates that tree against a values array - shared by both <see cref="Expr.ProvideValue"/>
/// (values sourced from a <see cref="System.Windows.Data.MultiBinding"/>) and
/// <see cref="Expr.Evaluate"/> (values sourced from plain reflection); only "how do we get a
/// value for this identifier name" differs between the two callers.
/// </summary>
internal static class ExprEvaluator
{
    /// <summary>
    /// Walks <paramref name="root"/>, assigning each distinct <see cref="IdentifierNode.Path"/>
    /// a slot index in first-occurrence order (an identifier repeated in the expression, e.g.
    /// <c>a + a &gt; 10</c>, shares one slot across both occurrences) and stamping that index
    /// back onto every matching <see cref="IdentifierNode"/>.
    /// </summary>
    public static IReadOnlyList<string> BindIdentifiers(ExprNode root)
    {
        var slots = new Dictionary<string, int>();
        Visit(root);
        return [.. slots.Keys];

        void Visit(ExprNode node)
        {
            switch (node)
            {
                case IdentifierNode identifier:
                    if (!slots.TryGetValue(identifier.Path, out var slot))
                    {
                        slot = slots.Count;
                        slots[identifier.Path] = slot;
                    }

                    identifier.Slot = slot;
                    break;
                case UnaryNode unary:
                    Visit(unary.Operand);
                    break;
                case BinaryNode binary:
                    Visit(binary.Left);
                    Visit(binary.Right);
                    break;
                case TernaryNode ternary:
                    Visit(ternary.Condition);
                    Visit(ternary.WhenTrue);
                    Visit(ternary.WhenFalse);
                    break;
            }
        }
    }

    public static object? Evaluate(ExprNode node, IReadOnlyList<object?> values) => node switch
    {
        LiteralNode literal => literal.Value,
        IdentifierNode identifier => values[identifier.Slot],
        UnaryNode { Operator: UnaryOperator.Not } unary => !RequireBool(Evaluate(unary.Operand, values), "!"),
        UnaryNode { Operator: UnaryOperator.Negate } unary => -RequireDouble(Evaluate(unary.Operand, values), "unary -"),
        BinaryNode { Operator: BinaryOperator.Or } binary => EvaluateOr(binary, values),
        BinaryNode { Operator: BinaryOperator.And } binary => EvaluateAnd(binary, values),
        BinaryNode { Operator: BinaryOperator.Equal } binary => AreEqual(Evaluate(binary.Left, values), Evaluate(binary.Right, values)),
        BinaryNode { Operator: BinaryOperator.NotEqual } binary => !AreEqual(Evaluate(binary.Left, values), Evaluate(binary.Right, values)),
        BinaryNode { Operator: BinaryOperator.GreaterThan } binary => Compare(Evaluate(binary.Left, values), Evaluate(binary.Right, values)) > 0,
        BinaryNode { Operator: BinaryOperator.GreaterThanOrEqual } binary => Compare(Evaluate(binary.Left, values), Evaluate(binary.Right, values)) >= 0,
        BinaryNode { Operator: BinaryOperator.LessThan } binary => Compare(Evaluate(binary.Left, values), Evaluate(binary.Right, values)) < 0,
        BinaryNode { Operator: BinaryOperator.LessThanOrEqual } binary => Compare(Evaluate(binary.Left, values), Evaluate(binary.Right, values)) <= 0,
        BinaryNode { Operator: BinaryOperator.Add } binary => RequireDouble(Evaluate(binary.Left, values), "+") + RequireDouble(Evaluate(binary.Right, values), "+"),
        BinaryNode { Operator: BinaryOperator.Subtract } binary => RequireDouble(Evaluate(binary.Left, values), "-") - RequireDouble(Evaluate(binary.Right, values), "-"),
        BinaryNode { Operator: BinaryOperator.Multiply } binary => RequireDouble(Evaluate(binary.Left, values), "*") * RequireDouble(Evaluate(binary.Right, values), "*"),
        BinaryNode { Operator: BinaryOperator.Divide } binary => RequireDouble(Evaluate(binary.Left, values), "/") / RequireDouble(Evaluate(binary.Right, values), "/"),
        // Only the taken branch is evaluated, same as C#/JS - not both, and not neither.
        TernaryNode ternary => RequireBool(Evaluate(ternary.Condition, values), "?:")
            ? Evaluate(ternary.WhenTrue, values)
            : Evaluate(ternary.WhenFalse, values),
        _ => throw new InvalidOperationException($"Expr: internal error - unhandled node type '{node.GetType().Name}'."),
    };

    // "||"/"&&" short-circuit: the right operand is only evaluated when it can actually
    // affect the result, same as C#/JS.
    private static bool EvaluateOr(BinaryNode binary, IReadOnlyList<object?> values) =>
        RequireBool(Evaluate(binary.Left, values), "||") || RequireBool(Evaluate(binary.Right, values), "||");

    private static bool EvaluateAnd(BinaryNode binary, IReadOnlyList<object?> values) =>
        RequireBool(Evaluate(binary.Left, values), "&&") && RequireBool(Evaluate(binary.Right, values), "&&");

    private static bool RequireBool(object? value, string op) => value switch
    {
        bool b => b,
        null => throw new InvalidOperationException($"Expr: '{op}' requires a boolean operand, but got null."),
        _ => throw new InvalidOperationException($"Expr: '{op}' requires a boolean operand, but got '{value}' ({value.GetType().Name})."),
    };

    private static double RequireDouble(object? value, string op)
    {
        // Convert.ToDouble(null) surprisingly returns 0.0 rather than throwing - guard
        // explicitly so a null-valued property doesn't silently compare as zero.
        if (value is null)
            throw new InvalidOperationException($"Expr: '{op}' requires a number, but got null.");

        try
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            throw new InvalidOperationException($"Expr: '{op}' requires a number, but '{value}' ({value.GetType().Name}) isn't one.", ex);
        }
    }

    private static bool TryToDouble(object value, out double result)
    {
        try
        {
            result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            result = 0;
            return false;
        }
    }

    private static bool AreEqual(object? left, object? right)
    {
        if (left is null || right is null)
            return left is null && right is null;

        // Enum-vs-string is how enum comparison is supported (`status == "Active"`) -
        // deliberately in place of a bare EnumType.Member literal, which would need
        // type-directed parsing to tell apart from an ordinary dotted property path.
        if (left is Enum && right is string rightText)
            return left.ToString() == rightText;

        if (right is Enum && left is string leftText)
            return right.ToString() == leftText;

        // Two enums (same or different type) compare by type+value via Equals, not by
        // coincidentally-matching underlying numbers - e.g. Status.Active and
        // Priority.High can both happen to be 1 under the hood without meaning the same
        // thing. A bare enum compared to a plain number (Status == 1) still falls through
        // to the numeric path below - a different, intentional incidental capability.
        if (left is Enum && right is Enum)
            return Equals(left, right);

        if (TryToDouble(left, out var leftNumber) && TryToDouble(right, out var rightNumber))
            return leftNumber.Equals(rightNumber);

        return Equals(left, right);
    }

    private static int Compare(object? left, object? right)
    {
        if (left is null || right is null)
            throw new InvalidOperationException("Expr: relational operators ('>', '>=', '<', '<=') don't support null operands.");

        if (TryToDouble(left, out var leftNumber) && TryToDouble(right, out var rightNumber))
            return leftNumber.CompareTo(rightNumber);

        if (left.GetType() == right.GetType() && left is IComparable comparable)
            return comparable.CompareTo(right);

        throw new InvalidOperationException($"Expr: cannot compare '{left}' ({left.GetType().Name}) with '{right}' ({right.GetType().Name}).");
    }
}
