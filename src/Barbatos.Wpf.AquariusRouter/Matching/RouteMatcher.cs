// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.AquariusRouter.Routing;

namespace Barbatos.Wpf.AquariusRouter.Matching;

/// <summary>
/// Compiles a route table (a tree of <see cref="RouteRecord"/>s) into matchable
/// <see cref="PathParser"/>s and resolves a <see cref="RouteLocationRaw"/> against them - the
/// WPF/C# counterpart of Vue Router's own internal matcher. <see cref="Router"/> owns one of
/// these; it's also a real, independently useful, directly unit-testable public type on its own
/// (no <c>InternalsVisibleTo</c> anywhere in this repo - the same reasoning as <see cref="Barbatos.Wpf.Aquarius.Xaml.Expr"/>'s
/// public <c>Evaluate</c> escape hatch).
/// </summary>
/// <remarks>
/// Operates directly on <see cref="Routing.RouteRecord"/> - unlike Vue Router's own matcher,
/// which works against a second, matcher-only record type kept separate from the public
/// <c>RouteRecordRaw</c> for npm-package-boundary reasons that don't apply here (this is one
/// assembly, not two).
/// <para>
/// <b>Aliases work differently from Vue Router here, deliberately</b>: an alias path gets its own
/// matcher entry (so it can be resolved/ranked independently), but - unlike Vue Router, which
/// clones a whole second record per alias - it points at the exact same <see cref="RouteRecord"/>
/// object as the record it aliases, rather than a copy. That means an alias and its original
/// share the same <see cref="RouteRecord.Instances"/> runtime state (simpler, and arguably more
/// intuitive: whichever path you navigated through, "the same route" really does mean the same
/// mounted instance). One consequence: aliasing a record with <see cref="RouteRecord.Children"/>
/// only makes that record's own path reachable under the alias - it does not, unlike Vue Router,
/// recursively re-attach the whole children subtree under the alias path too. This is a
/// deliberately scoped-down corner of alias support, not an oversight.
/// </para>
/// </remarks>
public sealed class RouteMatcher
{
    private readonly PathParserOptions _options;
    private readonly List<MatcherNode> _matchers = [];
    private readonly Dictionary<string, MatcherNode> _matchersByName = [];

    /// <summary>Must mirror how <see cref="PathParser"/> compiles its own regex (case-insensitive unless <see cref="PathParserOptions.Sensitive"/>), or <see cref="FindMatcherForPath"/> would prune a route the regex would in fact have accepted.</summary>
    private readonly StringComparison _segmentComparison;

    /// <summary>
    /// Every node registered per record - the canonical one plus one per <see cref="RouteRecord.Alias"/>.
    /// Keyed by reference identity, matching <see cref="RouteRecord"/>'s own "object identity is
    /// what makes a route the same route" contract. Exists so <see cref="RemoveRoute(RouteRecord)"/>
    /// can find a record's nodes even when the record itself was never matchable (a pure grouping
    /// parent, which never enters <see cref="_matchers"/> but can still own matchable children).
    /// </summary>
    private readonly Dictionary<RouteRecord, List<MatcherNode>> _nodesByRecord = new(ReferenceEqualityComparer.Instance);

    /// <summary><see cref="GetRoutes"/>'s memoized result, dropped whenever a node is registered/unregistered.</summary>
    private IReadOnlyList<RouteRecord>? _routesCache;

    private static readonly IReadOnlyDictionary<string, object?> EmptyParams = new Dictionary<string, object?>();

    /// <summary>Creates a matcher, optionally pre-populated with <paramref name="routes"/> (added in order via <see cref="AddRoute(RouteRecord)"/>).</summary>
    public RouteMatcher(IEnumerable<RouteRecord>? routes = null, PathParserOptions? options = null)
    {
        _options = options ?? new PathParserOptions();
        _segmentComparison = _options.Sensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        foreach (var route in routes ?? [])
            AddRoute(route);
    }

    /// <summary>
    /// Registers <paramref name="record"/> (and, recursively, its own <see cref="RouteRecord.Children"/>
    /// and <see cref="RouteRecord.Alias"/> paths) as a top-level route.
    /// </summary>
    public void AddRoute(RouteRecord record) => AddRouteNode(record, null);

