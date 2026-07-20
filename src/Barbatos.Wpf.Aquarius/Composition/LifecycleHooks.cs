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
/// </summary>
public interface IOnBeforeMount
{
    /// <summary>
    /// Mirrors <c>onBeforeMount</c>: the view exists (its constructor/XAML has run) but
    /// is not yet part of a live visual tree.
    /// </summary>
    void OnBeforeMount();
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
