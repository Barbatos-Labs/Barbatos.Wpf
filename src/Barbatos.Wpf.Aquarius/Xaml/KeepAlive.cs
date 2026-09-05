// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace Barbatos.Wpf.Aquarius.Xaml;

/// <summary>
/// Caches the Views a <see cref="Component"/> switches between instead of rebuilding them - the
/// Aquarius counterpart of Vue's <c>&lt;KeepAlive&gt;</c>.
/// </summary>
/// <remarks>
/// <code>
/// &lt;aq:KeepAlive Max="3" Exclude="ReportView"&gt;
///     &lt;aq:Component Is="{Binding CurrentView.Value}" /&gt;
/// &lt;/aq:KeepAlive&gt;
/// </code>
/// Exactly Vue's shape: a <c>&lt;KeepAlive&gt;</c> wrapping one dynamic component. Switching
/// <see cref="Component.Is"/> reveals the cached View for that type, building it only the first
/// time. A ViewModel under <see cref="Composition.Lifecycle.EnableProperty"/> therefore sees only
/// <c>IOnDeactivated</c> when you switch away and <c>IOnActivated</c> when you come back - never an
/// unmount - so everything it holds (a scroll position, a half-filled form, a loaded page of
/// results) is still there. Without the wrapper, <see cref="Component"/> destroys the outgoing View
/// as Vue does.
/// <para>
/// That is what neither a <c>TabControl</c> nor <c>Frame</c>/<c>Page</c> gives you: both look like
/// they should, and both were confirmed <i>by running them</i> to genuinely destroy and recreate
/// content on every switch - see <c>KeepAliveTests</c> in the test project.
/// </para>
/// <para>
/// <b>What still unmounts</b>, exactly as in Vue: a View evicted by <see cref="Max"/>, one filtered
/// out by <see cref="Include"/>/<see cref="Exclude"/>, and every cached View when this control
/// itself is unloaded. That last one is a WPF fact rather than a choice - <c>Loaded</c>/
/// <c>Unloaded</c> are broadcast through a subtree regardless of <c>Visibility</c> - so putting a
/// <see cref="KeepAlive"/> inside a <c>TabItem</c> or an <see cref="If"/> means every cached View
/// gets a real unmount/remount cycle whenever that container comes and goes. The <i>ViewModel
/// instances</i> survive that (their <c>DataContext</c> is never touched), so state is still
/// preserved; the hooks simply run again.
/// </para>
/// <para>
/// <b>This control owns its children's <see cref="UIElement.Visibility"/></b>, writing it as a
/// local value - so do not also put <see cref="Directives.ShowProperty"/> on a cached View, or the
/// two will fight over it. Adding children to <see cref="Panel.Children"/> from outside throws:
/// they would have no cache entry and no eviction position, and would simply sit on top of
/// whatever is active.
/// </para>
/// </remarks>
[ContentProperty(nameof(Child))]
public class KeepAlive : Panel
{
    /// <summary>
    /// The most Views to keep cached at once, least-recently-activated evicted first. <c>0</c>
    /// (the default) means unlimited - mirroring Vue's <c>max</c>, which is likewise opt-in.
    /// </summary>
    /// <remarks>
    /// Counts every cached View including the one showing, the same way Vue's <c>max</c> does.
    /// Eviction is a real unmount, so the evicted View's ViewModel state is gone - switching back
    /// to it builds from scratch.
    /// </remarks>
    public static readonly DependencyProperty MaxProperty =
        DependencyProperty.Register(nameof(Max), typeof(int), typeof(KeepAlive), new PropertyMetadata(0, OnMaxChanged));

    /// <summary>
    /// A comma-separated allow-list of View <b>type names</b> that may be cached; anything else is
    /// dropped as soon as you switch away from it. Unset (the default) caches everything.
    /// </summary>
    /// <remarks>
    /// Matches <see cref="System.Reflection.MemberInfo.Name"/> - the WPF counterpart of Vue's
    /// <c>include</c>, which matches a component's <c>name</c> option. Vue's regex and array forms
    /// have no concise XAML literal, so the comma-separated string is the one form here, the same
    /// shape <see cref="Directives.ModifiersProperty"/> already uses.
    /// </remarks>
    public static readonly DependencyProperty IncludeProperty =
        DependencyProperty.Register(nameof(Include), typeof(string), typeof(KeepAlive), new PropertyMetadata(null, OnCacheFilterChanged));

