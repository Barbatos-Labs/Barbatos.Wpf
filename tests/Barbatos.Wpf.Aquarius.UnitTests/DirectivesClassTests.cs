using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Barbatos.Wpf.Aquarius.UnitTests;

public class DirectivesClassTests
{
    private static Border CreateBorderWithStyles()
    {
        var border = new Border();

        var active = new Style(typeof(Border));
        active.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.Red));
        border.Resources["active"] = active;

        var bold = new Style(typeof(Border));
        bold.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(3)));
        border.Resources["bold"] = bold;

        return border;
    }

    [Fact]
    public void ApplyingASingleClassSetsItsProperties()
    {
        StaThread.Run(() =>
        {
            var border = CreateBorderWithStyles();

            Directives.SetClass(border, "active");

            Assert.Equal(Brushes.Red, border.Background);
        });
    }

    [Fact]
    public void ApplyingMultipleClassesMergesAllSetters()
    {
        StaThread.Run(() =>
        {
            var border = CreateBorderWithStyles();

            Directives.SetClass(border, "active bold");

            Assert.Equal(Brushes.Red, border.Background);
            Assert.Equal(new Thickness(3), border.BorderThickness);
        });
    }

    [Fact]
    public void LaterTokenWinsOnAConflictingProperty()
    {
        StaThread.Run(() =>
        {
            var border = CreateBorderWithStyles();
            var muted = new Style(typeof(Border));
            muted.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.Gray));
            border.Resources["muted"] = muted;

            Directives.SetClass(border, "active muted");

            Assert.Equal(Brushes.Gray, border.Background);
        });
    }

    [Fact]
    public void BasedOnSettersAreIncludedAtLowerPriority()
    {
        StaThread.Run(() =>
        {
            var border = new Border();
            var baseStyle = new Style(typeof(Border));
            baseStyle.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.Red));
            baseStyle.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(1)));
            var derived = new Style(typeof(Border), baseStyle);
            derived.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(5)));
            border.Resources["derived"] = derived;

            Directives.SetClass(border, "derived");

            Assert.Equal(Brushes.Red, border.Background);
            Assert.Equal(new Thickness(5), border.BorderThickness);
        });
    }

    [Fact]
    public void RemovingATokenOnUpdateRevertsItsProperties()
    {
        StaThread.Run(() =>
        {
            var border = CreateBorderWithStyles();
            Directives.SetClass(border, "active bold");

            Directives.SetClass(border, "bold");

            Assert.Equal(new Thickness(3), border.BorderThickness);
            Assert.Equal(DependencyProperty.UnsetValue, border.ReadLocalValue(Border.BackgroundProperty));
        });
    }

    [Fact]
    public void EmptyStringClearsAllPreviouslyAppliedClasses()
    {
        StaThread.Run(() =>
        {
            var border = CreateBorderWithStyles();
            Directives.SetClass(border, "active");

            Directives.SetClass(border, "");

            Assert.Equal(DependencyProperty.UnsetValue, border.ReadLocalValue(Border.BackgroundProperty));
        });
    }

    [Fact]
    public void UnknownClassNameThrows()
    {
        StaThread.Run(() =>
        {
            var border = new Border();

            Assert.Throws<InvalidOperationException>(() => Directives.SetClass(border, "does-not-exist"));
        });
    }
}
