// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows.Input;
using System.Windows.Threading;
using Barbatos.Wpf.Input;
using Microsoft.Extensions.DependencyInjection;
using Dispatcher = Barbatos.Wpf.Dispatching.Dispatcher;

namespace Barbatos.Wpf.Core.UnitTests;

public class InputChordTests
{
    [Fact]
    public void CompletingEveryStepInOrderPerformsTheAction()
    {
        var map = new InputActionMap("Test");
        map.AddAction("Chord").AddChordBinding((Key.K, ModifierKeys.Control), (Key.C, ModifierKeys.Control));

        var service = BuildService(options => options.ActionMaps.Add(map));
        var action = service.FindAction("Chord")!;

        var events = new List<InputActionPhase>();
        action.Started += (_, e) => events.Add(e.Phase);
        action.Performed += (_, e) => events.Add(e.Phase);
        action.Canceled += (_, e) => events.Add(e.Phase);

        service.ProcessKeyDown(Key.K, ModifierKeys.Control);
        service.ProcessKeyDown(Key.C, ModifierKeys.Control);

        Assert.Equal([InputActionPhase.Started, InputActionPhase.Performed], events);
    }

    [Fact]
    public void FirstStepAloneOnlyStarts()
    {
        var map = new InputActionMap("Test");
        map.AddAction("Chord").AddChordBinding((Key.K, ModifierKeys.Control), (Key.C, ModifierKeys.Control));

        var service = BuildService(options => options.ActionMaps.Add(map));
        var action = service.FindAction("Chord")!;

        var started = 0;
        var performed = 0;
        action.Started += (_, _) => started++;
        action.Performed += (_, _) => performed++;

        service.ProcessKeyDown(Key.K, ModifierKeys.Control);

        Assert.Equal(1, started);
        Assert.Equal(0, performed);
    }

    [Fact]
    public void OffPathKeyCancelsAndTrulyResetsProgressForANewAttempt()
    {
        var map = new InputActionMap("Test");
        map.AddAction("Chord").AddChordBinding((Key.K, ModifierKeys.Control), (Key.C, ModifierKeys.Control));

        var service = BuildService(options => options.ActionMaps.Add(map));
        var action = service.FindAction("Chord")!;

        var canceled = 0;
        var performed = 0;
        action.Canceled += (_, _) => canceled++;
        action.Performed += (_, _) => performed++;

        service.ProcessKeyDown(Key.K, ModifierKeys.Control);
        service.ProcessKeyDown(Key.X); // off-path - not step 2 (C+Ctrl).

        Assert.Equal(1, canceled);
        Assert.Equal(0, performed);

        // A truly reset progress (not stuck at some other index) means a fresh full attempt
        // succeeds normally afterwards.
        service.ProcessKeyDown(Key.K, ModifierKeys.Control);
        service.ProcessKeyDown(Key.C, ModifierKeys.Control);

        Assert.Equal(1, performed);
    }

    [Fact]
    public void RemoveBindingOnAChordStopsItFromAdvancingOnTheNextKeystroke()
    {
        // Chords have their own separate OfType<ChordBinding>() dispatch path in Process(),
        // distinct from KeyBinding's - worth its own regression test rather than assuming the
        // KeyBinding-focused version of this same test generalizes.
        var map = new InputActionMap("Test");
        var action = map.AddAction("Chord");
        var chord = action.AddChordBinding((Key.K, ModifierKeys.Control), (Key.C, ModifierKeys.Control)).Bindings[0];

        var service = BuildService(options => options.ActionMaps.Add(map));

        var started = 0;
        action.Started += (_, _) => started++;

        service.ProcessKeyDown(Key.K, ModifierKeys.Control);
        Assert.Equal(1, started);

        action.RemoveBinding(chord);

        service.ProcessKeyDown(Key.K, ModifierKeys.Control);
        Assert.Equal(1, started);
    }

