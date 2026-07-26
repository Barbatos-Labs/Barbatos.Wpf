// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Input;

namespace Barbatos.Wpf.Core.UnitTests;

/// <summary>
/// Pure state-machine tests against <see cref="InputInteractionContext"/> directly - no DI, no
/// WPF, no <see cref="IInputSystemService"/> involved, mirroring how <c>HotkeyGestureTests.cs</c>
/// tested the removed feature's own pure-parsing logic in isolation.
/// </summary>
public class InputInteractionTests
{
    [Fact]
    public void DefaultInteractionStartsAndPerformsTogetherOnPressThenCancelsOnRelease()
    {
        var interaction = DefaultInteraction.Instance;

        var press = new InputInteractionContext(isPressed: true);
        interaction.Process(ref press);
        Assert.True(press.WasStarted);
        Assert.True(press.WasPerformed);
        Assert.False(press.WasCanceled);
        Assert.Null(press.RequestedTimeout);

        var release = new InputInteractionContext(isPressed: false);
        interaction.Process(ref release);
        Assert.True(release.WasCanceled);
        Assert.False(release.WasStarted);
        Assert.False(release.WasPerformed);
    }

    [Fact]
    public void HoldInteractionPerformsOnlyAfterTheTimeoutElapses()
    {
        var interaction = new HoldInteraction(TimeSpan.FromMilliseconds(500));

        var press = new InputInteractionContext(isPressed: true);
        interaction.Process(ref press);
        Assert.True(press.WasStarted);
        Assert.False(press.WasPerformed);
        Assert.Equal(TimeSpan.FromMilliseconds(500), press.RequestedTimeout);

        var timeout = new InputInteractionContext(isPressed: true, isTimeout: true);
        interaction.Process(ref timeout);
        Assert.True(timeout.WasPerformed);
        Assert.False(timeout.WasStarted);
    }

    [Fact]
    public void HoldInteractionCancelsOnAnEarlyRelease()
    {
        var interaction = new HoldInteraction(TimeSpan.FromMilliseconds(500));

        var press = new InputInteractionContext(isPressed: true);
        interaction.Process(ref press);

        var release = new InputInteractionContext(isPressed: false);
        interaction.Process(ref release);
        Assert.True(release.WasCanceled);
        Assert.False(release.WasPerformed);
    }

    [Fact]
    public void TapInteractionPerformsOnAReleaseWithinTheWindow()
    {
        var interaction = new TapInteraction(TimeSpan.FromMilliseconds(300));

        var press = new InputInteractionContext(isPressed: true);
        interaction.Process(ref press);
        Assert.True(press.WasStarted);

        var release = new InputInteractionContext(isPressed: false);
        interaction.Process(ref release);
        Assert.True(release.WasPerformed);
        Assert.False(release.WasCanceled);
    }

    [Fact]
    public void TapInteractionCancelsWhenTheTimeoutElapsesBeforeRelease()
    {
        var interaction = new TapInteraction(TimeSpan.FromMilliseconds(300));

        var press = new InputInteractionContext(isPressed: true);
        interaction.Process(ref press);

        var timeout = new InputInteractionContext(isPressed: true, isTimeout: true);
        interaction.Process(ref timeout);
        Assert.False(timeout.WasStarted);
        Assert.False(timeout.WasPerformed);
        Assert.False(timeout.WasCanceled); // still waiting for the (now too-late) release.

        var release = new InputInteractionContext(isPressed: false);
        interaction.Process(ref release);
        Assert.True(release.WasCanceled);
        Assert.False(release.WasPerformed);
    }

    [Fact]
    public void TapInteractionResetAllowsAFreshAttempt()
    {
        var interaction = new TapInteraction(TimeSpan.FromMilliseconds(300));

        var press = new InputInteractionContext(isPressed: true);
        interaction.Process(ref press);
        var timeout = new InputInteractionContext(isPressed: true, isTimeout: true);
        interaction.Process(ref timeout);

        interaction.Reset();

        var freshPress = new InputInteractionContext(isPressed: true);
        interaction.Process(ref freshPress);
        var freshRelease = new InputInteractionContext(isPressed: false);
        interaction.Process(ref freshRelease);
        Assert.True(freshRelease.WasPerformed);
    }

