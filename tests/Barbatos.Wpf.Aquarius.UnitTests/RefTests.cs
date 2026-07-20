namespace Barbatos.Wpf.Aquarius.UnitTests;

public class RefTests
{
    [Fact]
    public void ValueDefaultsToDefaultOfT()
    {
        var r = new Ref<int>();

        Assert.Equal(0, r.Value);
    }

    [Fact]
    public void ConstructorSetsTheInitialValue()
    {
        var r = new Ref<string>("hello");

        Assert.Equal("hello", r.Value);
    }

    [Fact]
    public void SettingValueRaisesPropertyChanged()
    {
        var r = new Ref<int>(1);
        var raised = 0;
        r.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Ref<int>.Value))
                raised++;
        };

        r.Value = 2;

        Assert.Equal(1, raised);
        Assert.Equal(2, r.Value);
    }

    [Fact]
    public void SettingTheSameValueDoesNotRaisePropertyChanged()
    {
        var r = new Ref<int>(5);
        var raised = 0;
        r.PropertyChanged += (_, _) => raised++;

        r.Value = 5;

        Assert.Equal(0, raised);
    }
}
