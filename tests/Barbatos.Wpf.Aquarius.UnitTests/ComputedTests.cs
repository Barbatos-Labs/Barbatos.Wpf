namespace Barbatos.Wpf.Aquarius.UnitTests;

public class ComputedTests
{
    [Fact]
    public void ValueReflectsTheGetterAtCreation()
    {
        var count = new Ref<int>(2);
        using var doubled = Computed<int>.From(() => count.Value * 2, count);

        Assert.Equal(4, doubled.Value);
    }

    [Fact]
    public void RecomputesWhenADependencyChanges()
    {
        var count = new Ref<int>(2);
        using var doubled = Computed<int>.From(() => count.Value * 2, count);

        count.Value = 5;

        Assert.Equal(10, doubled.Value);
    }

    [Fact]
    public void DoesNotRaisePropertyChangedWhenTheComputedResultIsUnchanged()
    {
        var count = new Ref<int>(2);
        using var isEven = Computed<bool>.From(() => count.Value % 2 == 0, count);
        var raised = 0;
        isEven.PropertyChanged += (_, _) => raised++;

        count.Value = 4; // still even: the dependency changed, but the computed result didn't

        Assert.Equal(0, raised);
        Assert.True(isEven.Value);
    }

    [Fact]
    public void RaisesPropertyChangedWhenTheComputedResultChanges()
    {
        var count = new Ref<int>(2);
        using var isEven = Computed<bool>.From(() => count.Value % 2 == 0, count);
        var raised = 0;
        isEven.PropertyChanged += (_, _) => raised++;

        count.Value = 3; // now odd: the result flips

        Assert.Equal(1, raised);
        Assert.False(isEven.Value);
    }

    [Fact]
    public void DisposeStopsTrackingDependencies()
    {
        var count = new Ref<int>(1);
        var doubled = Computed<int>.From(() => count.Value * 2, count);

        doubled.Dispose();
        count.Value = 100;

        Assert.Equal(2, doubled.Value); // stayed at the last computed value
    }

    [Fact]
    public void WritableComputedSetterMutatesTheDependencyItReadsFrom()
    {
        var celsius = new Ref<double>(0);
        using var fahrenheit = Computed<double>.From(
            () => celsius.Value * 9 / 5 + 32,
            f => celsius.Value = (f - 32) * 5 / 9,
            celsius);

        fahrenheit.Value = 212;

        Assert.Equal(100, celsius.Value, precision: 5);
        Assert.Equal(212, fahrenheit.Value, precision: 5);
    }

    [Fact]
    public void ReadOnlyComputedThrowsWhenAssigned()
    {
        var count = new Ref<int>(1);
        using var doubled = Computed<int>.From(() => count.Value * 2, count);

        Assert.Throws<InvalidOperationException>(() => doubled.Value = 10);
    }
}
