namespace Barbatos.Wpf.Aquarius.UnitTests;

public class NextTickTests
{
    [Fact]
    public void RunDoesNotExecuteSynchronously()
    {
        StaThread.Run(() =>
        {
            var ran = false;

            NextTick.Run(() => ran = true);

            Assert.False(ran);

            StaThread.PumpDispatcher();

            Assert.True(ran);
        });
    }

    [Fact]
    public void RunAsyncCompletesOnlyAfterThePump()
    {
        StaThread.Run(() =>
        {
            var task = NextTick.RunAsync(() => { });

            Assert.False(task.IsCompleted);

            StaThread.PumpDispatcher();

            Assert.True(task.IsCompletedSuccessfully);
        });
    }
}
