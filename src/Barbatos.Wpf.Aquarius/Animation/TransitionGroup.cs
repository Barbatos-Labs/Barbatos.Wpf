// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;

namespace Barbatos.Wpf.Aquarius.Animation;

/// <summary>
/// Plays an animation as items are added to an <see cref="ItemsControl"/> - the Aquarius
/// counterpart of Vue's <c>&lt;TransitionGroup&gt;</c>.
/// </summary>
/// <remarks>
/// <code>
/// &lt;ListBox ItemsSource="{Binding Items}" aq:TransitionGroup.Enter="{StaticResource FadeIn}" /&gt;
/// </code>
/// <b>Scope note:</b> this only ports the enter side. Vue's real <c>&lt;TransitionGroup&gt;</c>
/// also animates items *leaving* the list and, more remarkably, computes each surviving
/// item's position delta and animates the reflow (FLIP) when the list reorders - both
/// require intercepting removal/reflow *before* the panel actually applies it. For a
/// plain <see cref="ItemsControl"/>/<see cref="Panel"/> that means owning a custom panel
/// that defers container removal for its own animation, a materially bigger and riskier
/// problem than enter (virtualization/container-recycling interactions especially) - out
/// of scope for this pass, left for a dedicated follow-up rather than rushed here.
///
/// Works by giving every newly-generated item container a one-time <c>Loaded</c> hook
/// that plays <see cref="EnterProperty"/> (cloned per container, the same reason
/// <see cref="Transition"/> clones its storyboards - see its remarks). Since
/// <c>Loaded</c> only fires when a container is genuinely, newly attached to a live
/// visual tree, already-realized containers are untouched with no extra bookkeeping
/// needed to tell "new" from "existing". One caveat: a *virtualizing* panel
/// (the default for <see cref="ListBox"/>/<see cref="ListView"/>) can satisfy a newly
/// added item by reusing a recycled container instead of creating a new one, and a
/// reused container does not re-fire <c>Loaded</c> - if the enter animation should be
/// guaranteed for every new item, disable virtualization
/// (<c>VirtualizingPanel.IsVirtualizing="False"</c>) on lists small enough for that to be
/// affordable.
/// </remarks>
public static class TransitionGroup
{
    /// <summary>Played once for each item container newly added to the live visual tree.</summary>
    public static readonly DependencyProperty EnterProperty =
        DependencyProperty.RegisterAttached(
            "Enter",
            typeof(Storyboard),
            typeof(TransitionGroup),
            new PropertyMetadata(null, OnEnterChanged));

    /// <summary>Sets <see cref="EnterProperty"/>.</summary>
    public static void SetEnter(DependencyObject element, Storyboard? value) => element.SetValue(EnterProperty, value);

    /// <summary>Gets <see cref="EnterProperty"/>.</summary>
    public static Storyboard? GetEnter(DependencyObject element) => (Storyboard?)element.GetValue(EnterProperty);

    private static readonly DependencyProperty StateProperty =
        DependencyProperty.RegisterAttached("State", typeof(EnterState), typeof(TransitionGroup));

    private static readonly DependencyProperty HookedProperty =
        DependencyProperty.RegisterAttached("Hooked", typeof(bool), typeof(TransitionGroup), new PropertyMetadata(false));

    private static void OnEnterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ItemsControl itemsControl)
            throw new InvalidOperationException("TransitionGroup.Enter can only be set on an ItemsControl.");

        if (d.GetValue(StateProperty) is EnterState existing)
        {
            existing.Detach();
            d.ClearValue(StateProperty);
        }

        if (e.NewValue is Storyboard)
        {
            var state = new EnterState(itemsControl);
            d.SetValue(StateProperty, state);
            state.Attach();
        }
    }

    private sealed class EnterState(ItemsControl itemsControl)
    {
        public void Attach() => itemsControl.ItemContainerGenerator.StatusChanged += OnStatusChanged;

        public void Detach() => itemsControl.ItemContainerGenerator.StatusChanged -= OnStatusChanged;

        private void OnStatusChanged(object? sender, EventArgs e)
        {
            if (itemsControl.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                return;

            foreach (var item in itemsControl.Items)
            {
                if (itemsControl.ItemContainerGenerator.ContainerFromItem(item) is FrameworkElement container)
                    HookLoaded(container);
            }
        }

        private void HookLoaded(FrameworkElement container)
        {
            if ((bool)container.GetValue(HookedProperty))
                return;

            container.SetValue(HookedProperty, true);
            container.Loaded += OnContainerLoaded;
        }

        private void OnContainerLoaded(object sender, RoutedEventArgs e) =>
            GetEnter(itemsControl)?.Clone().Begin((FrameworkElement)sender);
    }
}
