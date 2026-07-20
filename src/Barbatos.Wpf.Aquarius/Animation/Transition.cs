// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Animation;

namespace Barbatos.Wpf.Animation;

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
/// <c>false</c>→<c>true"</c> toggle.
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

    private Storyboard? _runningLeave;

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

        // If Show is false, _runningLeave (if any) owns Content until it completes; the new
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
        // A re-toggle mid-Leave: stop the in-flight animation (detaching Completed first, so
        // Stop() can never trigger a stray removal) rather than letting two animations fight.
        if (_runningLeave is { } leave)
        {
            leave.Completed -= OnLeaveCompleted;
            leave.Stop(this);
            _runningLeave = null;
        }

        Content = Child;
        Enter?.Clone().Begin(this);
    }

    private void HideContent()
    {
        if (Leave is not { } leave)
        {
            Content = null;
            return;
        }

        // Clone before Begin(): Storyboard.Completed is a plain CLR event on the (often
        // resource-shared) Timeline object itself, not scoped per-target - without cloning,
        // two Transitions sharing one Leave resource would each receive *both* completions.
        var instance = leave.Clone();
        _runningLeave = instance;
        instance.Completed += OnLeaveCompleted;
        instance.Begin(this);
    }

    private void OnLeaveCompleted(object? sender, EventArgs e)
    {
        if (sender is Storyboard storyboard)
            storyboard.Completed -= OnLeaveCompleted;

        _runningLeave = null;
        Content = null;
    }
}
