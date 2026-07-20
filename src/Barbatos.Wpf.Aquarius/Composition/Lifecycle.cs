// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using Barbatos.Wpf.Reactivity;

namespace Barbatos.Wpf.Composition;

/// <summary>
/// Wires a <see cref="FrameworkElement"/>'s real lifecycle events to whichever
/// <c>IOnXxx</c> hook interfaces (see <see cref="IOnBeforeMount"/> and friends) its
/// <c>DataContext</c> implements - no code-behind required.
/// </summary>
/// <remarks>
/// <code>
/// &lt;UserControl aq:Lifecycle.Enable="True" ... /&gt;
/// </code>
/// is enough: the element's <c>DataContext</c> (the ViewModel - the WPF/MVVM analogue of
/// a Vue component's <c>setup()</c>) is checked against every hook interface with a
/// simple <see langword="is"/> pattern, so a ViewModel that implements none of them costs
/// nothing, and one implementing all of them behaves like a full Options-API component.
/// </remarks>
public static class Lifecycle
{
    /// <summary>
    /// Attached property that turns lifecycle-hook dispatch on or off for a
    /// <see cref="FrameworkElement"/>.
    /// </summary>
    public static readonly DependencyProperty EnableProperty =
        DependencyProperty.RegisterAttached(
            "Enable",
            typeof(bool),
            typeof(Lifecycle),
            new PropertyMetadata(false, OnEnableChanged));

    /// <summary>Sets <see cref="EnableProperty"/>.</summary>
    public static void SetEnable(DependencyObject element, bool value) => element.SetValue(EnableProperty, value);

    /// <summary>Gets <see cref="EnableProperty"/>.</summary>
    public static bool GetEnable(DependencyObject element) => (bool)element.GetValue(EnableProperty);

    private static readonly DependencyProperty StateProperty =
        DependencyProperty.RegisterAttached("State", typeof(LifecycleState), typeof(Lifecycle));

    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;

        if (element.GetValue(StateProperty) is LifecycleState existing)
        {
            existing.Detach();
            element.ClearValue(StateProperty);
        }

