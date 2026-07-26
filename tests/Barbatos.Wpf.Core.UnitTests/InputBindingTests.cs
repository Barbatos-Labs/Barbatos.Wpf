// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows.Input;
using Barbatos.Wpf.Input;
// WPF's own System.Windows.Input.KeyBinding collides with Barbatos.Wpf.Input.KeyBinding - this
// alias resolves every unqualified KeyBinding reference in this file to ours.
using KeyBinding = Barbatos.Wpf.Input.KeyBinding;

namespace Barbatos.Wpf.Core.UnitTests;

public class InputBindingTests
{
    [Fact]
    public void KeyBindingRejectsKeyNone()
    {
        Assert.Throws<ArgumentException>(() => new KeyBinding(Key.None));
    }

    [Fact]
    public void GlobalKeyBindingRejectsKeyNone()
    {
        Assert.Throws<ArgumentException>(() => new GlobalKeyBinding(Key.None));
    }

    [Fact]
    public void GlobalKeyBindingNeverHasAnInteraction()
    {
        var binding = new GlobalKeyBinding(Key.F8, ModifierKeys.Control);

        Assert.Null(binding.Interaction);
    }

    [Fact]
    public void MatchesRequiresTheExactSameModifiers()
    {
        var binding = new KeyBinding(Key.F8, ModifierKeys.Control);

        Assert.True(binding.Matches(Key.F8, ModifierKeys.Control));
        Assert.False(binding.Matches(Key.F8, ModifierKeys.None));
        Assert.False(binding.Matches(Key.F8, ModifierKeys.Control | ModifierKeys.Shift));
        Assert.False(binding.Matches(Key.F9, ModifierKeys.Control));
    }

    [Fact]
    public void NoModifiersBindingDoesNotMatchWhileAnyModifierIsHeld()
    {
        // The mechanism that mostly eliminates "F8 vs Ctrl+F8" shortcut-disambiguation by
        // construction - see InputBinding's own remarks.
        var binding = new KeyBinding(Key.F8);

        Assert.True(binding.Matches(Key.F8, ModifierKeys.None));
        Assert.False(binding.Matches(Key.F8, ModifierKeys.Control));
        Assert.False(binding.Matches(Key.F8, ModifierKeys.Shift));
    }

    [Fact]
    public void KeyBindingAndGlobalKeyBindingAreStillSingleKeyBindings()
    {
        // Regression guard for the SingleKeyBinding intermediate-class refactor - anything that
        // did `.OfType<KeyBinding>()`/`.OfType<GlobalKeyBinding>()` before must keep working.
        Assert.IsAssignableFrom<SingleKeyBinding>(new KeyBinding(Key.F8));
        Assert.IsAssignableFrom<SingleKeyBinding>(new GlobalKeyBinding(Key.F8));
    }

    [Fact]
    public void ChordBindingRejectsFewerThanTwoSteps()
    {
        Assert.Throws<ArgumentException>(() => new ChordBinding((Key.K, ModifierKeys.Control)));
        Assert.Throws<ArgumentException>(() => new ChordBinding());
    }

    [Fact]
    public void ChordBindingRejectsKeyNoneInAnyStep()
    {
        Assert.Throws<ArgumentException>(() => new ChordBinding((Key.None, ModifierKeys.Control), (Key.C, ModifierKeys.Control)));
        Assert.Throws<ArgumentException>(() => new ChordBinding((Key.K, ModifierKeys.Control), (Key.None, ModifierKeys.Control)));
    }

    [Fact]
    public void ChordBindingPreservesStepOrder()
    {
        var chord = new ChordBinding((Key.K, ModifierKeys.Control), (Key.C, ModifierKeys.Control), (Key.P, ModifierKeys.None));

        Assert.Equal(3, chord.Steps.Count);
        Assert.Equal((Key.K, ModifierKeys.Control), chord.Steps[0]);
        Assert.Equal((Key.C, ModifierKeys.Control), chord.Steps[1]);
        Assert.Equal((Key.P, ModifierKeys.None), chord.Steps[2]);
    }

    [Fact]
    public void ChordBindingStepTimeoutDefaultsAndIsSettable()
    {
        var chord = new ChordBinding((Key.K, ModifierKeys.Control), (Key.C, ModifierKeys.Control));
        Assert.Equal(ChordBinding.DefaultStepTimeout, chord.StepTimeout);

        chord.StepTimeout = TimeSpan.FromMilliseconds(250);
        Assert.Equal(TimeSpan.FromMilliseconds(250), chord.StepTimeout);
    }

    [Fact]
    public void ChordBindingMatchesStepChecksTheExactStep()
    {
        var chord = new ChordBinding((Key.K, ModifierKeys.Control), (Key.C, ModifierKeys.Control));

        Assert.True(chord.MatchesStep(0, Key.K, ModifierKeys.Control));
        Assert.False(chord.MatchesStep(0, Key.C, ModifierKeys.Control));
        Assert.False(chord.MatchesStep(0, Key.K, ModifierKeys.None));
        Assert.True(chord.MatchesStep(1, Key.C, ModifierKeys.Control));
    }

    [Fact]
    public void ChordBindingMatchesStepReturnsFalseForAnOutOfRangeIndex()
    {
        var chord = new ChordBinding((Key.K, ModifierKeys.Control), (Key.C, ModifierKeys.Control));

        Assert.False(chord.MatchesStep(-1, Key.K, ModifierKeys.Control));
        Assert.False(chord.MatchesStep(2, Key.K, ModifierKeys.Control));
    }
}
