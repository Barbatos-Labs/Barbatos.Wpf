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
