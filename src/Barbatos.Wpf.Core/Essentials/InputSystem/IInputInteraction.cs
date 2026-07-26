// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Input;

/// <summary>
/// A pluggable gesture state machine attached to a <see cref="KeyBinding"/>, reinterpreting raw
/// press/release into something like "held for 2 seconds" or "tapped twice" - ported near-verbatim
/// from Unity Input System's own <c>IInputInteraction</c>/<c>Process(ref context)</c> shape. See
/// <see cref="HoldInteraction"/>, <see cref="TapInteraction"/>, <see cref="MultiTapInteraction"/>,
/// <see cref="PressInteraction"/> for the built-ins, and <see cref="DefaultInteraction"/> for the
/// implicit behavior when <see cref="SingleKeyBinding.Interaction"/> is left <see langword="null"/>.
/// </summary>
public interface IInputInteraction
{
    /// <summary>
    /// Called whenever the bound key's pressed state changes, and again whenever a previously
    /// requested <see cref="InputInteractionContext.SetTimeout"/> elapses unsuperseded. Drive
    /// phase transitions via <paramref name="context"/>'s <c>Started</c>/<c>Performed</c>/<c>Canceled</c>
    /// methods - all three are independent flags, not a single overwritable result, so it is
    /// valid (and common - see <see cref="DefaultInteraction"/>) to call more than one in the
    /// same <see cref="Process"/> call, e.g. <c>Started</c> immediately followed by <c>Performed</c>.
    /// </summary>
    void Process(ref InputInteractionContext context);

    /// <summary>Called when <see cref="IInputSystemService.SetEnabled"/> turns the whole feature off - discard any in-progress state (e.g. a MultiTap mid-sequence).</summary>
    void Reset();
}

/// <summary>
/// Carries one raw key-state change (or elapsed-timeout follow-up) into
/// <see cref="IInputInteraction.Process"/>, and carries the interaction's decision back out.
/// </summary>
public struct InputInteractionContext
{
    public InputInteractionContext(bool isPressed, bool isTimeout = false)
    {
        IsPressed = isPressed;
        IsTimeout = isTimeout;
    }

    /// <summary>Whether the bound key is currently physically held down.</summary>
    public bool IsPressed { get; }

    /// <summary>Whether this call is a follow-up from an elapsed <see cref="SetTimeout"/> rather than an actual key-state change.</summary>
    public bool IsTimeout { get; }

    /// <summary>Whether <see cref="Started"/> was called during this <see cref="IInputInteraction.Process"/> call.</summary>
    public bool WasStarted { get; private set; }

    /// <summary>Whether <see cref="Performed"/> was called during this <see cref="IInputInteraction.Process"/> call.</summary>
    public bool WasPerformed { get; private set; }

    /// <summary>Whether <see cref="Canceled"/> was called during this <see cref="IInputInteraction.Process"/> call.</summary>
    public bool WasCanceled { get; private set; }

    /// <summary>The duration passed to <see cref="SetTimeout"/> during this call, if any.</summary>
    public TimeSpan? RequestedTimeout { get; private set; }

    public void Started() => WasStarted = true;

    public void Performed() => WasPerformed = true;

    public void Canceled() => WasCanceled = true;

    /// <summary>Requests a follow-up <see cref="IInputInteraction.Process"/> call after <paramref name="timeout"/> elapses, unless superseded by a newer key event first.</summary>
    public void SetTimeout(TimeSpan timeout) => RequestedTimeout = timeout;
}
