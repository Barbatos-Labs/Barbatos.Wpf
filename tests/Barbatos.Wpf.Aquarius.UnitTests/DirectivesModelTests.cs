// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.Aquarius.UnitTests;

public partial class DirectivesModelTests
{
    private sealed partial class TestViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _text = "";

        [ObservableProperty]
        private bool _flag;

        [ObservableProperty]
        private double _amount;

        [ObservableProperty]
        private string? _nickname;

        [ObservableProperty]
        private string? _selectedFruit;
    }

    [Fact]
    public void TextBoxGetsATwoWayBindingThatUpdatesOnEveryKeystroke()
    {
        StaThread.Run(() =>
        {
            var vm = new TestViewModel { Text = "hello" };
            var textBox = new TextBox { DataContext = vm };
            BindingOperations.SetBinding(textBox, Directives.ModelProperty, new Binding(nameof(TestViewModel.Text)));

            Assert.Equal("hello", textBox.Text);

            textBox.Text = "changed"; // UpdateSourceTrigger=PropertyChanged: no explicit UpdateSource() needed
            Assert.Equal("changed", vm.Text);

            vm.Text = "from vm";
            Assert.Equal("from vm", textBox.Text);
        });
    }

    [Fact]
    public void CheckBoxGetsATwoWayBindingToIsChecked()
    {
        StaThread.Run(() =>
        {
            var vm = new TestViewModel { Flag = false };
            var checkBox = new CheckBox { DataContext = vm };
            BindingOperations.SetBinding(checkBox, Directives.ModelProperty, new Binding(nameof(TestViewModel.Flag)));

            checkBox.IsChecked = true;

            Assert.True(vm.Flag);
        });
    }

    [Fact]
    public void SliderGetsATwoWayBindingToValue()
    {
        StaThread.Run(() =>
        {
            var vm = new TestViewModel { Amount = 1 };
            var slider = new Slider { DataContext = vm };
            BindingOperations.SetBinding(slider, Directives.ModelProperty, new Binding(nameof(TestViewModel.Amount)));

            slider.Value = 42;

            Assert.Equal(42, vm.Amount);
        });
    }

    [Fact]
    public void TextBoxBoundToAnInitiallyNullValueStillWritesTypedTextBack()
    {
        // WPF runs a property-changed callback only when the effective value changes. With a null
        // default, a Model binding whose first value was also null never reached it, so no
        // TwoWay binding existed: the box looked bound, but what the user typed went nowhere.
        StaThread.Run(() =>
        {
            var vm = new TestViewModel(); // Nickname starts null
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, Directives.ModelProperty, new Binding(nameof(TestViewModel.Nickname)));

            // DataContext arriving after the binding is what an inherited DataContext looks like
            // while the XAML loader builds the tree. WPF does not resolve a binding that had no
            // context when it was set until the dispatcher runs its deferred data-binding work, so
            // pump before typing - as a real window does before anyone can type. (CI showed that
            // without the pump the two-way binding does not exist yet, fixed or not.)
            textBox.DataContext = vm;
            StaThread.PumpDispatcher();

            textBox.Text = "typed";

            Assert.Equal("typed", vm.Nickname);
        });
    }

    [Fact]
    public void SelectorWithNothingSelectedYetStillWritesTheSelectionBack()
    {
        StaThread.Run(() =>
        {
            var vm = new TestViewModel(); // SelectedFruit starts null - nothing selected yet
            var listBox = new ListBox { ItemsSource = new[] { "Apple", "Banana" }, DataContext = vm };
            BindingOperations.SetBinding(listBox, Directives.ModelProperty, new Binding(nameof(TestViewModel.SelectedFruit)));

            listBox.SelectedItem = "Banana";

            Assert.Equal("Banana", vm.SelectedFruit);
        });
    }

    [Fact]
    public void ClearingTheModelBindingIsNotMistakenForAPlainValue()
    {
        StaThread.Run(() =>
        {
            var vm = new TestViewModel { Text = "hello" };
            var textBox = new TextBox { DataContext = vm };
            BindingOperations.SetBinding(textBox, Directives.ModelProperty, new Binding(nameof(TestViewModel.Text)));

            var exception = Record.Exception(() => BindingOperations.ClearBinding(textBox, Directives.ModelProperty));

            Assert.Null(exception);
            Assert.Null(Directives.GetModel(textBox));
        });
    }

    [Fact]
    public void UnsupportedElementTypeThrows()
    {
        StaThread.Run(() =>
        {
            var vm = new TestViewModel();
            var border = new Border { DataContext = vm };

            Assert.Throws<InvalidOperationException>(() =>
                BindingOperations.SetBinding(border, Directives.ModelProperty, new Binding(nameof(TestViewModel.Text))));
        });
    }

    [Fact]
    public void PasswordBoxThrowsWithASecurityExplanation()
    {
        StaThread.Run(() =>
        {
            var vm = new TestViewModel();
            var passwordBox = new PasswordBox { DataContext = vm };

            var ex = Assert.Throws<InvalidOperationException>(() =>
                BindingOperations.SetBinding(passwordBox, Directives.ModelProperty, new Binding(nameof(TestViewModel.Text))));

            Assert.Contains("PasswordBox", ex.Message);
        });
    }

    [Fact]
    public void SettingAPlainValueInsteadOfABindingThrows()
    {
        StaThread.Run(() =>
        {
            var textBox = new TextBox();

            Assert.Throws<InvalidOperationException>(() => Directives.SetModel(textBox, "not a binding"));
        });
    }
}
