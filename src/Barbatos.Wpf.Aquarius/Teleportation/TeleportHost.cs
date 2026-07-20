// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.Teleportation;

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
/// </remarks>
public static class TeleportHost
{
    private static readonly Dictionary<string, Panel> Hosts = [];

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
