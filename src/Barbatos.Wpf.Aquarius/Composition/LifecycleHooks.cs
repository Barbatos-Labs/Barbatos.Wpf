// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Composition;

/// <summary>
/// Vue's Composition API hooks (<c>onMounted</c>, <c>onUnmounted</c>, ...) run inside a
/// component's <c>setup()</c> - the WPF/MVVM analogue of <c>setup()</c> is the
/// ViewModel. Each hook below is its own tiny interface so a ViewModel opts into exactly
/// the ones it needs, the same way a Vue component only imports the hooks it uses.
/// Attaching <see cref="Lifecycle.EnableProperty"/> to a <see cref="System.Windows.FrameworkElement"/>
/// is what actually invokes whichever of these its <c>DataContext</c> implements.
/// Mirrors Vue's <c>beforeCreate</c>.
/// </summary>
/// <remarks>
/// Since a ViewModel's constructor has necessarily already run by the time it can be
/// observed as a <c>DataContext</c> at all - there is no WPF equivalent of hooking in
/// earlier than that - this fires at the same first opportunity <see cref="IOnBeforeMount"/>
/// does (<c>Initialized</c>, or as a guaranteed fallback on <c>Loaded</c>), just one step
/// before it.
/// </remarks>
public interface IOnBeforeCreate
{
    /// <inheritdoc cref="IOnBeforeCreate"/>
    void OnBeforeCreate();
}

/// <inheritdoc cref="IOnBeforeCreate"/>
/// <remarks>
/// The async counterpart of <see cref="IOnBeforeCreate"/> - see
/// <see cref="IOnMountedAsync"/> for the shared rules every <c>*Async</c> hook interface
/// follows (fire-and-forget with respect to the rest of the lifecycle, exceptions routed
/// through <see cref="IOnErrorCaptured"/>). Implement this one instead of the sync version
/// when the work needs <see langword="await"/>; implement both only if you genuinely need
/// two independent calls, which is unusual.
/// </remarks>
public interface IOnBeforeCreateAsync
{
    /// <inheritdoc cref="IOnBeforeCreateAsync"/>
    Task OnBeforeCreateAsync();
}

/// <summary>
/// Mirrors Vue's <c>created</c>: fires immediately after <see cref="IOnBeforeCreate"/>,
/// still before <see cref="IOnBeforeMount"/>. Vue itself does reactive-system setup work
/// between the two - a ViewModel's own constructor (and, for
/// <see cref="Barbatos.Wpf.Reactivity"/> types, its base class) already did the
/// equivalent work before either hook could fire here, so nothing observable happens
/// between them in this port.
/// </summary>
public interface IOnCreated
{
    /// <inheritdoc cref="IOnCreated"/>
    void OnCreated();
}

/// <inheritdoc cref="IOnCreated"/>
/// <remarks>See <see cref="IOnMountedAsync"/> for the shared <c>*Async</c> hook rules.</remarks>
public interface IOnCreatedAsync
{
    /// <inheritdoc cref="IOnCreatedAsync"/>
    Task OnCreatedAsync();
}

/// <summary>
/// Mirrors <c>onBeforeMount</c>: the view exists (its constructor/XAML has run) but
/// is not yet part of a live visual tree.
/// </summary>
public interface IOnBeforeMount
{
    /// <inheritdoc cref="IOnBeforeMount"/>
    void OnBeforeMount();
}

/// <inheritdoc cref="IOnBeforeMount"/>
/// <remarks>See <see cref="IOnMountedAsync"/> for the shared <c>*Async</c> hook rules.</remarks>
public interface IOnBeforeMountAsync
{
    /// <inheritdoc cref="IOnBeforeMountAsync"/>
    Task OnBeforeMountAsync();
}

/// <summary>
/// Mirrors Vue's <c>onMounted</c>: the view has been inserted into a live visual tree
/// and rendered at least once.
/// </summary>
public interface IOnMounted
{
    /// <inheritdoc cref="IOnMounted"/>
    void OnMounted();
}

