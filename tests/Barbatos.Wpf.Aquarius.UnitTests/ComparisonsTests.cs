using System.Globalization;
using System.Windows.Data;

namespace Barbatos.Wpf.Aquarius.UnitTests;

public class ComparisonsTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void NotNegatesBooleans(bool input, bool expected)
    {
        var result = Comparisons.Not.Convert(input, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void NotConvertBackAlsoNegatesForTwoWayBinding()
    {
        var result = Comparisons.Not.ConvertBack(true, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(false, result);
    }

    [Fact]
    public void NotPassesThroughNonBooleanValues()
    {
        var result = Comparisons.Not.Convert("not a bool", typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(Binding.DoNothing, result);
    }

    [Fact]
    public void IsNullIsTrueForNull()
    {
        var result = Comparisons.IsNull.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(true, result);
    }

    [Fact]
    public void IsNullIsFalseForANonNullValue()
    {
        var result = Comparisons.IsNull.Convert("value", typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(false, result);
    }

    [Fact]
    public void IsNullConvertBackIsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() =>
            Comparisons.IsNull.ConvertBack(true, typeof(object), null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void IsEqualToComparesAgainstTheConverterParameter()
    {
        Assert.Equal(true, Comparisons.IsEqualTo.Convert(3, typeof(bool), 3, CultureInfo.InvariantCulture));
        Assert.Equal(false, Comparisons.IsEqualTo.Convert(3, typeof(bool), 4, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void IsEqualToTreatsNullParameterAsAValidComparand()
    {
        var result = Comparisons.IsEqualTo.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(true, result);
    }

    [Fact]
    public void IsEqualToConvertBackIsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() =>
            Comparisons.IsEqualTo.ConvertBack(true, typeof(object), 3, CultureInfo.InvariantCulture));
    }
}
