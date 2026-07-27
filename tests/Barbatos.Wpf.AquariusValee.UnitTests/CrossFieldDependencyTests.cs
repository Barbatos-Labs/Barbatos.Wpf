namespace Barbatos.Wpf.AquariusValee.UnitTests;

public class CrossFieldDependencyTests
{
    [Fact]
    public async Task DependsOnRevalidatesTheDependentFieldWhenTheSourceFieldsValueChanges()
    {
        var password = new Field<string>("Password", "secret123");
        var confirm = new Field<string>("ConfirmPassword", "secret123",
            value => value == password.Value ? null : "Passwords must match");
        confirm.DependsOn(password);

        // Establish Validated=true first (matching vee-validate's own ValidatedOnly gate -
        // an untouched field doesn't suddenly show an error purely because a sibling changed).
        await confirm.ValidateAsync();
        Assert.Empty(confirm.Errors);

        password.Value = "different";
        await Task.Delay(1); // let the fire-and-forget ValidatedOnly pass triggered by DependsOn run

        Assert.NotEmpty(confirm.Errors);
    }

    [Fact]
    public async Task MutualDependenciesDoNotInfiniteLoopBecauseValidatingDoesNotChangeTheWatchedValue()
    {
        // Watch.On (used by DependsOn) only reacts to the wrapped Ref<T>.Value itself changing -
        // never to Errors/Meta changes a validation pass causes - so two fields that depend on
        // each other cannot ping-pong forever the way Watch.Effect (which reacts to ANY property
        // change) would. This test proves it terminates rather than hanging/stack-overflowing.
        var aRuleCalls = 0;
        var bRuleCalls = 0;

        var a = new Field<string>("A", "1", value => { aRuleCalls++; return null; });
        var b = new Field<string>("B", "1", value => { bRuleCalls++; return null; });
        a.DependsOn(b);
        b.DependsOn(a);

        var callsAfterConstruction = aRuleCalls;

        a.Value = "2";
        await Task.Delay(20); // give any (incorrect) cascade a real chance to happen

        // Exactly one additional run for A's own change, plus at most one more for B's
        // resulting ValidatedOnly pass (which itself does not change B's Value, so it must not
        // cause a third, fourth, ... run of either rule).
        Assert.True(aRuleCalls <= callsAfterConstruction + 1);
    }

    [Fact]
    public async Task DisposingAFieldStopsItFromReactingToItsDependency()
    {
        var source = new Field<string>("Source", "1");
        var revalidateCount = 0;
        var dependent = new Field<string>("Dependent", "1", value => { revalidateCount++; return null; });
        dependent.DependsOn(source);

        var countAfterConstruction = revalidateCount;
        dependent.Dispose();

        source.Value = "2";
        await Task.Delay(20);

        Assert.Equal(countAfterConstruction, revalidateCount);
    }
}