    /// <summary>
    /// A comma-separated deny-list of View type names that must never be cached - the inverse of
    /// <see cref="Include"/>, and checked after it. The natural home for the one screen that is too
    /// expensive to keep in memory, or that must re-read its data on every visit.
    /// </summary>
    public static readonly DependencyProperty ExcludeProperty =
        DependencyProperty.Register(nameof(Exclude), typeof(string), typeof(KeepAlive), new PropertyMetadata(null, OnCacheFilterChanged));

    /// <summary>Cached Views by type, plus the least-recently-activated-first order <see cref="Max"/> evicts along.</summary>
    private readonly Dictionary<Type, CacheEntry> _entries = [];
    private readonly List<Type> _recency = [];

    private Component? _child;
    private Type? _activeKey;
    private bool _mutating;

    /// <summary>Creates a <see cref="KeepAlive"/>.</summary>
    public KeepAlive()
    {
        // Loaded, not the Child setter: a bound Component.Is has not resolved its DataContext yet
        // at parse time, so the first meaningful read of it is here - the same "pull-based and
        // Loaded-gated" rule every other cross-tree lookup in this library follows.
        Loaded += (_, _) => Update();
    }

    /// <inheritdoc cref="MaxProperty"/>
    public int Max
    {
        get => (int)GetValue(MaxProperty);
        set => SetValue(MaxProperty, value);
    }

    /// <inheritdoc cref="IncludeProperty"/>
    public string? Include
    {
        get => (string?)GetValue(IncludeProperty);
        set => SetValue(IncludeProperty, value);
    }

    /// <inheritdoc cref="ExcludeProperty"/>
    public string? Exclude
    {
        get => (string?)GetValue(ExcludeProperty);
        set => SetValue(ExcludeProperty, value);
    }

    /// <summary>
    /// The single dynamic component this control caches for - the XAML content property. Vue's
    /// <c>&lt;KeepAlive&gt;</c> likewise takes exactly one dynamic child.
    /// </summary>
    /// <remarks>
    /// It is a logical child but never a visual one: the resolved Views are what this panel
    /// renders, and the <see cref="Component"/> itself is only the declaration of which one. Being
    /// a logical child is what lets <c>Is="{Binding ...}"</c> inherit this control's
    /// <c>DataContext</c> - see <see cref="LogicalChildren"/>.
    /// </remarks>
    public Component? Child
    {
        get => _child;
        set
        {
            if (ReferenceEquals(_child, value))
                return;

            if (_child is { } previous)
            {
                previous.IsChanged -= OnChildIsChanged;
                previous.Owner = null;
                RemoveLogicalChild(previous);
            }

            _child = value;

            if (_child is { } next)
            {
                AddLogicalChild(next);
                next.Owner = this;
                next.IsChanged += OnChildIsChanged;
            }

            Update();
        }
    }

    /// <summary>The View currently revealed, or <see langword="null"/> if nothing is.</summary>
    public FrameworkElement? ActiveView => _activeKey is { } key && _entries.TryGetValue(key, out var entry) ? entry.Element : null;

    /// <summary>
    /// Every currently cached View type, least-recently-activated first - a snapshot, safe to
    /// iterate while calling <see cref="Release"/> for the entries it names.
    /// </summary>
    public IReadOnlyList<Type> CachedTypes => [.. _recency];

    /// <inheritdoc/>
    /// <remarks>
    /// <see cref="Child"/> is reported here so WPF's property inheritance reaches it - without
    /// that, a <c>Is="{Binding ...}"</c> on a <see cref="Component"/> that is deliberately not a
    /// visual child would have no <c>DataContext</c> to resolve against and would silently never
    /// produce a value.
    /// </remarks>
    protected override IEnumerator LogicalChildren
    {
        get
        {
            if (_child is { } child)
                yield return child;

            foreach (UIElement cached in InternalChildren)
                yield return cached;
        }
    }

    /// <summary>Whether <paramref name="viewType"/> may stay mounted after you switch away from it, per <see cref="Include"/>/<see cref="Exclude"/>.</summary>
    public bool IsCacheable(Type viewType)
    {
        ArgumentNullException.ThrowIfNull(viewType);

        if (Directives.ParseModifiers(Include) is { Count: > 0 } include && !include.Contains(viewType.Name))
            return false;

        return !Directives.ParseModifiers(Exclude).Contains(viewType.Name);
    }

