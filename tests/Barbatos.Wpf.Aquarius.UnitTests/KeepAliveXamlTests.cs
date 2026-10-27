// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Barbatos.Wpf.Aquarius.UnitTests;

// XamlReader.Parse on real XAML strings rather than element construction in C#, following
// SlotXamlTests/ExprXamlTests' own precedent - because the things most likely to be wrong about
// KeepAlive are XAML-loader facts that constructing the object graph by hand cannot reproduce:
// that [ContentProperty(nameof(Child))] really overrides the Children content property KeepAlive
// inherits from Panel, and above all that `Is="{Binding ...}"` on a Component that is deliberately
// NOT a visual child still inherits KeepAlive's DataContext - which only works because KeepAlive
// reports it through LogicalChildren.
public class KeepAliveXamlTests
{
    [Fact]
    public void ChildIsTheContentPropertyAndABoundIsResolvesThroughTheLogicalTree()
    {
        StaThread.Run(() =>
        {
            var xaml = """
                <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:aq="http://schemas.barbatos.co/aquarius/2026/xaml"
                        Width="200" Height="200">
                    <aq:KeepAlive x:Name="Host" Max="2" Exclude="Border">
                        <aq:Component Is="{Binding Current.Value}" />
                    </aq:KeepAlive>
                </Window>
                """;

            var window = (Window)XamlReader.Parse(xaml);
            var keepAlive = (KeepAlive)window.FindName("Host");
            var viewModel = new SectionViewModel();
            window.DataContext = viewModel;

            Assert.NotNull(keepAlive.Child);
            Assert.Equal(2, keepAlive.Max);
            Assert.Equal("Border", keepAlive.Exclude);

            window.Show();
            StaThread.PumpDispatcher();

            // The binding resolved at all - the whole point of Child being a logical child.
            Assert.Equal(typeof(TextBlock), keepAlive.Child!.Is);
            Assert.IsType<TextBlock>(keepAlive.ActiveView);

            var first = keepAlive.ActiveView;

            viewModel.Current.Value = typeof(Button);
            StaThread.PumpDispatcher();

            Assert.IsType<Button>(keepAlive.ActiveView);
            Assert.Equal([typeof(TextBlock), typeof(Button)], keepAlive.CachedTypes); // TextBlock kept, just hidden

            viewModel.Current.Value = typeof(TextBlock);
            StaThread.PumpDispatcher();

            Assert.Same(first, keepAlive.ActiveView); // the very same instance came back

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void AComponentOutsideAKeepAliveRendersAndReplacesItsOwnContent()
    {
        // Vue's <component :is> works on its own; <KeepAlive> is the opt-in wrapper. Standalone,
        // switching genuinely swaps the View out - which is what makes the old one collectable.
        StaThread.Run(() =>
        {
            var xaml = """
                <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:aq="http://schemas.barbatos.co/aquarius/2026/xaml"
                        Width="200" Height="200">
                    <aq:Component x:Name="Host" Is="{Binding Current.Value}" />
                </Window>
                """;

            var window = (Window)XamlReader.Parse(xaml);
            var component = (Component)window.FindName("Host");
            var viewModel = new SectionViewModel();
            window.DataContext = viewModel;

            window.Show();
            StaThread.PumpDispatcher();

            Assert.IsType<TextBlock>(component.Content);

            viewModel.Current.Value = typeof(Button);
            StaThread.PumpDispatcher();

            Assert.IsType<Button>(component.Content);

            viewModel.Current.Value = null;
            StaThread.PumpDispatcher();

            Assert.Null(component.Content);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    /// <summary>Drives <see cref="Component.Is"/> from a <see cref="Ref{T}"/>, the way a real ViewModel would.</summary>
    private sealed class SectionViewModel
    {
        public Ref<Type?> Current { get; } = new(typeof(TextBlock));
    }
}
