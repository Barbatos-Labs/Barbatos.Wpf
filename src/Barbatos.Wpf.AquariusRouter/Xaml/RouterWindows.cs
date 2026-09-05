// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Media;
using Barbatos.Wpf.Aquarius.Reactivity;
using Barbatos.Wpf.AquariusRouter.Routing;

namespace Barbatos.Wpf.AquariusRouter.Xaml;

using RoutingRouter = Routing.Router;

/// <summary>
/// Keeps the <see cref="Window"/>s declared by <see cref="RouteRecord.Window"/> in step with the
/// current route - opening one when its record is matched, closing it when it stops being.
/// </summary>
/// <remarks>
/// <code>
/// &lt;Window x:Class="...MainWindow" aqr:RouterWindows.Enable="True" ...&gt;
/// </code>
/// One attached property on the app's main window, and every modal route in the table works from
/// then on - from a <see cref="RouterLink"/>, a <see cref="Routing.Router.Push"/> in a ViewModel, a
/// keyboard shortcut, in-app search or a URI activation alike. Nothing at the call site has to know
/// a window is involved, which is the whole reason this lives on the route record rather than on
/// the link.
/// <para>
/// <b>This is the only piece in the library that opens a window</b>, and it lives in the Xaml layer
/// deliberately: <see cref="Routing.Router"/> stays free of <see cref="Window"/>/
/// <see cref="Application"/> entirely, so the whole navigation pipeline remains testable with no
/// <see cref="Application"/> at all. It watches <see cref="Routing.Router.CurrentRoute"/> exactly
/// the way <see cref="RouterView"/> does - pull-based, <c>Loaded</c>-gated, and released on
/// <c>Unloaded</c>.
/// </para>
/// <para>
/// <b>Closing is symmetric with navigating.</b> Navigating away from a modal record closes its
/// window; closing the window with its own [X] navigates away, to the parent record if it has a
/// <see cref="RouteRecord.Name"/> and otherwise via <see cref="Routing.Router.Back"/>. Without
/// that second half the route would go on claiming a window that is no longer there.
/// </para>
/// </remarks>
public static class RouterWindows
{
    /// <summary>Turns modal-route window management on for the window this is set on.</summary>
    public static readonly DependencyProperty EnableProperty =
        DependencyProperty.RegisterAttached(
            "Enable",
            typeof(bool),
            typeof(RouterWindows),
            new PropertyMetadata(false, OnEnableChanged));

    /// <summary>
    /// The record a modal-route window was opened for, set on the window itself so the
    /// <see cref="RouterView"/> inside it knows what to render.
    /// </summary>
    /// <remarks>
    /// A dialog is its own visual-tree root, so a <see cref="RouterView"/> in one cannot infer its
    /// depth by walking ancestors the way every other outlet does (it would always come out as 0).
    /// Handing it the record outright sidesteps the question entirely - and is exact, where a
    /// declared depth would silently go stale the moment the route table is renested.
    /// </remarks>
    internal static readonly DependencyProperty RecordProperty =
        DependencyProperty.RegisterAttached("Record", typeof(RouteRecord), typeof(RouterWindows));

    private static readonly DependencyProperty StateProperty =
        DependencyProperty.RegisterAttached("State", typeof(WindowsState), typeof(RouterWindows));

    /// <summary>Sets <see cref="EnableProperty"/>.</summary>
    public static void SetEnable(DependencyObject element, bool value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(EnableProperty, value);
    }

    /// <summary>Gets <see cref="EnableProperty"/>.</summary>
    public static bool GetEnable(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (bool)element.GetValue(EnableProperty);
    }

