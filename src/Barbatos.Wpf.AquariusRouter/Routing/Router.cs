// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using Barbatos.Wpf.AquariusRouter.Matching;
using Barbatos.Wpf.Aquarius.Composition;
using Barbatos.Wpf.Aquarius.Reactivity;

namespace Barbatos.Wpf.AquariusRouter.Routing;

/// <summary>
/// A router - the WPF/C# counterpart of a Vue Router instance created by <c>createRouter()</c>.
/// Owns a <see cref="Matching.RouteMatcher"/>, an <see cref="IRouterHistory"/>, and the live
/// <see cref="CurrentRoute"/>; <see cref="Push"/>/<see cref="Replace"/>/<see cref="Go"/> run the
/// full navigation-guard pipeline before ever committing a change.
/// </summary>
/// <remarks>
/// <code>
/// var router = new Router(
/// [
///     new RouteRecord { Path = "/", Name = "home", View = typeof(HomeView) },
///     new RouteRecord { Path = "/users/:id", Name = "user", View = typeof(UserView) },
/// ]);
/// Router.Current = router;
/// await router.Start("/");
/// </code>
/// <b>Owns the entire guard pipeline itself - unlike a first-draft design where
/// <see cref="Xaml.RouterView"/> would have resolved/constructed/guard-checked on its own.</b>
/// Two things forced this: (1) <see cref="IOnBeforeRouteLeave"/>/<see cref="IOnBeforeRouteUpdate"/>
/// need to run against whichever instance is <i>currently mounted</i> for a leaving/updating
/// record, tracked centrally via <see cref="RouteRecord.Instances"/> - there would be nowhere to
/// look that up if each <see cref="Xaml.RouterView"/> only knew about its own outlet; and (2) WPF
/// constructs a View's whole visual subtree eagerly, all at once
/// (<c>Activator.CreateInstance</c>/<c>InitializeComponent()</c>), unlike Vue's lazy, top-down
/// renderer - a nested <see cref="Xaml.RouterView"/> declared inside a matched View's own XAML
/// would construct <i>before</i> its own ambient depth/router context could possibly be available
/// if construction/guard-checking happened per-<see cref="Xaml.RouterView"/> instead of centrally
/// here, ahead of time, independent of the visual tree entirely.
/// <para>
/// <b>Entering Views are constructed synchronously, before <see cref="IOnBeforeRouteEnter"/> runs</b> -
/// via the same <see cref="Setup.ServiceProvider"/>-first-else-<see cref="Activator.CreateInstance(Type)"/>
/// resolution <see cref="Setup"/> itself uses, so a routed View's own
/// <c>aq:Setup.Enable</c>/<c>aq:Setup.ViewModel</c> resolves its <c>DataContext</c> exactly like
/// any other Aquarius View - <see cref="Router"/> never needs its own parallel
/// View-to-ViewModel-resolution mechanism. This is also why <see cref="IOnBeforeRouteEnter"/> gets
/// a real instance/<see langword="this"/>, unlike Vue Router's own <c>beforeRouteEnter</c> - see
/// that interface's own remarks.
/// </para>
/// </remarks>
public sealed class Router
{
    private static readonly IReadOnlyDictionary<string, string?> EmptyQuery = new Dictionary<string, string?>();

    private readonly RouteMatcher _matcher;
    private readonly IRouterHistory _history;
    private readonly List<NavigationGuard> _beforeEachGuards = [];
    private readonly List<NavigationGuard> _beforeResolveGuards = [];
    private readonly List<NavigationHook> _afterEachHooks = [];
    private RouteLocation? _pendingLocation;

    /// <summary>Creates a router from a route table, optionally with a non-default <see cref="IRouterHistory"/> or path-matching options.</summary>
    /// <param name="routes">The top-level route table - see <see cref="RouteRecord"/>.</param>
    /// <param name="history">Defaults to a fresh <see cref="MemoryRouterHistory"/>.</param>
    /// <param name="pathOptions">Applied uniformly to every route's compiled <see cref="PathParser"/> - see <see cref="PathParserOptions"/>.</param>
    public Router(IEnumerable<RouteRecord> routes, IRouterHistory? history = null, PathParserOptions? pathOptions = null)
    {
        _matcher = new RouteMatcher(routes, pathOptions);
        _history = history ?? new MemoryRouterHistory();
        CurrentRoute = new Ref<RouteLocation>(RouteLocation.Start);
    }

