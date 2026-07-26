// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.AquariusRouter.Routing;

/// <summary>
/// Mirrors Vue Router's in-component <c>beforeRouteEnter</c>: a guard checked against a
/// ViewModel that is about to be mounted for a newly-entered <see cref="RouteRecord"/>.
/// </summary>
/// <remarks>
/// <b>Unlike Vue Router</b>, this fires with a real <see langword="this"/> - <c>to.Params</c> is
/// available directly, no <c>next(vm =&gt; ...)</c> callback-capture dance needed. Vue Router only
/// needs that dance because it defers creating the entering component instance until *after*
/// every guard resolves (including async component loading); a WPF View's <c>DataContext</c> is
/// already resolved (via <see cref="Barbatos.Wpf.Aquarius.Composition.Setup"/>) by the time <see cref="Router"/>
/// constructs it and checks this hook, so there is no such gap to work around.
/// <para>
/// A ViewModel that just needs its own route params, with nothing to guard/cancel, doesn't need
/// to implement this at all - see <see cref="IOnRouteEnter"/> instead.
/// </para>
/// </remarks>
public interface IOnBeforeRouteEnter
{
    /// <inheritdoc cref="IOnBeforeRouteEnter"/>
    /// <param name="to">The location being navigated to (this record is one of <see cref="RouteLocation.Matched"/>).</param>
    /// <param name="from">The location being navigated away from.</param>
    Task<NavigationGuardResult> OnBeforeRouteEnter(RouteLocation to, RouteLocation from);
}

/// <summary>
/// Mirrors Vue Router's in-component <c>beforeRouteUpdate</c>: a guard checked against a
/// ViewModel that stays mounted across a navigation because the same <see cref="RouteRecord"/>
/// matched both before and after (only its params/query changed) - e.g. moving from
/// <c>/users/1</c> to <c>/users/2</c>.
/// </summary>
public interface IOnBeforeRouteUpdate
{
    /// <inheritdoc cref="IOnBeforeRouteUpdate"/>
    /// <param name="to">The location being navigated to.</param>
    /// <param name="from">The location being navigated away from.</param>
    Task<NavigationGuardResult> OnBeforeRouteUpdate(RouteLocation to, RouteLocation from);
}

/// <summary>
/// Mirrors Vue Router's in-component <c>beforeRouteLeave</c>: a guard checked against a
/// currently-mounted ViewModel whose <see cref="RouteRecord"/> is not part of the new location at
/// all. The common use is confirming an unsaved-changes prompt before letting the user navigate
/// away - <see cref="NavigationGuardResult.Cancel"/> keeps the current View mounted exactly as if
/// nothing happened.
/// </summary>
public interface IOnBeforeRouteLeave
{
    /// <inheritdoc cref="IOnBeforeRouteLeave"/>
    /// <param name="to">The location being navigated to.</param>
    /// <param name="from">The location being navigated away from (this record is one of <see cref="RouteLocation.Matched"/>).</param>
    Task<NavigationGuardResult> OnBeforeRouteLeave(RouteLocation to, RouteLocation from);
}

/// <summary>
/// A non-cancelling companion to <see cref="IOnBeforeRouteEnter"/>: fires unconditionally right
/// before it, for a ViewModel that just needs <c>to.Params</c>/<c>to.Query</c> to initialize
/// itself and has nothing to guard or redirect. The closest analogue of Vue Router's
/// <c>props: true</c> (spreading <c>route.params</c> onto a component) without a second,
/// parallel prop-binding mechanism - implement this and assign to your own properties directly.
/// </summary>
public interface IOnRouteEnter
{
    /// <inheritdoc cref="IOnRouteEnter"/>
    /// <param name="to">The location being navigated to (this record is one of <see cref="RouteLocation.Matched"/>).</param>
    /// <param name="from">The location being navigated away from.</param>
    void OnRouteEnter(RouteLocation to, RouteLocation from);
}

/// <summary>
/// The non-cancelling companion to <see cref="IOnBeforeRouteUpdate"/> - fires unconditionally
/// right before it, for a ViewModel that just needs to re-read <c>to.Params</c> when its own
/// record is reused across a param-only navigation (e.g. <c>/users/1</c> to <c>/users/2</c>).
/// </summary>
public interface IOnRouteUpdate
{
    /// <inheritdoc cref="IOnRouteUpdate"/>
    /// <param name="to">The location being navigated to.</param>
    /// <param name="from">The location being navigated away from.</param>
    void OnRouteUpdate(RouteLocation to, RouteLocation from);
}
