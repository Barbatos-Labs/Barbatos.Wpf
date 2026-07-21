// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows.Markup;

namespace Barbatos.Wpf.Xaml;

/// <summary>
/// Wraps a piece of content with a free-form name, for use inside a <see cref="SlotHost"/>'s
/// <see cref="SlotHost.Items"/> - the Aquarius counterpart of Vue's
/// <c>&lt;template #name&gt;...&lt;/template&gt;</c>.
/// </summary>
/// <remarks>
/// <code>
/// &lt;local:Card&gt;
///     &lt;aq:Slot Name="header"&gt;
///         &lt;TextBlock Text="Title" /&gt;
///     &lt;/aq:Slot&gt;
///
///     &lt;TextBlock Text="Default slot content - no aq:Slot wrapper needed" /&gt;
/// &lt;/local:Card&gt;
/// </code>
/// A top-level child of a <see cref="SlotHost"/> that is *not* wrapped in <see cref="Slot"/>
/// is implicitly the default slot's content - exactly Vue's own rule that "all top-level
/// non-&lt;template&gt; nodes are implicitly treated as content for the default slot."
/// <see cref="Slot"/> itself is a plain object, not a <see cref="System.Windows.DependencyObject"/> -
/// it never enters the visual tree on its own, only <see cref="Content"/> does once resolved
/// by <see cref="SlotContent"/>/<see cref="SlotProvided"/> and handed to a
/// <c>ContentPresenter</c> (the same "plain CLR type living in a XAML collection" pattern
/// WPF's own <see cref="System.Windows.Setter"/> already uses inside <c>Style.Setters</c>).
/// </remarks>
[ContentProperty(nameof(Content))]
public sealed class Slot
{
    /// <summary>The slot name this content is for. Empty (the default) targets the default slot.</summary>
    public string Name { get; set; } = "";

    /// <summary>The content to project into the matching <see cref="SlotContent"/> outlet.</summary>
    public object? Content { get; set; }
}
