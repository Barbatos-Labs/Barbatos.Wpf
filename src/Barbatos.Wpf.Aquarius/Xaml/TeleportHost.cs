// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.Aquarius.Xaml;

/// <summary>
/// Marks a <see cref="Panel"/> as a named mount point a <see cref="Teleport"/> can render
/// into - the Aquarius counterpart of the DOM element a Vue <c>&lt;Teleport to="#target"&gt;</c>
/// targets.
/// </summary>
/// <remarks>
/// <code>
/// &lt;Grid aq:TeleportHost.RegisterHost="Overlay" Panel.ZIndex="100" /&gt;
/// </code>
/// Registration happens while the panel is loaded, keyed by name in a process-wide
/// registry - like a DOM <c>id</c>, names are expected to be unique; if two hosts
/// register the same name, the most recently loaded one wins. <see cref="HostRegistered"/>
/// lets a <see cref="Teleport"/> that mounts before its target host does find it once it
/// becomes available, instead of silently never teleporting.
/// <para>
/// <b>Any <see cref="Panel"/> works, and several <see cref="Teleport"/>s may target the same
/// host</b> - which is the point of not restricting this to a <see cref="Grid"/>. Teleported
/// content becomes a real child of the panel, so the panel's own layout applies to it
/// directly, and so do layout attached properties set on the content itself:
/// <code>
/// &lt;!-- A toolbar assembled from widgets declared all over the app: --&gt;
/// &lt;UniformGrid Rows="1" aq:TeleportHost.RegisterHost="Toolbar" /&gt;
///
/// &lt;!-- ...or a docking edge, where each teleported item picks its own side: --&gt;
/// &lt;DockPanel aq:TeleportHost.RegisterHost="Shell"&gt;
///     &lt;StatusBar DockPanel.Dock="Bottom" /&gt;   &lt;!-- the host's own static child --&gt;
/// &lt;/DockPanel&gt;
/// </code>
/// Relative order is <see cref="Teleport.Order"/>, never load order - see that property.
/// Teleported children are kept contiguous and are placed <i>after</i> whatever static
/// children the host declares itself, so a host can have its own permanent content.
/// </para>
/// </remarks>
public static class TeleportHost
{
    private static readonly Dictionary<string, Panel> Hosts = [];

    /// <summary>
    /// The <see cref="Teleport"/>s currently rendering into a given host, ordered exactly as
    /// their content sits in <see cref="Panel.Children"/>.
    /// </summary>
    /// <remarks>
    /// Stored on the panel itself rather than in a static <c>Dictionary&lt;Panel, ...&gt;</c>:
    /// a static map keyed by panel would keep every host that ever registered alive for the
    /// life of the process, which is the exact failure mode <see cref="Teleport"/>'s own
    /// subscription to the static events below had to be fixed for. An attached property dies
    /// with its panel.
    /// </remarks>
    private static readonly DependencyProperty TeleportedProperty =
        DependencyProperty.RegisterAttached("Teleported", typeof(List<Teleport>), typeof(TeleportHost));

    /// <summary>
    /// Raised whenever a host registers (or re-registers) under a name, so a
    /// <see cref="Teleport"/> waiting on that name can retry.
    /// </summary>
    public static event Action<string>? HostRegistered;

    /// <summary>
    /// Raised whenever the currently-registered host under a name unregisters (its
    /// <see cref="FrameworkElement.Unloaded"/> fired, or another value replaced its
    /// <see cref="RegisterHostProperty"/>) - lets a <see cref="Teleport"/> that was
    /// rendered into that host bring its content home instead of leaving it orphaned
    /// inside a torn-down visual tree (e.g. a floating dialog <see cref="Window"/> that
    /// just closed).
    /// </summary>
    public static event Action<string>? HostUnregistered;

    /// <summary>The attached property that registers a <see cref="Panel"/> under a name.</summary>
    public static readonly DependencyProperty RegisterHostProperty =
        DependencyProperty.RegisterAttached(
            "RegisterHost",
            typeof(string),
            typeof(TeleportHost),
            new PropertyMetadata(null, OnRegisterHostChanged));

    /// <summary>Sets <see cref="RegisterHostProperty"/>.</summary>
    public static void SetRegisterHost(DependencyObject element, string? value) => element.SetValue(RegisterHostProperty, value);

