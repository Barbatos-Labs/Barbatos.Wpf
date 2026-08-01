// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Barbatos.Wpf.AquariusValee.Rules;

/// <summary>
/// Validates that a string matches a regular expression, with full <see cref="RegexOptions"/>
/// control - the WPF/.NET counterpart of vee-validate's <c>regex</c> rule. A <see langword="null"/>
/// or empty value is valid; pair with <see cref="RequiredAttribute"/> if that should fail instead.
/// </summary>
/// <remarks>
/// <para>
/// <b>Prefer the BCL's own <see cref="RegularExpressionAttribute"/> whenever a plain, case-sensitive
/// pattern is all you need</b> - it needs no extra package and already covers that case. Reach for
/// this attribute for the one thing the BCL attribute genuinely cannot express: it exposes no
/// <see cref="RegexOptions"/> property at all (confirmed empirically), so
/// <see cref="RegexOptions.IgnoreCase"/>, <see cref="RegexOptions.Multiline"/>,
/// <see cref="RegexOptions.CultureInvariant"/>, <see cref="RegexOptions.IgnorePatternWhitespace"/>
/// and friends are reachable only by embedding inline flags such as <c>(?i)</c> in the pattern
/// string itself - workable, but easy to get wrong and invisible to anyone reading the attribute.
/// </para>
/// <para>
/// Unlike <see cref="RegularExpressionAttribute"/>, which matches with
/// <see cref="Regex.IsMatch(string, string)"/> semantics against the whole input, this attribute
/// also requires the match to span the <b>entire</b> value by default
/// (<see cref="MustMatchEntireValue"/>), matching vee-validate's own <c>regex</c> behavior and
/// avoiding the classic "<c>^[a-z]+$</c> forgotten, so <c>abc123</c> passes a <c>[a-z]+</c> rule"
/// trap. Set it to <see langword="false"/> for a substring/"contains a match" check.
/// </para>
/// <para>
/// The compiled <see cref="Regex"/> is built once per attribute instance and reused. A
/// <see cref="MatchTimeoutInMilliseconds"/> guard (defaulting to the same two seconds
/// <see cref="RegularExpressionAttribute"/> itself uses) bounds catastrophic backtracking on a
/// pathological pattern/input pair; a timeout is reported as invalid rather than thrown, so a slow
/// pattern degrades into a failed field instead of crashing the UI thread.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class PatternAttribute : ValidationAttribute
{
    private readonly Lazy<Regex> _regex;

    /// <summary>Creates the attribute with the regular expression <paramref name="pattern"/> to match.</summary>
    public PatternAttribute(string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        Pattern = pattern;
        ErrorMessage ??= "The {0} field is not in the expected format.";
        _regex = new Lazy<Regex>(() => new Regex(Pattern, Options, TimeSpan.FromMilliseconds(MatchTimeoutInMilliseconds)));
    }

    /// <summary>The regular expression pattern, as passed to the constructor.</summary>
    public string Pattern { get; }

    /// <summary>
    /// The <see cref="RegexOptions"/> to compile <see cref="Pattern"/> with - the reason this
    /// attribute exists alongside <see cref="RegularExpressionAttribute"/>. Defaults to
    /// <see cref="RegexOptions.None"/>.
    /// </summary>
    public RegexOptions Options { get; init; } = RegexOptions.None;

    /// <summary>
    /// Whether the pattern must match the value end to end (the default, mirroring vee-validate's
    /// <c>regex</c>) rather than merely occurring somewhere inside it.
    /// </summary>
    public bool MustMatchEntireValue { get; init; } = true;

    /// <summary>How long a single match attempt may run before being treated as a failure. Defaults to 2000ms, the same bound <see cref="RegularExpressionAttribute"/> uses.</summary>
    public int MatchTimeoutInMilliseconds { get; init; } = 2000;

    /// <inheritdoc/>
    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        var text = value as string ?? value.ToString();

        if (string.IsNullOrEmpty(text))
            return true;

        try
        {
            var match = _regex.Value.Match(text);
            return match.Success && (!MustMatchEntireValue || (match.Index == 0 && match.Length == text.Length));
        }
        catch (RegexMatchTimeoutException)
        {
            // A pattern/input pair that backtracks catastrophically fails the field rather than
            // tearing down the UI thread - the value genuinely could not be shown to be valid.
            return false;
        }
    }
}
