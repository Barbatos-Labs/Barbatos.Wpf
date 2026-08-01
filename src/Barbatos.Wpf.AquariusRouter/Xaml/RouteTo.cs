// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows.Markup;
using Barbatos.Wpf.AquariusRouter.Routing;

namespace Barbatos.Wpf.AquariusRouter.Xaml;

/// <summary>
/// Builds a <see cref="RouteLocationRaw"/> straight from XAML - the concise way to point a
/// <see cref="RouterLink"/> at a <i>named</i> route (with params) instead of hardcoding a path
/// string, the XAML counterpart of Vue Router's own <c>:to="{ name: 'user', params: { id: 42 }}"</c>.
/// </summary>
/// <remarks>
/// <code>
/// &lt;!-- By name, no params - never hardcodes where that route currently lives: --&gt;
/// &lt;Button aqr:RouterLink.To="{aqr:RouteTo Name=settings-security}" Content="Security" /&gt;
///
/// &lt;!-- By name, with params (quote the value - '=' and ',' are XAML's own separators): --&gt;
/// &lt;Button aqr:RouterLink.To="{aqr:RouteTo Name=user, Params='id=42'}" Content="Profile" /&gt;
///
/// &lt;!-- Several params/query values, and a repeatable param: --&gt;
/// &lt;Button aqr:RouterLink.To="{aqr:RouteTo Name=user, Params='id=42;tab=billing', Query='ref=nav'}" /&gt;
/// &lt;Button aqr:RouterLink.To="{aqr:RouteTo Name=files, Params='segments=docs,2026,report'}" /&gt;
///
/// &lt;!-- A literal path, positionally - equivalent to just RouterLink.To="/about": --&gt;
/// &lt;Button aqr:RouterLink.To="{aqr:RouteTo /about}" /&gt;
/// </code>
/// <b>For a target that changes at runtime, bind <see cref="RouterLink.ToProperty"/> to a
/// <see cref="RouteLocationRaw"/> property on your ViewModel instead.</b> A markup extension is
/// evaluated once, when the XAML is parsed, so a <c>{Binding}</c> nested inside one of these
/// properties would be captured as a <c>Binding</c> object rather than its value - this extension
/// is deliberately for targets that are constant in the markup (which is the overwhelmingly common
/// case for a nav bar), not a replacement for binding.
/// <para>
/// <see cref="Params"/>/<see cref="Query"/> use <c>key=value</c> pairs separated by <c>;</c> -
/// <c>;</c> rather than <c>,</c> because a comma already separates a markup extension's own
/// arguments. Within a <see cref="Params"/> value, a comma splits into the string list a
/// <i>repeatable</i> param (<c>:segments+</c>/<c>:segments*</c>) expects.
/// </para>
/// </remarks>
[MarkupExtensionReturnType(typeof(RouteLocationRaw))]
public sealed class RouteTo : MarkupExtension
{
    /// <summary>Creates an empty extension - set <see cref="Name"/> or <see cref="Path"/> before it is evaluated.</summary>
    public RouteTo()
    {
    }

    /// <summary>Creates an extension targeting a literal <paramref name="path"/> - the positional form, <c>{aqr:RouteTo /about}</c>.</summary>
    public RouteTo(string path) => Path = path;

    /// <summary>A literal path, e.g. <c>/users/42</c>. Mutually exclusive with <see cref="Name"/>; ignores <see cref="Params"/> exactly like <see cref="RouteLocationRaw.Path"/> does.</summary>
    public string? Path { get; set; }

    /// <summary>A route's <see cref="RouteRecord.Name"/>, combined with <see cref="Params"/>. Mutually exclusive with <see cref="Path"/>.</summary>
    public string? Name { get; set; }

    /// <summary>Param values as <c>key=value</c> pairs separated by <c>;</c> - e.g. <c>'id=42;tab=billing'</c>. A comma inside one value splits it into the list a repeatable param expects.</summary>
    public string? Params { get; set; }

    /// <summary>Query values as <c>key=value</c> pairs separated by <c>;</c> - e.g. <c>'ref=nav;page=2'</c>.</summary>
    public string? Query { get; set; }

    /// <inheritdoc cref="RouteLocationRaw.Hash"/>
    public string? Hash { get; set; }

    /// <inheritdoc cref="RouteLocationRaw.Replace"/>
    public bool Replace { get; set; }

    /// <inheritdoc cref="RouteLocationRaw.Force"/>
    public bool Force { get; set; }

    /// <summary>Builds the <see cref="RouteLocationRaw"/> this extension describes.</summary>
    /// <exception cref="InvalidOperationException">Neither <see cref="Name"/> nor <see cref="Path"/> was set, or both were.</exception>
    /// <exception cref="FormatException"><see cref="Params"/>/<see cref="Query"/> is not <c>key=value</c> pairs separated by <c>;</c>.</exception>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (Name is null && Path is null)
            throw new InvalidOperationException("RouteTo needs either a Name or a Path - e.g. {aqr:RouteTo Name=user, Params='id=42'} or {aqr:RouteTo /users/42}.");

        if (Name is not null && Path is not null)
            throw new InvalidOperationException($"RouteTo has both Name (\"{Name}\") and Path (\"{Path}\") set - use one or the other, since a path is always used verbatim and would silently ignore Params (see RouteLocationRaw's own remarks).");

        return new RouteLocationRaw
        {
            Path = Path,
            Name = Name,
            Params = Params is null ? null : ParseParams(Params),
            Query = Query is null ? null : ParseQuery(Query),
            Hash = Hash,
            Replace = Replace,
            Force = Force,
        };
    }

    private static IReadOnlyDictionary<string, object?> ParseParams(string text)
    {
        var result = new Dictionary<string, object?>();
        foreach (var (key, value) in ParsePairs(text, nameof(Params)))
        {
            // A comma is how a repeatable param (:segments+ / :segments*) spells its several
            // values - PathParser expects an IReadOnlyList<string> for exactly those.
            result[key] = value.Contains(',', StringComparison.Ordinal)
                ? value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                : value;
        }

        return result;
    }

    private static IReadOnlyDictionary<string, string?> ParseQuery(string text)
    {
        var result = new Dictionary<string, string?>();
        foreach (var (key, value) in ParsePairs(text, nameof(Query)))
            result[key] = value;

        return result;
    }

    private static IEnumerable<(string Key, string Value)> ParsePairs(string text, string propertyName)
    {
        foreach (var pair in text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = pair.IndexOf('=', StringComparison.Ordinal);
            if (separator <= 0)
                throw new FormatException($"RouteTo.{propertyName} must be \"key=value\" pairs separated by ';' - \"{pair}\" is not one.");

            yield return (pair[..separator].TrimEnd(), pair[(separator + 1)..].TrimStart());
        }
    }
}
