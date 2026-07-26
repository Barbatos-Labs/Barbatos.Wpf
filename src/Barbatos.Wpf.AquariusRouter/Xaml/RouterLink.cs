// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using Barbatos.Wpf.Aquarius.Reactivity;
using Barbatos.Wpf.Aquarius.Xaml;
using Barbatos.Wpf.AquariusRouter.Routing;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.AquariusRouter.Xaml;

using RoutingRouter = Routing.Router;

/// <summary>
/// Turns any <see cref="ButtonBase"/> or <see cref="Hyperlink"/> into a navigation trigger, with
/// live <see cref="IsActiveProperty"/>/<see cref="IsExactActiveProperty"/> for style triggers -
/// the WPF/C# counterpart of Vue Router's own <c>&lt;router-link&gt;</c>.
/// </summary>
/// <remarks>
/// <code>
/// &lt;Button aqr:RouterLink.To="/about" Content="About" /&gt;
/// &lt;Hyperlink aqr:RouterLink.To="{aqr:RouteTo Name=user, Params={...}}"&gt;Profile&lt;/Hyperlink&gt;
///
/// &lt;Button aqr:RouterLink.To="/about"&gt;
///     &lt;Button.Style&gt;
///         &lt;Style TargetType="Button"&gt;
///             &lt;Style.Triggers&gt;
///                 &lt;DataTrigger Binding="{Binding (aqr:RouterLink.IsActive), RelativeSource={RelativeSource Self}}" Value="True"&gt;
///                     &lt;Setter Property="FontWeight" Value="Bold" /&gt;
///                 &lt;/DataTrigger&gt;
///             &lt;/Style.Triggers&gt;
///         &lt;/Style&gt;
///     &lt;/Button.Style&gt;
/// &lt;/Button&gt;
/// </code>
/// A set of attached properties on an existing clickable element, deliberately not a new
/// templated control - matching this library's own established split (<see cref="Barbatos.Wpf.Aquarius.Composition.Setup"/>,
/// <see cref="Directives"/>, <see cref="Barbatos.Wpf.Aquarius.Composition.Provide"/>/<see cref="Barbatos.Wpf.Aquarius.Composition.Inject"/>,
/// <see cref="TeleportHost.RegisterHostProperty"/> are all attached properties for "add behavior
/// to an existing element"; a new control is reserved for something that actually manages child
/// content/visibility/lifecycle, which a link does not). <see cref="ToProperty"/>'s click wiring
/// is itself built on <see cref="Directives.EventProperty"/>/<see cref="Directives.CommandProperty"/>
/// rather than a second, hand-rolled event-subscription mechanism.
/// <para>
/// <b>Vue Router's click-guarding (ignoring a modified click/right-click/<c>target=_blank</c> so
/// the browser's native anchor behavior takes over) is deliberately not ported</b> - a WPF
/// <see cref="ButtonBase.Click"/>/<see cref="Hyperlink.Click"/>'s <see cref="RoutedEventArgs"/>
/// carries no modifier-key or mouse-button information to guard on in the first place; there is
/// no "native behavior" to fall back to either. This is a documented "not applicable," not a gap.
/// </para>
/// <para>
/// <b><see cref="IsActiveProperty"/>/<see cref="IsExactActiveProperty"/></b> are the native-WPF
/// mapping of Vue Router's <c>linkActiveClass</c>/<c>linkExactActiveClass</c> CSS-class-list
/// mechanism: two plain bindable booleans, consumed via ordinary <c>DataTrigger</c>/
/// <c>Style.Triggers</c> instead of a class-name string. <c>IsActive</c> is true while the
/// current route is this link's target <i>or any of its descendants</i> (so a link to a parent/
/// index route stays highlighted while any child route is active); <c>IsExactActive</c> requires
/// an exact match, including every param.
/// </para>
/// </remarks>
public static class RouterLink
{
    /// <summary>The navigation target - a <see cref="string"/> path, or a bound <see cref="RouteLocationRaw"/> for a named route with params.</summary>
    public static readonly DependencyProperty ToProperty =
        DependencyProperty.RegisterAttached("To", typeof(object), typeof(RouterLink), new PropertyMetadata(null, OnToChanged));

    /// <summary>When <see langword="true"/>, navigates via <see cref="RoutingRouter.Replace"/> instead of <see cref="RoutingRouter.Push"/>.</summary>
    public static readonly DependencyProperty ReplaceProperty =
        DependencyProperty.RegisterAttached("Replace", typeof(bool), typeof(RouterLink), new PropertyMetadata(false));

    /// <summary>An explicit <see cref="Routing.Router"/> to navigate/compare against, overriding <see cref="RoutingRouter.Current"/>.</summary>
    public static readonly DependencyProperty RouterProperty =
        DependencyProperty.RegisterAttached("Router", typeof(RoutingRouter), typeof(RouterLink), new PropertyMetadata(null));

