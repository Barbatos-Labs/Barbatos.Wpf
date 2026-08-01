// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Barbatos.Wpf.Aquarius.Reactivity;

namespace Barbatos.Wpf.AquariusRouter.Xaml;

using RoutingRouter = Routing.Router;

/// <summary>
/// Hosts whichever View is currently matched for this outlet, at this position in the route tree -
/// the WPF/C# counterpart of Vue Router's own <c>&lt;router-view&gt;</c>.
/// </summary>
/// <remarks>
/// <code>
/// &lt;!-- The simple, single-outlet case: --&gt;
/// &lt;aqr:RouterView /&gt;
///
/// &lt;!-- Nested routes: a matched View's own XAML just declares another one inside it - no
///      extra wiring needed, it finds its own place in the route tree automatically: --&gt;
/// &lt;DockPanel&gt;
///     &lt;views:SidebarView DockPanel.Dock="Left" /&gt;
///     &lt;aqr:RouterView /&gt;
/// &lt;/DockPanel&gt;
///
/// &lt;!-- Named outlets: several RouterViews at the same depth, fed by one route record's
///      RouteRecord.Views map: --&gt;
/// &lt;aqr:RouterView OutletName="sidebar" /&gt;
/// &lt;aqr:RouterView /&gt;
/// </code>
/// <b>Never assign <see cref="ContentControl.Content"/> directly</b> - it is entirely managed by
/// this control based on the current route, the same way Aquarius's own
/// <see cref="Barbatos.Wpf.Aquarius.Xaml.If"/> reserves its <c>Content</c> for whatever branch is currently showing.
/// <b><see cref="OutletNameProperty"/> - not <see cref="FrameworkElement.Name"/> - is the outlet
/// name</b>; <c>OutletName</c> avoids colliding with WPF's own unrelated, ubiquitous element-naming
/// property.
/// <para>
/// <b>Depth is resolved by walking up the visual tree in <see cref="FrameworkElement.Loaded"/>,
/// not via an inherited <see cref="DependencyProperty"/>.</b> WPF constructs a whole View subtree
/// eagerly, all at once (<c>Activator.CreateInstance</c>/<c>InitializeComponent()</c>) - unlike
/// Vue's lazy, top-down renderer, a nested <see cref="RouterView"/> declared inside a matched
/// View's own XAML has its own constructor run <i>before</i> the outer <see cref="RouterView"/>
/// even gets that View instance back, so any ambient value read eagerly (the constructor, or
/// <see cref="FrameworkElement.Initialized"/>) would see nothing there yet - silently wrong, not
/// an exception. <see cref="FrameworkElement.Loaded"/> only fires once an element is genuinely
/// connected into a live, rooted tree, so walking ancestors there is guaranteed correct. This
/// also matches how every other cross-tree lookup in this library family works (<c>Provide</c>/
/// <c>Inject</c>, <c>TeleportHost</c>) - pull-based and <c>Loaded</c>-gated, never eager
/// <c>Inherits</c> attached-property propagation.
/// </para>
/// <para>
/// <b>Construction and guard-checking already happened before this control ever sees a new
/// instance</b> - <see cref="Routing.Router"/> itself builds every entering View (and checks
/// <see cref="Routing.IOnBeforeRouteEnter"/> against it) as part of its own navigation pipeline,
/// storing the result in <see cref="Routing.RouteRecord.Instances"/>. This control's whole job is
/// just: find the right record for its own depth (skipping any record with no
/// <see cref="Routing.RouteRecord.Views"/> of its own - a pure grouping/layout record, mirroring
/// Vue Router's own identical skip-loop), then pull whatever instance is already sitting in
/// <see cref="Routing.RouteRecord.Instances"/> under this control's own <see cref="OutletName"/>
/// and display it. Reassigning <see cref="ContentControl.Content"/> to the exact same instance it
/// already holds is a safe no-op (WPF's own <see cref="DependencyProperty"/> change detection
/// skips the callback for a reference-equal value), which is what makes an "updating" record
/// (e.g. <c>/users/1</c> -&gt; <c>/users/2</c>, same reused View instance) never spuriously
/// remount here.
/// </para>
/// </remarks>
public sealed class RouterView : ContentControl
{
    /// <summary>
    /// An explicit <see cref="Routing.Router"/> for this outlet and its descendants, overriding
    /// <see cref="Routing.Router.Current"/> - only needed for a multi-router app (e.g.
    /// independent per-window navigation stacks); the common single-router app never sets this.
    /// </summary>
    public static readonly DependencyProperty RouterProperty =
        DependencyProperty.Register(nameof(Router), typeof(RoutingRouter), typeof(RouterView), new PropertyMetadata(null));