    /// <summary>Gets <see cref="RegisterHostProperty"/>.</summary>
    public static string? GetRegisterHost(DependencyObject element) => (string?)element.GetValue(RegisterHostProperty);

    /// <summary>Looks up the currently registered host <see cref="Panel"/> for <paramref name="name"/>.</summary>
    public static bool TryGetHost(string name, out Panel panel)
    {
        if (Hosts.TryGetValue(name, out var found))
        {
            panel = found;
            return true;
        }

        panel = null!;
        return false;
    }

    private static void OnRegisterHostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Panel panel)
            throw new InvalidOperationException("TeleportHost.RegisterHost can only be set on a Panel.");

        if (e.OldValue is string oldName)
            Unregister(oldName, panel);

        panel.Loaded -= OnPanelLoaded;
        panel.Unloaded -= OnPanelUnloaded;

        if (e.NewValue is string newName)
        {
            panel.Loaded += OnPanelLoaded;
            panel.Unloaded += OnPanelUnloaded;

            if (panel.IsLoaded)
                Register(newName, panel);
        }
    }

    private static void OnPanelLoaded(object sender, RoutedEventArgs e)
    {
        var panel = (Panel)sender;

        if (GetRegisterHost(panel) is { } name)
            Register(name, panel);
    }

    private static void OnPanelUnloaded(object sender, RoutedEventArgs e)
    {
        var panel = (Panel)sender;

        if (GetRegisterHost(panel) is { } name)
            Unregister(name, panel);
    }

    /// <summary>
    /// Inserts <paramref name="content"/> into <paramref name="host"/> at the position
    /// <paramref name="teleport"/>'s <see cref="Teleport.Order"/> puts it among the other
    /// <see cref="Teleport"/>s already rendering there.
    /// </summary>
    /// <remarks>
    /// The index is computed <i>relative to the teleported children only</i>: sorting before
    /// the first one inserts at its index, sorting after all of them inserts just past the
    /// last, and being the first arrival appends at the end. That keeps the host's own static
    /// children exactly where the host declared them (a <c>StackPanel</c>'s fixed header, a
    /// <c>DockPanel</c>'s status bar) while still giving the teleported ones a total order
    /// that does not depend on which one happened to load first.
    /// </remarks>
    internal static void AddTeleportedContent(Panel host, Teleport teleport, UIElement content)
    {
        var tracked = (List<Teleport>?)host.GetValue(TeleportedProperty);
        if (tracked is null)
        {
            tracked = [];
            host.SetValue(TeleportedProperty, tracked);
        }

        var position = 0;
        while (position < tracked.Count && Teleport.CompareOrder(tracked[position], teleport) <= 0)
            position++;

        // IndexOf can only return -1 if a tracked Teleport's content is somehow no longer a
        // child of this host; clamping keeps that a cosmetic ordering glitch rather than an
        // ArgumentOutOfRangeException thrown from inside a Loaded handler.
        // Every tracked Teleport has non-null TeleportedContent by construction - it is only
        // added here, immediately after that field is assigned, and removed the moment it is
        // cleared.
        var anchor = position == 0
            ? tracked.Count == 0 ? host.Children.Count : host.Children.IndexOf(tracked[0].TeleportedContent!)
            : host.Children.IndexOf(tracked[position - 1].TeleportedContent!) + 1;

        host.Children.Insert(Math.Clamp(anchor, 0, host.Children.Count), content);
        tracked.Insert(position, teleport);
    }

    /// <summary>Removes <paramref name="content"/> from <paramref name="host"/> and stops tracking <paramref name="teleport"/> there.</summary>
    internal static void RemoveTeleportedContent(Panel host, Teleport teleport, UIElement content)
    {
        host.Children.Remove(content);

        if (host.GetValue(TeleportedProperty) is not List<Teleport> tracked)
            return;

        tracked.Remove(teleport);

        if (tracked.Count == 0)
            host.ClearValue(TeleportedProperty);
    }

    private static void Register(string name, Panel panel)
    {
        Hosts[name] = panel;
        HostRegistered?.Invoke(name);
    }

    private static void Unregister(string name, Panel panel)
    {
        if (Hosts.TryGetValue(name, out var current) && ReferenceEquals(current, panel))
        {
            Hosts.Remove(name);
            HostUnregistered?.Invoke(name);
        }
    }
}
