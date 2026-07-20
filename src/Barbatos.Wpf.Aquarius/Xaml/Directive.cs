// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;

namespace Barbatos.Wpf.Xaml;

/// <summary>
/// The value/argument/modifiers a <see cref="Directive"/> hook is invoked with - the
/// Aquarius counterpart of the <c>binding</c> object Vue passes to every custom
/// directive hook.
/// </summary>
/// <remarks>
/// Vue's <c>v-my-directive:arg.mod1.mod2="value"</c> has no XAML equivalent syntax
/// (attached property names can't carry a <c>:arg.mod</c> suffix), so each piece is set
/// through its own sibling attached property instead - see
/// <see cref="Directives.UseValueProperty"/>, <see cref="Directives.ArgumentProperty"/>,
/// and <see cref="Directives.ModifiersProperty"/>.
/// </remarks>
public sealed class DirectiveBinding
{
    /// <summary>The current value from <see cref="Directives.UseValueProperty"/>, if bound.</summary>
    public object? Value { get; init; }

    /// <summary>The value <see cref="Value"/> had before this change, if any.</summary>
    public object? OldValue { get; init; }

    /// <summary>The string from <see cref="Directives.ArgumentProperty"/>, if set.</summary>
    public string? Argument { get; init; }

    /// <summary>The parsed set from <see cref="Directives.ModifiersProperty"/>. Never null - empty when unset.</summary>
    public IReadOnlySet<string> Modifiers { get; init; } = new HashSet<string>();
}

/// <summary>
/// Base class for a custom directive - the Aquarius counterpart of a Vue 3 custom
/// directive object (<c>{ mounted(el, binding) { ... }, updated(el, binding) { ... },
/// unmounted(el, binding) { ... } }</c>).
/// </summary>
/// <remarks>
/// Attach an instance via <see cref="Directives.UseProperty"/>. Custom directives are meant to be
/// user-authored, so this library only ships the extensibility point, not any built-in
/// directives - the canonical Vue docs example for a custom directive is <c>v-focus</c>
/// (focus an input as soon as it mounts); the Aquarius sample ports that exact example as
/// <c>FocusDirective</c> rather than shipping it here.
/// </remarks>
public abstract class Directive
{
    /// <summary>
    /// Called once the target element has been loaded into a live visual tree, mirroring
    /// a Vue custom directive's <c>mounted(el, binding)</c> hook.
    /// </summary>
    public virtual void Mounted(FrameworkElement element, DirectiveBinding binding)
    {
    }

    /// <summary>
    /// Called whenever <see cref="Directives.UseValueProperty"/>/
    /// <see cref="Directives.ArgumentProperty"/>/<see cref="Directives.ModifiersProperty"/>
    /// change while the target element is mounted, mirroring a Vue custom directive's
    /// <c>updated(el, binding)</c> hook.
    /// </summary>
    public virtual void Updated(FrameworkElement element, DirectiveBinding binding)
    {
    }

    /// <summary>
    /// Called once the target element has been removed from the visual tree, mirroring
    /// a Vue custom directive's <c>unmounted(el, binding)</c> hook.
    /// </summary>
    public virtual void Unmounted(FrameworkElement element, DirectiveBinding binding)
    {
    }
}