    /// <summary>
    /// The ambient default <see cref="Router"/>, consulted by <see cref="Xaml.RouterView"/>/
    /// <see cref="Xaml.RouterLink"/> when neither sets one explicitly - set this once at startup
    /// for the common single-router app, mirroring <see cref="Application.Current"/>'s own
    /// ambient-singleton shape. A multi-window app that wants independent per-window navigation
    /// stacks can skip this and set <see cref="Xaml.RouterView.Router"/> explicitly per window instead.
    /// </summary>
    public static Router? Current { get; set; }

    /// <summary>
    /// The current, fully-resolved location - swapped wholesale to a new
    /// <see cref="RouteLocation"/> on every committed navigation. Unlike Vue Router's own
    /// <c>useRoute()</c> (a single object whose properties are getters forwarding to whatever's
    /// current, needed because Vue's reactivity system requires watching a stable object
    /// reference), a WPF binding already re-evaluates correctly through an ordinary
    /// <see cref="Ref{T}"/> replacement - no equivalent indirection is needed here.
    /// </summary>
    public Ref<RouteLocation> CurrentRoute { get; }

    /// <summary>
    /// Resolves <paramref name="location"/> against the current route without navigating -
    /// the WPF/C# counterpart of Vue Router's own <c>router.resolve()</c>.
    /// </summary>
    public RouteLocation Resolve(RouteLocationRaw location)
    {
        ArgumentNullException.ThrowIfNull(location);

        var resolved = _matcher.Resolve(location, CurrentRoute.Value);
        var query = location.Query ?? EmptyQuery;
        return resolved with { Query = query, Hash = location.Hash, FullPath = BuildFullPath(resolved.Path, query, location.Hash) };
    }

    /// <summary>
    /// Navigates to <paramref name="to"/>, running the full guard pipeline (see this type's own
    /// remarks) and pushing a new <see cref="IRouterHistory"/> entry on success. Also the method
    /// to call for the very first navigation - <see cref="Start"/> is a clarity alias for it, see
    /// its own remarks for why no special-casing is needed.
    /// </summary>
    /// <returns><see langword="null"/> on success, otherwise why the navigation didn't happen.</returns>
    public Task<NavigationFailure?> Push(RouteLocationRaw to)
    {
        ArgumentNullException.ThrowIfNull(to);
        return NavigateWithRedirect(to, redirectedFrom: null, redirectDepth: 0);
    }

    /// <summary>Like <see cref="Push"/>, but replaces the current <see cref="IRouterHistory"/> entry instead of adding a new one.</summary>
    public Task<NavigationFailure?> Replace(RouteLocationRaw to)
    {
        ArgumentNullException.ThrowIfNull(to);
        return NavigateWithRedirect(to with { Replace = true }, redirectedFrom: null, redirectDepth: 0);
    }

    /// <summary>
    /// Performs the very first navigation. A plain alias for <see cref="Push"/> - unlike Vue
    /// Router (where the very first navigation is a documented special case memory history
    /// requires you to trigger yourself), no separate code path is needed here: <see cref="CurrentRoute"/>
    /// already starts at <see cref="RouteLocation.Start"/>, whose empty <see cref="RouteLocation.Matched"/>
    /// list already makes every matched record of the first real navigation "entering" with
    /// nothing "leaving", and the history-commit step already special-cases replacing (rather
    /// than pushing) whenever <c>from</c> is <see cref="RouteLocation.Start"/> - both exactly the
    /// behavior the first navigation needs, for free.
    /// </summary>
    public Task<NavigationFailure?> Start(RouteLocationRaw initial) => Push(initial);

