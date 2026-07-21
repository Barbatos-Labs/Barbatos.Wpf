// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Barbatos.Wpf.Xaml;

/// <summary>
/// Projects a named slot's content out of the enclosing <see cref="SlotHost"/> - used inside
/// a component author's own <c>ControlTemplate</c>. The Aquarius counterpart of Vue reading
/// <c>slots.header</c> (or <c>slots.default</c>) inside a component's own template.
/// </summary>
/// <remarks>
/// <code>
/// &lt;ContentPresenter Content="{aq:SlotContent header}" /&gt;
/// &lt;ContentPresenter Content="{aq:SlotContent}" /&gt; &lt;!-- default slot --&gt;
/// &lt;ContentPresenter Content="{aq:SlotContent header, Fallback='Untitled'}" /&gt;
/// </code>
/// <see cref="ProvideValue"/> returns a real, deferred <see cref="Binding"/>
/// (<see cref="RelativeSource.TemplatedParent"/> against <see cref="SlotHost.ResolvedSlots"/>)
/// rather than resolving anything eagerly - the same idiom <see cref="Expr"/>/
/// <see cref="Composition.Inject"/> already establish, necessary because a <c>ControlTemplate</c>
/// is parsed/shared once but a real <see cref="Binding"/> correctly re-evaluates per control
/// instance once WPF clones the template.
///
/// <see cref="Fallback"/> is a convenience for a simple/primitive substitute value when the
/// slot wasn't provided at all - for complex fallback content (or to skip a wrapper element
/// entirely, matching Vue's own Conditional Slots pattern), compose <see cref="SlotProvided"/>
/// with <see cref="If"/> instead. A slot that *was* provided but is empty
/// (<c>&lt;aq:Slot Name="header" /&gt;</c>, no content) is a present key with a <see langword="null"/>
/// value, and correctly does *not* fall back - only a genuinely absent key does, matching
/// Vue's real rule for when fallback content actually applies.
/// <see cref="BindingBase.FallbackValue"/>/<see cref="BindingBase.TargetNullValue"/> cannot
/// express this: <c>FallbackValue</c> only activates when a binding fails to produce a value
/// at all, and this lookup always succeeds (even a "not found" result is a successful
/// <see langword="null"/>); <c>TargetNullValue</c> checks the pre-conversion source
/// (<see cref="SlotHost.ResolvedSlots"/> itself, never null) rather than the post-lookup entry.
/// </remarks>
[MarkupExtensionReturnType(typeof(object))]
public class SlotContent : MarkupExtension
{
    /// <summary>Creates an instance targeting the default slot - set <see cref="Name"/> via the <c>Name=</c>/positional syntax for a named one.</summary>
    public SlotContent()
    {
    }

    /// <summary>Creates an instance for the positional form, <c>{aq:SlotContent header}</c>.</summary>
    public SlotContent(string name) => Name = name;

    /// <summary>The slot name to project. Empty (the default) targets the default slot.</summary>
    [ConstructorArgument("name")]
    public string Name { get; set; } = "";

    /// <summary>Shown when <see cref="Name"/> was never provided at all - see the type-level remarks for why an explicitly-empty slot does not fall back.</summary>
    public object? Fallback { get; set; }

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding(nameof(SlotHost.ResolvedSlots))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
            Mode = BindingMode.OneWay,
            Converter = new SlotLookupConverter(Name, Fallback),
        };

        return binding.ProvideValue(serviceProvider);
    }

    private sealed class SlotLookupConverter(string name, object? fallback) : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is IReadOnlyDictionary<string, object?> slots && slots.TryGetValue(name, out var content)
                ? content
                : fallback;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException($"{nameof(SlotContent)} does not support two-way binding.");
    }
}
