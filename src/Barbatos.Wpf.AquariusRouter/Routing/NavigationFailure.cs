// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.AquariusRouter.Routing;

/// <summary>
/// Why a navigation didn't happen - the WPF/C# counterpart of Vue Router's
/// <c>NavigationFailureType</c>. A real bitmask (not sequential values), matching Vue Router's
/// own actual values exactly, so <see cref="NavigationFailures.IsNavigationFailure"/> can test
/// more than one type in a single call: <c>IsNavigationFailure(failure, Aborted | Cancelled)</c>.
/// </summary>
[Flags]
public enum NavigationFailureType
{
    /// <summary>A guard returned/redirected to <see cref="NavigationGuardResult.Cancel"/>.</summary>
    Aborted = 4,

    /// <summary>A newer navigation started before this one finished - see <see cref="Router.Push"/>'s remarks on this race.</summary>
    Cancelled = 8,

    /// <summary>The resolved target is <see cref="RouteLocation.IsSameLocation"/> as the current route, and <see cref="RouteLocationRaw.Force"/> wasn't set.</summary>
    Duplicated = 16,
}

/// <summary>
/// Why <see cref="Router.Push"/>/<see cref="Router.Replace"/>/<see cref="Router.Go"/> didn't
/// result in a navigation - returned instead of throwing, since "the user stayed where they
/// were" is an ordinary, expected outcome (an unsaved-changes leave-guard cancelling is not a
/// bug), not an exceptional one. A guard that throws a real <see cref="Exception"/> is not
/// wrapped as one of these - it propagates as-is, matching Vue Router's own real distinction
/// between a guard returning <see langword="false"/>/redirecting and a guard throwing.
/// </summary>
public sealed record NavigationFailure
{
    /// <summary>Which kind of failure this is.</summary>
    public required NavigationFailureType Type { get; init; }

    /// <summary>The location that was being navigated to.</summary>
    public required RouteLocation To { get; init; }

    /// <summary>The location that was being navigated away from.</summary>
    public required RouteLocation From { get; init; }
}

/// <summary>Helpers for working with a <see cref="NavigationFailure"/>.</summary>
public static class NavigationFailures
{
    /// <summary>
    /// Whether <paramref name="failure"/> is non-<see langword="null"/> and, if <paramref name="type"/>
    /// is given, whether its <see cref="NavigationFailure.Type"/> includes at least one of the
    /// requested flags.
    /// </summary>
    public static bool IsNavigationFailure(NavigationFailure? failure, NavigationFailureType? type = null) =>
        failure is not null && (type is null || (failure.Type & type.Value) != 0);
}
