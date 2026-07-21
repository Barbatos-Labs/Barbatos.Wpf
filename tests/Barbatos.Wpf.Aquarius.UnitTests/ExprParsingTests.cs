namespace Barbatos.Wpf.Aquarius.UnitTests;

public class ExprParsingTests
{
    [Theory]
    [InlineData("2 + 3", 5.0)]
    [InlineData("5 - 2", 3.0)]
    [InlineData("2 * 3", 6.0)]
    [InlineData("10 / 4", 2.5)]
    [InlineData("-5", -5.0)]
    public void ArithmeticOperatorsEvaluateAsDouble(string expression, double expected)
    {
        Assert.Equal(expected, Expr.Evaluate(expression, null));
    }

    [Theory]
    [InlineData("5 > 3", true)]
    [InlineData("3 > 5", false)]
    [InlineData("5 >= 5", true)]
    [InlineData("6 >= 5", true)]
    [InlineData("4 >= 5", false)]
    [InlineData("3 < 5", true)]
    [InlineData("5 <= 5", true)]
    [InlineData("5 == 5", true)]
    [InlineData("5 == 6", false)]
    [InlineData("5 != 3", true)]
    [InlineData("5 != 5", false)]
    public void ComparisonOperatorsEvaluate(string expression, bool expected)
    {
        Assert.Equal(expected, Expr.Evaluate(expression, null));
    }

    [Theory]
    [InlineData("true && true", true)]
    [InlineData("true && false", false)]
    [InlineData("true || false", true)]
    [InlineData("false || false", false)]
    [InlineData("!true", false)]
    [InlineData("!false", true)]
    public void LogicalOperatorsEvaluate(string expression, bool expected)
    {
        Assert.Equal(expected, Expr.Evaluate(expression, null));
    }

    [Fact]
    public void MultiplicationBindsTighterThanAddition()
    {
        Assert.Equal(14.0, Expr.Evaluate("2 + 3 * 4", null));
    }

    [Fact]
    public void ParenthesesOverridePrecedence()
    {
        Assert.Equal(20.0, Expr.Evaluate("(2 + 3) * 4", null));
    }

    [Fact]
    public void ComplexArithmeticExpressionMatchesTheUserSuppliedExample()
    {
        // Identifiers resolve as ordinary C# property names (PascalCase, matching how a
        // plain {Binding A} would resolve too - reflection lookup is case-sensitive), not
        // the lowercase "a, b, c" shorthand the request was phrased with.
        var vm = new ExprTestViewModel { A = 1, B = 2, C = 3 };

        // a + b == c + d - (2*e - f), with d/e/f folded into literals here since the shape
        // (nested parens, mixed precedence) is what matters.
        var result = Expr.Evaluate("A + B == C + 4 - (2 * 5 - 6)", vm);

        // 1 + 2 == 3 + 4 - (10 - 6)  =>  3 == 7 - 4  =>  3 == 3
        Assert.Equal(true, result);
    }

    [Fact]
    public void UnaryOperatorsCanNest()
    {
        Assert.Equal(true, Expr.Evaluate("!(5 > 10)", null));
        Assert.Equal(5.0, Expr.Evaluate("- -5", null));
    }

    [Fact]
    public void StringLiteralSupportsEscapedQuoteAndBackslash()
    {
        var result = Expr.Evaluate("""
            "She said \"hi\" then left \\"
            """, null);

        Assert.Equal("She said \"hi\" then left \\", result);
    }

    [Fact]
    public void ShortCircuitOrSkipsTheRightOperandWhenLeftIsTrue()
    {
        // The right side would throw if evaluated (a string isn't comparable to a number) -
        // this only returns true without throwing if "||" genuinely short-circuits.
        var result = Expr.Evaluate("""true || ("x" > 5)""", null);

        Assert.Equal(true, result);
    }

    [Fact]
    public void OrEvaluatesTheRightOperandWhenLeftIsFalse()
    {
        Assert.Throws<InvalidOperationException>(() => Expr.Evaluate("""false || ("x" > 5)""", null));
    }

