// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Barbatos.Wpf.Xaml;

/// <summary>
/// Evaluates a small comparison/arithmetic/logical expression over bound properties - the
/// closest Aquarius counterpart to writing a boolean expression directly inline in a Vue
/// template (<c>v-if="a + b >= c"</c>), which a plain WPF <see cref="Binding"/> otherwise
/// has no way to do (a binding path is just a property path, not an expression language).
/// </summary>
/// <remarks>
/// <code>
/// &lt;aq:If Condition="{aq:Expr 'count &gt; 0'}"&gt;
/// &lt;Border aq:Directives.Show="{aq:Expr 'status == &amp;quot;Active&amp;quot;'}"&gt;
/// &lt;Border aq:Directives.Show="{aq:Expr 'selectedOrder != null'}"&gt;
/// </code>
///
/// Supports, over identifiers resolved as ordinary property paths against the ambient
/// <c>DataContext</c> (dotted paths like <c>order.total</c> work, same as a plain
/// <c>{Binding}</c>):
/// <list type="bullet">
/// <item>Comparison: <c>&gt; &gt;= &lt; &lt;= == !=</c> - <c>&gt;</c>/<c>&gt;=</c>/<c>&lt;</c>/<c>&lt;=</c>
/// require two numbers (or two same-type <see cref="IComparable"/> values); <c>==</c>/<c>!=</c>
/// work over any object type, not just primitives - two operands compare via
/// <see cref="object.Equals(object, object)"/> (reference equality unless the type overrides
/// it) once numeric coercion and the enum cases below don't apply, so <c>someObject ==
/// otherRef</c> and null-checks like <c>selectedOrder != null</c> both just work</item>
/// <item>Logical, short-circuiting: <c>&amp;&amp; || !</c></item>
/// <item>Arithmetic: <c>+ - * /</c> and parentheses, all evaluated as <see cref="double"/></item>
/// <item>Ternary, right-associative: <c>condition ? whenTrue : whenFalse</c> (only the
/// taken branch is evaluated)</item>
/// <item>Literals: numbers (<c>1</c>, <c>2.5</c>), strings (<c>"Hello World"</c>), and
/// lowercase <c>true</c>/<c>false</c>/<c>null</c></item>
/// </list>
///
/// <b>Element-referenced identifiers</b>: prefix an identifier with <c>#</c> to resolve it
/// against a named element instead of <c>DataContext</c> - <c>#MySlider.Value &gt; 50</c>
/// binds via <see cref="Binding.ElementName"/> the same way <c>{Binding ElementName=MySlider, Path=Value}</c>
/// would. A bare <c>#MySlider</c> (no dot) binds to the element itself (an empty
/// <see cref="Binding.Path"/>). Only <c>ElementName</c> is supported this way -
/// <c>RelativeSource</c> is not: an <c>AncestorType</c> reference needs to resolve a type
/// name via XAML's own type-resolution service, which would need a materially bigger
/// identifier grammar for a need this rarely comes up for; a plain <c>{Binding RelativeSource=...}</c>
/// on the element itself, combined with <c>Directives.Class</c>/<c>Directives.Style</c> or a
/// code-behind handler, covers it today. <see cref="Evaluate"/> cannot resolve <c>#</c>
/// identifiers at all (there is no visual tree to search outside a real XAML load) and
/// throws clearly if one appears.
///
/// <b>Enum comparison</b> works through the string form rather than a bare
/// <c>EnumType.Member</c> literal: <c>status == "Active"</c> compares an enum-typed
/// <c>status</c> against the member name via <see cref="object.ToString"/>. A bare enum
/// literal would need to distinguish a type name from an ordinary dotted property path at
/// parse time, for no real benefit over the string form - deliberately not supported.
///
/// <b>Deliberately out of scope</b>: string concatenation via <c>+</c> (both consumers this
/// was built for - <see cref="If.Condition"/>/<see cref="Directives.ShowProperty"/> - are
/// booleans; use <c>StringFormat</c> or multiple <c>Run</c>s to build display text instead),
/// method calls, and indexers. Relational operators (<c>&gt; &gt;= &lt; &lt;=</c>) still only
/// work for numbers or two same-concrete-type <see cref="IComparable"/> values - there is no
/// general ordering for arbitrary objects. For anything beyond this grammar, a
/// <see cref="Reactivity.Computed{T}"/> in the ViewModel remains the right answer, same as
/// Vue's own guidance to move non-trivial template expressions into a computed property.
///
/// <b>Typos are the main risk of writing a real grammar inside a XAML string</b>: neither
/// Visual Studio nor any other XAML editor can syntax-highlight, IntelliSense, or
/// rename-refactor an identifier that only this parser understands, so a renamed property
/// silently stops matching with no compiler error. By default an unresolved identifier
/// fails exactly the way a plain <c>{Binding TypoPath}</c> already does (evaluates as
/// <see cref="DependencyProperty.UnsetValue"/>, "fails open") - set
/// <see cref="ThrowOnUnresolvedIdentifiers"/> during app startup to turn that into an
/// immediate, specific exception instead. <see cref="Evaluate"/>'s reflection-based path
/// always throws regardless (see <see cref="ResolvePath"/>) since it has no binding-failure
/// convention to mirror.
///
/// <b>XAML quoting</b>: since the whole markup extension already sits inside a
/// double-quoted XML attribute, a string literal inside the expression needs its quotes
/// written as <c>&amp;quot;</c> (an XML entity, decoded before the expression text ever
/// reaches this parser) rather than an escaped <c>\"</c> - XML attribute values have no
/// backslash-escaping mechanism at all, so a literal <c>\"</c> would not protect the
/// attribute boundary and would produce invalid XML. This parser's own string-literal
/// grammar still supports <c>\"</c>/<c>\\</c> for a literal quote/backslash *inside* the
/// compared value itself (relevant when calling <see cref="Evaluate"/> directly from C#,
/// where no XML layer is involved) - a different, unrelated concern from the XML-attribute
/// quoting one above.
/// </remarks>
[MarkupExtensionReturnType(typeof(object))]
public class Expr : MarkupExtension
{
    /// <summary>Creates an instance with <see cref="Expression"/> unset - set it via the <c>Expression=</c> property syntax.</summary>
    public Expr()
    {
    }

