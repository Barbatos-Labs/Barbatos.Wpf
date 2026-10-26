// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows.Input;

namespace Barbatos.Wpf.Input;

/// <summary>
/// One way an <see cref="InputAction"/> can be triggered - a <see cref="SingleKeyBinding"/>
/// (one key/modifier combination, pressed at once - <see cref="KeyBinding"/> or
/// <see cref="GlobalKeyBinding"/>) or a <see cref="ChordBinding"/> (several combinations,
/// pressed one after another - "Ctrl+K, Ctrl+C").
/// </summary>
public abstract class InputBinding
{
    // Closes the hierarchy to exactly the members declared in this file - see SingleKeyBinding's
    // own remarks for why.
    private protected InputBinding()
    {
    }
}

/// <summary>
/// A single key/modifier combination pressed all at once - <see cref="KeyBinding"/> (in-app) or
/// <see cref="GlobalKeyBinding"/> (OS-level).
/// </summary>
/// <remarks>
/// Deliberately just <see cref="Key"/> + <see cref="ModifierKeys"/>, not Unity Input System's
/// wildcard control-path DSL (<c>&lt;Keyboard&gt;/f8</c>) or its generic multi-part Composite
/// Binding framework - WPF's own <see cref="ModifierKeys"/> is already a <see langword="flags"/>
/// enum, so <c>new KeyBinding(Key.F8, ModifierKeys.Control)</c> already <i>is</i> Unity's
/// <c>OneModifierComposite</c> ("Ctrl+F8") in one binding; there is no closed device/control set
/// to query over in the first place. Matching is always exact - <see cref="ModifierKeys.None"/>
/// does not match while any modifier is held, the same way WPF's own native <see cref="KeyGesture"/>
/// behaves - which is what mostly avoids Unity's own "F8 vs Ctrl+F8" shortcut-disambiguation
/// problem by construction, instead of needing to port its complexity-ordering/Priority system.
/// <see langword="private protected"/> keeps this hierarchy closed to <see cref="KeyBinding"/>/
/// <see cref="GlobalKeyBinding"/> - the same reasoning that already applied to
/// <see cref="InputBinding"/> itself before <see cref="ChordBinding"/> existed.
/// </remarks>
public abstract class SingleKeyBinding : InputBinding
{
    private protected SingleKeyBinding(Key key, ModifierKeys modifiers, IInputInteraction? interaction)
    {
        if (key == Key.None)
            throw new ArgumentException("A binding requires a key.", nameof(key));

        Key = key;
        Modifiers = modifiers;
        Interaction = interaction;
    }

    /// <summary>The key that triggers this binding. Never <see cref="Key.None"/>.</summary>
    public Key Key { get; }

    /// <summary>The exact modifier combination that must be held with <see cref="Key"/> - see this type's remarks on exact matching.</summary>
    public ModifierKeys Modifiers { get; }

    /// <summary>
    /// Drives this binding's <see cref="InputActionPhase"/> transitions over time (e.g. a hold/
    /// tap/multi-tap gesture). <see langword="null"/> (the default) behaves like
    /// <see cref="DefaultInteraction"/> - <c>Started</c>+<c>Performed</c> together on
    /// press, <c>Canceled</c> on release. Always <see langword="null"/> on a
    /// <see cref="GlobalKeyBinding"/> - see its own remarks for why.
    /// </summary>
    public IInputInteraction? Interaction { get; }

    /// <summary>Exact match only - see this type's own remarks.</summary>
    public bool Matches(Key key, ModifierKeys modifiers) => Key == key && Modifiers == modifiers;
}

/// <summary>An in-app shortcut - fires only while its <see cref="InputActionMap.Scope"/> has keyboard focus (or the whole application does, when <see cref="InputActionMap.Scope"/> is <see langword="null"/>).</summary>
public sealed class KeyBinding : SingleKeyBinding
{
    /// <summary>Creates an in-app binding.</summary>
    /// <param name="key">The key that triggers the binding.</param>
    /// <param name="modifiers">The exact modifier combination that must be held with <paramref name="key"/>.</param>
    /// <param name="interaction">The gesture to apply, or <see langword="null"/> for <see cref="DefaultInteraction"/>'s behavior.</param>
    /// <exception cref="ArgumentException"><paramref name="key"/> is <see cref="Key.None"/>.</exception>
    public KeyBinding(Key key, ModifierKeys modifiers = ModifierKeys.None, IInputInteraction? interaction = null)
        : base(key, modifiers, interaction)
    {
    }
}