        if ((bool)e.NewValue)
        {
            var state = new LifecycleState(element);
            element.SetValue(StateProperty, state);
            state.Attach();
        }
    }

    /// <summary>
    /// Owns the event subscriptions and update-batching state for one
    /// <see cref="FrameworkElement"/> that has <see cref="EnableProperty"/> set.
    /// </summary>
    private sealed class LifecycleState(FrameworkElement element)
    {
        private readonly FrameworkElement _element = element;
        private INotifyPropertyChanged? _watchedDataContext;
        private bool _mounted;
        private bool _beforeMountInvoked;
        private bool _visibleActivated;
        private bool _updateDirty;
        private bool _updateScheduled;

        public void Attach()
        {
            _element.Initialized += OnInitialized;
            _element.Loaded += OnLoaded;
            _element.Unloaded += OnUnloaded;

            if (_element is Window window)
            {
                window.Activated += OnActivated;
                window.Deactivated += OnDeactivated;
            }
        }

        public void Detach()
        {
            _element.Initialized -= OnInitialized;
            _element.Loaded -= OnLoaded;
            _element.Unloaded -= OnUnloaded;

            if (_element is Window window)
            {
                window.Activated -= OnActivated;
                window.Deactivated -= OnDeactivated;
            }

            if (_mounted)
                StopMountedTracking();
        }

        private void OnInitialized(object? sender, EventArgs e)
        {
            // DataContext is very often set by the parent (DataContext="{Binding ...}" at
            // the usage site) *after* this element's own Initialized fires - so this is a
            // best-effort early call; OnLoaded below guarantees it fires at least once.
            if (_element.DataContext is IOnBeforeMount hook)
            {
                _beforeMountInvoked = true;
                hook.OnBeforeMount();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_beforeMountInvoked)
                InvokeHook<IOnBeforeMount>(hook => hook.OnBeforeMount());

            _mounted = true;
            StartMountedTracking();
            InvokeHook<IOnMounted>(hook => hook.OnMounted());

            // Mirrors Vue's own note: "onActivated is also called on mount" - a ViewModel
            // that only cares about IOnActivated/IOnDeactivated (not the full Mounted/
            // Unmounted pair) still sees a consistent "currently shown" signal.
            SetVisibleActivated(true);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Reset so a later remount (e.g. `If.Condition` flipping true again, which
            // detaches and reattaches this same instance) fires OnBeforeMount again too.
            _beforeMountInvoked = false;

            InvokeHook<IOnBeforeUnmount>(hook => hook.OnBeforeUnmount());

            // Mirrors Vue's "...and onDeactivated on unmount" - a no-op if IsVisibleChanged
            // already reported false first (e.g. it was hidden, then removed).
            SetVisibleActivated(false);

            _mounted = false;
            StopMountedTracking();
            InvokeHook<IOnUnmounted>(hook => hook.OnUnmounted());
        }

        /// <summary>
        /// Window <c>Activated</c>/<c>Deactivated</c> is a distinct signal from visibility -
        /// a window can be visible but not focused - so it fires
        /// <see cref="IOnActivated"/>/<see cref="IOnDeactivated"/> directly, independent of
        /// (and in addition to) <see cref="SetVisibleActivated"/>'s dedup below.
        /// </summary>
        private void OnActivated(object? sender, EventArgs e) =>
            InvokeHook<IOnActivated>(hook => hook.OnActivated());

        private void OnDeactivated(object? sender, EventArgs e) =>
            InvokeHook<IOnDeactivated>(hook => hook.OnDeactivated());

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Covers the "hidden-but-alive sibling/tab" case Vue's <KeepAlive> exists for:
            // WPF never destroys inactive content by default, so this is the piece that was
            // actually missing - not a cache, just this notification.
            if (_mounted)
                SetVisibleActivated((bool)e.NewValue);
        }

        /// <summary>
        /// Dedups <see cref="IOnActivated"/>/<see cref="IOnDeactivated"/> across the three
        /// signals that all mean "currently shown, or not": mount/unmount and
        /// <see cref="UIElement.IsVisibleChanged"/>. Without this, a mount (which
        /// already implies visible) followed immediately by an IsVisibleChanged notification
        /// for that same transition would double-fire.
        /// </summary>
        private void SetVisibleActivated(bool activated)
        {
            if (_visibleActivated == activated)
                return;

            _visibleActivated = activated;

            if (activated)
                InvokeHook<IOnActivated>(hook => hook.OnActivated());
            else
                InvokeHook<IOnDeactivated>(hook => hook.OnDeactivated());
        }

        private void StartMountedTracking()
        {
            _element.DataContextChanged += OnDataContextChanged;
            WatchDataContext(_element.DataContext as INotifyPropertyChanged);
            _element.IsVisibleChanged += OnIsVisibleChanged;

            if (Application.Current is { } app)
                app.DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        private void StopMountedTracking()
        {
            _element.DataContextChanged -= OnDataContextChanged;
            WatchDataContext(null);
            _element.IsVisibleChanged -= OnIsVisibleChanged;
            _updateDirty = false;
            _updateScheduled = false;

            if (Application.Current is { } app)
                app.DispatcherUnhandledException -= OnDispatcherUnhandledException;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) =>
            WatchDataContext(e.NewValue as INotifyPropertyChanged);

        private void WatchDataContext(INotifyPropertyChanged? next)
        {
            if (_watchedDataContext is not null)
                _watchedDataContext.PropertyChanged -= OnDataContextPropertyChanged;

            _watchedDataContext = next;

            if (_watchedDataContext is not null)
                _watchedDataContext.PropertyChanged += OnDataContextPropertyChanged;
        }

        private void OnDataContextPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!_mounted)
                return;

            if (!_updateDirty)
            {
                _updateDirty = true;
                InvokeHook<IOnBeforeUpdate>(hook => hook.OnBeforeUpdate());
            }

            if (_updateScheduled)
                return;

            _updateScheduled = true;
            NextTick.Run(() =>
            {
                _updateScheduled = false;

                if (!_updateDirty)
                    return;

                _updateDirty = false;
                InvokeHook<IOnUpdated>(hook => hook.OnUpdated());
            });
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (_element.DataContext is IOnErrorCaptured hook && hook.OnErrorCaptured(e.Exception))
                e.Handled = true;
        }

        private void InvokeHook<THook>(Action<THook> invoke)
        {
            if (_element.DataContext is THook hook)
                invoke(hook);
        }
    }
}