    /// <summary>
    /// Moves <paramref name="delta"/> steps through <see cref="IRouterHistory"/> (negative =
    /// back, positive = forward), running the full guard pipeline first and only actually moving
    /// on success. Resolves to <see langword="null"/> (a silent no-op, matching Vue Router's own
    /// documented behavior) if <paramref name="delta"/> is out of range.
    /// </summary>
    public async Task<NavigationFailure?> Go(int delta)
    {
        var targetPath = _history.Peek(delta);
        if (targetPath is null)
            return null;

        var from = CurrentRoute.Value;

        // A history entry is a serialized FullPath, so it can carry "?query#hash" - feeding that
        // whole string back as a bare Path would ask the matcher to match a route against it
        // verbatim, which no route's regex can accept.
        var to = Resolve(RouteLocationRaw.Parse(targetPath));
        _pendingLocation = to;

        var outcome = await RunGuardPipeline(to, from).ConfigureAwait(true);

        if (outcome.Result == GuardPipelineOutcome.Kind.Redirect)
        {
            // There is no sensible way to "redirect" a history-cursor move - abandon the Go
            // and start a fresh Push at the guard's requested target instead (which fires its
            // own AfterEach on completion, same reasoning as NavigateWithRedirect's own redirect case).
            return await NavigateWithRedirect(outcome.RedirectTo!, redirectedFrom: to, redirectDepth: 0).ConfigureAwait(true);
        }

        NavigationFailure? failure = outcome.Result switch
        {
            GuardPipelineOutcome.Kind.Cancelled => new NavigationFailure { Type = NavigationFailureType.Cancelled, To = to, From = from },
            GuardPipelineOutcome.Kind.Aborted => new NavigationFailure { Type = NavigationFailureType.Aborted, To = to, From = from },
            _ => null,
        };

        if (failure is null)
        {
            _history.Go(delta);
            CurrentRoute.Value = to;
        }

        TriggerAfterEach(to, from, failure);
        return failure;
    }

    /// <summary>Shorthand for <c>Go(-1)</c>.</summary>
    public Task<NavigationFailure?> Back() => Go(-1);

    /// <summary>Shorthand for <c>Go(1)</c>.</summary>
    public Task<NavigationFailure?> Forward() => Go(1);

    /// <summary>
    /// Registers a global guard checked on every navigation, after leave guards and before any
    /// per-route/in-component guard - see this type's own remarks for the full pipeline order.
    /// Returns an <see cref="IDisposable"/> that unregisters it, matching this project's own
    /// <see cref="Watch"/> "subscribe, get a disposable stop handle" convention.
    /// </summary>
    public IDisposable BeforeEach(NavigationGuard guard) => Subscribe(_beforeEachGuards, guard);

    /// <summary>Registers a global guard checked last, after every other guard has already allowed the navigation to proceed.</summary>
    public IDisposable BeforeResolve(NavigationGuard guard) => Subscribe(_beforeResolveGuards, guard);

    /// <summary>Registers a non-cancelling observer called once a navigation has finished (successfully or not) - see <see cref="NavigationHook"/>.</summary>
    public IDisposable AfterEach(NavigationHook hook) => Subscribe(_afterEachHooks, hook);

    /// <summary>Registers <paramref name="record"/> as a new top-level route - see <see cref="Matching.RouteMatcher.AddRoute(RouteRecord)"/>.</summary>
    public void AddRoute(RouteRecord record) => _matcher.AddRoute(record);

    /// <summary>Registers <paramref name="record"/> as a child of the route named <paramref name="parentName"/> - see <see cref="Matching.RouteMatcher.AddRoute(string,RouteRecord)"/>.</summary>
    public void AddRoute(string parentName, RouteRecord record) => _matcher.AddRoute(parentName, record);

    /// <summary>Unregisters <paramref name="record"/> (and its children) - see <see cref="Matching.RouteMatcher.RemoveRoute(RouteRecord)"/>.</summary>
    public void RemoveRoute(RouteRecord record) => _matcher.RemoveRoute(record);

    /// <summary>Unregisters whichever route is currently named <paramref name="name"/>, if any.</summary>
    public void RemoveRoute(string name) => _matcher.RemoveRoute(name);

    /// <summary>Whether a route named <paramref name="name"/> is currently registered.</summary>
    public bool HasRoute(string name) => _matcher.HasRoute(name);

    /// <summary>Every currently-registered route.</summary>
    public IReadOnlyList<RouteRecord> GetRoutes() => _matcher.GetRoutes();