    [Fact]
    public void BareModifierKeyPressDoesNotCountAsAStepOrCancelAPendingChord()
    {
        var map = new InputActionMap("Test");
        map.AddAction("Chord").AddChordBinding((Key.K, ModifierKeys.Control), (Key.C, ModifierKeys.Control));

        var service = BuildService(options => options.ActionMaps.Add(map));
        var action = service.FindAction("Chord")!;

        var canceled = 0;
        var performed = 0;
        action.Canceled += (_, _) => canceled++;
        action.Performed += (_, _) => performed++;

        service.ProcessKeyDown(Key.K, ModifierKeys.Control);

        // The bare Ctrl keydown that genuinely precedes "C" in a real Ctrl+C press - must be
        // transparent, not treated as an off-path key that cancels the attempt.
        service.ProcessKeyDown(Key.LeftCtrl, ModifierKeys.Control);

        service.ProcessKeyDown(Key.C, ModifierKeys.Control);

        Assert.Equal(0, canceled);
        Assert.Equal(1, performed);
    }

    [Fact]
    public void TwoChordsSharingAFirstStepTrackIndependently()
    {
        var map = new InputActionMap("Test");
        map.AddAction("ChordA").AddChordBinding((Key.K, ModifierKeys.Control), (Key.A, ModifierKeys.None));
        map.AddAction("ChordB").AddChordBinding((Key.K, ModifierKeys.Control), (Key.B, ModifierKeys.None));

        var service = BuildService(options => options.ActionMaps.Add(map));
        var actionA = service.FindAction("ChordA")!;
        var actionB = service.FindAction("ChordB")!;

        var performedA = 0;
        var canceledB = 0;
        actionA.Performed += (_, _) => performedA++;
        actionB.Canceled += (_, _) => canceledB++;

        service.ProcessKeyDown(Key.K, ModifierKeys.Control); // both advance to step 1 independently.
        service.ProcessKeyDown(Key.A); // completes ChordA; is off-path (and so cancels) ChordB.

        Assert.Equal(1, performedA);
        Assert.Equal(1, canceledB);
    }

    [Fact]
    public void ChordAlongsideAPlainKeyBindingOnTheSameFirstKeyBothFireFromOneKeystroke()
    {
        var map = new InputActionMap("Test");
        map.AddAction("PlainSave").AddKeyBinding(Key.K, ModifierKeys.Control);
        map.AddAction("Chord").AddChordBinding((Key.K, ModifierKeys.Control), (Key.P, ModifierKeys.Control));

        var service = BuildService(options => options.ActionMaps.Add(map));
        var plainSave = service.FindAction("PlainSave")!;
        var chord = service.FindAction("Chord")!;

        var plainPerformed = 0;
        var chordStarted = 0;
        plainSave.Performed += (_, _) => plainPerformed++;
        chord.Started += (_, _) => chordStarted++;

        service.ProcessKeyDown(Key.K, ModifierKeys.Control);

        Assert.Equal(1, plainPerformed);
        Assert.Equal(1, chordStarted);
    }

    [Fact]
    public void DisabledActionMapDoesNotTrackChordProgress()
    {
        var map = new InputActionMap("Test") { Enabled = false };
        map.AddAction("Chord").AddChordBinding((Key.K, ModifierKeys.Control), (Key.C, ModifierKeys.Control));

        var service = BuildService(options => options.ActionMaps.Add(map));
        var action = service.FindAction("Chord")!;

        var started = 0;
        action.Started += (_, _) => started++;

        service.ProcessKeyDown(Key.K, ModifierKeys.Control);

        Assert.Equal(0, started);
    }

    [Fact]
    public void SetEnabledFalseResetsInProgressChordProgress()
    {
        var map = new InputActionMap("Test");
        map.AddAction("Chord").AddChordBinding((Key.K, ModifierKeys.Control), (Key.C, ModifierKeys.Control));

        var service = BuildService(options => options.ActionMaps.Add(map));
        var action = service.FindAction("Chord")!;

        var performed = 0;
        action.Performed += (_, _) => performed++;

        service.ProcessKeyDown(Key.K, ModifierKeys.Control); // progress -> 1.

        service.SetEnabled(false);
        service.SetEnabled(true);

        service.ProcessKeyDown(Key.C, ModifierKeys.Control); // would complete if progress had survived.

        Assert.Equal(0, performed);

        // The service still works normally afterwards - a fresh full attempt completes.
        service.ProcessKeyDown(Key.K, ModifierKeys.Control);
        service.ProcessKeyDown(Key.C, ModifierKeys.Control);

        Assert.Equal(1, performed);
    }