    /// <summary>
    /// Registers <paramref name="record"/> as a child of the already-registered route named
    /// <paramref name="parentName"/> - the dynamic-routing counterpart of nesting it under
    /// <see cref="RouteRecord.Children"/> directly at route-table-authoring time.
    /// </summary>
    public void AddRoute(string parentName, RouteRecord record)
    {
        if (!_matchersByName.TryGetValue(parentName, out var parent))
            throw new InvalidOperationException($"No route named \"{parentName}\" exists to add a child route to.");

        AddRouteNode(record, parent);
    }

    /// <summary>
    /// Unregisters <paramref name="record"/> from this matcher - together with its
    /// <see cref="RouteRecord.Alias"/> paths and every descendant route, whether that descendant
    /// was declared in <see cref="RouteRecord.Children"/> or added later via
    /// <see cref="AddRoute(string,RouteRecord)"/>.
    /// </summary>
    public void RemoveRoute(RouteRecord record) => RemoveRouteInternal(record);

    /// <summary>Unregisters whichever route is currently named <paramref name="name"/>, if any.</summary>
    public void RemoveRoute(string name)
    {
        if (_matchersByName.TryGetValue(name, out var node))
            RemoveRouteInternal(node.Record);
    }

    /// <summary>Whether a route named <paramref name="name"/> is currently registered.</summary>
    public bool HasRoute(string name) => _matchersByName.ContainsKey(name);

    /// <summary>
    /// Every currently-registered, matchable <see cref="RouteRecord"/> (deduplicated - see this
    /// type's remarks on how alias records are represented). Memoized and only rebuilt after a
    /// route is added/removed, since callers that poll it per user keystroke (notably
    /// <see cref="Searching.RouteSearchIndex.Query"/>) would otherwise pay a fresh
    /// allocate-and-deduplicate pass over the whole route table every single time.
    /// </summary>
    public IReadOnlyList<RouteRecord> GetRoutes() => _routesCache ??= BuildRoutesSnapshot();

    private RouteRecord[] BuildRoutesSnapshot()
    {
        var seen = new HashSet<RouteRecord>(ReferenceEqualityComparer.Instance);
        var records = new List<RouteRecord>(_matchers.Count);

        foreach (var matcher in _matchers)
        {
            if (seen.Add(matcher.Record))
                records.Add(matcher.Record);
        }

        return records.ToArray();
    }

