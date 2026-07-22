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
                        xmlns:aq="http://schemas.barbatos.co/aquarius/2026/xaml">
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
                        xmlns:aq="http://schemas.barbatos.co/aquarius/2026/xaml">
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
                        xmlns:aq="http://schemas.barbatos.co/aquarius/2026/xaml">
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
                        xmlns:aq="http://schemas.barbatos.co/aquarius/2026/xaml">
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
                        xmlns:aq="http://schemas.barbatos.co/aquarius/2026/xaml">
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
    public void NullComparisonReactsThroughTheReactivePathForAnObjectTypedProperty()
    {
        StaThread.Run(() =>
        {
            var xaml = """
                <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:aq="http://schemas.barbatos.co/aquarius/2026/xaml">
                    <Border x:Name="Target" aq:Directives.Show="{aq:Expr 'Order != null'}" />
                </Window>
                """;

            var window = (Window)XamlReader.Parse(xaml);
            var target = (Border)window.FindName("Target");
            var vm = new ExprTestViewModel(); // Order starts null
            window.DataContext = vm;

            window.Show();
            StaThread.PumpDispatcher();

            Assert.Equal(Visibility.Collapsed, target.Visibility);

            vm.Order = new ExprTestOrder { Total = 5 };
            StaThread.PumpDispatcher();

            Assert.Equal(Visibility.Visible, target.Visibility);

            vm.Order = null;
            StaThread.PumpDispatcher();

            Assert.Equal(Visibility.Collapsed, target.Visibility);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void ThrowOnUnresolvedIdentifiersThrowsClearlyInsteadOfFailingOpen()
    {
        // The opt-in answer to "a typo'd property name inside an Expr string is invisible to
        // both the IDE and, by default, the app at runtime": flipping this switch turns that
        // same DoesNotExist typo from UnresolvedIdentifierFallsBackTheSameWayAPlainBindingWould's
        // silent "fail open" into an immediate, specific exception instead. Reset in `finally`
        // - this is a process-wide static switch, and other tests in this suite rely on the
        // default `false` (fail-open) behavior.
        Expr.ThrowOnUnresolvedIdentifiers = true;
        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() => StaThread.Run(() =>
            {
                var xaml = """
                    <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                            xmlns:aq="http://schemas.barbatos.co/aquarius/2026/xaml">
                        <Border x:Name="Target" aq:Directives.Show="{aq:Expr 'DoesNotExist > 0'}" />
                    </Window>
                    """;

                var window = (Window)XamlReader.Parse(xaml);
                window.DataContext = new ExprTestViewModel();

                window.Show();
                StaThread.PumpDispatcher();
            }));

            Assert.Contains("DoesNotExist", ex.Message);
            Assert.Contains("DoesNotExist > 0", ex.Message);
        }
        finally
        {
            Expr.ThrowOnUnresolvedIdentifiers = false;
        }
    }

    [Fact]
    public void UnresolvedIdentifierFallsBackTheSameWayAPlainBindingWould()
    {
        StaThread.Run(() =>
        {
            var xamlWithExpr = """
                <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:aq="http://schemas.barbatos.co/aquarius/2026/xaml">
                    <Border x:Name="Target" aq:Directives.Show="{aq:Expr 'DoesNotExist > 0'}" />
                </Window>
                """;
            var xamlWithPlainBinding = """
                <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:aq="http://schemas.barbatos.co/aquarius/2026/xaml">
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
