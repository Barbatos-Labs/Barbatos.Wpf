// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.AquariusRouter.UnitTests;

public class MemoryRouterHistoryTests
{
    [Fact]
    public void StartsAtRootWithNoState()
    {
        var history = new MemoryRouterHistory();
        Assert.Equal("/", history.Location);
        Assert.Null(history.State);
    }

    [Fact]
    public void PushAddsAndMovesToANewEntry()
    {
        var history = new MemoryRouterHistory();
        history.Push("/a");
        Assert.Equal("/a", history.Location);
        history.Push("/b");
        Assert.Equal("/b", history.Location);
    }

    [Fact]
    public void ReplaceSwapsTheCurrentEntryWithoutAddingOne()
    {
        var history = new MemoryRouterHistory();
        history.Push("/a");
        history.Replace("/a-replaced");

        Assert.Equal("/a-replaced", history.Location);
        // Still only 2 entries total ("/", "/a-replaced") - Replace touched only the current
        // position, so the ROOT entry one step back is untouched and still reachable.
        Assert.Equal("/", history.Peek(-1));
        Assert.Null(history.Peek(-2));
    }

    [Fact]
    public void GoBackAndForwardMoveTheCursor()
    {
        var history = new MemoryRouterHistory();
        history.Push("/a");
        history.Push("/b");

        history.Go(-1);
        Assert.Equal("/a", history.Location);

        history.Go(1);
        Assert.Equal("/b", history.Location);
    }

    [Fact]
    public void PushAfterGoingBackDiscardsTheAbandonedForwardBranch()
    {
        var history = new MemoryRouterHistory();
        history.Push("/a");
        history.Push("/b");
        history.Go(-1); // back to /a

        history.Push("/c");

        Assert.Equal("/c", history.Location);
        Assert.Null(history.Peek(1)); // "/b" is gone - a new branch replaced it
    }

    [Fact]
    public void GoClampsToTheValidRangeInsteadOfThrowing()
    {
        var history = new MemoryRouterHistory();
        history.Push("/a");

        history.Go(-100);
        Assert.Equal("/", history.Location);

        history.Go(100);
        Assert.Equal("/a", history.Location);
    }

    [Fact]
    public void PeekOutOfRangeReturnsNullWithoutMovingAnything()
    {
        var history = new MemoryRouterHistory();
        history.Push("/a");

        Assert.Null(history.Peek(-100));
        Assert.Null(history.Peek(100));
        Assert.Equal("/a", history.Location); // unmoved
    }

    [Fact]
    public void PeekReflectsWhatGoWouldLandOnWithoutMovingTheCursor()
    {
        var history = new MemoryRouterHistory();
        history.Push("/a");
        history.Push("/b");

        Assert.Equal("/a", history.Peek(-1));
        Assert.Equal("/b", history.Location); // Peek did not move anything
    }

    [Fact]
    public void StateIsCarriedAlongsideEachEntry()
    {
        var history = new MemoryRouterHistory();
        history.Push("/a", "state-a");
        Assert.Equal("state-a", history.State);

        history.Push("/b", "state-b");
        history.Go(-1);
        Assert.Equal("state-a", history.State);
    }
}