    private async Task<NavigationFailure?> NavigateWithRedirect(RouteLocationRaw rawTo, RouteLocation? redirectedFrom, int redirectDepth)
    {
        var from = CurrentRoute.Value;
        var to = Resolve(rawTo);

        // A static RouteRecord.Redirect on the resolved leaf is handled before any guard runs
        // (and before the duplicate check) - only the eventual real target's guards ever run, its
        // own redirecting record's never do. No cycle counter here, unlike the guard-redirect
        // case below - matches Vue Router's own real asymmetry between the two redirect sources.
        var leafRedirect = to.Matched.Count > 0 ? to.Matched[^1].Redirect : null;
        if (leafRedirect is not null)
            return await NavigateWithRedirect(leafRedirect(to), redirectedFrom ?? to, redirectDepth).ConfigureAwait(true);

        if (!rawTo.Force && from.IsSameLocation(to))
        {
            var duplicated = new NavigationFailure { Type = NavigationFailureType.Duplicated, To = to, From = from };
            TriggerAfterEach(to, from, duplicated);
            return duplicated;
        }

        _pendingLocation = to;

        var outcome = await RunGuardPipeline(to, from).ConfigureAwait(true);

        if (outcome.Result == GuardPipelineOutcome.Kind.Redirect)
        {
            if (redirectDepth >= 30)
                throw new InvalidOperationException($"Infinite redirect loop detected in a navigation guard while navigating to \"{to.FullPath}\".");
            return await NavigateWithRedirect(outcome.RedirectTo!, redirectedFrom ?? to, redirectDepth + 1).ConfigureAwait(true);
        }

        // Every outcome except an in-progress redirect gets exactly one AfterEach call, including
        // Cancelled/Aborted/Duplicated - matching Vue Router's own real behavior ("run regardless
        // of outcome" - the guide's canonical failure-detection idiom is inside an AfterEach hook,
        // not a try/catch around push()). Only a redirect defers to its own eventual recursive
        // call instead of firing twice.
        NavigationFailure? failure = outcome.Result switch
        {
            GuardPipelineOutcome.Kind.Cancelled => new NavigationFailure { Type = NavigationFailureType.Cancelled, To = to, From = from },
            GuardPipelineOutcome.Kind.Aborted => new NavigationFailure { Type = NavigationFailureType.Aborted, To = to, From = from },
            _ => null,
        };

        if (failure is null)
        {
            if (rawTo.Replace || ReferenceEquals(from, RouteLocation.Start))
                _history.Replace(to.FullPath, to);
            else
                _history.Push(to.FullPath, to);

            CurrentRoute.Value = to;
        }

        TriggerAfterEach(to, from, failure);
        return failure;
    }

    /// <summary>
    /// Runs every guard stage in order (see this type's own remarks), re-checking after each one
    /// whether a newer navigation has since superseded this one (<see cref="_pendingLocation"/>
    /// is reassigned at the top of every <see cref="Push"/>/<see cref="Go"/> call) - a guard can
    /// itself be slow/async, so a second navigation racing the first one is a real, not
    /// theoretical, possibility. Does not commit anything - the caller decides what to do with
    /// the outcome. Each stage returns <see langword="null"/> to mean "continue to the next
    /// stage," or a concrete outcome to mean "stop here."
    /// </summary>
    private async Task<GuardPipelineOutcome> RunGuardPipeline(RouteLocation to, RouteLocation from)
    {
        var (leaving, updating, entering) = DiffMatched(to, from);
        leaving.Reverse(); // deepest-child-first; updating/entering stay root-first.

        if (await RunStage(LeaveGuardsFor(leaving), to, from).ConfigureAwait(true) is { } leaveResult)
            return leaveResult;

        if (await RunStage(_beforeEachGuards, to, from).ConfigureAwait(true) is { } beforeEachResult)
            return beforeEachResult;

        InvokeDataHooks<IOnRouteUpdate>(updating, static (hook, t, f) => hook.OnRouteUpdate(t, f), to, from);
        if (await RunStage(UpdateGuardsFor(updating), to, from).ConfigureAwait(true) is { } updateResult)
            return updateResult;

        var beforeEnterGuards = entering.Where(r => r.BeforeEnter is not null).Select(r => r.BeforeEnter!);
        if (await RunStage(beforeEnterGuards, to, from).ConfigureAwait(true) is { } beforeEnterResult)
            return beforeEnterResult;

        // From here on the entering Views really exist, so every remaining early-return has to
        // undo them (see ReleaseConstructedViews) - Vue Router never creates the component at all
        // when a guard rejects, and a leftover instance here would be silently reused by the next
        // attempt instead of being constructed fresh.
        var constructed = ConstructEnteringViews(entering);
        InvokeDataHooks<IOnRouteEnter>(entering, static (hook, t, f) => hook.OnRouteEnter(t, f), to, from);
        if (await RunStage(EnterGuardsFor(entering), to, from).ConfigureAwait(true) is { } enterResult)
        {
            ReleaseConstructedViews(constructed);
            return enterResult;
        }

        if (await RunStage(_beforeResolveGuards, to, from).ConfigureAwait(true) is { } beforeResolveResult)
        {
            ReleaseConstructedViews(constructed);
            return beforeResolveResult;
        }

        // Every guard passed, and the commit below is unconditional from here - safe to release
        // the leaving records' instances now. A RouterView never reads a leaving record's own
        // Instances (it only ever looks at records in the NEW Matched chain), so the exact timing
        // relative to the caller's own CurrentRoute/history commit doesn't matter; what matters is
        // that a later re-entry to the same route finds Instances empty and constructs fresh,
        // instead of reusing a torn-down instance.
        foreach (var record in leaving)
            record.Instances.Clear();

        return GuardPipelineOutcome.Success;
    }

