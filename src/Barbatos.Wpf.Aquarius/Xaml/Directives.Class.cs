// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel;
using System.Windows;

namespace Barbatos.Wpf.Aquarius.Xaml;

public static partial class Directives
{
    /// <summary>
    /// Conditionally applies one or more named <see cref="Style"/>s' setters directly onto
    /// the element - the Aquarius counterpart of Vue's <c>:class="isActive ? 'active bold' : ''"</c>
    /// string form (the most common real-world <c>:class</c> usage).
    /// </summary>
    /// <remarks>
    /// <code>
    /// &lt;Border aq:Directives.Class="{Binding ActiveClasses}"&gt;...&lt;/Border&gt;
    /// &lt;!-- where ActiveClasses might be "active bold" or "" -->
    /// </code>
    /// This exists as the lighter-weight alternative to a <c>DataTrigger</c> the user
    /// actually wants most of the time: a space-separated list of "class names," each
    /// looked up as a <see cref="Style"/> resource key (via
    /// <see cref="FrameworkElement.TryFindResource"/>, so classes can live in
    /// <c>Window.Resources</c>/<c>App.Resources</c> same as any other style). Every active
    /// token's setters (including any it inherits through <see cref="Style.BasedOn"/>) are
    /// applied directly via <see cref="DependencyObject.SetValue(DependencyProperty, object)"/>
    /// - not by assigning <see cref="FrameworkElement.Style"/> itself, since WPF only allows
    /// one <see cref="Style"/> at a time but several simultaneously-active "classes" need to
    /// layer the way CSS classes do. Later tokens win on conflicting properties, matching
    /// CSS cascade order. Properties set by a token that is no longer active are reverted
    /// (<see cref="DependencyObject.ClearValue(DependencyProperty)"/>) before the new set is
    /// applied, so classes compose cleanly across updates instead of leaving stale values
    /// behind.
    /// </remarks>
    public static readonly DependencyProperty ClassProperty =
        DependencyProperty.RegisterAttached(
            "Class",
            typeof(string),
            typeof(Directives),
            new PropertyMetadata(null, OnClassChanged));

    /// <summary>Sets <see cref="ClassProperty"/>.</summary>
    public static void SetClass(DependencyObject element, string? value) => element.SetValue(ClassProperty, value);

    /// <summary>Gets <see cref="ClassProperty"/>.</summary>
    public static string? GetClass(DependencyObject element) => (string?)element.GetValue(ClassProperty);

    /// <summary>
    /// Applies a bag of property-name/value pairs directly onto the element - the Aquarius
    /// counterpart of Vue's object form of <c>:style="{ color: activeColor, fontSize: size }"</c>.
    /// </summary>
    /// <remarks>
    /// <code>
    /// &lt;Border aq:Directives.Style="{Binding InlineStyles}"&gt;...&lt;/Border&gt;
    /// &lt;!-- where InlineStyles might be new Dictionary&lt;string, object&gt; { ["Background"] = Brushes.Red } -->
    /// </code>
    /// Each key is resolved to a <see cref="DependencyProperty"/> by name against the
    /// element's own type via <see cref="System.ComponentModel.DependencyPropertyDescriptor.FromName(string, Type, Type)"/>
    /// (an unresolvable name throws, the same fail-loud philosophy
    /// <see cref="Directives.ModelProperty"/>'s unsupported-element-type check already uses) and set
    /// directly. Like <see cref="ClassProperty"/>, properties from a previous update that are
    /// no longer present are reverted first.
    /// </remarks>
    public static readonly DependencyProperty StyleProperty =
        DependencyProperty.RegisterAttached(
            "Style",
            typeof(IDictionary<string, object>),
            typeof(Directives),
            new PropertyMetadata(null, OnStyleChanged));

    /// <summary>Sets <see cref="StyleProperty"/>.</summary>
    public static void SetStyle(DependencyObject element, IDictionary<string, object>? value) => element.SetValue(StyleProperty, value);

    /// <summary>Gets <see cref="StyleProperty"/>.</summary>
    public static IDictionary<string, object>? GetStyle(DependencyObject element) => (IDictionary<string, object>?)element.GetValue(StyleProperty);

    private static readonly DependencyProperty AppliedClassPropertiesProperty =
        DependencyProperty.RegisterAttached("AppliedClassProperties", typeof(List<DependencyProperty>), typeof(Directives));

    private static readonly DependencyProperty AppliedStylePropertiesProperty =
        DependencyProperty.RegisterAttached("AppliedStyleProperties", typeof(List<DependencyProperty>), typeof(Directives));

    private static void OnClassChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        RevertPreviouslyApplied(d, AppliedClassPropertiesProperty);

        var tokens = string.IsNullOrWhiteSpace((string?)e.NewValue)
            ? []
            : ((string)e.NewValue).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var applied = new List<DependencyProperty>();

        if (tokens.Length > 0)
        {
            if (d is not FrameworkElement element)
                throw new InvalidOperationException("Directives.Class can only be set on a FrameworkElement.");

            foreach (var token in tokens)
            {
                if (element.TryFindResource(token) is not Style style)
                    throw new InvalidOperationException($"Directives.Class: no Style resource named '{token}' was found.");

                foreach (var setter in EnumerateSetters(style))
                {
                    d.SetValue(setter.Property, setter.Value);
                    applied.Add(setter.Property);
                }
            }
        }

        d.SetValue(AppliedClassPropertiesProperty, applied);
    }

    private static IEnumerable<Setter> EnumerateSetters(Style style)
    {
        // Base-style setters first (lower priority), so the derived style's own setters -
        // yielded after - correctly win on conflicts, mirroring how WPF itself resolves a
        // BasedOn chain.
        if (style.BasedOn is not null)
        {
            foreach (var inherited in EnumerateSetters(style.BasedOn))
                yield return inherited;
        }

        foreach (var setterBase in style.Setters)
        {
            if (setterBase is Setter setter)
                yield return setter;
        }
    }

    private static void OnStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        RevertPreviouslyApplied(d, AppliedStylePropertiesProperty);

        var applied = new List<DependencyProperty>();

        if (e.NewValue is IDictionary<string, object> values)
        {
            foreach (var (name, value) in values)
            {
                var property = DependencyPropertyDescriptor.FromName(name, d.GetType(), d.GetType())?.DependencyProperty
                    ?? throw new InvalidOperationException($"Directives.Style: '{d.GetType().Name}' has no dependency property named '{name}'.");

                d.SetValue(property, value);
                applied.Add(property);
            }
        }

        d.SetValue(AppliedStylePropertiesProperty, applied);
    }

    private static void RevertPreviouslyApplied(DependencyObject d, DependencyProperty trackingProperty)
    {
        if (d.GetValue(trackingProperty) is List<DependencyProperty> previouslyApplied)
        {
            foreach (var property in previouslyApplied)
                d.ClearValue(property);
        }
    }
}
