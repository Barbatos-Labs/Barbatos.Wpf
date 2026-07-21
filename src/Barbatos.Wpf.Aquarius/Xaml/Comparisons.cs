// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Windows.Data;

namespace Barbatos.Wpf.Xaml;

/// <summary>
/// Ready-made converters for the handful of trivial inline conditions that come up
/// constantly with <see cref="If"/>/<see cref="Directives.ShowProperty"/>/
/// <see cref="Suspense"/>, so they don't each need their own hand-written
/// <see cref="IValueConverter"/> class.
/// </summary>
/// <remarks>
/// <code>
/// &lt;aq:If Condition="{Binding Items.Count, Converter={x:Static aq:Comparisons.IsNull}}"&gt;
/// </code>
/// Vue templates can write a boolean expression inline (<c>v-if="count &gt; 0"</c>); WPF
/// bindings are plain property paths with no expression language. Vue's own guidance is to
/// move anything beyond a trivial check into a computed property anyway - so requiring a
/// bound property/converter here isn't a step down from Vue's own best practice, it's the
/// same one. These three exist purely to remove the ceremony of writing a whole
/// <see cref="IValueConverter"/> class for the simplest, most common single-value cases;
/// for a compound condition over several values (<c>a + b &gt;= c</c>,
/// <c>a &gt;= 1 &amp;&amp; a &lt;= 2</c>), <see cref="Expr"/> covers the middle ground
/// between these and a full <see cref="Reactivity.Computed{T}"/> - which remains the right
/// answer once things get more involved than that, same as in Vue.
/// </remarks>
public static class Comparisons
{
    /// <summary>Negates a <see cref="bool"/> - mirrors <c>!value</c>.</summary>
    public static readonly IValueConverter Not = new NotConverter();

    /// <summary>Whether the value is <see langword="null"/> - mirrors <c>value === null</c>.</summary>
    public static readonly IValueConverter IsNull = new IsNullConverter();

    /// <summary>
    /// Whether the value equals <c>ConverterParameter</c> - mirrors <c>value === other</c>.
    /// </summary>
    public static readonly IValueConverter IsEqualTo = new IsEqualToConverter();

    private sealed class NotConverter : IValueConverter
    {
        // Negation is its own inverse, so this supports two-way binding (e.g.
        // Directives.Show="{Binding IsCollapsed, Converter={x:Static aq:Comparisons.Not}}")
        // for free.
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is bool b ? !b : Binding.DoNothing;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is bool b ? !b : Binding.DoNothing;
    }

    private sealed class IsNullConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is null;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException($"{nameof(Comparisons)}.{nameof(IsNull)} does not support two-way binding.");
    }

    private sealed class IsEqualToConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => Equals(value, parameter);

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException($"{nameof(Comparisons)}.{nameof(IsEqualTo)} does not support two-way binding.");
    }
}
