// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;

namespace Barbatos.Wpf.Aquarius.Xaml;

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
/// keeps its object identity, its own state and any binding sourced from an inherited
/// <c>DataContext</c> that stays the same across the move keep working afterwards - it is
/// still "the same component" from a plain-C#-object point of view.
/// <para>
/// Unlike Vue's own <c>&lt;Teleport&gt;</c>, this does <b>not</b> extend to
/// <see cref="Composition.Lifecycle.EnableProperty"/> hooks: Vue's virtual-DOM diff decides
/// a vnode's final location before ever committing it to the real DOM, so a teleported
/// component's instance is never considered unmounted. WPF has no such pre-commit step -
/// moving an already-loaded element from one <see cref="Panel"/> to another is a genuine
/// detach followed by a reattach, so every move (including the very first time content
/// settles into a host that was already registered when this control loaded) fires a full
/// <c>OnBeforeUnmount</c>/<c>OnUnmounted</c> then <c>OnBeforeMount</c>/<c>OnMounted</c>
/// pair, exactly as if the content had been torn down and recreated. See
/// <c>TeleportTests</c> in the test project for the confirmed hook sequences.
/// </para>
/// <para>
/// <b>Several <see cref="Teleport"/>s may target the same host</b> - an overlay stack, a toolbar
/// assembled from widgets declared all over the app, a docking edge. Their relative order is
/// <see cref="Order"/>, never the order they happened to load in, so the result is the same
/// whether a host registers before or after its content. Pick the host <see cref="Panel"/> for
/// the layout you want - <c>UniformGrid</c>, <c>StackPanel</c>, <c>WrapPanel</c>,
/// <c>DockPanel</c>, <c>Grid</c> - and set that panel's own layout attached properties
/// (<c>DockPanel.Dock</c>, <c>Grid.Row</c>, ...) on the <i>content</i> inside this control, not
/// on the control itself: it is the content that becomes the panel's real child.
/// <code>
/// &lt;UniformGrid Rows="1" aq:TeleportHost.RegisterHost="Toolbar" /&gt;
/// &lt;!-- ...declared anywhere, in any order: --&gt;
/// &lt;aq:Teleport To="Toolbar" Order="10"&gt;&lt;views:SaveButton /&gt;&lt;/aq:Teleport&gt;
/// &lt;aq:Teleport To="Toolbar" Order="0"&gt;&lt;views:SearchBox /&gt;&lt;/aq:Teleport&gt;
/// </code>
/// </para>
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

    /// <summary>
    /// Where this control's content sits relative to the other <see cref="Teleport"/>s
    /// rendering into the same host - lower first. Defaults to <c>0</c>.
    /// </summary>
    /// <remarks>
    /// Without this, several <see cref="Teleport"/>s sharing a host would land in whatever
    /// order they happened to load in - which depends on where each one is declared in the
    /// tree, whether its host was already registered, and whether it sits inside a tab that
    /// has been opened yet. <see cref="Order"/> makes the result deterministic; ties break by
    /// creation order, so leaving every <see cref="Teleport"/> at the default still gives a
    /// stable (declaration-order) result rather than an arbitrary one.
    /// <para>
    /// Changing this while content is already teleported repositions <i>only</i> this control's
    /// content, leaving its siblings untouched - but repositioning is a real
    /// <see cref="Panel.Children"/> remove-then-insert (WPF's <c>UIElementCollection</c> has no
    /// move operation), so it fires the same full unmount/remount pair every other
    /// <see cref="Teleport"/> move does. See this type's own remarks.
    /// </para>
    /// </remarks>
    public static readonly DependencyProperty OrderProperty =
        DependencyProperty.Register(nameof(Order), typeof(int), typeof(Teleport), new PropertyMetadata(0, OnOrderChanged));

    private static long _sequenceCounter;

    private readonly long _sequence = Interlocked.Increment(ref _sequenceCounter);

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

    /// <inheritdoc cref="OrderProperty"/>
    public int Order
    {
        get => (int)GetValue(OrderProperty);
        set => SetValue(OrderProperty, value);
    }

    public Teleport()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>The content currently rendered into a host, for <see cref="TeleportHost"/>'s own index bookkeeping.</summary>
    internal UIElement? TeleportedContent => _teleportedContent;

    /// <summary>Orders two <see cref="Teleport"/>s sharing a host: <see cref="Order"/> first, creation order as a stable tie-break.</summary>
    internal static int CompareOrder(Teleport left, Teleport right)
    {
        var byOrder = left.Order.CompareTo(right.Order);
        return byOrder != 0 ? byOrder : left._sequence.CompareTo(right._sequence);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Only subscribe on a transition into the loaded state, never on every Loaded. WPF
        // raises Loaded a second time with no Unloaded in between (a TabControl realizes every
        // tab's content up front, then raises Loaded again on whichever one later becomes
        // selected - see Lifecycle's own OnLoaded), and `-=` removes exactly one subscription
        // per Unloaded. Subscribing unconditionally therefore left a live handler on a static,
        // process-lifetime event, pinning this control, its content subtree and its whole
        // DataContext graph forever. UpdateTeleport still runs on every Loaded, so a genuine
        // re-Loaded (a tab becoming selected after its host registered) is still picked up.
        if (!_loaded)
        {
            _loaded = true;
            TeleportHost.HostRegistered += OnHostRegistered;
            TeleportHost.HostUnregistered += OnHostUnregistered;
        }

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

    /// <summary>
    /// Repositions this control's content within the host it is already rendering into -
    /// deliberately not a <see cref="RestoreLocal"/>/<see cref="UpdateTeleport"/> round trip,
    /// which would bounce the content back through this control on the way and cost two
    /// unmount/remount cycles instead of one.
    /// </summary>
    private static void OnOrderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var teleport = (Teleport)d;

        if (teleport._teleportedContent is not { } content || teleport._currentHost is not { } host)
            return;

        TeleportHost.RemoveTeleportedContent(host, teleport, content);
        TeleportHost.AddTeleportedContent(host, teleport, content);
    }

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

            // Content must be cleared first: an element still owned by this ContentControl
            // has a logical parent, and Panel.Children.Add refuses to adopt one that does.
            Content = null;
            TeleportHost.AddTeleportedContent(host, this, content);
        }
    }

    private void RestoreLocal()
    {
        if (_teleportedContent is null)
            return;

        if (_currentHost is { } host)
            TeleportHost.RemoveTeleportedContent(host, this, _teleportedContent);

        Content = _teleportedContent;
        _teleportedContent = null;
        _currentHost = null;
    }
}