/// <summary>
/// The async counterpart of <see cref="IOnMounted"/> - implement this instead when
/// mounting needs to <see langword="await"/> something (loading data, warming a cache,
/// ...), the same way Vue lets a lifecycle callback be an async function.
/// </summary>
/// <remarks>
/// Every <c>*Async</c> hook interface in this file (<see cref="IOnBeforeCreateAsync"/>,
/// <see cref="IOnCreatedAsync"/>, <see cref="IOnBeforeMountAsync"/>, this one,
/// <see cref="IOnBeforeUnmountAsync"/>, <see cref="IOnUnmountedAsync"/>,
/// <see cref="IOnActivatedAsync"/>, <see cref="IOnDeactivatedAsync"/>) follows the same
/// shape and the same rules - purely additive alongside the existing synchronous
/// interfaces, not a replacement:
/// <list type="bullet">
/// <item>Fires at exactly the same point its synchronous counterpart does, in the same
/// call order - only the <em>completion</em> is not waited for. Vue itself does not await
/// an async lifecycle callback before continuing either; a slow <c>OnMountedAsync</c>
/// does not delay <c>OnActivated</c>, a later remount, or anything else.</item>
/// <item>Returns <see cref="Task"/>, not <see cref="ValueTask"/>: each hook fires at most
/// once per mount/unmount/etc., never in a tight hot loop, so there is no meaningful
/// allocation to save - <see cref="ValueTask"/> would only add sharp edges (cannot be
/// awaited twice, cannot be inspected after it's already been awaited) for no benefit here.</item>
/// <item>Invoked by <see cref="Lifecycle"/> as "fire, don't await, but never let a fault
/// disappear silently" - never a bare <c>async void</c>. An exception thrown from the
/// async method (before or after its first <see langword="await"/>) is rethrown onto the
/// element's own <see cref="System.Windows.Threading.Dispatcher"/>, which is exactly what
/// makes an ordinary unhandled exception reach <see cref="IOnErrorCaptured"/> today too -
/// no separate async error hook is needed, faults from these hooks surface through the
/// same, already-existing mechanism.</item>
/// <item>There is no async counterpart of <see cref="IOnBeforeUpdate"/>/<see cref="IOnUpdated"/>
/// (those are tied to synchronous, single-batch <see cref="Reactivity.NextTick"/> coalescing -
/// an async hook firing partway through that would not compose with it) or of
/// <see cref="IOnErrorCaptured"/> (its <see langword="bool"/> return has to decide
/// <c>Handled</c> synchronously, before the dispatcher's own exception handling moves on -
/// there is no "await, then decide" version of that contract).</item>
/// </list>
/// </remarks>
public interface IOnMountedAsync
{
    /// <inheritdoc cref="IOnMountedAsync"/>
    Task OnMountedAsync();
}

/// <summary>
/// Mirrors Vue's <c>onBeforeUpdate</c>: the bound <c>DataContext</c> just raised its
/// first <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/> in a
/// new update batch. Fires synchronously, before the batch is flushed - see
/// <see cref="IOnUpdated"/>.
/// </summary>
public interface IOnBeforeUpdate
{
    /// <inheritdoc cref="IOnBeforeUpdate"/>
    void OnBeforeUpdate();
}

/// <summary>
/// Mirrors Vue's <c>onUpdated</c>: fires once after one or more
/// <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/> events from
/// the bound <c>DataContext</c>, coalesced through
/// <see cref="Barbatos.Wpf.Reactivity.NextTick"/> the same way Vue batches multiple
/// synchronous mutations into a single asynchronous DOM update.
/// </summary>
public interface IOnUpdated
{
    /// <inheritdoc cref="IOnUpdated"/>
    void OnUpdated();
}

/// <summary>
/// Mirrors Vue's <c>onBeforeUnmount</c>: the view is still in the visual tree, but is
/// about to be removed.
/// </summary>
public interface IOnBeforeUnmount
{
    /// <inheritdoc cref="IOnBeforeUnmount"/>
    void OnBeforeUnmount();
}

/// <inheritdoc cref="IOnBeforeUnmount"/>
/// <remarks>
/// See <see cref="IOnMountedAsync"/> for the shared <c>*Async</c> hook rules. Useful for
/// flushing/saving state before the view actually goes away - the fire-and-forget nature
/// still applies, so this cannot itself delay the real unmount from proceeding (mirroring
/// how Vue does not wait for an async <c>onBeforeUnmount</c> either).
/// </remarks>
public interface IOnBeforeUnmountAsync
{
    /// <inheritdoc cref="IOnBeforeUnmountAsync"/>
    Task OnBeforeUnmountAsync();
}