    [Fact]
    public void MultiTapInteractionPerformsOnlyAfterTheConfiguredTapCount()
    {
        var interaction = new MultiTapInteraction(tapCount: 2, tapTime: TimeSpan.FromMilliseconds(200), tapDelay: TimeSpan.FromMilliseconds(300));

        // First tap.
        var press1 = new InputInteractionContext(isPressed: true);
        interaction.Process(ref press1);
        var release1 = new InputInteractionContext(isPressed: false);
        interaction.Process(ref release1);
        Assert.False(release1.WasPerformed); // one tap counted, not enough yet.

        // Second tap.
        var press2 = new InputInteractionContext(isPressed: true);
        interaction.Process(ref press2);
        var release2 = new InputInteractionContext(isPressed: false);
        interaction.Process(ref release2);
        Assert.True(release2.WasPerformed);
    }

    [Fact]
    public void MultiTapInteractionCancelsWhenTheGapBetweenTapsTimesOut()
    {
        var interaction = new MultiTapInteraction(tapCount: 2, tapTime: TimeSpan.FromMilliseconds(200), tapDelay: TimeSpan.FromMilliseconds(300));

        var press1 = new InputInteractionContext(isPressed: true);
        interaction.Process(ref press1);
        var release1 = new InputInteractionContext(isPressed: false);
        interaction.Process(ref release1);

        var gapTimeout = new InputInteractionContext(isPressed: false, isTimeout: true);
        interaction.Process(ref gapTimeout);
        Assert.True(gapTimeout.WasCanceled);

        // The sequence must have reset - a subsequent single tap alone must not complete it.
        var press2 = new InputInteractionContext(isPressed: true);
        interaction.Process(ref press2);
        var release2 = new InputInteractionContext(isPressed: false);
        interaction.Process(ref release2);
        Assert.False(release2.WasPerformed);
    }

    [Theory]
    [InlineData(PressInteractionTrigger.Press)]
    [InlineData(PressInteractionTrigger.Release)]
    [InlineData(PressInteractionTrigger.PressAndRelease)]
    public void PressInteractionAlwaysStartsOnPress(PressInteractionTrigger trigger)
    {
        var interaction = new PressInteraction(trigger);

        var press = new InputInteractionContext(isPressed: true);
        interaction.Process(ref press);
        Assert.True(press.WasStarted);
    }

    [Fact]
    public void PressInteractionDefaultsToPerformingOnPress()
    {
        var interaction = new PressInteraction();

        var press = new InputInteractionContext(isPressed: true);
        interaction.Process(ref press);
        Assert.True(press.WasStarted);
        Assert.True(press.WasPerformed);

        var release = new InputInteractionContext(isPressed: false);
        interaction.Process(ref release);
        Assert.True(release.WasCanceled);
        Assert.False(release.WasPerformed);
    }

    [Fact]
    public void PressInteractionReleaseTriggerPerformsOnlyOnRelease()
    {
        var interaction = new PressInteraction(PressInteractionTrigger.Release);

        var press = new InputInteractionContext(isPressed: true);
        interaction.Process(ref press);
        Assert.True(press.WasStarted);
        Assert.False(press.WasPerformed);

        var release = new InputInteractionContext(isPressed: false);
        interaction.Process(ref release);
        Assert.True(release.WasPerformed);
        Assert.False(release.WasCanceled);
    }

    [Fact]
    public void PressInteractionPressAndReleaseTriggerPerformsOnBothEdges()
    {
        var interaction = new PressInteraction(PressInteractionTrigger.PressAndRelease);

        var press = new InputInteractionContext(isPressed: true);
        interaction.Process(ref press);
        Assert.True(press.WasStarted);
        Assert.True(press.WasPerformed);

        var release = new InputInteractionContext(isPressed: false);
        interaction.Process(ref release);
        Assert.True(release.WasPerformed);
        Assert.False(release.WasCanceled);
    }
}
