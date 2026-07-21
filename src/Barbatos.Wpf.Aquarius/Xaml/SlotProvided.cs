// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Barbatos.Wpf.Xaml;

/// <summary>
/// Whether a named slot was actually supplied by the consumer - the Aquarius counterpart of
/// Vue's <c>$slots.header</c>, typically composed with <see cref="If"/> to skip a wrapper
/// element entirely (Vue's own Conditional Slots pattern) rather than just showing blank
/// content in its place.
/// </summary>
/// <remarks>
/// <code>
/// &lt;aq:If Condition="{aq:SlotProvided header}"&gt;
///     &lt;Border Style="{StaticResource CardHeaderStyle}"&gt;
///         &lt;ContentPresenter Content="{aq:SlotContent header}" /&gt;
///     &lt;/Border&gt;
/// &lt;/aq:If&gt;
/// </code>
/// True iff the name is a *present key* in the enclosing <see cref="SlotHost"/>'s
/// <see cref="SlotHost.ResolvedSlots"/> - presence, not non-nullness, so a slot that was
/// provided but left empty (<c>&lt;aq:Slot Name="header" /&gt;</c>) still counts as provided.
/// Like <see cref="SlotContent"/>, <see cref="ProvideValue"/> returns a real, deferred
/// <see cref="Binding"/> rather than resolving anything eagerly, for the same
/// shared-`ControlTemplate`/per-instance-`TemplatedParent` reason.
/// </remarks>
[MarkupExtensionReturnType(typeof(bool))]
public class SlotProvided : MarkupExtension
{
    /// <summary>Creates an instance targeting the default slot - set <see cref="Name"/> via the <c>Name=</c>/positional syntax for a named one.</summary>
    public SlotProvided()
    {
    }

    /// <summary>Creates an instance for the positional form, <c>{aq:SlotProvided header}</c>.</summary>
    public SlotProvided(string name) => Name = name;

    /// <summary>The slot name to check. Empty (the default) checks the default slot.</summary>
    [ConstructorArgument("name")]
    public string Name { get; set; } = "";

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding(nameof(SlotHost.ResolvedSlots))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
            Mode = BindingMode.OneWay,
            Converter = new SlotProvidedConverter(Name),
        };

        return binding.ProvideValue(serviceProvider);
    }

    private sealed class SlotProvidedConverter(string name) : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is IReadOnlyDictionary<string, object?> slots && slots.ContainsKey(name);

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException($"{nameof(SlotProvided)} does not support two-way binding.");
    }
}