    [Fact]
    public void ShortCircuitAndSkipsTheRightOperandWhenLeftIsFalse()
    {
        var result = Expr.Evaluate("""false && ("x" > 5)""", null);

        Assert.Equal(false, result);
    }

    [Fact]
    public void AndEvaluatesTheRightOperandWhenLeftIsTrue()
    {
        Assert.Throws<InvalidOperationException>(() => Expr.Evaluate("""true && ("x" > 5)""", null));
    }

    // Plain [Fact]s rather than [Theory]/[InlineData] here - Status is internal, and an
    // InlineData-fed parameter on a public (xUnit-discoverable) test method can't be an
    // internal type (CS0051).
    [Fact]
    public void EnumEqualsStringComparesByMemberNameWhenActive()
    {
        var vm = new ExprTestViewModel { Status = Status.Active };
        Assert.Equal(true, Expr.Evaluate("""Status == "Active" """, vm));
    }

    [Fact]
    public void EnumEqualsStringComparesByMemberNameWhenNotMatching()
    {
        var vm = new ExprTestViewModel { Status = Status.Active };
        Assert.Equal(false, Expr.Evaluate("""Status == "Inactive" """, vm));
    }

    [Fact]
    public void EnumEqualsStringComparesByMemberNameForAnotherMember()
    {
        var vm = new ExprTestViewModel { Status = Status.Pending };
        Assert.Equal(true, Expr.Evaluate("""Status == "Pending" """, vm));
    }

    [Fact]
    public void EnumEqualsStringWorksInBothOperandOrders()
    {
        var vm = new ExprTestViewModel { Status = Status.Active };

        Assert.Equal(true, Expr.Evaluate("""Status == "Active" """, vm));
        Assert.Equal(true, Expr.Evaluate("""  "Active" == Status""", vm));
    }

    [Fact]
    public void SameTypeEnumsCompareByValue()
    {
        var vm = new ExprTestViewModel { Status = Status.Active, OtherStatus = Status.Active };
        Assert.Equal(true, Expr.Evaluate("Status == OtherStatus", vm));

        vm.OtherStatus = Status.Pending;
        Assert.Equal(false, Expr.Evaluate("Status == OtherStatus", vm));
    }

    [Fact]
    public void DifferentEnumTypesCompareAsUnequalRatherThanThrowing()
    {
        var vm = new ExprTestViewModel { Status = Status.Active, Priority = Priority.High };

        // Status.Active and Priority.High even share the same underlying int (1) - this
        // must still compare false (type+value-aware via Equals), not slip through the
        // numeric-coercion path and compare equal just because the raw numbers match.
        var result = Expr.Evaluate("Status == Priority", vm);

        Assert.Equal(false, result);
    }

    [Fact]
    public void NullOperandInArithmeticThrowsRatherThanSilentlyBecomingZero()
    {
        var vm = new ExprTestViewModel(); // Order is null

        Assert.Throws<InvalidOperationException>(() => Expr.Evaluate("Order.Total + 1", vm));
    }

    [Fact]
    public void NullValuedIdentifierIsNotAccidentallyEqualToZero()
    {
        var vm = new ExprTestViewModel(); // Order is null

        // Convert.ToDouble(null) surprisingly returns 0.0 - AreEqual must guard against
        // that so a null property doesn't silently compare equal to the number 0, and this
        // must not throw either (unlike the arithmetic case above).
        Assert.Equal(false, Expr.Evaluate("Order == 0", vm));
    }

    [Fact]
    public void DivisionByZeroYieldsInfinityWithoutThrowing()
    {
        Assert.Equal(double.PositiveInfinity, Expr.Evaluate("1 / 0", null));
        Assert.True(double.IsNaN((double)Expr.Evaluate("0 / 0", null)!));
    }

    [Fact]
    public void NestedDottedPathResolvesThroughAnObjectGraph()
    {
        var vm = new ExprTestViewModel { Order = new ExprTestOrder { Total = 42.5 } };

        Assert.Equal(true, Expr.Evaluate("Order.Total > 40", vm));
    }

