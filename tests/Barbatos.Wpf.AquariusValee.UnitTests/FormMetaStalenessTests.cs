namespace Barbatos.Wpf.AquariusValee.UnitTests;

/// <summary>
/// Regression coverage for a real race condition caught during design review: an earlier draft
/// cached <see cref="FormMeta"/>'s aggregate properties and only recomputed them on the next
/// dispatcher tick (mirroring <c>Lifecycle</c>'s own <c>IOnUpdated</c> batching too literally).
/// That would let <see cref="Form.HandleSubmit"/>'s synchronous read of <c>Meta.Valid</c> -
/// immediately after <c>await</c>-ing a full validation pass - see a stale, not-yet-recomputed
/// value. The fix: every <see cref="FormMeta"/> property is a plain on-demand getter, computed
/// fresh on every read; <c>NextTick</c> batches only the outgoing notification, never the value
/// itself. These tests deliberately never pump a dispatcher, so a regression back to a cached
/// design would fail them (the notification would never have "arrived" to refresh a cache).
/// </summary>
public class FormMetaStalenessTests
{
    [Fact]
    public async Task MetaValidIsCorrectImmediatelyAfterValidateAllAsyncWithNoDispatcherPump()
    {
        var form = new Form();
        var gate = new TaskCompletionSource<string?>();
        var username = form.DefineField("Username", "someone", (value, ct) => gate.Task);

        var validateTask = form.ValidateAllAsync();
        gate.SetResult("Username already taken");
        var isValid = await validateTask;

        // No StaThread/PumpDispatcher anywhere in this test - if FormMeta.Valid were cached and
        // only refreshed via a NextTick callback, this read would still see the pre-validation
        // (stale) value here, since nothing ever pumped the dispatcher to let that callback run.
        Assert.False(isValid);
        Assert.False(form.Meta.Valid);
        Assert.NotEmpty(username.Errors);
    }

    [Fact]
    public async Task HandleSubmitInvokesOnValidVersusOnInvalidBasedOnTheTrueUpToDateOutcomeNotAStaleOne()
    {
        var form = new Form();
        var gate = new TaskCompletionSource<string?>();
        form.DefineField("Username", "someone", (value, ct) => gate.Task);

        var onValidCalled = false;
        var onInvalidCalled = false;
        var command = form.HandleSubmit(
            _ => { onValidCalled = true; return Task.CompletedTask; },
            (_, _) => onInvalidCalled = true);

        var executeTask = command.ExecuteAsync(null);
        gate.SetResult("Username already taken");
        await executeTask;

        Assert.False(onValidCalled);
        Assert.True(onInvalidCalled);
    }

    [Fact]
    public async Task MultipleFieldsChangingTouchedInOneBurstStillReportAnAccurateAggregateOnImmediateRead()
    {
        var form = new Form();
        var a = form.DefineField("A", "");
        var b = form.DefineField("B", "");
        var c = form.DefineField("C", "");

        // Touch all three back to back, in the same callstack, with no pump in between -
        // exactly the shape Form.HandleSubmit's own touch-all-fields step produces.
        a.HandleBlur();
        b.HandleBlur();
        c.HandleBlur();

        Assert.True(form.Meta.Touched);
        await Task.CompletedTask;
    }
}
