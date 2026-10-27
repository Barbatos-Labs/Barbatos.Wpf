// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Apsu;

/// <summary>
/// A lazily-computed, cached derived value - the <c>computed()</c> counterpart for a
/// <see cref="StoreBase"/>. The cached value is invalidated whenever any declared dependency
/// store raises <see cref="StoreBase.StateChanged"/> - a coarse invalidation (any state change on any
/// dependency recomputes the getter, not just changes to the specific property it actually reads),
/// a deliberate simplification: C# has no proxy-based dependency tracking to do better than that
/// automatically, and each store already batches its own multi-property changes via
/// <see cref="StoreBase.Patch"/> so a single edit does not thrash this cache repeatedly. Declaring
/// more than one dependency is how a getter combines data from several stores - Pinia's own
/// equivalent ("a getter that reads two different stores' state") needs no special syntax because
/// Vue's reactivity auto-tracks whichever stores the getter function actually reads; here, every
/// store it reads must be listed explicitly.
/// </summary>
/// <remarks>
/// <para>
/// A getter raises no change notification of its own. A store property that returns
/// <see cref="Value"/> is a plain read, so a XAML binding to it reads it once and never updates:
/// expose it through a ViewModel property that raises <c>PropertyChanged</c> on
/// <see cref="StoreBase.StateChanged"/> of every dependency store (never from the store's own
/// <see cref="StoreBase.StateChanged"/> handler - each raise would fire
/// <see cref="StoreBase.StateChanged"/> again).
/// </para>
/// <para>
/// Forward on <see cref="StoreBase.StateChanged"/>, not on <c>PropertyChanged</c>. Outside
/// <see cref="StoreBase.Patch"/>, the store's own <c>PropertyChanged</c> handler (subscribed
/// first, in <see cref="StoreBase"/>'s constructor) raises <see cref="StoreBase.StateChanged"/>
/// before any later subscriber runs, so the getter is already invalidated. Inside
/// <see cref="StoreBase.Patch"/> that event is deferred until the patch returns:
/// <c>PropertyChanged</c> handlers - and <see cref="Value"/> reads inside the patch callback -
/// see the stale cached value, and nothing notifies again once it is invalidated.
/// </para>
/// <para>
/// There is no <c>Dispose</c>. The constructor subscribes to every dependency's
/// <see cref="StoreBase.StateChanged"/> and never unsubscribes, so each dependency keeps this
/// getter - and everything <c>compute</c> captures - alive for as long as it lives itself.
/// Inside a store that costs nothing (the store and its getter share one lifetime); a getter
/// built in a short-lived object over an app-lifetime store keeps that object alive for the
/// life of the app. Create getters inside stores only, and derive per-screen values in the
/// ViewModel with something it can dispose.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the computed value.</typeparam>
public sealed class Getter<T>
{
    readonly Func<T> _compute;
    bool _hasValue;
    T? _value;

    /// <summary>
    /// Creates a getter computed on first access (and after every invalidation) by
    /// <paramref name="compute"/>, invalidated whenever any of <paramref name="dependencies"/>
    /// changes. <paramref name="compute"/> typically closes over those same
    /// <paramref name="dependencies"/> (e.g. <c>() => Store.Count * 2</c>) - unlike Pinia, which
    /// binds a getter function's <c>this</c> to the store by hand, a C# closure already captures
    /// whatever store(s) it needs on its own; <paramref name="dependencies"/> exists purely to
    /// tell this type when to invalidate, not to make the store(s) reachable.
    /// </summary>
    public Getter(Func<T> compute, params StoreBase[] dependencies)
    {
        ArgumentNullException.ThrowIfNull(compute);
        ArgumentNullException.ThrowIfNull(dependencies);

        _compute = compute;
        foreach (var dependency in dependencies)
            dependency.StateChanged += (_, _) => _hasValue = false;
    }

    /// <summary>The current computed value, recomputing only if invalidated since the last access.</summary>
    public T Value
    {
        get
        {
            if (!_hasValue)
            {
                _value = _compute();
                _hasValue = true;
            }

            return _value!;
        }
    }
}
