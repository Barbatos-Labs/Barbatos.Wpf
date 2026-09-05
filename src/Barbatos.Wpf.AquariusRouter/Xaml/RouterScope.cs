// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;

namespace Barbatos.Wpf.AquariusRouter.Xaml;

using RoutingRouter = Routing.Router;

/// <summary>
/// Declares which <see cref="Routing.Router"/> every <see cref="RouterView"/> and
/// <see cref="RouterLink"/> beneath an element belongs to - set it once on a
/// <see cref="Window"/> to give that window its own navigation stack.
/// </summary>
/// <remarks>
/// <code>
/// &lt;!-- A dialog with its own independent navigation, separate from the main window's: --&gt;
/// &lt;Window x:Class="...ReportDialog" aqr:RouterScope.Router="{Binding ReportRouter}"&gt;
///     &lt;DockPanel&gt;
///         &lt;StackPanel DockPanel.Dock="Top" Orientation="Horizontal"&gt;
///             &lt;Button aqr:RouterLink.To="/summary" Content="Summary" /&gt;
///             &lt;Button aqr:RouterLink.To="/details" Content="Details" /&gt;
///         &lt;/StackPanel&gt;
///         &lt;aqr:RouterView /&gt;
///     &lt;/DockPanel&gt;
/// &lt;/Window&gt;
/// </code>
/// This is the ergonomic half of multi-router support. <see cref="RouterView.Router"/> and
/// <see cref="RouterLink.RouterProperty"/> could always be set explicitly, but that meant setting
/// them on <i>every single link</i> in a dialog - miss one and it silently navigates the main
/// window instead. An inherited attached property is set once, at the root, and every descendant
/// picks it up.
/// <para>
/// <b>Resolution order</b> for both controls: an explicit <c>Router</c> on the control itself, then
/// the nearest inherited <see cref="RouterProperty"/>, then (for a nested
/// <see cref="RouterView"/>) whatever its ancestor <see cref="RouterView"/> resolved, and finally
/// <see cref="Routing.Router.Current"/>. Read at <see cref="FrameworkElement.Loaded"/>, by which
/// point property inheritance has settled - the same reasoning <see cref="RouterView"/> documents
/// for resolving its own depth there rather than eagerly.
/// </para>
/// <para>
/// <b>Property inheritance does not cross a <see cref="Window"/> boundary</b>, which is exactly
/// what makes this work: a dialog is its own tree root, so it gets its own scope and cannot
/// accidentally inherit the main window's. A dialog that should instead share the app's router
/// simply sets nothing and falls through to <see cref="Routing.Router.Current"/> - though see
/// <see cref="RouterView"/>'s remarks on why a shared router usually is not what you want there.
/// </para>
/// </remarks>
public static class RouterScope
{
    /// <summary>The <see cref="Routing.Router"/> that this element and everything below it navigates.</summary>
    public static readonly DependencyProperty RouterProperty =
        DependencyProperty.RegisterAttached(
            "Router",
            typeof(RoutingRouter),
            typeof(RouterScope),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>Sets <see cref="RouterProperty"/>.</summary>
    public static void SetRouter(DependencyObject element, RoutingRouter? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(RouterProperty, value);
    }

    /// <summary>Gets <see cref="RouterProperty"/> - the value inherited from the nearest ancestor that set one, if this element did not.</summary>
    public static RoutingRouter? GetRouter(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (RoutingRouter?)element.GetValue(RouterProperty);
    }
}