    /// <summary>Whether <paramref name="viewType"/> currently has a cached View, and what it is.</summary>
    public bool TryGetCached(Type viewType, out FrameworkElement element)
    {
        ArgumentNullException.ThrowIfNull(viewType);

        if (_entries.TryGetValue(viewType, out var entry))
        {
            element = entry.Element;
            return true;
        }

        element = null!;
        return false;
    }

    /// <summary>
    /// Drops <paramref name="viewType"/>'s cached View from the tree entirely - a real unmount,
    /// not a hide. Returns whether anything was cached for it.
    /// </summary>
    public bool Release(Type viewType)
    {
        ArgumentNullException.ThrowIfNull(viewType);

        if (!_entries.TryGetValue(viewType, out var entry))
            return false;

        if (_activeKey == viewType)
        {
            // Collapse first so the View reports OnDeactivated before OnUnmounted, matching the
            // order a plain removal produces anyway (IsVisibleChanged fires synchronously during
            // the disconnect, ahead of Unloaded).
            entry.Element.Visibility = Visibility.Collapsed;
            _activeKey = null;
        }

        _entries.Remove(viewType);
        _recency.Remove(viewType);
        Mutate(() => Children.Remove(entry.Element));

        return true;
    }

    /// <summary>Releases every cached View - the escape hatch for "this whole cache is stale now".</summary>
    public void ReleaseAll()
    {
        foreach (var viewType in CachedTypes)
            Release(viewType);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Collapsed children short-circuit inside <see cref="UIElement.Measure"/> to a <c>(0,0)</c>
    /// <see cref="UIElement.DesiredSize"/> without ever entering their own measure pass, so in
    /// practice this is "the size of the View being shown" - a cached View that is hidden costs
    /// nothing, and one that has never been shown does not even have its template applied.
    /// </remarks>
    protected override Size MeasureOverride(Size availableSize)
    {
        var desired = new Size();

        foreach (UIElement child in InternalChildren)
        {
            child.Measure(availableSize);
            desired.Width = Math.Max(desired.Width, child.DesiredSize.Width);
            desired.Height = Math.Max(desired.Height, child.DesiredSize.Height);
        }

        return desired;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (UIElement child in InternalChildren)
            child.Arrange(new Rect(finalSize));

        return finalSize;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <see cref="Panel"/> declares <see cref="Panel.Children"/> as its XAML content property, and
    /// although <see cref="ContentPropertyAttribute"/> redirects that to <see cref="Child"/> here,
    /// nothing stops code from adding to <c>Children</c> directly - and such a child would have no
    /// cache entry and no eviction position, and would be arranged full-size over whatever is
    /// showing. Failing loudly beats that silent layout bug, matching how <see cref="TeleportHost"/>
    /// and <see cref="Animation.TransitionGroup"/> already reject misuse.
    /// </remarks>
    protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
    {
        if (visualAdded is not null && !_mutating)
        {
            throw new InvalidOperationException(
                "KeepAlive manages its own Children - put an <aq:Component Is=\"...\"/> inside it, don't add to Children directly.");
        }

        base.OnVisualChildrenChanged(visualAdded, visualRemoved);
    }

    private static void OnMaxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((KeepAlive)d).Trim();

    private static void OnCacheFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((KeepAlive)d).ApplyCacheFilters();

    private void OnChildIsChanged(object? sender, EventArgs e) => Update();

    /// <summary>
    /// Brings what is showing back in line with <see cref="Child"/>'s current
    /// <see cref="Component.Is"/>.
    /// </summary>
    /// <remarks>
    /// Deliberately re-derives the answer every time rather than short-circuiting on "the type
    /// hasn't changed since I last looked" - that bookkeeping goes stale the moment the cache is
    /// emptied from outside through <see cref="Release"/>/<see cref="ReleaseAll"/>, and would
    /// leave this control blank for good. Re-deriving costs a dictionary lookup, since revealing
    /// the View that is already showing is a no-op.
    /// </remarks>
    private void Update()
    {
        if (_child?.Is is not { } viewType)
        {
            DeactivateCurrent(next: null);
            return;
        }

        if (_entries.TryGetValue(viewType, out var existing))
        {
            // Already cached - a reveal, never a rebuild. Whether it may stay cached can have
            // changed since (an Include/Exclude edit), so keep the entry's own flag current.
            existing.Cacheable = IsCacheable(viewType);
            Reveal(viewType, existing);
            return;
        }

        var entry = new CacheEntry(_child.Resolve()) { Cacheable = IsCacheable(viewType) };
        _entries[viewType] = entry;

        // Hide whatever is showing BEFORE the new View enters the tree, so the hooks come out as
        // OnDeactivated-then-OnActivated rather than interleaved. Adding the child already Visible
        // matters too: WPF updates IsVisible synchronously as the visual parent changes, well
        // before the Loaded broadcast reaches it, so Lifecycle sees the right value and reports
        // OnMounted then OnActivated exactly once.
        DeactivateCurrent(viewType);

        entry.Element.Visibility = Visibility.Visible;
        Mutate(() => Children.Add(entry.Element));

        _activeKey = viewType;
        Touch(viewType);
        Trim();
        RestoreFocus(entry);
    }

    private void Reveal(Type viewType, CacheEntry entry)
    {
        if (_activeKey == viewType)
            return;

        DeactivateCurrent(viewType);

        entry.Element.Visibility = Visibility.Visible;
        _activeKey = viewType;
        Touch(viewType);
        RestoreFocus(entry);
    }

    /// <summary>
    /// Hides the View that is showing (and drops it outright if it is not cacheable), remembering
    /// where keyboard focus was so <see cref="RestoreFocus"/> can put it back on return.
    /// </summary>
    private void DeactivateCurrent(Type? next)
    {
        if (_activeKey is not { } current || current == next)
            return;

        if (!_entries.TryGetValue(current, out var entry))
        {
            _activeKey = null;
            return;
        }

        // A TabControl restores focus per tab; losing it on every switch would be a visible
        // regression against the primitive this control replaces.
        entry.FocusedElement = Keyboard.FocusedElement is DependencyObject focused && entry.Element.IsAncestorOf(focused)
            ? focused as IInputElement
            : null;

        entry.Element.Visibility = Visibility.Collapsed;
        _activeKey = null;

        if (!entry.Cacheable)
            Release(current);
    }

    private void RestoreFocus(CacheEntry entry)
    {
        if (entry.FocusedElement is { } focused)
        {
            entry.FocusedElement = null;
            Keyboard.Focus(focused);
        }
    }

    private void Touch(Type viewType)
    {
        _recency.Remove(viewType);
        _recency.Add(viewType);
    }

    /// <summary>Evicts least-recently-activated Views until the cache fits <see cref="Max"/>, never touching the one showing.</summary>
    private void Trim()
    {
        var max = Max;
        if (max <= 0)
            return;

        for (var i = 0; i < _recency.Count && _entries.Count > max; i++)
        {
            var candidate = _recency[i];
            if (candidate == _activeKey)
                continue;

            Release(candidate);
            i--;
        }
    }

    /// <summary>
    /// Applies an <see cref="Include"/>/<see cref="Exclude"/> edit to what is <i>already</i>
    /// cached, not just to future switches - Vue watches both for exactly this reason. Without it,
    /// adding a type to <see cref="Exclude"/> would leave it sitting in the cache, mounted, until
    /// something else happened to evict it.
    /// </summary>
    /// <remarks>
    /// The View currently showing is marked rather than released: dropping it outright would blank
    /// the control, so it goes the moment you switch away instead.
    /// </remarks>
    private void ApplyCacheFilters()
    {
        foreach (var viewType in CachedTypes)
        {
            if (!_entries.TryGetValue(viewType, out var entry))
                continue;

            entry.Cacheable = IsCacheable(viewType);

            if (!entry.Cacheable && _activeKey != viewType)
                Release(viewType);
        }
    }

    /// <summary>Runs a <see cref="Panel.Children"/> mutation this control is making itself, past <see cref="OnVisualChildrenChanged"/>'s guard.</summary>
    private void Mutate(Action mutation)
    {
        _mutating = true;
        try
        {
            mutation();
        }
        finally
        {
            _mutating = false;
        }
    }

    private sealed class CacheEntry(FrameworkElement element)
    {
        public FrameworkElement Element { get; } = element;

        public bool Cacheable { get; set; } = true;

        /// <summary>Where keyboard focus was when this entry was last deactivated, if anywhere inside it.</summary>
        public IInputElement? FocusedElement { get; set; }
    }
}
