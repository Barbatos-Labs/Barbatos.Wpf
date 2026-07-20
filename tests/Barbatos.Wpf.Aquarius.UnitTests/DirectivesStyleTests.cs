using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.Aquarius.UnitTests;

public class DirectivesStyleTests
{
    [Fact]
    public void AppliesASinglePropertyByName()
    {
        StaThread.Run(() =>
        {
            var block = new TextBlock();

            Directives.SetStyle(block, new Dictionary<string, object> { ["FontSize"] = 24.0 });

            Assert.Equal(24.0, block.FontSize);
        });
    }

    [Fact]
    public void AppliesMultipleProperties()
    {
        StaThread.Run(() =>
        {
            var block = new TextBlock();

            Directives.SetStyle(block, new Dictionary<string, object>
            {
                ["FontSize"] = 20.0,
                ["Opacity"] = 0.5,
            });

            Assert.Equal(20.0, block.FontSize);
            Assert.Equal(0.5, block.Opacity);
        });
    }

    [Fact]
    public void UpdatingTheDictionaryRevertsPropertiesNoLongerPresent()
    {
        StaThread.Run(() =>
        {
            var block = new TextBlock();
            Directives.SetStyle(block, new Dictionary<string, object>
            {
                ["FontSize"] = 20.0,
                ["Opacity"] = 0.5,
            });

            Directives.SetStyle(block, new Dictionary<string, object> { ["FontSize"] = 12.0 });

            Assert.Equal(12.0, block.FontSize);
            Assert.Equal(DependencyProperty.UnsetValue, block.ReadLocalValue(TextBlock.OpacityProperty));
        });
    }

    [Fact]
    public void SettingToNullClearsAllPreviouslyAppliedProperties()
    {
        StaThread.Run(() =>
        {
            var block = new TextBlock();
            Directives.SetStyle(block, new Dictionary<string, object> { ["FontSize"] = 20.0 });

            Directives.SetStyle(block, null);

            Assert.Equal(DependencyProperty.UnsetValue, block.ReadLocalValue(TextBlock.FontSizeProperty));
        });
    }

    [Fact]
    public void UnknownPropertyNameThrows()
    {
        StaThread.Run(() =>
        {
            var block = new TextBlock();

            Assert.Throws<InvalidOperationException>(() =>
                Directives.SetStyle(block, new Dictionary<string, object> { ["NotARealProperty"] = 1 }));
        });
    }
}