    private static readonly DependencyPropertyKey IsActivePropertyKey =
        DependencyProperty.RegisterAttachedReadOnly("IsActive", typeof(bool), typeof(RouterLink), new PropertyMetadata(false));

    /// <summary>Whether the current route is <see cref="ToProperty"/>'s target, or a descendant of it - see this type's own remarks.</summary>
    public static readonly DependencyProperty IsActiveProperty = IsActivePropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsExactActivePropertyKey =
        DependencyProperty.RegisterAttachedReadOnly("IsExactActive", typeof(bool), typeof(RouterLink), new PropertyMetadata(false));

    /// <summary>Whether the current route is exactly <see cref="ToProperty"/>'s target (including every param) - see this type's own remarks.</summary>
    public static readonly DependencyProperty IsExactActiveProperty = IsExactActivePropertyKey.DependencyProperty;

    private static readonly DependencyProperty StateProperty =
        DependencyProperty.RegisterAttached("State", typeof(LinkState), typeof(RouterLink));

    /// <summary>Sets <see cref="ToProperty"/>.</summary>
    public static void SetTo(DependencyObject element, object? value) => element.SetValue(ToProperty, value);

    /// <summary>Gets <see cref="ToProperty"/>.</summary>
    public static object? GetTo(DependencyObject element) => element.GetValue(ToProperty);

    /// <summary>Sets <see cref="ReplaceProperty"/>.</summary>
    public static void SetReplace(DependencyObject element, bool value) => element.SetValue(ReplaceProperty, value);

    /// <summary>Gets <see cref="ReplaceProperty"/>.</summary>
    public static bool GetReplace(DependencyObject element) => (bool)element.GetValue(ReplaceProperty);

    /// <summary>Sets <see cref="RouterProperty"/>.</summary>
    public static void SetRouter(DependencyObject element, RoutingRouter? value) => element.SetValue(RouterProperty, value);

    /// <summary>Gets <see cref="RouterProperty"/>.</summary>
    public static RoutingRouter? GetRouter(DependencyObject element) => (RoutingRouter?)element.GetValue(RouterProperty);

    /// <summary>Gets <see cref="IsActiveProperty"/>.</summary>
    public static bool GetIsActive(DependencyObject element) => (bool)element.GetValue(IsActiveProperty);

    /// <summary>Gets <see cref="IsExactActiveProperty"/>.</summary>
    public static bool GetIsExactActive(DependencyObject element) => (bool)element.GetValue(IsExactActiveProperty);