/// <summary>
/// Mirrors Vue's <c>onUnmounted</c>: the view has been removed from the visual tree.
/// </summary>
/// <remarks>
/// WPF raises a single <c>Unloaded</c> event where Vue has two distinct moments -
/// <see cref="IOnBeforeUnmount"/> and <see cref="IOnUnmounted"/> both fire from that one
/// event, back to back, rather than bracketing a real teardown phase.
/// </remarks>
public interface IOnUnmounted
{
    /// <inheritdoc cref="IOnUnmounted"/>
    void OnUnmounted();
}

/// <inheritdoc cref="IOnUnmounted"/>
/// <remarks>See <see cref="IOnMountedAsync"/> for the shared <c>*Async</c> hook rules.</remarks>
public interface IOnUnmountedAsync
{
    /// <inheritdoc cref="IOnUnmountedAsync"/>
    Task OnUnmountedAsync();
}

/// <summary>
/// Mirrors Vue's <c>onActivated</c> (<c>&lt;KeepAlive&gt;</c>). Raised whenever an element
/// with <see cref="Lifecycle.EnableProperty"/> becomes visible - on mount (Vue: "also called
/// on mount"), whenever its <see cref="System.Windows.UIElement.IsVisible"/> flips back to
/// <see langword="true"/> (e.g. a hidden-but-alive sibling/tab becoming the shown one - WPF
/// never destroys inactive content by default, so this is the actual counterpart of
/// <c>&lt;KeepAlive&gt;</c>, not a cache), and additionally on a <see cref="System.Windows.Window"/>
/// regaining focus (<see cref="System.Windows.Window.Activated"/> - a distinct signal from
/// visibility, since a window can be visible without being focused).
/// </summary>
public interface IOnActivated
{
    /// <inheritdoc cref="IOnActivated"/>
    void OnActivated();
}

/// <inheritdoc cref="IOnActivated"/>
/// <remarks>See <see cref="IOnMountedAsync"/> for the shared <c>*Async</c> hook rules.</remarks>
public interface IOnActivatedAsync
{
    /// <inheritdoc cref="IOnActivatedAsync"/>
    Task OnActivatedAsync();
}

/// <summary>
/// Mirrors Vue's <c>onDeactivated</c> (<c>&lt;KeepAlive&gt;</c>) - the counterpart to
/// <see cref="IOnActivated"/>, raised on unmount, on
/// <see cref="System.Windows.UIElement.IsVisible"/> flipping to <see langword="false"/>
/// while still mounted, and additionally on a <see cref="System.Windows.Window"/> losing
/// focus (<see cref="System.Windows.Window.Deactivated"/>).
/// </summary>
public interface IOnDeactivated
{
    /// <inheritdoc cref="IOnDeactivated"/>
    void OnDeactivated();
}

/// <inheritdoc cref="IOnDeactivated"/>
/// <remarks>See <see cref="IOnMountedAsync"/> for the shared <c>*Async</c> hook rules.</remarks>
public interface IOnDeactivatedAsync
{
    /// <inheritdoc cref="IOnDeactivatedAsync"/>
    Task OnDeactivatedAsync();
}

/// <summary>
/// Mirrors Vue's <c>onErrorCaptured</c>: called when an unhandled exception reaches the
/// dispatcher while this view is mounted.
/// </summary>
/// <remarks>
/// Vue's own hook stops the error from propagating further when it returns
/// <c>false</c>. Here the return value instead follows WPF's own long-standing
/// <c>Handled</c> convention (<see cref="System.Windows.RoutedEventArgs.Handled"/>
/// and friends) so it behaves the way every other "did you handle this" callback in WPF
/// already does: return <c>true</c> to mark the exception handled (suppresses the app's
/// unhandled-exception behavior), or <c>false</c> to let it keep propagating.
/// </remarks>
public interface IOnErrorCaptured
{
    /// <summary>
    /// Called with the exception that reached the dispatcher. Return <c>true</c> to mark
    /// it handled, <c>false</c> to let it keep propagating.
    /// </summary>
    bool OnErrorCaptured(Exception exception);
}
