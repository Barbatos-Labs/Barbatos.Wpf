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
