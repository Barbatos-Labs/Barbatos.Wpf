// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Aquarius.Xaml;

// Not partial-class fragments of Expr - separate internal types colocated by filename
// (ExprAst.cs/ExprLexer.cs/ExprParser.cs/ExprEvaluator.cs) purely for discoverability, unlike
// this codebase's Directives.Xxx.cs files, which really are partial fragments of Directives.

internal abstract class ExprNode;

internal sealed class LiteralNode(object? value) : ExprNode
{
    public object? Value { get; } = value;
}

internal sealed class IdentifierNode(string path) : ExprNode
{
    public string Path { get; } = path;

    /// <summary>Filled in by <see cref="ExprEvaluator.BindIdentifiers"/> - the index into the resolved values array this identifier reads from.</summary>
    public int Slot { get; set; }
}

internal enum UnaryOperator
{
    Not,
    Negate,
}

internal sealed class UnaryNode(UnaryOperator @operator, ExprNode operand) : ExprNode
{
    public UnaryOperator Operator { get; } = @operator;
    public ExprNode Operand { get; } = operand;
}

internal enum BinaryOperator
{
    Or,
    And,
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Add,
    Subtract,
    Multiply,
    Divide,
}

internal sealed class BinaryNode(BinaryOperator @operator, ExprNode left, ExprNode right) : ExprNode
{
    public BinaryOperator Operator { get; } = @operator;
    public ExprNode Left { get; } = left;
    public ExprNode Right { get; } = right;
}

/// <summary><c>condition ? whenTrue : whenFalse</c> - the lowest-precedence operator, right-associative (so <c>a ? b : c ? d : e</c> chains as <c>a ? b : (c ? d : e)</c>).</summary>
internal sealed class TernaryNode(ExprNode condition, ExprNode whenTrue, ExprNode whenFalse) : ExprNode
{
    public ExprNode Condition { get; } = condition;
    public ExprNode WhenTrue { get; } = whenTrue;
    public ExprNode WhenFalse { get; } = whenFalse;
}
