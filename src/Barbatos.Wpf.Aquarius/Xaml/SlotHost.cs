// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Barbatos.Wpf.Aquarius.Xaml;

/// <summary>
/// Base class for a custom control that wants free-form named slots - the Aquarius
/// counterpart of a Vue component template with <c>&lt;slot&gt;</c>/<c>&lt;slot name="x"&gt;</c>
/// outlets.
/// </summary>
/// <remarks>
/// A component author derives from <see cref="SlotHost"/> (e.g. <c>public class Card : SlotHost</c>)
/// and writes its own <c>ControlTemplate</c> using <see cref="SlotContent"/>/<see cref="SlotProvided"/>
/// to pull named content back out - see those types' own remarks for the full worked example.
/// Consumers write a flat mix of <see cref="Slot"/>-wrapped (named) and plain (implicit
/// default slot) children directly inside the control, exactly mirroring Vue's own rule that
/// "all top-level non-&lt;template&gt; nodes are implicitly treated as content for the
/// default slot":
/// <code>
/// &lt;local:Card&gt;
///     &lt;aq:Slot Name="header"&gt;&lt;TextBlock Text="Title" /&gt;&lt;/aq:Slot&gt;
///     &lt;TextBlock Text="Default slot content" /&gt;
/// &lt;/local:Card&gt;
/// </code>
///
/// This must derive from <see cref="Control"/> specifically, not <see cref="ContentControl"/>
/// or <see cref="ItemsControl"/>: <see cref="SlotContent"/>/<see cref="SlotProvided"/>'s
/// reactive lookup depends on <see cref="System.Windows.Data.RelativeSource.TemplatedParent"/>,
/// which only resolves for content rendered through a real <see cref="Control.Template"/> - a
/// plain <see cref="FrameworkElement"/> has no <c>Template</c> at all, and
/// <see cref="ItemsControl"/> would add unwanted automatic layout of <see cref="Items"/>,
/// which this class deliberately has none of - the derived control's own <c>ControlTemplate</c>
/// does 100% of the visual layout, picking named pieces out of <see cref="Items"/> one at a
/// time.
///
/// <see cref="Items"/> is watched for changes and re-resolved into <see cref="ResolvedSlots"/>
/// (a real, read-only <see cref="DependencyProperty"/>) on every add/remove - reassigning a
/// real <see cref="DependencyProperty"/> is what makes bindings sourced from it re-evaluate,
/// so adding or removing a <see cref="Slot"/> at runtime (not just during initial XAML parse)
/// updates whatever is currently displaying it. Mutating an already-added <see cref="Slot"/>'s
/// own <see cref="Slot.Name"/>/<see cref="Slot.Content"/> in place will *not* trigger
/// re-resolution, since <see cref="Slot"/> deliberately has no property-changed notification
/// of its own (the same well-known characteristic <see cref="ObservableCollection{T}"/>
/// itself already has toward its own elements) - replace the <see cref="Slot"/> object in
/// <see cref="Items"/> instead of mutating it.
///
/// Two items claiming the same slot name (including two un-wrapped items both implicitly
/// claiming the default slot) throws <see cref="InvalidOperationException"/> - each slot
/// (default included) holds exactly one content object, the same rule <see cref="If.Child"/>/
/// <see cref="Suspense.Child"/>/<see cref="Teleport"/>'s content already have throughout this
/// library; wrap multiple sibling nodes for one slot in a single <see cref="Panel"/> instead.
/// </remarks>
[ContentProperty(nameof(Items))]
public class SlotHost : Control
{
    private static readonly IReadOnlyDictionary<string, object?> EmptySlots = new Dictionary<string, object?>();

    private static readonly DependencyPropertyKey ResolvedSlotsPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(ResolvedSlots),
            typeof(IReadOnlyDictionary<string, object?>),
            typeof(SlotHost),
            new PropertyMetadata(EmptySlots));

    /// <summary>The current name-to-content map, recomputed from <see cref="Items"/> on every change.</summary>
    public static readonly DependencyProperty ResolvedSlotsProperty = ResolvedSlotsPropertyKey.DependencyProperty;

    /// <summary>
    /// The XAML content property: a flat mix of <see cref="Slot"/>-wrapped (named) and plain
    /// (implicit default) child content.
    /// </summary>
    public ObservableCollection<object> Items { get; } = [];

    /// <inheritdoc cref="ResolvedSlotsProperty"/>
    public IReadOnlyDictionary<string, object?> ResolvedSlots => (IReadOnlyDictionary<string, object?>)GetValue(ResolvedSlotsProperty);

    public SlotHost() => Items.CollectionChanged += OnItemsChanged;

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Resolve();

    private void Resolve()
    {
        var resolved = new Dictionary<string, object?>();

        foreach (var item in Items)
        {
            var (name, content) = item is Slot slot ? (slot.Name, slot.Content) : ("", item);

            if (!resolved.TryAdd(name, content))
            {
                var label = name.Length == 0 ? "the default slot" : $"the slot named '{name}'";
                throw new InvalidOperationException($"SlotHost: more than one item claims {label}.");
            }
        }

        SetValue(ResolvedSlotsPropertyKey, resolved);
    }

    /// <summary>
    /// Reads the current content for <paramref name="name"/> directly, without going through
    /// XAML - the C#-callable counterpart of <see cref="SlotContent"/>, mirroring
    /// <see cref="Composition.Inject.Get{T}"/>'s relationship to <c>{aq:Inject}</c>. Returns
    /// <see langword="null"/> both when the slot was never provided and when it was provided
    /// but empty - use <see cref="IsSlotProvided"/> to distinguish those.
    /// </summary>
    protected object? GetSlotContent(string name = "") => ResolvedSlots.TryGetValue(name, out var content) ? content : null;

    /// <summary>
    /// Whether <paramref name="name"/> was actually supplied by the consumer - the
    /// C#-callable counterpart of <see cref="SlotProvided"/>, mirroring Vue's <c>$slots.x</c>.
    /// </summary>
    protected bool IsSlotProvided(string name = "") => ResolvedSlots.ContainsKey(name);
}