/// <summary>
/// A true OS-level global hotkey, backed by the Win32 <c>RegisterHotKey</c> API - fires even
/// while this application is unfocused or minimized.
/// </summary>
/// <remarks>
/// Cannot carry an <see cref="SingleKeyBinding.Interaction"/>: <c>RegisterHotKey</c>/<c>WM_HOTKEY</c>
/// is a single edge-triggered notification per physical press (registered with
/// <c>MOD_NOREPEAT</c>), with no corresponding "key released" message - a hard OS-level
/// constraint, not a design choice. Hold/Tap/MultiTap/release-triggered Press semantics have no
/// signal to work from outside an in-app <see cref="KeyBinding"/>. For the same reason there is
/// no global counterpart of <see cref="ChordBinding"/> either - a chord is tracked entirely from
/// WPF's own key events, which never arrive while unfocused in the first place.
/// </remarks>
public sealed class GlobalKeyBinding : SingleKeyBinding
{
    /// <summary>Creates an OS-level global hotkey binding.</summary>
    /// <param name="key">The key that triggers the hotkey.</param>
    /// <param name="modifiers">The exact modifier combination that must be held with <paramref name="key"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="key"/> is <see cref="Key.None"/>.</exception>
    public GlobalKeyBinding(Key key, ModifierKeys modifiers = ModifierKeys.None)
        : base(key, modifiers, interaction: null)
    {
    }
}

/// <summary>
/// A sequence of key/modifier combinations pressed one after another, each within
/// <see cref="StepTimeout"/> of the previous one - VS Code's "leader key" shortcuts
/// (<c>Ctrl+K</c>, then <c>Ctrl+C</c>) are the model. In-app only - see
/// <see cref="GlobalKeyBinding"/>'s own remarks for why there is no global equivalent.
/// </summary>
/// <remarks>
/// No <see cref="SingleKeyBinding.Interaction"/> slot: the sequence itself already is the
/// gesture, there is nothing left for a Hold/Tap/MultiTap reinterpretation to apply to. An
/// off-sequence key at any step cancels the whole attempt back to step zero - the same key
/// pressed again afterwards is treated as a fresh attempt at step one, not silently checked
/// against step one in the same keystroke that just canceled it (which would fire
/// <c>Canceled</c> then <c>Started</c> from one physical keystroke). A bare modifier press
/// (<c>Key.LeftCtrl</c>/<c>RightCtrl</c>/etc., which genuinely arrives as its own key event
/// before the "K" in "Ctrl+K" does) never counts as a step and never cancels a pending
/// sequence - the same way it's already a harmless no-op against a plain <see cref="KeyBinding"/>,
/// since nothing is ever bound directly to a bare modifier key.
/// </remarks>
public sealed class ChordBinding : InputBinding
{
    /// <summary>The default <see cref="StepTimeout"/> - 1.5 seconds, comfortably slower than a deliberate two-key sequence but fast enough that an unrelated later keypress won't be mistaken for a stale step.</summary>
    public static readonly TimeSpan DefaultStepTimeout = TimeSpan.FromSeconds(1.5);

    /// <summary>Creates a chord from <paramref name="steps"/>, with <see cref="DefaultStepTimeout"/> between steps.</summary>
    /// <param name="steps">The combinations to press in order - at least two.</param>
    /// <exception cref="ArgumentNullException"><paramref name="steps"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Fewer than two steps, or a step whose key is <see cref="Key.None"/>.</exception>
    public ChordBinding(params (Key Key, ModifierKeys Modifiers)[] steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        if (steps.Length < 2)
            throw new ArgumentException("A chord requires at least two steps - a single step is an ordinary KeyBinding.", nameof(steps));

        foreach (var step in steps)
            if (step.Key == Key.None)
                throw new ArgumentException("A chord step requires a key.", nameof(steps));

        Steps = steps;
    }

    /// <summary>The combinations that must be pressed in order, each within <see cref="StepTimeout"/> of the previous one.</summary>
    public IReadOnlyList<(Key Key, ModifierKeys Modifiers)> Steps { get; }

    /// <summary>How long to wait for the next step before canceling the attempt. Defaults to <see cref="DefaultStepTimeout"/>; re-armed after every successful non-final step, not just the first.</summary>
    public TimeSpan StepTimeout { get; set; } = DefaultStepTimeout;

    /// <summary>Whether <paramref name="key"/>/<paramref name="modifiers"/> matches the step at <paramref name="stepIndex"/>. Always <see langword="false"/> for an out-of-range index, rather than throwing - convenient for a caller tracking progress as a plain <see langword="int"/>.</summary>
    public bool MatchesStep(int stepIndex, Key key, ModifierKeys modifiers) =>
        stepIndex >= 0 && stepIndex < Steps.Count && Steps[stepIndex].Key == key && Steps[stepIndex].Modifiers == modifiers;
}
