// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.AquariusValee.UnitTests;

public class FieldMetaTests
{
    [Fact]
    public async Task ValidatedAndTouchedAreIndependentAxes()
    {
        var validatedOnly = new Field<string>("Email", "", new RequiredAttribute());
        await validatedOnly.ValidateAsync();
        Assert.True(validatedOnly.Meta.Validated);
        Assert.False(validatedOnly.Meta.Touched);

        var touchedOnly = new Field<string>("Email", "", new RequiredAttribute()) { Triggers = ValidateTrigger.None };
        touchedOnly.HandleBlur();
        Assert.True(touchedOnly.Meta.Touched);
        Assert.False(touchedOnly.Meta.Validated);
    }

    [Fact]
    public void ValidDefaultsToTrueBeforeAnAsyncSilentPassHasHadAChanceToComplete()
    {
        var gate = new TaskCompletionSource<string?>();
        var field = new Field<string>("Username", "someone", async (value, ct) => await gate.Task);

        // The constructor's silent pass genuinely suspends here (a real await), so it has not
        // written Meta.Valid yet - "no errors seen yet" defaults to true, not "unknown"/false,
        // matching vee-validate's own real semantics.
        Assert.True(field.Meta.Valid);

        gate.SetResult("Username taken");
    }

    [Fact]
    public async Task PendingIsTrueOnlyWhileAnAsyncRuleIsActuallyInFlight()
    {
        var gate = new TaskCompletionSource<string?>();
        var field = new Field<string>("Username", "someone", (value, ct) => gate.Task);

        var validateTask = field.ValidateAsync();

        Assert.True(field.Meta.Pending);

        gate.SetResult(null);
        await validateTask;

        Assert.False(field.Meta.Pending);
    }

    [Fact]
    public void SilentConstructionTimePassNeverSetsPendingEvenWithAnAsyncRule()
    {
        var gate = new TaskCompletionSource<string?>();
        var field = new Field<string>("Username", "someone", (value, ct) => gate.Task);

        // The Silent mode used for the one pass every field runs at construction deliberately
        // never touches Pending - only a real (ValidatedOnly/Force) pass does.
        Assert.False(field.Meta.Pending);

        gate.SetResult(null);
    }
}
