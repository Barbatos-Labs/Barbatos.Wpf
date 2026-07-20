using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Barbatos.Wpf.Aquarius.UnitTests;

public class ProvideInjectTests
{
    [Fact]
    public void FindsTheNearestProvidedValueSkippingIntermediatesThatDidNotProvideIt()
    {
        StaThread.Run(() =>
        {
            var outer = new Grid();
            Provide.SetKey(outer, "Theme");
            Provide.SetValue(outer, "outer-value");

            var middle = new Border(); // provides nothing
            var inner = new TextBlock();
            outer.Children.Add(middle);
            middle.Child = inner;

            Assert.Equal("outer-value", Inject.Get<string>(inner, "Theme"));
        });
    }

    [Fact]
    public void ANearerProvideOverridesAFartherOneForTheSameKey()
    {
        StaThread.Run(() =>
        {
            var outer = new Grid();
            Provide.SetKey(outer, "Theme");
            Provide.SetValue(outer, "outer-value");

            var inner = new Border();
            Provide.SetKey(inner, "Theme");
            Provide.SetValue(inner, "inner-value");
            outer.Children.Add(inner);

            var textBlock = new TextBlock();
            inner.Child = textBlock;

            Assert.Equal("inner-value", Inject.Get<string>(textBlock, "Theme"));
        });
    }

    [Fact]
    public void GetReturnsTheFallbackWhenNothingProvidesTheKey()
    {
        StaThread.Run(() =>
        {
            var element = new TextBlock();

            Assert.Equal("fallback-value", Inject.Get(element, "NoSuchKey", "fallback-value"));
        });
    }

    [Fact]
    public void ChangingKeyMovesTheRegistrationToTheNewKey()
    {
        StaThread.Run(() =>
        {
            var grid = new Grid();
            Provide.SetKey(grid, "Old");
            Provide.SetValue(grid, "value");

            Provide.SetKey(grid, "New");

            Assert.False(grid.Resources.Contains("Old"));
            Assert.Equal("value", grid.Resources["New"]);
        });
    }

    [Fact]
    public void ChangingValueAloneOverwritesInPlaceWithoutMovingTheKey()
    {
        StaThread.Run(() =>
        {
            var grid = new Grid();
            Provide.SetKey(grid, "Theme");
            Provide.SetValue(grid, "first");

            Provide.SetValue(grid, "second");

            Assert.Equal("second", grid.Resources["Theme"]);
        });
    }

    [Fact]
    public void ProvideThrowsWhenSetOnANonFrameworkElement()
    {
        StaThread.Run(() =>
        {
            var notAFrameworkElement = new DependencyObject();

            Assert.Throws<InvalidOperationException>(() => Provide.SetKey(notAFrameworkElement, "Key"));
        });
    }

    [Fact]
    public void ProvidedValueIsLiveUpdatedTheSameWayInjectResolvesIt()
    {
        StaThread.Run(() =>
        {
            const string key = "TestColor";
            var grid = new Grid();
            Provide.SetKey(grid, key);
            Provide.SetValue(grid, Brushes.Red);

            var textBlock = new TextBlock();
            grid.Children.Add(textBlock);

            // The programmatic equivalent of `{aq:Inject TestColor}` (which Inject.ProvideValue
            // delegates to internally via DynamicResourceExtension).
            textBlock.SetResourceReference(TextBlock.ForegroundProperty, key);

            Assert.Equal(Brushes.Red, textBlock.Foreground);

            Provide.SetValue(grid, Brushes.Blue);

            Assert.Equal(Brushes.Blue, textBlock.Foreground);
        });
    }
}