    /// <summary>
    /// Resolves <paramref name="location"/> (by <see cref="RouteLocationRaw.Name"/>, by
    /// <see cref="RouteLocationRaw.Path"/>, or - if neither is set - relative to
    /// <paramref name="current"/>) into a <see cref="RouteLocation"/>.
    /// </summary>
    /// <remarks>
    /// Deliberately handles only path-matching concerns - <see cref="RouteLocationRaw.Query"/>/
    /// <see cref="RouteLocationRaw.Hash"/> are not read here at all, and the returned
    /// <see cref="RouteLocation.FullPath"/> is always just <see cref="RouteLocation.Path"/>
    /// unchanged. <see cref="Router.Resolve"/> is what layers query/hash/full-path composition on
    /// top - the same separation Vue Router's own matcher/router split has, kept here even though
    /// both live in one assembly, because it's genuinely a different concern (matching vs.
    /// everything-else) worth keeping this type's own responsibility narrow and independently
    /// testable.
    /// </remarks>
    public RouteLocation Resolve(RouteLocationRaw location, RouteLocation? current = null)
    {
        MatcherNode matcher;
        IReadOnlyDictionary<string, object?> parameters;
        string path;

        if (location.Name is not null)
        {
            if (!_matchersByName.TryGetValue(location.Name, out matcher!))
                throw new InvalidOperationException($"No route named \"{location.Name}\" exists.");

            var currentParams = current?.Params ?? EmptyParams;
            var seeded = new Dictionary<string, object?>();

            // Seed from the current location: this route's own required keys, plus any keys its
            // PARENT declares as optional - lets navigating by name to a sibling route inherit
            // e.g. a shared ":lang" segment without the caller having to repeat it.
            foreach (var key in matcher.Parser.Keys)
            {
                if (!key.Optional && currentParams.TryGetValue(key.Name, out var value))
                    seeded[key.Name] = value;
            }
            if (matcher.Parent is not null)
            {
                foreach (var key in matcher.Parent.Parser.Keys)
                {
                    if (key.Optional && currentParams.TryGetValue(key.Name, out var value))
                        seeded[key.Name] = value;
                }
            }

            // Then let explicitly-passed params overlay the seeded ones.
            if (location.Params is not null)
            {
                foreach (var key in matcher.Parser.Keys)
                {
                    if (location.Params.TryGetValue(key.Name, out var value))
                        seeded[key.Name] = value;
                }
            }

            parameters = seeded;
            path = matcher.Parser.Stringify(seeded);
        }
        else if (location.Path is not null)
        {
            path = location.Path;
            matcher = FindMatcherForPath(path)
                ?? throw new InvalidOperationException($"No route matches path \"{path}\".");

            var parsed = matcher.Parser.Parse(path)
                ?? throw new InvalidOperationException($"No route matches path \"{path}\".");

            // A falsy optional param (parsed as "") is normalized to "absent" rather than kept
            // as an empty string, matching PathParser.Parse's own documented contract. Driven off
            // Keys rather than the parsed dictionary so each key's Optional flag comes straight
            // from the key itself - looking it back up per param (Keys.First(k => k.Name == key))
            // would make this quadratic in the param count, on the hottest path in the library.
            var keys = matcher.Parser.Keys;
            var normalized = new Dictionary<string, object?>(parsed.Count);
            foreach (var key in keys)
            {
                if (!parsed.TryGetValue(key.Name, out var value))
                    continue;
                if (key.Optional && value is string { Length: 0 })
                    continue;

                normalized[key.Name] = value;
            }
            parameters = normalized;
        }
        else
        {
            if (current is null)
                throw new InvalidOperationException("A relative navigation (no Path or Name set) requires a current location to resolve against.");

            matcher = (current.Name is not null && _matchersByName.TryGetValue(current.Name, out var byName))
                ? byName
                : _matchers.FirstOrDefault(m => m.Parser.Regex.IsMatch(current.Path))
                    ?? throw new InvalidOperationException($"No route matches the current path \"{current.Path}\".");

            var merged = new Dictionary<string, object?>(current.Params);
            if (location.Params is not null)
                foreach (var (key, value) in location.Params)
                    merged[key] = value;

            parameters = merged;
            path = matcher.Parser.Stringify(merged);
        }

        var matched = BuildMatchedChain(matcher);

        return new RouteLocation
        {
            Path = path,
            FullPath = path,
            Name = matcher.Record.Name,
            Params = parameters,
            Matched = matched,
            Meta = MergeMeta(matched),
        };
    }

