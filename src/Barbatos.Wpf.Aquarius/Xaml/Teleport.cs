// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.Xaml;

/// <summary>
/// Renders its content into a different part of the visual tree - the Aquarius
/// counterpart of Vue's <c>&lt;Teleport to="#target"&gt;</c>.
/// </summary>
/// <remarks>
/// <code>
/// &lt;aq:Teleport To="Overlay" Disabled="{Binding IsCompact}"&gt;
///     &lt;views:ToastView /&gt;
/// &lt;/aq:Teleport&gt;
/// </code>
/// While mounted and enabled, the <see cref="ContentControl.Content"/> element is
/// detached from this control and added to the <see cref="Panel.Children"/> of whichever
/// <see cref="Panel"/> registered itself via <see cref="TeleportHost.RegisterHostProperty"/>
/// under <see cref="To"/> - commonly an overlay <c>Grid</c> pinned at a window's root, so
/// content declared deep inside some nested layout (and subject to its clipping/z-order)
/// can still render pinned above everything else. <see cref="Disabled"/> (mirroring
/// Vue's <c>:disabled</c>) keeps the content local instead. Because the moved element
/// keeps its object identity, its own bindings/<c>DataContext</c>/
/// <see cref="Composition.Lifecycle.EnableProperty"/> hooks keep working after the move - it is
/// still "the same component", just rendered elsewhere.
/// <para>
/// This is also the building block for a dockable panel: keep this <see cref="Teleport"/>
/// declared in one stable place (e.g. the main window) and flip <see cref="To"/> between a
/// docked host and a floating dialog's host to move the same live panel back and forth. If
/// the current host disappears while still holding this control's content - e.g. its owning
/// floating <c>Window</c> is closed without first flipping <see cref="To"/> back - the content
/// is automatically brought home instead of being torn down with it, via
/// <see cref="TeleportHost.HostUnregistered"/>; <see cref="To"/> itself is left unchanged, so
/// re-registering the same host name later (e.g. reopening that dialog) re-floats the content
/// there again automatically.
/// </para>
/// </remarks>
public class Teleport : ContentControl
{
    /// <summary>The registered <see cref="TeleportHost"/> name to render into.</summary>
    public static readonly DependencyProperty ToProperty =
        DependencyProperty.Register(nameof(To), typeof(string), typeof(Teleport), new PropertyMetadata(null, OnToOrDisabledChanged));

    /// <summary>When <c>true</c>, content renders locally instead of teleporting.</summary>
    public static readonly DependencyProperty DisabledProperty =
        DependencyProperty.Register(nameof(Disabled), typeof(bool), typeof(Teleport), new PropertyMetadata(false, OnToOrDisabledChanged));

    private UIElement? _teleportedContent;
    private Panel? _currentHost;
    private bool _loaded;

    /// <inheritdoc cref="ToProperty"/>
    public string? To
    {
        get => (string?)GetValue(ToProperty);
        set => SetValue(ToProperty, value);
    }

    /// <inheritdoc cref="DisabledProperty"/>
    public bool Disabled
    {
        get => (bool)GetValue(DisabledProperty);
        set => SetValue(DisabledProperty, value);
    }

    public Teleport()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _loaded = true;
        TeleportHost.HostRegistered += OnHostRegistered;
        TeleportHost.HostUnregistered += OnHostUnregistered;
        UpdateTeleport();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _loaded = false;
        TeleportHost.HostRegistered -= OnHostRegistered;
        TeleportHost.HostUnregistered -= OnHostUnregistered;
        RestoreLocal();
    }

    private void OnHostRegistered(string name)
    {
        if (name == To)
            UpdateTeleport();
    }

    private void OnHostUnregistered(string name)
    {
        // The host our content was actually sitting in just disappeared (e.g. its owning
        // dialog Window closed) - bring the content home instead of leaving it orphaned
        // inside a torn-down visual tree. To is left untouched: if a host under this same
        // name registers again later, content re-floats there automatically, same as any
        // host that was simply slow to load in the first place.
        if (name == To && _teleportedContent is not null)
            RestoreLocal();
    }

    private static void OnToOrDisabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((Teleport)d).UpdateTeleport();

    private void UpdateTeleport()
    {
        if (!_loaded)
            return;

        if (Disabled || To is not { } targetName || !TeleportHost.TryGetHost(targetName, out var host))
        {
            RestoreLocal();
            return;
        }

        if (ReferenceEquals(_currentHost, host) && _teleportedContent is not null)
            return;

        RestoreLocal();

        if (Content is UIElement content)
        {
            _teleportedContent = content;
            _currentHost = host;
            Content = null;
            host.Children.Add(content);
        }
    }

    private void RestoreLocal()
    {
        if (_teleportedContent is null)
            return;

        _currentHost?.Children.Remove(_teleportedContent);
        Content = _teleportedContent;
        _teleportedContent = null;
        _currentHost = null;
    }
}
