// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.Aquarius.Reactivity;

/// <summary>
/// A reactive value derived from other observables - the Aquarius counterpart of Vue's
/// <c>computed()</c>.
/// </summary>
/// <remarks>
/// Vue's <c>computed(() =&gt; ...)</c> auto-tracks whatever reactive state its getter
/// reads, through a reactive proxy. C# has no equivalent interception, so
/// <see cref="From(Func{T}, INotifyPropertyChanged[])"/> takes the dependencies
/// explicitly - this is the one deliberate divergence from Vue's own API. The getter is
/// re-evaluated, and <see cref="Value"/>'s change notification raised (only if the
/// result actually changed), whenever one of the declared dependencies raises
/// <see cref="INotifyPropertyChanged.PropertyChanged"/>. Between dependency changes,
/// reads are served from cache, matching Vue's own computed-caching behavior.
/// </remarks>
public sealed class Computed<T> : ObservableObject, IDisposable
{
    private readonly Func<T> _getter;
    private readonly Action<T>? _setter;
    private readonly INotifyPropertyChanged[] _dependencies;
    private readonly PropertyChangedEventHandler _onDependencyChanged;
    private T _value;
    private bool _disposed;

    private Computed(Func<T> getter, Action<T>? setter, INotifyPropertyChanged[] dependencies)
    {
        _getter = getter;
        _setter = setter;
        _dependencies = dependencies;
        // T is unconstrained (Computed<string?> is a legitimate use), so the compiler can't
        // know whether a null getter result is actually valid for T - that's exactly what
        // SetProperty<T> itself is designed to accept.
#pragma warning disable CS8601 // Possible null reference assignment.
        _onDependencyChanged = (_, _) => SetProperty(ref _value, _getter(), nameof(Value));
#pragma warning restore CS8601
        _value = _getter();

        foreach (var dependency in _dependencies)
            dependency.PropertyChanged += _onDependencyChanged;
    }

    /// <summary>
    /// Creates a read-only computed value that re-evaluates <paramref name="getter"/>
    /// whenever any of <paramref name="dependencies"/> changes, mirroring
    /// <c>const doubled = computed(() =&gt; count.value * 2)</c>.
    /// </summary>
    public static Computed<T> From(Func<T> getter, params INotifyPropertyChanged[] dependencies)
    {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(dependencies);

        return new Computed<T>(getter, null, dependencies);
    }

    /// <summary>
    /// Creates a writable computed value, mirroring Vue's get/set form:
    /// <c>computed({ get: () =&gt; ..., set: (v) =&gt; ... })</c>. <paramref name="setter"/>
    /// is expected to mutate one or more of <paramref name="dependencies"/> (e.g. the
    /// <see cref="Ref{T}"/>s the getter reads) - <see cref="Value"/> then reflects the
    /// new result the normal way, through that dependency's own change notification,
    /// rather than being written to directly.
    /// </summary>
    public static Computed<T> From(Func<T> getter, Action<T> setter, params INotifyPropertyChanged[] dependencies)
    {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);
        ArgumentNullException.ThrowIfNull(dependencies);

        return new Computed<T>(getter, setter, dependencies);
    }

    /// <summary>
    /// The cached, most recently computed value. Assigning to it calls the setter passed
    /// to <see cref="From(Func{T}, Action{T}, INotifyPropertyChanged[])"/>, if this
    /// instance was created with one; otherwise throws, mirroring Vue's runtime warning
    /// for assigning to a getter-only computed.
    /// </summary>
    public T Value
    {
        get => _value;
        set
        {
            if (_setter is null)
            {
                throw new InvalidOperationException(
                    "This Computed<T> has no setter - create it via Computed<T>.From(getter, setter, ...) " +
                    "to make it writable.");
            }

            _setter(value);
        }
    }

    /// <summary>
    /// Stops tracking every dependency, mirroring the cleanup Vue performs when a
    /// computed's owning effect scope is torn down.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (var dependency in _dependencies)
            dependency.PropertyChanged -= _onDependencyChanged;
    }

    /// <inheritdoc/>
    public override string? ToString() => Value?.ToString();
}