    private static void OnToChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d.GetValue(StateProperty) is LinkState existing)
        {
            existing.Detach();
            d.ClearValue(StateProperty);
        }

        if (e.NewValue is null)
            return;

        if (d is not (ButtonBase or Hyperlink))
            throw new InvalidOperationException($"RouterLink.To can only be set on a ButtonBase or a Hyperlink, not '{d.GetType()}'.");

        var state = new LinkState(d);
        d.SetValue(StateProperty, state);
        state.Attach();
    }

    private static RouteLocationRaw ToRouteLocationRaw(object to) => to switch
    {
        RouteLocationRaw raw => raw,
        string path => path,
        _ => throw new InvalidOperationException($"RouterLink.To must be a string path or a RouteLocationRaw, not '{to.GetType()}'."),
    };

    /// <summary>
    /// Owns one element's click-wiring and live active-state tracking. Named after, and
    /// deliberately shaped like, <c>Lifecycle</c>'s own <c>LifecycleState</c> in Aquarius -
    /// one small state object per attached element, attached/detached as <see cref="ToProperty"/>
    /// changes.
    /// </summary>
    private sealed class LinkState(DependencyObject target)
    {
        private IDisposable? _watchHandle;

        public void Attach()
        {
            Directives.SetEvent(target, "Click");
            Directives.SetCommand(target, new RelayCommand(Navigate));

            switch (target)
            {
                case FrameworkElement element:
                    element.Loaded += OnLoaded;
                    element.Unloaded += OnUnloaded;
                    break;
                case FrameworkContentElement contentElement:
                    contentElement.Loaded += OnLoaded;
                    contentElement.Unloaded += OnUnloaded;
                    break;
            }
        }

        public void Detach()
        {
            Directives.SetEvent(target, null);
            Directives.SetCommand(target, null);

            switch (target)
            {
                case FrameworkElement element:
                    element.Loaded -= OnLoaded;
                    element.Unloaded -= OnUnloaded;
                    break;
                case FrameworkContentElement contentElement:
                    contentElement.Loaded -= OnLoaded;
                    contentElement.Unloaded -= OnUnloaded;
                    break;
            }

            _watchHandle?.Dispose();
            _watchHandle = null;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var router = TryResolveRouter();
            if (router is null)
            {
                ReportMissingRouter();
                return;
            }

            UpdateActiveState(router);
            _watchHandle = Watch.On(router.CurrentRoute, (_, _) => UpdateActiveState(router));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _watchHandle?.Dispose();
            _watchHandle = null;
        }

        private RoutingRouter? TryResolveRouter() => GetRouter(target) ?? RoutingRouter.Current;

        /// <summary>
        /// Defers the "no Router available" failure through this element's own
        /// <see cref="System.Windows.Threading.Dispatcher"/> rather than throwing synchronously -
        /// mirroring <see cref="RouterView"/>'s own identical, empirically-confirmed reasoning:
        /// <see cref="OnLoaded"/> in particular can be reached from deep inside WPF's own render
        /// pipeline, where a direct throw risks crashing the process instead of being merely
        /// catchable.
        /// </summary>
        private void ReportMissingRouter() =>
            target.Dispatcher.BeginInvoke((Action)(() => throw new InvalidOperationException(
                "No Router is available for this RouterLink - set RouterLink.Router explicitly, or Router.Current once at app startup.")));

        private void UpdateActiveState(RoutingRouter router)
        {
            var to = GetTo(target);
            if (to is null)
                return;

            var targetLocation = router.Resolve(ToRouteLocationRaw(to));
            var current = router.CurrentRoute.Value;

            var (isActive, isExactActive) = ComputeActiveState(targetLocation, current);
            target.SetValue(IsActivePropertyKey, isActive);
            target.SetValue(IsExactActivePropertyKey, isExactActive);
        }

        private void Navigate()
        {
            var router = TryResolveRouter();
            if (router is null)
            {
                ReportMissingRouter();
                return;
            }

            var to = GetTo(target);
            if (to is null)
                return;

            var raw = ToRouteLocationRaw(to);
            if (GetReplace(target))
                raw = raw with { Replace = true };

            FireAndForget(router.Push(raw));
        }

        /// <summary>
        /// Fires (but does not await) a navigation from a synchronous click handler - mirroring
        /// Aquarius's own <c>Lifecycle</c> <c>FireAndForget</c>/<c>ObserveFault</c> pair: a
        /// <see cref="NavigationFailure"/> (a cancelled/aborted/duplicate navigation) is an
        /// ordinary, silently-discarded outcome here, exactly like Vue Router's own
        /// <c>RouterLink</c> (<c>router.push(...).catch(noop)</c>) - but a guard or the pipeline
        /// itself throwing a genuine <see cref="Exception"/> is rethrown onto this element's own
        /// <see cref="System.Windows.Threading.Dispatcher"/> rather than silently vanishing.
        /// </summary>
        private void FireAndForget(Task task)
        {
            if (task.IsCompleted)
            {
                ObserveFault(task);
                return;
            }

            task.ContinueWith(ObserveFault, TaskScheduler.Default);
        }

        private void ObserveFault(Task task)
        {
            if (task.Exception is not { } aggregate)
                return;

            var exception = aggregate.Flatten().InnerException ?? aggregate;
            target.Dispatcher.BeginInvoke((Action)(() => throw exception));
        }

        /// <summary>
        /// A simplified approximation of Vue Router's real <c>isActive</c>/<c>isExactActive</c>
        /// algorithm (<c>RouterLink.ts</c>): the leaf record of <paramref name="target"/> is
        /// searched for anywhere in <paramref name="current"/>'s matched chain; if not found
        /// directly but the leaf is an "index" record (<see cref="RouteRecord.Path"/> empty - it
        /// resolves to exactly its own parent's path), the search retries against the parent -
        /// this is what keeps a link to a parent/index route highlighted while any of that
        /// parent's other children are active.
        /// </summary>
        private static (bool IsActive, bool IsExactActive) ComputeActiveState(RouteLocation target, RouteLocation current)
        {
            var activeIndex = ComputeActiveIndex(target, current);
            if (activeIndex < 0)
                return (false, false);

            var isActive = Includes(current.Params, target.Params);
            var isExactActive = isActive
                && activeIndex == current.Matched.Count - 1
                && RouteLocation.ParamsEqual(current.Params, target.Params);

            return (isActive, isExactActive);
        }

        private static int ComputeActiveIndex(RouteLocation target, RouteLocation current)
        {
            if (target.Matched.Count == 0)
                return -1;

            var leaf = target.Matched[^1];
            var index = IndexOfReference(current.Matched, leaf);
            if (index >= 0)
                return index;

            if (target.Matched.Count > 1 && leaf.Path.Length == 0)
                return IndexOfReference(current.Matched, target.Matched[^2]);

            return -1;
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

        /// <summary>Whether every param <paramref name="inner"/> cares about matches <paramref name="outer"/>'s value for it - <paramref name="outer"/> may have extra params <paramref name="inner"/> doesn't mention.</summary>
        private static bool Includes(IReadOnlyDictionary<string, object?> outer, IReadOnlyDictionary<string, object?> inner)
        {
            foreach (var (key, value) in inner)
            {
                if (!outer.TryGetValue(key, out var outerValue) || !RouteLocation.ParamValueEqual(value, outerValue))
                    return false;
            }
            return true;
        }
    }
}
