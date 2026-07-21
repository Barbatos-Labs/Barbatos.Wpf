using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Barbatos.Wpf.Aquarius.UnitTests;

// XamlReader.Parse on real XAML strings, not element construction in C# - Slot's reactive
// path (SlotContent/SlotProvided returning a RelativeSource=TemplatedParent Binding) needs a
// real ControlTemplate actually applied to a real control instance, which only genuine XAML
// loading provides correctly. Mirrors ExprXamlTests.cs's own precedent/rationale exactly.
public class SlotXamlTests
{
    [Fact]
    public void NamedAndDefaultSlotsRouteToTheirOwnPresenters()
    {
        StaThread.Run(() =>
        {
            var xaml = """
                <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:aq="http://schemas.barbatos.dev/aquarius">
                    <aq:SlotHost>
                        <aq:SlotHost.Template>
                            <ControlTemplate TargetType="aq:SlotHost">
                                <StackPanel>
                                    <ContentPresenter x:Name="HeaderPresenter" Content="{aq:SlotContent header}" />
                                    <ContentPresenter x:Name="DefaultPresenter" Content="{aq:SlotContent}" />
                                </StackPanel>
                            </ControlTemplate>
                        </aq:SlotHost.Template>
                        <aq:Slot Name="header">
                            <TextBlock x:Name="HeaderText" Text="Title" />
                        </aq:Slot>
                        <TextBlock x:Name="DefaultText" Text="Body" />
                    </aq:SlotHost>
                </Window>
                """;

            var window = (Window)XamlReader.Parse(xaml);
            var headerText = window.FindName("HeaderText");
            var defaultText = window.FindName("DefaultText");
            var host = (SlotHost)window.Content;

            window.Show();
            host.ApplyTemplate();
            StaThread.PumpDispatcher();

            var headerPresenter = (ContentPresenter)host.Template.FindName("HeaderPresenter", host);
            var defaultPresenter = (ContentPresenter)host.Template.FindName("DefaultPresenter", host);

            Assert.Same(headerText, headerPresenter.Content);
            Assert.Same(defaultText, defaultPresenter.Content);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void FallbackIsUsedWhenTheSlotWasNeverProvided()
    {
        StaThread.Run(() =>
        {
            var xaml = """
                <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:aq="http://schemas.barbatos.dev/aquarius">
                    <aq:SlotHost>
                        <aq:SlotHost.Template>
                            <ControlTemplate TargetType="aq:SlotHost">
                                <ContentPresenter x:Name="FooterPresenter" Content="{aq:SlotContent footer, Fallback=NoFooter}" />
                            </ControlTemplate>
                        </aq:SlotHost.Template>
                        <TextBlock Text="Body only - no footer slot at all" />
                    </aq:SlotHost>
                </Window>
                """;

            var window = (Window)XamlReader.Parse(xaml);
            var host = (SlotHost)window.Content;

            window.Show();
            host.ApplyTemplate();
            StaThread.PumpDispatcher();

            var footerPresenter = (ContentPresenter)host.Template.FindName("FooterPresenter", host);

            // This is also the executable proof that Binding.FallbackValue would have been
            // the wrong mechanism here: the lookup itself succeeds (returns null, a "not
            // found" result), so FallbackValue (which only fires when a binding *fails*)
            // would never have activated - only the converter's own explicit substitution does.
            Assert.Equal("NoFooter", footerPresenter.Content);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void SlotProvidedComposesWithIfToSkipTheWrapperEntirely()
    {
        StaThread.Run(() =>
        {
            var xaml = """
                <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:aq="http://schemas.barbatos.dev/aquarius">
                    <aq:SlotHost>
                        <aq:SlotHost.Template>
                            <ControlTemplate TargetType="aq:SlotHost">
                                <aq:If x:Name="HeaderIf" Condition="{aq:SlotProvided header}">
                                    <Border x:Name="HeaderWrapper">
                                        <ContentPresenter Content="{aq:SlotContent header}" />
                                    </Border>
                                </aq:If>
                            </ControlTemplate>
                        </aq:SlotHost.Template>
                        <aq:Slot Name="header">
                            <TextBlock Text="Title" />
                        </aq:Slot>
                    </aq:SlotHost>
                </Window>
                """;

            var window = (Window)XamlReader.Parse(xaml);
            var host = (SlotHost)window.Content;

            window.Show();
            host.ApplyTemplate();
            StaThread.PumpDispatcher();

            var headerIf = (If)host.Template.FindName("HeaderIf", host);
            Assert.NotNull(headerIf.Content); // header was provided - wrapper Border is shown

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void SlotProvidedIsFalseAndIfSkipsTheWrapperWhenNothingWasSupplied()
    {
        StaThread.Run(() =>
        {
            var xaml = """
                <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:aq="http://schemas.barbatos.dev/aquarius">
                    <aq:SlotHost>
                        <aq:SlotHost.Template>
                            <ControlTemplate TargetType="aq:SlotHost">
                                <aq:If x:Name="HeaderIf" Condition="{aq:SlotProvided header}">
                                    <Border x:Name="HeaderWrapper" />
                                </aq:If>
                            </ControlTemplate>
                        </aq:SlotHost.Template>
                    </aq:SlotHost>
                </Window>
                """;

            var window = (Window)XamlReader.Parse(xaml);
            var host = (SlotHost)window.Content;

            window.Show();
            host.ApplyTemplate();
            StaThread.PumpDispatcher();

            var headerIf = (If)host.Template.FindName("HeaderIf", host);
            Assert.Null(headerIf.Content); // no header slot at all - wrapper Border never shows

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void AddingASlotAfterShowingUpdatesTheBoundPresenterLive()
    {
        StaThread.Run(() =>
        {
            var xaml = """
                <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:aq="http://schemas.barbatos.dev/aquarius">
                    <aq:SlotHost>
                        <aq:SlotHost.Template>
                            <ControlTemplate TargetType="aq:SlotHost">
                                <ContentPresenter x:Name="HeaderPresenter" Content="{aq:SlotContent header}" />
                            </ControlTemplate>
                        </aq:SlotHost.Template>
                    </aq:SlotHost>
                </Window>
                """;

            var window = (Window)XamlReader.Parse(xaml);
            var host = (SlotHost)window.Content;

            window.Show();
            host.ApplyTemplate();
            StaThread.PumpDispatcher();

            var headerPresenter = (ContentPresenter)host.Template.FindName("HeaderPresenter", host);
            Assert.Null(headerPresenter.Content);

            var newHeader = new TextBlock { Text = "Added later" };
            host.Items.Add(new Slot { Name = "header", Content = newHeader });
            StaThread.PumpDispatcher();

            Assert.Same(newHeader, headerPresenter.Content);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void RemovingASlotAfterShowingUpdatesTheBoundPresenterLive()
    {
        StaThread.Run(() =>
        {
            var xaml = """
                <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:aq="http://schemas.barbatos.dev/aquarius">
                    <aq:SlotHost>
                        <aq:SlotHost.Template>
                            <ControlTemplate TargetType="aq:SlotHost">
                                <ContentPresenter x:Name="HeaderPresenter" Content="{aq:SlotContent header}" />
                            </ControlTemplate>
                        </aq:SlotHost.Template>
                        <aq:Slot Name="header">
                            <TextBlock Text="Title" />
                        </aq:Slot>
                    </aq:SlotHost>
                </Window>
                """;

            var window = (Window)XamlReader.Parse(xaml);
            var host = (SlotHost)window.Content;

            window.Show();
            host.ApplyTemplate();
            StaThread.PumpDispatcher();

            var headerPresenter = (ContentPresenter)host.Template.FindName("HeaderPresenter", host);
            Assert.NotNull(headerPresenter.Content);

            var headerSlot = host.Items.OfType<Slot>().Single(s => s.Name == "header");
            host.Items.Remove(headerSlot);
            StaThread.PumpDispatcher();

            Assert.Null(headerPresenter.Content);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void SingleScopedSlotPatternNeedsNoNewSyntaxJustSettingDataContextDirectly()
    {
        StaThread.Run(() =>
        {
            var xaml = """
                <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:aq="http://schemas.barbatos.dev/aquarius">
                    <aq:SlotHost>
                        <aq:SlotHost.Template>
                            <ControlTemplate TargetType="aq:SlotHost">
                                <ContentPresenter Content="{aq:SlotContent}" />
                            </ControlTemplate>
                        </aq:SlotHost.Template>
                        <TextBlock x:Name="ScopedText" Text="{Binding Message}" />
                    </aq:SlotHost>
                </Window>
                """;

            var window = (Window)XamlReader.Parse(xaml);
            var host = (SlotHost)window.Content;
            var scopedText = (TextBlock)window.FindName("ScopedText");

            window.Show();
            host.ApplyTemplate();
            StaThread.PumpDispatcher();

            Assert.Equal("", scopedText.Text);

            // The "component author" pushes scoped data straight onto the content object it
            // already holds a reference to - ordinary WPF, no new Aquarius syntax needed.
            scopedText.DataContext = new { Message = "hello from the host" };
            StaThread.PumpDispatcher();

            Assert.Equal("hello from the host", scopedText.Text);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }
}
