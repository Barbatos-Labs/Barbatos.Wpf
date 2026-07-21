namespace Barbatos.Wpf.Aquarius.UnitTests;

public class SlotHostTests
{
    [Fact]
    public void SlotDefaultsToTheDefaultSlotNameAndNullContent()
    {
        var slot = new Slot();

        Assert.Equal("", slot.Name);
        Assert.Null(slot.Content);
    }

    [Fact]
    public void PlainItemsAreTreatedAsTheDefaultSlot()
    {
        StaThread.Run(() =>
        {
            var host = new TestSlotHost();
            var content = new object();

            host.Items.Add(content);

            Assert.True(host.PublicIsSlotProvided());
            Assert.Same(content, host.PublicGetSlotContent());
        });
    }

    [Fact]
    public void NamedSlotIsResolvedBySlotName()
    {
        StaThread.Run(() =>
        {
            var host = new TestSlotHost();
            var content = new object();

            host.Items.Add(new Slot { Name = "header", Content = content });

            Assert.True(host.PublicIsSlotProvided("header"));
            Assert.Same(content, host.PublicGetSlotContent("header"));
            Assert.False(host.PublicIsSlotProvided("footer"));
            Assert.Null(host.PublicGetSlotContent("footer"));
        });
    }

    [Fact]
    public void RemovingAnItemUnresolvesItsSlot()
    {
        StaThread.Run(() =>
        {
            var host = new TestSlotHost();
            var slot = new Slot { Name = "header", Content = new object() };
            host.Items.Add(slot);

            host.Items.Remove(slot);

            Assert.False(host.PublicIsSlotProvided("header"));
        });
    }

    [Fact]
    public void ClearingItemsResolvesToNoSlotsAtAll()
    {
        StaThread.Run(() =>
        {
            var host = new TestSlotHost();
            host.Items.Add(new Slot { Name = "header", Content = new object() });
            host.Items.Add(new object());

            host.Items.Clear();

            Assert.False(host.PublicIsSlotProvided());
            Assert.False(host.PublicIsSlotProvided("header"));
        });
    }

    [Fact]
    public void ReplacingAnItemAtAnIndexReResolves()
    {
        StaThread.Run(() =>
        {
            var host = new TestSlotHost();
            host.Items.Add(new Slot { Name = "header", Content = "old" });

            host.Items[0] = new Slot { Name = "header", Content = "new" };

            Assert.Equal("new", host.PublicGetSlotContent("header"));
        });
    }

    [Fact]
    public void TwoUnnamedItemsBothClaimingTheDefaultSlotThrows()
    {
        StaThread.Run(() =>
        {
            var host = new TestSlotHost();
            host.Items.Add(new object());

            Assert.Throws<InvalidOperationException>(() => host.Items.Add(new object()));
        });
    }

    [Fact]
    public void TwoItemsClaimingTheSameNamedSlotThrows()
    {
        StaThread.Run(() =>
        {
            var host = new TestSlotHost();
            host.Items.Add(new Slot { Name = "header", Content = new object() });

            Assert.Throws<InvalidOperationException>(() => host.Items.Add(new Slot { Name = "header", Content = new object() }));
        });
    }

    [Fact]
    public void GetSlotContentReturnsNullForAnAbsentSlot()
    {
        StaThread.Run(() =>
        {
            var host = new TestSlotHost();

            Assert.Null(host.PublicGetSlotContent("nonexistent"));
            Assert.False(host.PublicIsSlotProvided("nonexistent"));
        });
    }

    [Fact]
    public void ProvidedButEmptySlotIsStillProvided()
    {
        StaThread.Run(() =>
        {
            var host = new TestSlotHost();

            host.Items.Add(new Slot { Name = "header" }); // Content left null

            Assert.True(host.PublicIsSlotProvided("header"));
            Assert.Null(host.PublicGetSlotContent("header"));
        });
    }

    /// <summary>Exposes SlotHost's protected members publicly - GetSlotContent/IsSlotProvided are protected since component authors in any assembly need them, not just this test assembly (no InternalsVisibleTo seam exists in this repo).</summary>
    private sealed class TestSlotHost : SlotHost
    {
        public object? PublicGetSlotContent(string name = "") => GetSlotContent(name);

        public bool PublicIsSlotProvided(string name = "") => IsSlotProvided(name);
    }
}
