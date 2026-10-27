// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Animation;

namespace Barbatos.Wpf.Aquarius.Animation;

/// <summary>
/// Plays an animation around mounting/unmounting its content - the Aquarius counterpart
/// of Vue's <c>&lt;Transition&gt;</c>.
/// </summary>
/// <remarks>
/// <code>
/// &lt;aq:Transition Show="{Binding IsOpen}"&gt;
///     &lt;aq:Transition.Enter&gt;
///         &lt;Storyboard&gt;&lt;DoubleAnimation Storyboard.TargetProperty="Opacity" From="0" To="1" Duration="0:0:0.2" /&gt;&lt;/Storyboard&gt;
///     &lt;/aq:Transition.Enter&gt;
///     &lt;aq:Transition.Leave&gt;
///         &lt;Storyboard&gt;&lt;DoubleAnimation Storyboard.TargetProperty="Opacity" From="1" To="0" Duration="0:0:0.2" /&gt;&lt;/Storyboard&gt;
///     &lt;/aq:Transition.Leave&gt;
///     &lt;TextBlock Text="Now you see me" /&gt;
/// &lt;/aq:Transition&gt;
/// </code>
/// Vue ships zero default animations - <c>&lt;Transition&gt;</c> only orchestrates *when*
/// your own CSS classes/JS hooks run. This port keeps that philosophy: no canned
/// animations ship here, callers supply their own <see cref="Storyboard"/> resources for
/// <see cref="Enter"/>/<see cref="Leave"/> (both optional - a transition with neither set
/// behaves exactly like <see cref="Xaml.If"/>, immediate mount/unmount, no animation).
///
/// This deliberately merges what <see cref="Xaml.If"/> does (structural mount/unmount, via
/// the same <see cref="Child"/>-vs-<see cref="ContentControl.Content"/> split, for the same
/// reason) with animation timing, rather than trying to make <see cref="Xaml.If"/>
/// "animation-aware" from the outside - <see cref="Xaml.If"/> stays exactly as it is for
/// callers who don't need animation. <see cref="Show"/> going <c>false</c> plays
/// <see cref="Leave"/> first (if set) and only detaches <see cref="ContentControl.Content"/>
/// once it completes - the same deferred-removal idea, just with a pause for the animation.
/// Detaching still fires <c>Unloaded</c> on the content once it actually happens, so the
/// <see cref="Composition.Lifecycle.EnableProperty"/> synergy <see cref="Xaml.If"/> documents
/// applies here too. Vue does not play the enter animation on a component's very first
/// render unless the <c>appear</c> prop is set; this mirrors that - the initial
/// <see cref="Child"/> is displayed directly, <see cref="Enter"/> only plays on a later
/// <c>false</c>→<c>true</c> toggle.
/// <para>
/// Both storyboards are begun on this control itself, so they animate its own properties
/// (<c>Opacity</c> above). <see cref="Leave"/> is begun <i>controllable</i> and kept until the
/// next show, because a finished <see cref="Storyboard"/> keeps holding its end value
/// (<see cref="FillBehavior.HoldEnd"/>, the default). <see cref="Show"/> going back to
/// <c>true</c> - mid-<see cref="Leave"/> or long after it finished - plays <see cref="Enter"/>
/// first, which takes over whatever properties it animates (an animation in it with no
/// <c>From</c> starts from the value <see cref="Leave"/> had reached), then removes
/// <see cref="Leave"/>, so every property only <see cref="Leave"/> animated returns to its own
/// value. An earlier version began <see cref="Leave"/> non-controllable: stopping it on
/// a re-toggle did nothing, the animation ran on, and a <see cref="Transition"/> whose
/// <see cref="Enter"/> did not animate the same property (or that had no <see cref="Enter"/>)
/// re-showed its content still at the leave's end value - <c>Opacity</c> 0, invisible.
/// </para>
/// </remarks>
[ContentProperty(nameof(Child))]
public class Transition : ContentControl
{
    /// <summary>
    /// The content to conditionally render. This - not the inherited
    /// <see cref="ContentControl.Content"/> - is the XAML content property, mirroring
    /// <see cref="Xaml.If.Child"/> for the same reason.
    /// </summary>
    public static readonly DependencyProperty ChildProperty =
        DependencyProperty.Register(
            nameof(Child),
            typeof(object),
            typeof(Transition),
            new PropertyMetadata(null, OnChildChanged));

