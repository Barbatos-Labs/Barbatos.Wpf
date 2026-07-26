// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.AquariusRouter.Routing;

/// <summary>
/// The navigation-stack storage <see cref="Router"/> is built on - the WPF/C# counterpart of Vue
/// Router's own <c>RouterHistory</c> abstraction (which <c>html5</c>/<c>hash</c>/<c>memory</c>
/// history all implement). <see cref="MemoryRouterHistory"/> is the only implementation this
/// package ships, since a desktop app has no browser URL/address bar for an <c>html5</c>- or
/// <c>hash</c>-mode equivalent to shadow - but the abstraction stays pluggable, e.g. for a custom
/// implementation that persists the stack across app restarts.
/// </summary>
/// <remarks>
/// Deliberately a purer, simpler shape than Vue Router's own history interface: it deals only in
/// raw path strings and opaque state, has no <c>listen</c>/event mechanism, and
/// <see cref="Peek"/> has no Vue Router equivalent at all. That's because <see cref="Router"/>'s
/// own <see cref="Router.Go"/> runs the full navigation-guard pipeline <b>before</b> ever moving
/// the stack (only committing via <see cref="Go"/> once a guard hasn't cancelled/redirected it) -
/// <see cref="Peek"/> is what lets it find out which location a given <see cref="Router.Go"/>
/// delta would land on without moving anything yet. This sidesteps a whole class of problem Vue
/// Router's own <c>html5</c> history has to solve (a real browser back/forward button has
/// already changed the URL before Vue Router's <c>popstate</c> listener even fires, so a
/// cancelled guard needs an explicit "undo" <c>history.go()</c> in the opposite direction) -
/// nothing external can move a <see cref="MemoryRouterHistory"/>'s stack out-of-band, so there is
/// nothing to undo.
/// </remarks>
public interface IRouterHistory
{
    /// <summary>The path string at the current position.</summary>
    string Location { get; }

    /// <summary>Opaque data associated with the current position, if any was passed to <see cref="Push"/>/<see cref="Replace"/>.</summary>
    object? State { get; }

    /// <summary>Pushes a new entry, discarding any entries currently ahead of it (the usual "back then navigate somewhere new" behavior).</summary>
    void Push(string location, object? state = null);

    /// <summary>Replaces the entry at the current position in place, without adding a new one.</summary>
    void Replace(string location, object? state = null);

    /// <summary>
    /// The location <paramref name="delta"/> steps from the current position, without moving
    /// anything - or <see langword="null"/> if that position is out of range (matching Vue
    /// Router's own documented behavior: an out-of-range <see cref="Go"/> "fails silently").
    /// </summary>
    string? Peek(int delta);

    /// <summary>Moves the current position by <paramref name="delta"/>, clamped to the valid range.</summary>
    void Go(int delta);
}
