using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.Aquarius.UnitTests;

public class DirectivesShowTests
{
    [Fact]
    public void DefaultsToVisible()
    {
        StaThread.Run(() =>
        {
            var element = new Border();

            Assert.Equal(Visibility.Visible, element.Visibility);
        });
    }

    [Fact]
    public void FalseCollapsesTheElement()
    {
        StaThread.Run(() =>
        {
            var element = new Border();

            Directives.SetShow(element, false);

            Assert.Equal(Visibility.Collapsed, element.Visibility);
        });
    }

    [Fact]
    public void TrueRestoresVisible()
    {
        StaThread.Run(() =>
        {
            var element = new Border();
            Directives.SetShow(element, false);

            Directives.SetShow(element, true);

            Assert.Equal(Visibility.Visible, element.Visibility);
        });
    }
}