    private bool IsStillPending(RouteLocation to) => ReferenceEquals(_pendingLocation, to);

    /// <summary>Runs every guard in <paramref name="guards"/> in order; returns <see langword="null"/> only if all of them continued AND the navigation is still the current one.</summary>
    private async Task<GuardPipelineOutcome?> RunStage(IEnumerable<NavigationGuard> guards, RouteLocation to, RouteLocation from)
    {
        foreach (var guard in guards)
        {
            var result = await guard(to, from).ConfigureAwait(true);
            if (result.Effect != NavigationGuardEffect.Continue)
                return GuardPipelineOutcome.FromGuard(result);
        }

        return IsStillPending(to) ? null : GuardPipelineOutcome.Cancelled;
    }

    /// <summary>
    /// Builds every not-yet-constructed View for <paramref name="entering"/>, returning exactly
    /// the ones this call created so a later-rejecting guard can undo precisely those (and not
    /// some pre-existing instance it didn't build) via <see cref="ReleaseConstructedViews"/>.
    /// </summary>
    private static List<(RouteRecord Record, string OutletName)> ConstructEnteringViews(IEnumerable<RouteRecord> entering)
    {
        var constructed = new List<(RouteRecord, string)>();

        foreach (var record in entering)
        {
            var views = record.ResolvedViews;
            if (views is null)
                continue;

            foreach (var (outletName, viewType) in views)
            {
                if (record.Instances.ContainsKey(outletName))
                    continue;

                // Mirrors Setup's own DI-first-else-Activator.CreateInstance resolution exactly -
                // a routed View's own aq:Setup.Enable/ViewModel then resolves DataContext exactly
                // like any other Aquarius View, with no separate mechanism needed here.
                var element = (FrameworkElement)ResolveInstance(viewType);

                // ...unless the route table pairs the two itself (RouteRecord.ViewModel), which is
                // the only way to pair a View and a ViewModel that both ship inside libraries
                // nobody here is allowed to edit. Assigned after construction, so it deliberately
                // overrides whatever that View's own aq:Setup resolved during it.
                if (record.ResolvedViewModels?.TryGetValue(outletName, out var viewModelType) is true)
                    element.DataContext = ResolveInstance(viewModelType);

                record.Instances[outletName] = element;
                constructed.Add((record, outletName));
            }
        }

        return constructed;
    }

    /// <summary>
    /// The same DI-first-else-<see cref="Activator.CreateInstance(Type)"/> resolution
    /// <see cref="Setup"/> applies to a ViewModel, so a routed View and a route-declared ViewModel
    /// both come out of the app's own container when there is one - shared with <see cref="Setup"/>
    /// and Aquarius's own <c>Component</c>/<c>KeepAlive</c> rather than reimplemented here.
    /// </summary>
    private static object ResolveInstance(Type type) => Setup.Resolve(type);

    /// <summary>
    /// Discards the Views <see cref="ConstructEnteringViews"/> just built, for a navigation that
    /// then got cancelled/redirected/superseded. Nothing was ever mounted (no
    /// <see cref="Xaml.RouterView"/> reads a record until <see cref="CurrentRoute"/> commits), so
    /// dropping the reference is the whole of the cleanup.
    /// </summary>
    private static void ReleaseConstructedViews(List<(RouteRecord Record, string OutletName)> constructed)
    {
        foreach (var (record, outletName) in constructed)
            record.Instances.Remove(outletName);
    }

    private static void InvokeDataHooks<THook>(IEnumerable<RouteRecord> records, Action<THook, RouteLocation, RouteLocation> invoke, RouteLocation to, RouteLocation from)
    {
        foreach (var record in records)
            foreach (var view in record.Instances.Values)
                if (view.DataContext is THook hook)
                    invoke(hook, to, from);
    }

