// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.AquariusRouter.Routing;

/// <summary>
/// A navigation guard - global (<see cref="Router.BeforeEach"/>/<see cref="Router.BeforeResolve"/>),
/// per-route (<see cref="RouteRecord.BeforeEnter"/>), or in-component (<see cref="IOnBeforeRouteEnter"/>
/// and friends). Always <see langword="async"/>, unlike Vue Router's guards (which accept a plain
/// synchronous return too) - <see cref="Router"/>'s own pipeline awaits every guard unconditionally
/// either way, sync or not, so a C# guard with nothing to await can simply
/// <c>return Task.FromResult(NavigationGuardResult.Continue)</c> (or use the implicit
/// <see langword="bool"/> conversion, <c>return true;</c>, inside an <see langword="async"/> method).
/// </summary>
/// <param name="to">The location being navigated to.</param>
/// <param name="from">The location being navigated away from.</param>
public delegate Task<NavigationGuardResult> NavigationGuard(RouteLocation to, RouteLocation from);

/// <summary>A non-cancelling observer of a completed navigation - see <see cref="Router.AfterEach"/>.</summary>
/// <param name="to">The location navigated to.</param>
/// <param name="from">The location navigated away from.</param>
/// <param name="failure">
/// <see langword="null"/> on a successful navigation, otherwise why it didn't happen - see
/// <see cref="NavigationFailure"/>.
/// </param>
public delegate void NavigationHook(RouteLocation to, RouteLocation from, NavigationFailure? failure);

/// <summary>What a <see cref="NavigationGuard"/> decided - see <see cref="Continue"/>/<see cref="Cancel"/>/<see cref="Redirect"/>.</summary>
public enum NavigationGuardEffect
{
    /// <summary>Let the navigation proceed to the next guard/pipeline stage.</summary>
    Continue,

    /// <summary>Abort the navigation - surfaces as a <see cref="NavigationFailureType.Aborted"/> failure.</summary>
    Cancel,

    /// <summary>Abandon this navigation and start a new one at <see cref="NavigationGuardResult.RedirectTo"/> instead.</summary>
    Redirect,
}

/// <summary>
/// The result of a <see cref="NavigationGuard"/> - the WPF/C# counterpart of what Vue Router's
/// own guards return (<see langword="void"/>/<see langword="true"/>, <see langword="false"/>, or
/// a new route location; a thrown <see cref="Exception"/> plays the same role a thrown
/// <c>Error</c> does in Vue Router, so it isn't one of these cases - let it propagate normally).
/// </summary>
public readonly struct NavigationGuardResult
{
    private NavigationGuardResult(NavigationGuardEffect effect, RouteLocationRaw? redirectTo)
    {
        Effect = effect;
        RedirectTo = redirectTo;
    }

    /// <summary>What this result means for the navigation - see <see cref="NavigationGuardEffect"/>.</summary>
    public NavigationGuardEffect Effect { get; }

    /// <summary>The new target, when <see cref="Effect"/> is <see cref="NavigationGuardEffect.Redirect"/> - <see langword="null"/> otherwise.</summary>
    public RouteLocationRaw? RedirectTo { get; }

    /// <summary>Let the navigation proceed.</summary>
    public static NavigationGuardResult Continue { get; } = new(NavigationGuardEffect.Continue, null);

    /// <summary>Abort the navigation.</summary>
    public static NavigationGuardResult Cancel { get; } = new(NavigationGuardEffect.Cancel, null);

    /// <summary>Redirect the navigation to <paramref name="to"/> instead.</summary>
    public static NavigationGuardResult Redirect(RouteLocationRaw to) => new(NavigationGuardEffect.Redirect, to);

    /// <summary>Shorthand matching Vue Router's own guard return convention: <see langword="true"/>/<see langword="false"/> for <see cref="Continue"/>/<see cref="Cancel"/>.</summary>
    public static implicit operator NavigationGuardResult(bool shouldContinue) => shouldContinue ? Continue : Cancel;
}
