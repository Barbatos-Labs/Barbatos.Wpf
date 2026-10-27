// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

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
    public async Task DependsOnSupportsTwoDependenciesDrivingADynamicRangeInsteadOfAFixedConstant()
    {
        // [Range(13, 120)] cannot express "bounded by two other properties" at all - attribute
        // constructor arguments must be compile-time constants in C#, so there is no
        // [Range(MinPrice, MaxPrice)] referencing sibling properties. Field.DependsOn is the way
        // to do this instead, and it is not limited to one dependency/one equality check (as
        // Signup's confirm-password demo alone might suggest) - a field can depend on several
        // sources at once, each feeding the same rule.
        var minPrice = new Field<int>("MinPrice", 10);
        var maxPrice = new Field<int>("MaxPrice", 100);
        var price = new Field<int>("Price", 50,
            value => value < minPrice.Value ? $"Must be at least {minPrice.Value}." : null,
            value => value > maxPrice.Value ? $"Must be at most {maxPrice.Value}." : null);
        price.DependsOn(minPrice);
        price.DependsOn(maxPrice);

        await price.ValidateAsync();
        Assert.Empty(price.Errors);

        // Price itself is never touched here - only its dependencies change.
        minPrice.Value = 60;
        await Task.Delay(20);
        Assert.Equal("Must be at least 60.", price.ErrorMessage);

        minPrice.Value = 10;
        await Task.Delay(20);
        Assert.Empty(price.Errors);

        maxPrice.Value = 40;
        await Task.Delay(20);
        Assert.Equal("Must be at most 40.", price.ErrorMessage);
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