    [Fact]
    public void NonexistentPropertyNameThrows()
    {
        var vm = new ExprTestViewModel();

        Assert.Throws<InvalidOperationException>(() => Expr.Evaluate("DoesNotExist > 0", vm));
    }

    [Fact]
    public void IdentifierWithoutASourceObjectThrows()
    {
        Assert.Throws<InvalidOperationException>(() => Expr.Evaluate("A > 0", null));
    }

    [Fact]
    public void ExpressionWithNoIdentifiersNeedsNoSource()
    {
        Assert.Equal(true, Expr.Evaluate("2 + 2 == 4", null));
    }

    [Fact]
    public void RepeatedIdentifierResolvesConsistently()
    {
        var vm = new ExprTestViewModel { A = 6 };

        Assert.Equal(true, Expr.Evaluate("A + A > 10", vm));
    }

    [Fact]
    public void EmptyExpressionThrows()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Expr.Evaluate("", null));
        Assert.Contains("empty", ex.Message);
    }

    [Fact]
    public void UnknownCharacterThrowsWithSourceInMessage()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Expr.Evaluate("A = B", null));
        Assert.Contains("A = B", ex.Message);
    }

    [Fact]
    public void UnbalancedParenthesesThrowsWithSourceInMessage()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Expr.Evaluate("(A + B", null));
        Assert.Contains("(A + B", ex.Message);
    }

    [Fact]
    public void TrailingGarbageAfterAValidExpressionThrows()
    {
        // Parsing fails on the trailing token before identifier resolution ever runs, so no
        // source object is needed here.
        var ex = Assert.Throws<InvalidOperationException>(() => Expr.Evaluate("A > B C", null));
        Assert.Contains("A > B C", ex.Message);
    }

    [Theory]
    [InlineData("true ? 1 : 2", 1.0)]
    [InlineData("false ? 1 : 2", 2.0)]
    [InlineData("5 > 3 ? 1 : 2", 1.0)]
    public void TernaryPicksTheCorrectBranch(string expression, double expected)
    {
        Assert.Equal(expected, Expr.Evaluate(expression, null));
    }

    [Fact]
    public void TernaryIsRightAssociativeAndChains()
    {
        // Mirrors an if/else-if/else chain: each condition is tried in order, and a name
        // matching none of them falls through to the final "4".
        const string expression = """Name == "a" ? 1 : Name == "b" ? 2 : Name == "c" ? 3 : 4""";

        Assert.Equal(2.0, Expr.Evaluate(expression, new ExprTestViewModel { Name = "b" }));
        Assert.Equal(3.0, Expr.Evaluate(expression, new ExprTestViewModel { Name = "c" }));
        Assert.Equal(4.0, Expr.Evaluate(expression, new ExprTestViewModel { Name = "z" }));
    }

    [Fact]
    public void TernaryHasLowerPrecedenceThanComparisonAndArithmetic()
    {
        // Parses as (2 + 3 > 4) ? (10) : (20), not some other grouping.
        Assert.Equal(10.0, Expr.Evaluate("2 + 3 > 4 ? 10 : 20", null));
    }

    [Fact]
    public void TernaryOnlyEvaluatesTheTakenBranch()
    {
        // The untaken branch would throw if evaluated (a string isn't comparable to a
        // number) - this only succeeds if the ternary genuinely skips it.
        Assert.Equal(1.0, Expr.Evaluate("""true ? 1 : ("x" > 5 ? 2 : 3)""", null));
        Assert.Equal(1.0, Expr.Evaluate("""false ? ("x" > 5 ? 2 : 3) : 1""", null));
    }

    [Fact]
    public void TernaryConditionMustBeBoolean()
    {
        Assert.Throws<InvalidOperationException>(() => Expr.Evaluate("1 ? 2 : 3", null));
    }

    [Fact]
    public void ElementReferencedIdentifierThrowsFromEvaluateSinceThereIsNoVisualTree()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Expr.Evaluate("#SomeElement.Value > 0", null));
        Assert.Contains("#SomeElement.Value", ex.Message);
    }
}
