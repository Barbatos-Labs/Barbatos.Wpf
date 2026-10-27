// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.AquariusValee.UnitTests;

public class AsyncValidationRaceTests
{
    [Fact]
    public async Task TheLatestValidationCallsResultWinsOverAnOlderStillInFlightCall()
    {
        var firstGate = new TaskCompletionSource();
        var callIndex = 0;

        var field = new Field<string>("Username", "a", async (value, ct) =>
        {
            var myIndex = ++callIndex;

            if (myIndex == 1)
            {
                await firstGate.Task;
                return "First (stale) result";
            }

            return "Second (fresh) result";
        });

        var firstValidate = field.ValidateAsync();
        var secondValidate = field.ValidateAsync();

        firstGate.SetResult();
        await Task.WhenAll(firstValidate, secondValidate);

        Assert.Contains("Second (fresh) result", field.Errors);
        Assert.DoesNotContain("First (stale) result", field.Errors);
    }

    [Fact]
    public async Task AnOlderInFlightCallsCancellationTokenIsActuallyCancelledNotJustIgnored()
    {
        var gate = new TaskCompletionSource();
        var callCount = 0;
        var firstToken = CancellationToken.None;

        var field = new Field<string>("Username", "a", async (value, ct) =>
        {
            if (++callCount == 1)
            {
                firstToken = ct;
                await gate.Task;
            }

            return null;
        });

        var firstValidate = field.ValidateAsync();
        var secondValidate = field.ValidateAsync();

        // Real .NET cancellation, not vee-validate's "let the stale Promise finish and discard
        // its result" - the older call's own token observably flips before it even resumes.
        Assert.True(firstToken.IsCancellationRequested);

        gate.SetResult();
        await Task.WhenAll(firstValidate, secondValidate);
    }

    [Fact]
    public async Task PendingReturnsToFalseAfterTheLatestCallSettlesEvenIfAnOlderCallSettlesLater()
    {
        var firstGate = new TaskCompletionSource();
        var callIndex = 0;

        var field = new Field<string>("Username", "a", async (value, ct) =>
        {
            if (++callIndex == 1)
                await firstGate.Task;

            return null;
        });

        var firstValidate = field.ValidateAsync();
        var secondValidate = field.ValidateAsync();
        await secondValidate;

        Assert.False(field.Meta.Pending);

        firstGate.SetResult();
        await firstValidate;

        Assert.False(field.Meta.Pending);
    }
}