    /// <summary>Creates an instance for the positional form, <c>{aq:Expr 'a &gt; b'}</c>.</summary>
    public Expr(string expression) => Expression = expression;

    /// <summary>The expression text to parse and evaluate.</summary>
    [ConstructorArgument("expression")]
    public string? Expression { get; set; }

    /// <summary>
    /// When <see langword="true"/>, an identifier that fails to resolve to a value (almost
    /// always a typo'd property name - see <see cref="ResolvePath"/>'s equivalent, always-on
    /// check for the non-reactive <see cref="Evaluate"/> path) throws immediately instead of
    /// silently evaluating as <see cref="DependencyProperty.UnsetValue"/>, the same fallback a
    /// plain <c>{Binding TypoPath}</c> already produces today. Defaults to <see langword="false"/>
    /// - matches every existing binding's behavior unless explicitly opted in, since a
    /// <see cref="MultiBinding"/> child can also legitimately produce
    /// <see cref="DependencyProperty.UnsetValue"/> for reasons that are not typos (e.g. a
    /// <c>DataContext</c> that is temporarily <see langword="null"/> during setup/teardown).
    /// Deliberately a plain static switch rather than wired to
    /// <see cref="BuildConfiguration.IsDebug"/> automatically: set it once, explicitly, during
    /// app startup - e.g. <c>#if DEBUG Expr.ThrowOnUnresolvedIdentifiers = true; #endif</c> - so
    /// the choice of when this fires stays with the consuming app, not this library.
    /// </summary>
    public static bool ThrowOnUnresolvedIdentifiers { get; set; }

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrWhiteSpace(Expression))
            throw new InvalidOperationException("Expr: expression is empty.");

        var ast = ExprParser.Parse(Expression);
        var identifiers = ExprEvaluator.BindIdentifiers(ast);

        // A fresh MultiBinding every call, never cached on this instance - WPF re-invokes
        // ProvideValue once per template instantiation (e.g. inside a DataTemplate), and a
        // MultiBindingExpression can only ever be attached to one target at a time.
        var multiBinding = new MultiBinding { Converter = new ExprConverter(ast, identifiers, Expression) };

        foreach (var path in identifiers)
            multiBinding.Bindings.Add(CreateBinding(path));

        return multiBinding.ProvideValue(serviceProvider);
    }

    private static Binding CreateBinding(string identifierPath)
    {
        if (!identifierPath.StartsWith('#'))
            return new Binding(identifierPath);

        var (elementName, propertyPath) = SplitElementReference(identifierPath);
        return new Binding(propertyPath) { ElementName = elementName };
    }

    private static (string ElementName, string PropertyPath) SplitElementReference(string identifierPath)
    {
        var withoutHash = identifierPath[1..];
        var dotIndex = withoutHash.IndexOf('.');

        // No dot - bind to the named element itself (an empty Path), not one of its properties.
        return dotIndex < 0
            ? (withoutHash, string.Empty)
            : (withoutHash[..dotIndex], withoutHash[(dotIndex + 1)..]);
    }

    /// <summary>
    /// Evaluates <paramref name="expression"/> once, synchronously, against
    /// <paramref name="source"/> via plain reflection - the non-reactive, C#-callable
    /// counterpart to <see cref="ProvideValue"/>, mirroring how <see cref="Composition.Inject.Get{T}"/>
    /// relates to <c>{aq:Inject}</c>. Each identifier's dotted path is walked one property
    /// at a time via <see cref="Type.GetProperty(string)"/>; a segment that doesn't exist
    /// on the current type throws (a genuine mistake), while a segment that resolves to
    /// <see langword="null"/> partway through just makes that identifier's value
    /// <see langword="null"/> (ordinary null propagation, not an error).
    /// </summary>
    public static object? Evaluate(string expression, object? source)
    {
        var ast = ExprParser.Parse(expression);
        var identifiers = ExprEvaluator.BindIdentifiers(ast);

        var values = new object?[identifiers.Count];
        for (var i = 0; i < identifiers.Count; i++)
            values[i] = ResolvePath(identifiers[i], source, expression);

        return ExprEvaluator.Evaluate(ast, values);
    }

    private static object? ResolvePath(string path, object? source, string expression)
    {
        if (path.StartsWith('#'))
            throw new InvalidOperationException($"Expr: '{path}' is an element-referenced identifier - {nameof(Evaluate)} has no visual tree to resolve it against (only the reactive {nameof(ProvideValue)} path does), from \"{expression}\".");

        if (source is null)
            throw new InvalidOperationException($"Expr: \"{expression}\" references '{path}', but no source object was provided.");

        object? current = source;

        foreach (var segment in path.Split('.'))
        {
            if (current is null)
                return null;

            var property = current.GetType().GetProperty(segment)
                ?? throw new InvalidOperationException($"Expr: '{current.GetType().Name}' has no property named '{segment}' (from '{path}' in \"{expression}\").");

            current = property.GetValue(current);
        }

        return current;
    }

    private sealed class ExprConverter(ExprNode ast, IReadOnlyList<string> identifiers, string expression) : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            // An unresolved identifier (e.g. a typo'd path) lands here as UnsetValue for
            // that slot - pass it straight through rather than letting a coercion attempt
            // throw a confusing InvalidCastException. This lands on exactly the same
            // fallback a plain {Binding TypoPath} already produces today on If.Condition/
            // Directives.Show (both default true) - "fail open", not a special case - unless
            // ThrowOnUnresolvedIdentifiers has been explicitly opted into.
            var unresolvedIndex = Array.IndexOf(values, DependencyProperty.UnsetValue);
            if (unresolvedIndex >= 0)
            {
                if (ThrowOnUnresolvedIdentifiers)
                    throw new InvalidOperationException($"Expr: '{identifiers[unresolvedIndex]}' did not resolve to a value (check for a typo) in \"{expression}\".");

                return DependencyProperty.UnsetValue;
            }

            return ExprEvaluator.Evaluate(ast, values) ?? DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException($"{nameof(Expr)} does not support two-way binding.");
    }
}
