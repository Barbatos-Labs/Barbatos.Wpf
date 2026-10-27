// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
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
/// window; closing the window with its own [X] (or Alt+F4, or a <see cref="Window.Close"/> of
/// the app's own) is a request to navigate away - to the parent record if it has a
/// <see cref="RouteRecord.Name"/>, otherwise via <see cref="Routing.Router.Back"/> - and the
/// window goes only once the route has actually left it. A leave guard that refuses (an
/// unsaved-changes prompt answered "no"), a guard that throws, or a newer navigation that
/// supersedes this one keeps the window open, because the route still claims it; a guard that
/// is still awaiting something keeps it open until the navigation commits, which then closes it
/// like any other navigation away. Closing the window unconditionally and navigating afterwards -
/// the first design - left a refused navigation on a modal record with no window, where pushing
/// the same route again returned <see cref="NavigationFailureType.Duplicated"/> and nothing could
/// reopen it.
/// </para>
/// <para>
/// Whatever that navigation throws is rethrown on the window's
/// <see cref="System.Windows.Threading.Dispatcher"/>, as it is for a <see cref="RouterLink"/>
/// click, so it reaches <see cref="Application.DispatcherUnhandledException"/>. Nothing else
/// awaits it, and without that the exception would vanish, leaving a window that will not close
/// and no trace of why.
/// </para>
/// <para>
/// Two consequences for code of your own on such a window: a <see cref="Window.Closing"/>
/// handler of yours sees every attempt, including one that is then kept open, and runs again
/// when a navigation that committed later closes the window - do once-only work in
/// <see cref="Window.Closed"/>. And a <see cref="Window.Closing"/> handler of yours that cancels
/// is respected - no navigation is started - provided it runs first: one attached in XAML or in
/// the window's constructor, or an <see cref="Window.OnClosing"/> override that cancels before
/// calling the base. <see cref="RouterWindows"/> subscribes once the window has been constructed,
/// so a handler attached later than that runs after the navigation has already started. When the
/// main window or the application itself is closing, WPF ignores a cancelled close, so the window
/// goes regardless and the navigation away is attempted only once.
/// </para>
/// <para>
/// <b>Name the parent record.</b> With no name to push, the [X] falls back to
/// <see cref="Routing.Router.Back"/>, and where there is nothing to go back to - the app started
/// on the modal route itself - <see cref="Routing.Router.Back"/> is a silent no-op. The window
/// still closes then, because refusing would leave a window nothing can close, but the route stays
/// on the modal record: pushing it again returns <see cref="NavigationFailureType.Duplicated"/>
/// until something navigates elsewhere first. A named parent is always reachable, so this never
/// arises.
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

        /// <summary>
        /// Windows whose own close already started the navigation away, so the <c>Closed</c> that
        /// follows when WPF ignores the cancel (see <see cref="OnWindowClosed"/>) does not start a
        /// second one - which would run the leave guards, and any prompt in them, twice.
        /// </summary>
        private readonly HashSet<Window> _closeNavigationStarted = [];

        /// <summary>
        /// Windows whose own <c>Closing</c> is being handled right now - see <see cref="CloseWindow"/>.
        /// A set rather than one field because a guard's prompt runs a nested message loop, in
        /// which another dialog can be closed before the first close has been decided.
        /// </summary>
        private readonly HashSet<Window> _raisingClosing = [];

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

            window.Closing += (_, e) => OnWindowClosing(router, windowType, window, e);
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
            _closeNavigationStarted.Remove(window);

            // The window's own close, whose navigation away just committed synchronously from
            // inside its Closing handler: forgetting it is enough, since OnWindowClosing then lets
            // that close go ahead - and WPF throws on a Close() while a window is raising Closing.
            if (_raisingClosing.Contains(window))
                return;

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
        /// The user (or the app's own <see cref="Window.Close"/>) is closing the window - turn that
        /// into the navigation away it stands for, and let the window go only if the route has left it.
        /// </summary>
        private void OnWindowClosing(RoutingRouter router, Type windowType, Window window, CancelEventArgs e)
        {
            // Our own navigation-driven close, an app handler that already said no, or a window
            // this state no longer tracks (OnWindowClosed sorts that out).
            if (_closing || e.Cancel || !IsOpen(windowType, window, out var open))
                return;

            var before = router.CurrentRoute.Value;
            Task<NavigationFailure?>? navigation;
            _raisingClosing.Add(window);
            try
            {
                navigation = StartNavigationAway(router, open.Record, window.Dispatcher);
            }
            finally
            {
                _raisingClosing.Remove(window);
            }

            // A navigation whose guards all answer synchronously has committed or failed by now,
            // and a committed one has already run Sync through the CurrentRoute watch, so whether
            // the route still claims this window is the whole answer. Still claimed means refused,
            // threw, superseded, still awaiting an async guard (whose commit will close it through
            // Sync like any other navigation away), or moved to a record that shares this window.
            if (navigation is null || !IsOpen(windowType, window, out _))
                return;

            _closeNavigationStarted.Add(window);

            // ...or it means there was nowhere to go: Back with no earlier entry (an unnamed parent
            // on a route the app started on) completes at once, successfully, without touching the
            // route - the only navigation that does. Refusing the close then would leave a window
            // no [X] can ever close, so it goes, and the route stays on the modal record - see the
            // type's remarks.
            if (navigation.IsCompletedSuccessfully && navigation.Result is null && ReferenceEquals(router.CurrentRoute.Value, before))
                return;

            e.Cancel = true;
        }

        /// <summary>
        /// The window is gone. Normally <see cref="OnWindowClosing"/> or <see cref="CloseWindow"/>
        /// already dealt with it; this covers a close whose cancel WPF ignored - every owned window
        /// when its owner closes, every window on application shutdown, neither of which can be
        /// vetoed - and a destroy that raised no <c>Closing</c> at all.
        /// </summary>
        private void OnWindowClosed(RoutingRouter router, Type windowType, Window window)
        {
            var navigationStarted = _closeNavigationStarted.Remove(window);

            // The identity check matters once windows are reused: a Rebind swaps the record a
            // window shows, and a close-and-reopen of the same type would otherwise let a stale
            // handler evict the window that replaced it.
            if (_closing || !IsOpen(windowType, window, out var open))
                return;

            _open.Remove(windowType);

            // The route would otherwise go on claiming a window that no longer exists.
            if (!navigationStarted)
                _ = StartNavigationAway(router, open.Record, window.Dispatcher);
        }

        /// <summary>
        /// Starts the navigation that closing <paramref name="record"/>'s window stands for;
        /// <see langword="null"/> when the record is no longer matched and there is nothing to
        /// leave.
        /// </summary>
        private static Task<NavigationFailure?>? StartNavigationAway(RoutingRouter router, RouteRecord record, Dispatcher dispatcher)
        {
            var matched = router.CurrentRoute.Value.Matched;
            var index = IndexOfReference(matched, record);
            if (index < 0)
                return null;

            // The parent record is where "closing this dialog" naturally lands, and navigating to
            // it by name carries the current params along (a modal route is usually a child of a
            // parameterised one). An unnamed parent has no addressable target, so fall back to
            // Back - which is the same thing in every ordinary case, since arriving here is what
            // put the dialog on the history stack. Not awaited: OnWindowClosing reads the outcome
            // from the route itself, which also covers a navigation that has not finished yet, and
            // looks at the task only to tell a Back that went nowhere from a refusal.
            var parent = index > 0 ? matched[index - 1] : null;

            var navigation = parent?.Name is { } parentName
                ? router.Push(new RouteLocationRaw { Name = parentName, Params = router.CurrentRoute.Value.Params })
                : router.Back();

            ObserveFault(navigation, dispatcher);
            return navigation;
        }

        /// <summary>
        /// Rethrows a navigation's exception on <paramref name="dispatcher"/> - the treatment
        /// <see cref="RouterLink"/> gives a click's navigation, and for the reason in this type's
        /// remarks. A <see cref="NavigationFailure"/> is an ordinary outcome and stays silent.
        /// </summary>
        private static void ObserveFault(Task navigation, Dispatcher dispatcher)
        {
            if (navigation.IsCompleted)
            {
                Rethrow(navigation, dispatcher);
                return;
            }

            navigation.ContinueWith(task => Rethrow(task, dispatcher), TaskScheduler.Default);
        }

        private static void Rethrow(Task navigation, Dispatcher dispatcher)
        {
            if (navigation.Exception is not { } aggregate)
                return;

            var exception = aggregate.Flatten().InnerException ?? aggregate;
            dispatcher.BeginInvoke((Action)(() => throw exception));
        }

        private bool IsOpen(Type windowType, Window window, out (Window Window, RouteRecord Record) open) =>
            _open.TryGetValue(windowType, out open) && ReferenceEquals(open.Window, window);

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
