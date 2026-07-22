// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Markup;

namespace Barbatos.Wpf.Aquarius.Composition;

/// <summary>
/// Reads a value registered by an ancestor's <see cref="Provide"/> - the Aquarius
/// counterpart of Vue's <c>inject(key)</c>.
/// </summary>
/// <remarks>
/// <code>
/// &lt;TextBlock Foreground="{aq:Inject ThemeColor}" /&gt;
/// &lt;!-- or, equivalently: --&gt;
/// &lt;TextBlock Foreground="{aq:Inject Key=ThemeColor}" /&gt;
/// </code>
/// This is the primary, XAML-facing way to consume a provided value - arguably reading
/// better than Vue's own <c>inject()</c> in some ways, since it's declarative right where
/// it's used, rather than a step performed earlier in a separate <c>setup()</c> call.
///
/// Internally delegates to <see cref="DynamicResourceExtension"/> (since
/// <see cref="Provide"/> stores its value directly into
/// <see cref="FrameworkElement.Resources"/>, exactly what <c>DynamicResource</c> already
/// resolves against) rather than eagerly walking the tree itself - this reuses WPF's own
/// deferred, re-evaluate-on-invalidation resolution, which correctly handles the common
/// case of a direct XAML child not yet being attached to its eventual parent at the
/// moment this markup extension actually runs (a plain eager <c>FindResource</c> call
/// here would intermittently miss that case).
///
/// For the rare case of a View needing an injected value in C# rather than XAML (e.g. to
/// hand to its own DataContext), use <see cref="Get{T}"/> instead - ViewModels that need
/// a value for their whole lifetime should still prefer constructor injection through
/// Core's DI container; this is for values scoped to a *visual subtree* instead, a
/// different scoping model.
/// </remarks>
[MarkupExtensionReturnType(typeof(object))]
public class Inject : MarkupExtension
{
    /// <summary>Creates an instance with <see cref="Key"/> unset - set it via the <c>Key=</c> property syntax.</summary>
    public Inject()
    {
    }

    /// <summary>Creates an instance for the positional form, <c>{aq:Inject SomeKey}</c>.</summary>
    public Inject(object key) => Key = key;

    /// <summary>The key a <see cref="Provide"/> registered further up the tree.</summary>
    [ConstructorArgument("key")]
    public object? Key { get; set; }

    /// <inheritdoc/>
    public override object? ProvideValue(IServiceProvider serviceProvider)
    {
        if (Key is null)
            throw new InvalidOperationException("Inject requires a Key.");

        return new DynamicResourceExtension(Key).ProvideValue(serviceProvider);
    }

    /// <summary>
    /// Reads a provided value from C# code rather than XAML, mirroring <see cref="ProvideValue"/>
    /// but synchronous and immediate (no deferred re-resolution) - suitable for a
    /// one-time read, e.g. in a View's constructor or <c>Loaded</c> handler.
    /// </summary>
    /// <param name="from">The element to start walking up from.</param>
    /// <param name="key">The key a <see cref="Provide"/> registered further up the tree.</param>
    /// <param name="fallback">Returned if nothing provided <paramref name="key"/>.</param>
    public static T? Get<T>(FrameworkElement from, object key, T? fallback = default)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(key);

        return from.TryFindResource(key) is T value ? value : fallback;
    }
}
