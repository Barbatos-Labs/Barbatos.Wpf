// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Barbatos.Wpf.Aquarius.Xaml;

/// <summary>
/// Conditionally renders its content - the Aquarius counterpart of Vue's <c>v-if</c>
/// (plus <see cref="Else"/> for <c>v-else</c>).
/// </summary>
/// <remarks>
/// <code>
/// &lt;aq:If Condition="{Binding ShowPanel}"&gt;
///     &lt;TextBlock Text="Only in the tree while ShowPanel is true" /&gt;
/// &lt;/aq:If&gt;
/// </code>
/// Unlike <see cref="Directives.ShowProperty"/> (v-show, which only toggles
/// <see cref="UIElement.Visibility"/>), <see cref="Condition"/> going <c>false</c>
/// genuinely detaches the content from the visual tree - the same "destroy" Vue performs
/// for <c>v-if</c> - rather than just hiding it. The child is kept in
/// <see cref="Child"/> (the XAML content property) so it can be reattached to
/// <see cref="ContentControl.Content"/> unchanged once <see cref="Condition"/> is
/// <c>true</c> again.
///
/// Detaching a live <see cref="FrameworkElement"/> fires WPF's own <c>Unloaded</c> event
/// on it (and reattaching fires <c>Loaded</c>), so a child under
/// <see cref="Composition.Lifecycle.EnableProperty"/> genuinely receives <c>IOnUnmounted</c>/
/// <c>IOnMounted</c> calls as <see cref="Condition"/> toggles - the same way Vue actually
/// destroys and recreates a <c>v-if</c> subtree. The same is true of <see cref="Else"/>'s
/// content once it mounts.
///
/// <b>v-else-if</b> has no separate control - nest another <see cref="If"/> inside the
/// outer one's <c>&lt;aq:If.Else&gt;</c>:
/// <code>
/// &lt;aq:If Condition="{aq:Expr 'type == &amp;quot;A&amp;quot;'}"&gt;
///     A content
///     &lt;aq:If.Else&gt;
///         &lt;aq:If Condition="{aq:Expr 'type == &amp;quot;B&amp;quot;'}"&gt;
///             B content
///             &lt;aq:If.Else&gt;Fallback content&lt;/aq:If.Else&gt;
///         &lt;/aq:If&gt;
///     &lt;/aq:If.Else&gt;
/// &lt;/aq:If&gt;
/// </code>
/// This is correct "for free": each nested <see cref="If"/> keeps its own <see cref="ContentControl.Content"/>
/// current independent of whether its branch is currently attached, and jumping straight
/// from branch A to the final fallback (skipping B) never touches B's content or mounts
/// B's <c>DataContext</c> at all - only the outer <see cref="If"/>'s own <see cref="Condition"/>
/// changing ever attaches/detaches the nested one. It does read visibly worse than Vue's
/// flat sibling <c>v-else-if</c> past 3-4 branches, since WPF's content model has no
/// equivalent to "grouped flat siblings" - a <c>Switch</c>/<c>Case</c> control would be the
/// natural escape hatch if that becomes painful, deliberately not built here since it
/// wasn't asked for.
/// <para>
/// <b>Performance: prefer <see cref="Directives.ShowProperty"/> for content that toggles
/// often - including "realtime" data (a feed, a sensor reading, anything updating several
/// times a second).</b> Many synchronous <see cref="Condition"/> flips that all happen
/// before WPF's dispatcher next runs layout (e.g. several updates processed back to back in
/// the same callstack) cost nothing extra - only the net transition by the time layout
/// actually runs fires <c>Unloaded</c>/<c>Loaded</c>, verified in
/// <c>IfControlTests.ManySynchronousConditionFlipsBeforeAPumpCoalesceIntoAtMostOneMountUnmountPair</c>.
/// That coalescing is the exception, not the rule, though: it only happens because those
/// flips share one callstack. A periodic source - a <c>DispatcherTimer</c> tick, a message
/// from a live feed, anything arriving as its own separate dispatcher operation - gets none
/// of it, because <c>DispatcherTimer</c>'s default priority (<c>Background</c>) is lower
/// than the <c>Loaded</c>-priority connectivity work a <see cref="Condition"/> change
/// queues, so that work always drains before the next tick can even fire. Verified directly:
/// 5 timer ticks a second produced 5 full, uncoalesced mount-or-unmount sequences, not one
/// net transition (<c>IfControlTests.TimerDrivenTogglesFarApartEachProduceTheirOwnFullMountUnmountCycle</c>).
/// Each full cycle measured at roughly 0.7ms for a trivial child with no-op hooks, and more
/// for a visually complex subtree or hooks that do real work, since every hook re-runs from
/// scratch on every remount (see <see cref="Composition.Lifecycle"/>) - so a "realtime"
/// value driving <see cref="Condition"/> is exactly the case to avoid, matching Vue's own
/// documented advice for <c>v-if</c> vs <c>v-show</c>: reach for <c>Show</c> when the
/// condition changes frequently, and save <see cref="If"/> for content that changes rarely
/// (a tab's content, a logged-in/logged-out split, ...).
/// </para>
/// </remarks>
[ContentProperty(nameof(Child))]
public class If : ContentControl
{
    /// <summary>
    /// The content to conditionally render. This - not the inherited
    /// <see cref="ContentControl.Content"/> - is the XAML content property, since
    /// <see cref="ContentControl.Content"/> itself is reserved for whatever is currently
    /// actually displayed (<see cref="Else"/>, or <c>null</c> if unset, while <see cref="Condition"/>
    /// is <c>false</c>).
    /// </summary>
    public static readonly DependencyProperty ChildProperty =
        DependencyProperty.Register(
            nameof(Child),
            typeof(object),
            typeof(If),
            new PropertyMetadata(null, OnStateChanged));

    /// <summary>
    /// Whether <see cref="Child"/> (<c>true</c>) or <see cref="Else"/> (<c>false</c>)
    /// should currently be mounted.
    /// </summary>
    public static readonly DependencyProperty ConditionProperty =
        DependencyProperty.Register(
            nameof(Condition),
            typeof(bool),
            typeof(If),
            new PropertyMetadata(true, OnStateChanged));

    /// <summary>
    /// Shown when <see cref="Condition"/> is <c>false</c> - the Aquarius counterpart of
    /// Vue's <c>v-else</c>. Defaults to <see langword="null"/> (nothing shown), matching
    /// this control's original behavior before <see cref="Else"/> existed.
    /// </summary>
    public static readonly DependencyProperty ElseProperty =
        DependencyProperty.Register(
            nameof(Else),
            typeof(object),
            typeof(If),
            new PropertyMetadata(null, OnStateChanged));

    /// <inheritdoc cref="ChildProperty"/>
    public object? Child
    {
        get => GetValue(ChildProperty);
        set => SetValue(ChildProperty, value);
    }

    /// <inheritdoc cref="ConditionProperty"/>
    public bool Condition
    {
        get => (bool)GetValue(ConditionProperty);
        set => SetValue(ConditionProperty, value);
    }

    /// <inheritdoc cref="ElseProperty"/>
    public object? Else
    {
        get => GetValue(ElseProperty);
        set => SetValue(ElseProperty, value);
    }

    private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var element = (If)d;
        element.Content = element.Condition ? element.Child : element.Else;
    }
}