    [Fact]
    public void FirstStepAloneReallyCancelsAfterStepTimeoutElapses()
    {
        // Unlike every test above (which drives the state machine directly, synchronously), this
        // one exercises a REAL elapsed dispatcher timeout end to end - no existing interaction
        // test (Hold/Tap/MultiTap) does this; they all call .Process(ref context) directly.
        var map = new InputActionMap("Test");
        map.AddAction("Chord").AddChordBinding(TimeSpan.FromMilliseconds(50), (Key.K, ModifierKeys.Control), (Key.C, ModifierKeys.Control));

        var service = BuildService(options => options.ActionMaps.Add(map));
        var action = service.FindAction("Chord")!;

        var canceled = 0;
        action.Canceled += (_, _) => canceled++;

        service.ProcessKeyDown(Key.K, ModifierKeys.Control);

        PumpFor(TimeSpan.FromMilliseconds(300)); // comfortably longer than the 50ms StepTimeout.

        Assert.Equal(1, canceled);
    }

    [Fact]
    public void StepTimeoutReArmsOnEveryNonFinalStepForA3StepChord()
    {
        // Regression test for a real bug caught before shipping: the step timeout must re-arm on
        // EVERY successful non-final advance, not just the first step, or a 3+-step chord's total
        // time budget is wrongly capped at one StepTimeout from the very first key. Each gap here
        // (60ms) is comfortably under the 100ms StepTimeout, but the cumulative time from step 1
        // to step 3 (~120ms) is over it - this only completes if the timeout re-armed at step 2.
        var map = new InputActionMap("Test");
        map.AddAction("Chord").AddChordBinding(
            TimeSpan.FromMilliseconds(100),
            (Key.K, ModifierKeys.Control), (Key.C, ModifierKeys.Control), (Key.P, ModifierKeys.Control));

        var service = BuildService(options => options.ActionMaps.Add(map));
        var action = service.FindAction("Chord")!;

        var performed = 0;
        var canceled = 0;
        action.Performed += (_, _) => performed++;
        action.Canceled += (_, _) => canceled++;

        service.ProcessKeyDown(Key.K, ModifierKeys.Control);
        PumpFor(TimeSpan.FromMilliseconds(60)); // pumped, not just slept - lets a wrongly-firing timeout actually run.

        service.ProcessKeyDown(Key.C, ModifierKeys.Control);
        PumpFor(TimeSpan.FromMilliseconds(60));

        service.ProcessKeyDown(Key.P, ModifierKeys.Control);

        Assert.Equal(1, performed);
        Assert.Equal(0, canceled);
    }

    /// <summary>Pumps the current thread's real dispatcher for at least <paramref name="duration"/> - the same <c>CreateTimer</c>/<c>DispatcherFrame</c>/<c>PushFrame</c> pattern as <c>DispatchingTests.NonRepeatingTimerStopsAfterFirstTick</c>, factored out here since every real-timeout test in this file needs it.</summary>
    static void PumpFor(TimeSpan duration)
    {
        var dispatcher = Dispatcher.GetForCurrentThread()!;
        var pumpTimer = dispatcher.CreateTimer();
        pumpTimer.Interval = duration;
        pumpTimer.IsRepeating = false;

        var frame = new DispatcherFrame();
        pumpTimer.Tick += (_, _) => frame.Continue = false;
        pumpTimer.Start();

        System.Windows.Threading.Dispatcher.PushFrame(frame);
    }

    static IInputSystemService BuildService(Action<InputSystemOptions>? configure = null)
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IHotkeyPlatform>(new FakeHotkeyPlatform());
        builder.ConfigureInputSystem(configure);
        var wpfApp = builder.Build();

        return wpfApp.Services.GetRequiredService<IInputSystemService>();
    }
}