    /// <summary>
    /// Scans the score-ordered matcher list for the first route whose regex accepts
    /// <paramref name="path"/>, skipping - without running that regex at all - any route whose
    /// first path segment is a literal that cannot be what <paramref name="path"/> starts with.
    /// </summary>
    /// <remarks>
    /// A pure prune over the same list in the same order, so which route wins is bit-for-bit what
    /// an unconditional scan would pick - deliberately not a bucketed lookup keyed on that first
    /// segment, which would have to re-derive the global most-to-least-specific ordering
    /// <see cref="FindInsertionIndex"/> maintains and could quietly change which of two overlapping
    /// routes wins. In a real route table most paths start with a distinct literal, so this turns
    /// the great majority of candidates from a regex execution into an ordinal span comparison.
    /// </remarks>
    private MatcherNode? FindMatcherForPath(string path)
    {
        var canPrune = TryGetFirstSegment(path, out var firstSegment);

        foreach (var candidate in _matchers)
        {
            if (canPrune &&
                candidate.StaticFirstSegment is { } required &&
                !firstSegment.Equals(required, _segmentComparison))
            {
                continue;
            }

            if (candidate.Parser.Regex.IsMatch(path))
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// The text between <paramref name="path"/>'s leading <c>/</c> and the next one.
    /// <see langword="false"/> for a path that isn't rooted at all, where no route's leading
    /// <c>^/</c> could match anyway and pruning would only risk diverging from a plain scan.
    /// </summary>
    private static bool TryGetFirstSegment(string path, out ReadOnlySpan<char> segment)
    {
        if (path.Length == 0 || path[0] != '/')
        {
            segment = default;
            return false;
        }

        var end = path.IndexOf('/', 1);
        segment = end < 0 ? path.AsSpan(1) : path.AsSpan(1, end - 1);
        return true;
    }

    /// <summary>
    /// The literal text every path this parser matches must start with, or <see langword="null"/>
    /// when the first segment isn't a lone non-empty static token (a param, a mixed segment like
    /// <c>/:year-:month</c>, or the empty segment of <c>"/"</c>) and so constrains nothing.
    /// </summary>
    private static string? GetStaticFirstSegment(IReadOnlyList<IReadOnlyList<PathToken>> segments) =>
        segments.Count > 0 && segments[0] is [{ Kind: PathTokenKind.Static, Value.Length: > 0 } token]
            ? token.Value
            : null;

    private void AddRouteNode(RouteRecord record, MatcherNode? parent)
    {
        // Checked while the route table is being built rather than on some later navigation:
        // Views already covers the single-outlet case as its "default" entry, so setting View too
        // is always a mistake - and a silent one otherwise, since Views simply wins and the View
        // never renders anywhere.
        if (record.View is not null && record.Views is not null)
        {
            throw new InvalidOperationException(
                $"Route \"{record.Name ?? record.Path}\" sets both View and Views - use one or the other, since Views already covers the single-outlet case as its \"default\" entry.");
        }

        var resolvedPath = JoinPath(record.Path, parent?.ResolvedPath);
        var segments = PathTokenizer.Tokenize(resolvedPath);
        var parser = PathParser.Create(segments, _options);
        var node = new MatcherNode(record, parser, resolvedPath, parent)
        {
            StaticFirstSegment = GetStaticFirstSegment(segments),
        };

        parent?.Children.Add(node);
        RegisterNode(node);

        foreach (var child in record.Children ?? [])
            AddRouteNode(child, node);

        foreach (var aliasPath in record.Alias ?? [])
        {
            var aliasResolvedPath = JoinPath(aliasPath, parent?.ResolvedPath);
            var aliasSegments = PathTokenizer.Tokenize(aliasResolvedPath);
            var aliasParser = PathParser.Create(aliasSegments, _options);
            RegisterNode(new MatcherNode(record, aliasParser, aliasResolvedPath, parent)
            {
                IsAlias = true,
                StaticFirstSegment = GetStaticFirstSegment(aliasSegments),
            });
        }
    }

    private void RegisterNode(MatcherNode node)
    {
        if (IsMatchable(node.Record))
            _matchers.Insert(FindInsertionIndex(node), node);

        // An alias node shares its RouteRecord - and therefore its Name - with the canonical node
        // it aliases, and is always registered after it. Registering it by name too would
        // overwrite the canonical entry, so resolving that name would hand back the ALIAS path
        // instead of the real one. Aliases are reachable by path only, by design.
        if (!node.IsAlias && node.Record.Name is not null)
            _matchersByName[node.Record.Name] = node;

        if (!_nodesByRecord.TryGetValue(node.Record, out var nodes))
            _nodesByRecord[node.Record] = nodes = [];
        nodes.Add(node);

        _routesCache = null;
    }

    /// <summary>
    /// Unregisters every node this record owns - its canonical node, its alias nodes, and its
    /// whole descendant subtree. Walks the <see cref="MatcherNode"/> tree rather than
    /// <see cref="RouteRecord.Children"/>: a child registered at runtime via
    /// <see cref="AddRoute(string,RouteRecord)"/> exists only in the node tree (its parent's
    /// <see cref="RouteRecord.Children"/> is <c>init</c>-only and cannot have grown), so
    /// recursing over the record's own children would leave dynamically-added descendants behind,
    /// still matching, after their parent was removed.
    /// </summary>
    private void RemoveRouteInternal(RouteRecord record)
    {
        if (!_nodesByRecord.TryGetValue(record, out var nodes))
            return;

        foreach (var node in nodes.ToArray())
        {
            node.Parent?.Children.Remove(node);
            RemoveNodeSubtree(node);
        }
    }

    private void RemoveNodeSubtree(MatcherNode node)
    {
        foreach (var child in node.Children.ToArray())
            RemoveNodeSubtree(child);
        node.Children.Clear();

        _matchers.Remove(node);

        if (node.Record.Name is not null &&
            _matchersByName.TryGetValue(node.Record.Name, out var byName) &&
            ReferenceEquals(byName, node))
        {
            _matchersByName.Remove(node.Record.Name);
        }

        if (_nodesByRecord.TryGetValue(node.Record, out var siblings))
        {
            siblings.Remove(node);
            if (siblings.Count == 0)
                _nodesByRecord.Remove(node.Record);
        }

        _routesCache = null;
    }

    private static string JoinPath(string path, string? parentResolvedPath)
    {
        if (parentResolvedPath is null || (path.Length > 0 && path[0] == '/'))
            return path;

        if (path.Length == 0)
            return parentResolvedPath;

        var connectingSlash = parentResolvedPath.Length > 0 && parentResolvedPath[^1] == '/' ? "" : "/";
        return parentResolvedPath + connectingSlash + path;
    }

    private static bool IsMatchable(RouteRecord record) =>
        record.Name is not null || record.ResolvedViews is { Count: > 0 } || record.Redirect is not null;

    private static IReadOnlyList<RouteRecord> BuildMatchedChain(MatcherNode leaf)
    {
        var chain = new List<RouteRecord>();
        for (var node = (MatcherNode?)leaf; node is not null; node = node.Parent)
            chain.Insert(0, node.Record);
        return chain;
    }

    private static IReadOnlyDictionary<string, object?> MergeMeta(IReadOnlyList<RouteRecord> matched)
    {
        var meta = new Dictionary<string, object?>();
        foreach (var record in matched)
        {
            if (record.Meta is null)
                continue;

            foreach (var (key, value) in record.Meta)
                meta[key] = value;
        }
        return meta;
    }

    /// <summary>
    /// Binary-search insertion point that keeps <see cref="_matchers"/> sorted most-to-least
    /// specific (<see cref="PathParser.Compare"/>), with one tie-break: if the new matcher ties
    /// in score with a matchable ancestor of its own (typically an empty-path "index" child
    /// tying with its own parent), it's inserted at that ancestor's position instead - so a
    /// descendant sorts before its own ancestor on a score tie, and <see cref="Resolve"/>'s
    /// linear <c>FirstOrDefault</c> scan finds the more specific (child) route first.
    /// </summary>
    private int FindInsertionIndex(MatcherNode matcher)
    {
        var lower = 0;
        var upper = _matchers.Count;

        while (lower != upper)
        {
            var mid = (lower + upper) / 2;
            if (PathParser.Compare(matcher.Parser, _matchers[mid].Parser) < 0)
                upper = mid;
            else
                lower = mid + 1;
        }

        var ancestor = GetTiedAncestor(matcher);
        if (ancestor is not null)
        {
            var ancestorIndex = _matchers.LastIndexOf(ancestor, upper - 1);
            if (ancestorIndex >= 0)
                upper = ancestorIndex;
        }

        return upper;
    }

    private static MatcherNode? GetTiedAncestor(MatcherNode matcher)
    {
        for (var ancestor = matcher.Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            if (IsMatchable(ancestor.Record) && PathParser.Compare(matcher.Parser, ancestor.Parser) == 0)
                return ancestor;
        }
        return null;
    }

    private sealed class MatcherNode(RouteRecord record, PathParser parser, string resolvedPath, MatcherNode? parent)
    {
        public RouteRecord Record { get; } = record;
        public PathParser Parser { get; } = parser;
        public string ResolvedPath { get; } = resolvedPath;
        public MatcherNode? Parent { get; } = parent;
        public List<MatcherNode> Children { get; } = [];

        /// <summary>An extra path onto the same <see cref="Record"/> (see <see cref="RouteRecord.Alias"/>) rather than that record's canonical node - see <see cref="RegisterNode"/> for why the distinction matters.</summary>
        public bool IsAlias { get; init; }

        /// <inheritdoc cref="GetStaticFirstSegment"/>
        public string? StaticFirstSegment { get; init; }
    }
}