    private static IEnumerable<NavigationGuard> LeaveGuardsFor(IEnumerable<RouteRecord> records) =>
        GuardsFor<IOnBeforeRouteLeave>(records, hook => hook.OnBeforeRouteLeave);

    private static IEnumerable<NavigationGuard> UpdateGuardsFor(IEnumerable<RouteRecord> records) =>
        GuardsFor<IOnBeforeRouteUpdate>(records, hook => hook.OnBeforeRouteUpdate);

    private static IEnumerable<NavigationGuard> EnterGuardsFor(IEnumerable<RouteRecord> records) =>
        GuardsFor<IOnBeforeRouteEnter>(records, hook => hook.OnBeforeRouteEnter);

    private static IEnumerable<NavigationGuard> GuardsFor<THook>(IEnumerable<RouteRecord> records, Func<THook, NavigationGuard> select)
    {
        foreach (var record in records)
            foreach (var view in record.Instances.Values)
                if (view.DataContext is THook hook)
                    yield return select(hook);
    }

    /// <summary>
    /// Diffs <paramref name="to"/>/<paramref name="from"/>'s <see cref="RouteLocation.Matched"/>
    /// chains position-by-position - a record present in both is "updating" (even a reused
    /// instance across e.g. <c>/users/1</c> -&gt; <c>/users/2</c>), present only in
    /// <paramref name="from"/> is "leaving", present only in <paramref name="to"/> is "entering".
    /// Matches Vue Router's own real <c>extractChangingRecords</c> algorithm exactly.
    /// </summary>
    private static (List<RouteRecord> Leaving, List<RouteRecord> Updating, List<RouteRecord> Entering) DiffMatched(RouteLocation to, RouteLocation from)
    {
        var leaving = new List<RouteRecord>();
        var updating = new List<RouteRecord>();
        var entering = new List<RouteRecord>();

        var length = Math.Max(from.Matched.Count, to.Matched.Count);
        for (var i = 0; i < length; i++)
        {
            if (i < from.Matched.Count)
            {
                var record = from.Matched[i];
                if (ContainsReference(to.Matched, record))
                    updating.Add(record);
                else
                    leaving.Add(record);
            }

            if (i < to.Matched.Count)
            {
                var record = to.Matched[i];
                if (!ContainsReference(from.Matched, record))
                    entering.Add(record);
            }
        }

        return (leaving, updating, entering);
    }

    private static bool ContainsReference(IReadOnlyList<RouteRecord> records, RouteRecord target)
    {
        foreach (var record in records)
            if (ReferenceEquals(record, target))
                return true;
        return false;
    }

    private void TriggerAfterEach(RouteLocation to, RouteLocation from, NavigationFailure? failure)
    {
        foreach (var hook in _afterEachHooks.ToArray())
            hook(to, from, failure);
    }

    private static string BuildFullPath(string path, IReadOnlyDictionary<string, string?> query, string? hash)
    {
        var fullPath = path;

        if (query.Count > 0)
        {
            fullPath += "?" + string.Join('&', query.Select(pair =>
                pair.Value is null
                    ? Uri.EscapeDataString(pair.Key)
                    : $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        }

        if (!string.IsNullOrEmpty(hash))
            fullPath += "#" + hash;

        return fullPath;
    }

    private static IDisposable Subscribe<T>(List<T> list, T item)
    {
        list.Add(item);
        return new Unsubscriber(() => list.Remove(item!));
    }

    private sealed class Unsubscriber(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;

        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }

    private readonly struct GuardPipelineOutcome
    {
        public enum Kind { Success, Cancelled, Aborted, Redirect }

        public Kind Result { get; private init; }
        public RouteLocationRaw? RedirectTo { get; private init; }

        public static GuardPipelineOutcome Success { get; } = new() { Result = Kind.Success };
        public static GuardPipelineOutcome Cancelled { get; } = new() { Result = Kind.Cancelled };

        public static GuardPipelineOutcome FromGuard(NavigationGuardResult result) => result.Effect switch
        {
            NavigationGuardEffect.Cancel => new GuardPipelineOutcome { Result = Kind.Aborted },
            NavigationGuardEffect.Redirect => new GuardPipelineOutcome { Result = Kind.Redirect, RedirectTo = result.RedirectTo },
            _ => Success,
        };
    }
}