    /// <summary>The named outlet this control feeds from a matched record's <see cref="Routing.RouteRecord.Views"/> map. Defaults to <c>"default"</c>, matching <see cref="Routing.RouteRecord.View"/>'s own shorthand.</summary>
    public static readonly DependencyProperty OutletNameProperty =
        DependencyProperty.Register(nameof(OutletName), typeof(string), typeof(RouterView), new PropertyMetadata("default"));

    private int _depth;
    private IDisposable? _watchHandle;
    private RoutingRouter? _effectiveRouter;

    /// <summary>Creates a <see cref="RouterView"/>.</summary>
    public RouterView()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <inheritdoc cref="RouterProperty"/>
    public RoutingRouter? Router
    {
        get => (RoutingRouter?)GetValue(RouterProperty);
        set => SetValue(RouterProperty, value);
    }

    /// <inheritdoc cref="OutletNameProperty"/>
    public string OutletName
    {
        get => (string)GetValue(OutletNameProperty);
        set => SetValue(OutletNameProperty, value);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var ancestor = FindAncestor();
        _depth = ancestor is null ? 0 : ancestor._depth + 1;

        // An explicit Router set on THIS RouterView always wins; otherwise inherit whichever
        // Router the nearest ancestor RouterView resolved to (so setting Router explicitly once,
        // on an outer/root RouterView, correctly applies to every nested one inside it too -
        // without this, a multi-router app's nested RouterViews would each need Router set
        // individually, or would silently fall through to the unrelated global Router.Current).
        _effectiveRouter = Router ?? ancestor?._effectiveRouter ?? RoutingRouter.Current;

        if (_effectiveRouter is null)
        {
            // Confirmed empirically: Loaded here is raised from deep inside WPF's own render
            // pipeline (MediaContext.FireLoadedPendingCallbacks, itself reached from an HWND
            // resize/paint message) - a synchronous throw from within it propagates through
            // WPF's internal Dispatcher.Invoke call and crashes the process outright, well
            // before it would ever reach an ordinary caller's try/catch around Show(). Deferring
            // it via BeginInvoke (the same "observe the fault, rethrow through the Dispatcher"
            // shape Aquarius's own Lifecycle/RouterLink already use) lets the current render
            // pass finish first, so the exception surfaces as a normal, catchable unhandled
            // dispatcher exception instead of tearing down the whole process mid-layout.
            Dispatcher.BeginInvoke((Action)(() => throw new InvalidOperationException(
                "No Router is available for this RouterView - set RouterView.Router explicitly (on this RouterView or an ancestor one), or Router.Current once at app startup.")));
            return;
        }

        UpdateContent(_effectiveRouter);
        _watchHandle = Watch.On(_effectiveRouter.CurrentRoute, (_, _) => UpdateContent(_effectiveRouter));
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _watchHandle?.Dispose();
        _watchHandle = null;
    }

    /// <summary>The nearest ancestor <see cref="RouterView"/> in the visual tree, or <see langword="null"/> if this is the outermost one.</summary>
    private RouterView? FindAncestor()
    {
        for (var parent = VisualTreeHelper.GetParent(this); parent is not null; parent = VisualTreeHelper.GetParent(parent))
        {
            if (parent is RouterView ancestor)
                return ancestor;
        }

        return null;
    }

    private void UpdateContent(RoutingRouter router)
    {
        var matched = router.CurrentRoute.Value.Matched;

        var depth = _depth;
        while (depth < matched.Count && matched[depth].ResolvedViews is null)
            depth++;

        Content = depth < matched.Count && matched[depth].Instances.TryGetValue(OutletName, out var instance)
            ? instance
            : null;
    }
}