    /// <summary>The record a modal-route window is showing, or <see langword="null"/> for an ordinary window.</summary>
    internal static RouteRecord? GetRecord(DependencyObject element) => (RouteRecord?)element.GetValue(RecordProperty);

    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement host)
            throw new InvalidOperationException("RouterWindows.Enable can only be set on a FrameworkElement - normally the app's main Window.");

        if (host.GetValue(StateProperty) is WindowsState existing)
        {
            existing.Detach();
            host.ClearValue(StateProperty);
        }

        if ((bool)e.NewValue)
        {
            var state = new WindowsState(host);
            host.SetValue(StateProperty, state);
            state.Attach();
        }
    }

    /// <summary>
    /// Owns the open modal-route windows for one host, and the <c>CurrentRoute</c> watch that
    /// keeps them in step. Shaped like <c>RouterLink</c>'s own <c>LinkState</c> - one small state
    /// object per attached element.
    /// </summary>
    private sealed class WindowsState(FrameworkElement host)
    {
        /// <summary>
        /// Open windows keyed by their <see cref="RouteRecord.Window"/> <b>type</b>, not by record.
        /// </summary>
        /// <remarks>
        /// Sibling modal records sharing one window type - a dialog with its own internal
        /// navigation - must keep the same window across a move between them, only swapping what
        /// its outlet shows. Keying by record would close and reopen it instead, throwing away the
        /// window's position, size and focus on every step of a navigation that never left it.
        /// </remarks>
        private readonly Dictionary<Type, (Window Window, RouteRecord Record)> _open = [];
        private IDisposable? _watchHandle;
        private RoutingRouter? _router;
        private bool _closing;

        public void Attach()
        {
            host.Loaded += OnLoaded;
            host.Unloaded += OnUnloaded;

            if (host.IsLoaded)
                OnLoaded(host, new RoutedEventArgs());
        }

        public void Detach()
        {
            host.Loaded -= OnLoaded;
            host.Unloaded -= OnUnloaded;
            OnUnloaded(host, new RoutedEventArgs());
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _router = RouterScope.GetRouter(host) ?? RoutingRouter.Current;

            if (_router is null)
            {
                // Same deferral RouterView uses, and for the same empirically-confirmed reason: a
                // synchronous throw from inside WPF's Loaded broadcast takes the process down
                // rather than surfacing catchably.
                host.Dispatcher.BeginInvoke((Action)(() => throw new InvalidOperationException(
                    "No Router is available for RouterWindows - set RouterScope.Router on this window, or Router.Current once at app startup.")));
                return;
            }

            // Guarded the same way RouterView's is: WPF raises Loaded twice with no Unloaded in
            // between, and a second live watch would leak the first against an app-lifetime Router.
            _watchHandle?.Dispose();
            Sync(_router);
            _watchHandle = Watch.On(_router.CurrentRoute, (_, _) => Sync(_router));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _watchHandle?.Dispose();
            _watchHandle = null;

            foreach (var windowType in _open.Keys.ToArray())
                CloseWindow(windowType);
        }

        /// <summary>Opens whatever the current route says should be open, closes whatever it no longer does, and re-points anything already open at its new record.</summary>
        private void Sync(RoutingRouter router)
        {
            var matched = router.CurrentRoute.Value.Matched;

            // The windows the route asks for, keyed the way _open is. A deeper record wins over a
            // shallower one declaring the same window, which is the only sane reading of a chain
            // that names one window twice: the deepest match is what the user navigated to.
            Dictionary<Type, RouteRecord>? required = null;
            foreach (var record in matched)
            {
                if (record.Window is { } windowType)
                    (required ??= [])[windowType] = record;
            }

            foreach (var windowType in _open.Keys.ToArray())
            {
                if (required is null || !required.ContainsKey(windowType))
                    CloseWindow(windowType);
            }

            if (required is null)
                return;

            foreach (var (windowType, record) in required)
            {
                if (_open.TryGetValue(windowType, out var open))
                    Rebind(windowType, open, record);
                else
                    OpenWindow(router, windowType, record);
            }
        }

        private void OpenWindow(RoutingRouter router, Type windowType, RouteRecord record)
        {
            var window = Barbatos.Wpf.Aquarius.Composition.Setup.Resolve(windowType) as Window
                ?? throw new InvalidOperationException($"RouteRecord.Window must be a Window type, not '{windowType}'.");

            // The record travels with the window so the RouterView inside it renders this record
            // rather than trying to infer a depth it has no way to know - see RecordProperty.
            window.SetValue(RecordProperty, record);
            RouterScope.SetRouter(window, router);

            if (Window.GetWindow(host) is { } owner && !ReferenceEquals(owner, window))
                window.Owner = owner;

            window.Closed += (_, _) => OnWindowClosed(router, windowType, window);

            // Pinned and tracked before Show(), because Show() is what raises Loaded on the
            // RouterView inside - it must already be able to see the record it is to render.
            _open[windowType] = (window, record);
            window.Show();
        }

        /// <summary>Points an already-open window at a different record - a navigation between siblings that never left the dialog.</summary>
        private void Rebind(Type windowType, (Window Window, RouteRecord Record) open, RouteRecord record)
        {
            if (ReferenceEquals(open.Record, record))
                return;

            _open[windowType] = (open.Window, record);
            open.Window.SetValue(RecordProperty, record);

            // Told rather than left to the outlet's own CurrentRoute watch: both fire for this
            // navigation, but their relative order is an artefact of who subscribed first, and the
            // outlet reads the pin at the moment it runs. Refreshing here makes the outcome the
            // same either way - the watch, whenever it lands, re-assigns the same instance and WPF
            // drops it as a no-op.
            RefreshOutlets(open.Window);
        }

        /// <summary>Closes a window because the route left it behind - the navigation-driven direction.</summary>
        private void CloseWindow(Type windowType)
        {
            if (!_open.Remove(windowType, out var open))
                return;

            var window = open.Window;

            // Suppresses OnWindowClosed's navigation: the route already moved on, and navigating
            // again here would fight whatever navigation caused this close in the first place.
            _closing = true;
            try
            {
                window.Close();
            }
            finally
            {
                _closing = false;
            }
        }

        /// <summary>
        /// The user closed the window themselves - navigate away so the route stops claiming a
        /// window that no longer exists.
        /// </summary>
        private void OnWindowClosed(RoutingRouter router, Type windowType, Window window)
        {
            // The identity check matters once windows are reused: a Rebind swaps the record a
            // window shows, and a close-and-reopen of the same type would otherwise let a stale
            // handler evict the window that replaced it.
            if (_closing || !_open.TryGetValue(windowType, out var open) || !ReferenceEquals(open.Window, window))
                return;

            _open.Remove(windowType);

            var matched = router.CurrentRoute.Value.Matched;
            var index = IndexOfReference(matched, open.Record);
            if (index < 0)
                return;

            // The parent record is where "closing this dialog" naturally lands, and navigating to
            // it by name carries the current params along (a modal route is usually a child of a
            // parameterised one). An unnamed parent has no addressable target, so fall back to
            // Back - which is the same thing in every ordinary case, since arriving here is what
            // put the dialog on the history stack.
            var parent = index > 0 ? matched[index - 1] : null;

            _ = parent?.Name is { } parentName
                ? router.Push(new RouteLocationRaw { Name = parentName, Params = router.CurrentRoute.Value.Params })
                : router.Back();
        }

        /// <summary>Tells every <see cref="RouterView"/> in a re-pinned window to re-read the route.</summary>
        private static void RefreshOutlets(DependencyObject root)
        {
            for (int i = 0, count = VisualTreeHelper.GetChildrenCount(root); i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);

                // No recursion past an outlet: what it hosts is a routed View whose own nested
                // outlets take their depth from it, and they are driven by their own watch.
                if (child is RouterView outlet)
                    outlet.Refresh();
                else
                    RefreshOutlets(child);
            }
        }

        private static int IndexOfReference(IReadOnlyList<RouteRecord> records, RouteRecord target)
        {
            for (var i = 0; i < records.Count; i++)
            {
                if (ReferenceEquals(records[i], target))
                    return i;
            }

            return -1;
        }
    }
}
