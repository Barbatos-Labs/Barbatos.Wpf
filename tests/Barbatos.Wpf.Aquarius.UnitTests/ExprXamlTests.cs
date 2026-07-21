using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Barbatos.Wpf.Aquarius.UnitTests;

// The only file in this suite using XamlReader.Parse on a real XAML string, rather than
// building elements directly in C#: Expr's reactive path (MultiBinding wiring) needs a real
// IServiceProvider/IProvideValueTarget to exercise at all, and real XAML loading already
// provides that correctly - simpler and more representative than hand-constructing a fake
// service provider. This also doubles as the end-to-end proof for Expr.cs's documented
// XAML-quoting story (&quot; entity vs. an invalid backslash escape).
public class ExprXamlTests
{
    [Fact]
    public void DirectivesShowReactsToTheParsedExpression()
    {
        StaThread.Run(() =>
        {
            var xaml = """
                <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:aq="http://schemas.barbatos.dev/aquarius">
                    <Border x:Name="Target" aq:Directives.Show="{aq:Expr 'A > 5'}" />
                </Window>
                """;

            var window = (Window)XamlReader.Parse(xaml);
            var target = (Border)window.FindName("Target");
            var vm = new ExprTestViewModel { A = 3 };
            window.DataContext = vm;

            window.Show();
            StaThread.PumpDispatcher();

            Assert.Equal(Visibility.Collapsed, target.Visibility);

            vm.A = 10;
            StaThread.PumpDispatcher();

            Assert.Equal(Visibility.Visible, target.Visibility);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void QuotedStringLiteralWorksThroughTheXmlEntityEscapedForm()
    {
        StaThread.Run(() =>
        {
            var xaml = """
                <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:aq="http://schemas.barbatos.dev/aquarius">
                    <Border x:Name="Target" aq:Directives.Show="{aq:Expr 'Status == &quot;Active&quot;'}" />
                </Window>
                """;

            var window = (Window)XamlReader.Parse(xaml);
            var target = (Border)window.FindName("Target");
            var vm = new ExprTestViewModel { Status = Status.Inactive };
            window.DataContext = vm;

            window.Show();
            StaThread.PumpDispatcher();

            Assert.Equal(Visibility.Collapsed, target.Visibility);

            vm.Status = Status.Active;
            StaThread.PumpDispatcher();

            Assert.Equal(Visibility.Visible, target.Visibility);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void IfConditionReactsToTheParsedExpression()
    {
        StaThread.Run(() =>
        {
            var xaml = """
                <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:aq="http://schemas.barbatos.dev/aquarius">
                    <aq:If x:Name="Target" Condition="{aq:Expr 'A + B >= 10'}">
                        <TextBlock Text="shown" />
                    </aq:If>
                </Window>
                """;

            var window = (Window)XamlReader.Parse(xaml);
            var target = (If)window.FindName("Target");
            var vm = new ExprTestViewModel { A = 2, B = 3 };
            window.DataContext = vm;

            window.Show();
            StaThread.PumpDispatcher();

            Assert.Null(target.Content);

            vm.B = 8;
            StaThread.PumpDispatcher();

            Assert.NotNull(target.Content);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void DuplicateIdentifierReactsCorrectlyThroughTheReactivePath()
    {
        StaThread.Run(() =>
        {
            var xaml = """
                <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:aq="http://schemas.barbatos.dev/aquarius">
                    <Border x:Name="Target" aq:Directives.Show="{aq:Expr 'A + A > 10'}" />
                </Window>
                """;

            var window = (Window)XamlReader.Parse(xaml);
            var target = (Border)window.FindName("Target");
            var vm = new ExprTestViewModel { A = 3 };
            window.DataContext = vm;

            window.Show();
            StaThread.PumpDispatcher();

            Assert.Equal(Visibility.Collapsed, target.Visibility); // 3+3=6, not >10

            vm.A = 6;
            StaThread.PumpDispatcher();

            Assert.Equal(Visibility.Visible, target.Visibility); // 6+6=12, >10

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void ElementReferencedIdentifierBindsToTheNamedElementInsteadOfDataContext()
    {
        StaThread.Run(() =>
        {
            var xaml = """
                <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:aq="http://schemas.barbatos.dev/aquarius">
                    <StackPanel>
                        <Slider x:Name="MySlider" Minimum="0" Maximum="100" />
                        <Border x:Name="Target" aq:Directives.Show="{aq:Expr '#MySlider.Value > 50'}" />
                    </StackPanel>
                </Window>
                """;

            var window = (Window)XamlReader.Parse(xaml);
            var slider = (Slider)window.FindName("MySlider");
            var target = (Border)window.FindName("Target");

            // Deliberately no DataContext at all - #MySlider.Value must resolve via
            // Binding.ElementName, not the (nonexistent) DataContext.
            window.Show();
            StaThread.PumpDispatcher();

            Assert.Equal(Visibility.Collapsed, target.Visibility);

            slider.Value = 75;
            StaThread.PumpDispatcher();

            Assert.Equal(Visibility.Visible, target.Visibility);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void UnresolvedIdentifierFallsBackTheSameWayAPlainBindingWould()
    {
        StaThread.Run(() =>
        {
            var xamlWithExpr = """
                <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:aq="http://schemas.barbatos.dev/aquarius">
                    <Border x:Name="Target" aq:Directives.Show="{aq:Expr 'DoesNotExist > 0'}" />
                </Window>
                """;
            var xamlWithPlainBinding = """
                <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:aq="http://schemas.barbatos.dev/aquarius">
                    <Border x:Name="Target" aq:Directives.Show="{Binding DoesNotExist}" />
                </Window>
                """;

            var vm = new ExprTestViewModel();

            var exprWindow = (Window)XamlReader.Parse(xamlWithExpr);
            var exprTarget = (Border)exprWindow.FindName("Target");
            exprWindow.DataContext = vm;
            exprWindow.Show();
            StaThread.PumpDispatcher();

            var bindingWindow = (Window)XamlReader.Parse(xamlWithPlainBinding);
            var bindingTarget = (Border)bindingWindow.FindName("Target");
            bindingWindow.DataContext = vm;
            bindingWindow.Show();
            StaThread.PumpDispatcher();

            Assert.Equal(bindingTarget.Visibility, exprTarget.Visibility);

            exprWindow.Close();
            bindingWindow.Close();
            StaThread.PumpDispatcher();
        });
    }
}