    /// <summary>Whether <see cref="Child"/> should currently be mounted.</summary>
    public static readonly DependencyProperty ShowProperty =
        DependencyProperty.Register(
            nameof(Show),
            typeof(bool),
            typeof(Transition),
            new PropertyMetadata(true, OnShowChanged));

    /// <summary>Played after mounting, once <see cref="Show"/> flips from <c>false</c> to <c>true</c>.</summary>
    public static readonly DependencyProperty EnterProperty =
        DependencyProperty.Register(nameof(Enter), typeof(Storyboard), typeof(Transition));

    /// <summary>Played before unmounting; content stays mounted until it completes.</summary>
    public static readonly DependencyProperty LeaveProperty =
        DependencyProperty.Register(nameof(Leave), typeof(Storyboard), typeof(Transition));

    // The Leave instance last begun - in flight, or finished but still holding its end value -
    // until the next ShowContent removes it.
    private Storyboard? _leave;

    /// <inheritdoc cref="ChildProperty"/>
    public object? Child
    {
        get => GetValue(ChildProperty);
        set => SetValue(ChildProperty, value);
    }

    /// <inheritdoc cref="ShowProperty"/>
    public bool Show
    {
        get => (bool)GetValue(ShowProperty);
        set => SetValue(ShowProperty, value);
    }

    /// <inheritdoc cref="EnterProperty"/>
    public Storyboard? Enter
    {
        get => (Storyboard?)GetValue(EnterProperty);
        set => SetValue(EnterProperty, value);
    }

    /// <inheritdoc cref="LeaveProperty"/>
    public Storyboard? Leave
    {
        get => (Storyboard?)GetValue(LeaveProperty);
        set => SetValue(LeaveProperty, value);
    }

    private static void OnChildChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var transition = (Transition)d;

        // If Show is false, a running _leave (if any) owns Content until it completes; the new
        // Child is simply what gets shown the next time Show flips true.
        if (transition.Show)
            transition.Content = transition.Child;
    }

    private static void OnShowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var transition = (Transition)d;

        if ((bool)e.NewValue)
            transition.ShowContent();
        else
            transition.HideContent();
    }

    private void ShowContent()
    {
        // Detached first, so a Leave still in flight can never complete and remove the content
        // this is about to restore.
        if (_leave is { } leave)
            leave.Completed -= OnLeaveCompleted;

        Content = Child;

        // Enter before removing Leave: under the default SnapshotAndReplace handoff an Enter
        // animation with no From starts from where Leave had the property (a half-faded panel fades
        // back in from there), while removing Leave first would snap it to its own value before
        // Enter began.
        Enter?.Clone().Begin(this);
        RemoveLeave();
    }

    private void HideContent()
    {
        RemoveLeave();

        if (Leave is not { } leave)
        {
            Content = null;
            return;
        }

        // Clone before Begin(): Storyboard.Completed is a plain CLR event on the (often
        // resource-shared) Timeline object itself, not scoped per-target - without cloning,
        // two Transitions sharing one Leave resource would each receive *both* completions.
        // Controllable, so RemoveLeave can actually stop it - see this type's remarks.
        var instance = leave.Clone();
        _leave = instance;
        instance.Completed += OnLeaveCompleted;
        instance.Begin(this, isControllable: true);
    }

    private void RemoveLeave()
    {
        if (_leave is not { } leave)
            return;

        _leave = null;
        leave.Completed -= OnLeaveCompleted;
        leave.Remove(this);
    }

    private void OnLeaveCompleted(object? sender, EventArgs e)
    {
        // The sender is the storyboard's Clock, not the Storyboard, so unsubscribe through the
        // field. _leave itself stays: its clock still holds the end value until the next show.
        if (_leave is { } leave)
            leave.Completed -= OnLeaveCompleted;

        if (!Show)
            Content = null;
    }
}
