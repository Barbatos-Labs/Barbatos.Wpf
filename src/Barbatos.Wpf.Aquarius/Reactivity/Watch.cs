// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Collections.Specialized;
using System.ComponentModel;

namespace Barbatos.Wpf.Aquarius.Reactivity;

/// <summary>
/// When a watcher's callback runs relative to the change that triggered it - the
/// Aquarius counterpart of Vue's <c>{ flush: 'sync' | 'post' }</c> watch option.
/// </summary>
public enum WatchFlush
{
    /// <summary>Invoke the callback immediately, synchronously - the default.</summary>
    Sync,

    /// <summary>
    /// Coalesce rapid-fire changes and invoke the callback once via
    /// <see cref="NextTick"/>, mirroring Vue's default <c>flush: 'post'</c> batching.
    /// </summary>
    Post,
}

/// <summary>
/// Reacts to changes in reactive state - the Aquarius counterpart of Vue's
/// <c>watch()</c>/<c>watchEffect()</c>.
/// </summary>
public static class Watch
{
    /// <summary>
    /// Invokes <paramref name="onChanged"/> with the new and previous value every time
    /// <paramref name="source"/> changes, mirroring <c>watch(source, (nv, ov) =&gt; ...)</c>.
    /// </summary>
    /// <param name="source">The <see cref="Ref{T}"/> to observe.</param>
    /// <param name="onChanged">Invoked as <c>(newValue, oldValue)</c>.</param>
    /// <param name="immediate">
    /// When <c>true</c>, also invokes <paramref name="onChanged"/> once immediately with
    /// the current value as both arguments, mirroring Vue's <c>{ immediate: true }</c>.
    /// </param>
    /// <param name="once">
    /// When <c>true</c>, stops watching right after the first triggered invocation,
    /// mirroring Vue's <c>{ once: true }</c> (3.4+).
    /// </param>
    /// <param name="deep">
    /// When <c>true</c> and <paramref name="source"/>'s value also implements
    /// <see cref="INotifyCollectionChanged"/> (e.g. an <c>ObservableCollection&lt;T&gt;</c>),
    /// also reacts to Add/Remove/Reset on it, not just wholesale replacement of
    /// <c>.Value</c>. This is a deliberately narrower stand-in for Vue's recursive
    /// <c>{ deep: true }</c> - C# has no reactive-proxy equivalent that could observe an
    /// arbitrary nested object graph the way Vue's <c>reactive()</c> does, but reacting to
    /// collection mutations covers the single most common real-world "deep" need.
    /// </param>
    /// <param name="flush">See <see cref="WatchFlush"/>.</param>
    /// <returns>
    /// A handle whose <see cref="IDisposable.Dispose"/> stops watching, mirroring the
    /// <c>stop()</c> handle Vue's <c>watch()</c> returns.
    /// </returns>
    public static IDisposable On<T>(
        Ref<T> source,
        Action<T, T> onChanged,
        bool immediate = false,
        bool once = false,
        bool deep = false,
        WatchFlush flush = WatchFlush.Sync)
        => On(source, (newValue, oldValue, _) => onChanged(newValue, oldValue), immediate, once, deep, flush);

    /// <summary>
    /// Overload whose callback also receives an <c>onCleanup</c> registrar, mirroring
    /// Vue's <c>watch(source, (nv, ov, onCleanup) =&gt; ...)</c>. Calling the registrar
    /// schedules an action to run right before the *next* invocation (or when the
    /// watcher stops) - the standard way to cancel stale async work (e.g. an in-flight
    /// request superseded by a newer value) before starting new work, avoiding
    /// last-write-wins races.
    /// </summary>
    public static IDisposable On<T>(
        Ref<T> source,
        Action<T, T, Action<Action>> onChanged,
        bool immediate = false,
        bool once = false,
        bool deep = false,
        WatchFlush flush = WatchFlush.Sync)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(onChanged);

        var previous = source.Value;
        Action? pendingCleanup = null;
        var scheduled = false;
        IDisposable stopHandle = null!;

        void RunCleanup()
        {
            var cleanup = pendingCleanup;
            pendingCleanup = null;
            cleanup?.Invoke();
        }

        void Fire(T current, T old)
        {
            RunCleanup();
            onChanged(current, old, action => pendingCleanup = action);

            if (once)
                stopHandle.Dispose();
        }

        void Trigger()
        {
            var current = source.Value;
            var old = previous;
            previous = current;

            if (flush == WatchFlush.Post)
            {
                if (scheduled)
                    return;

                scheduled = true;
                NextTick.Run(() =>
                {
                    scheduled = false;
                    Fire(current, old);
                });
            }
            else
            {
                Fire(current, old);
            }
        }

        void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Trigger();

        void RewireDeep(T? oldValue, T? newValue)
        {
            if (!deep)
                return;

            if (oldValue is INotifyCollectionChanged oldCollection)
                oldCollection.CollectionChanged -= OnCollectionChanged;

            if (newValue is INotifyCollectionChanged newCollection)
                newCollection.CollectionChanged += OnCollectionChanged;
        }

        void OnValueChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Ref<T>.Value))
                return;

            RewireDeep(previous, source.Value);
            Trigger();
        }

        source.PropertyChanged += OnValueChanged;
        RewireDeep(default, source.Value);

        stopHandle = new StopHandle(() =>
        {
            source.PropertyChanged -= OnValueChanged;
            RewireDeep(source.Value, default);
            RunCleanup();
        });

        if (immediate)
            Fire(source.Value, source.Value);

        return stopHandle;
    }

    /// <summary>
    /// Runs <paramref name="effect"/> immediately, then again every time any of
    /// <paramref name="dependencies"/> changes, mirroring <c>watchEffect(() =&gt; ...)</c>.
    /// </summary>
    /// <remarks>
    /// Vue's <c>watchEffect</c> auto-tracks whatever reactive state <paramref name="effect"/>
    /// reads; C# has no equivalent interception, so the dependencies to re-run on are
    /// passed explicitly, the same divergence <see cref="Computed{T}"/> documents.
    /// </remarks>
    /// <returns>
    /// A handle whose <see cref="IDisposable.Dispose"/> stops watching, mirroring the
    /// <c>stop()</c> handle Vue's <c>watchEffect()</c> returns.
    /// </returns>
    public static IDisposable Effect(Action effect, params INotifyPropertyChanged[] dependencies)
        => Effect(effect, once: false, flush: WatchFlush.Sync, dependencies);

    /// <summary>
    /// Overload accepting <paramref name="once"/>/<paramref name="flush"/>, the same
    /// options <see cref="On{T}(Ref{T}, Action{T, T}, bool, bool, bool, WatchFlush)"/> has
    /// (<c>deep</c> does not apply here - <paramref name="dependencies"/> are already
    /// explicit, there is no single "source" value to inspect for
    /// <see cref="System.Collections.Specialized.INotifyCollectionChanged"/>).
    /// </summary>
    public static IDisposable Effect(Action effect, bool once, WatchFlush flush, params INotifyPropertyChanged[] dependencies)
    {
        ArgumentNullException.ThrowIfNull(effect);
        ArgumentNullException.ThrowIfNull(dependencies);

        var scheduled = false;
        IDisposable stopHandle = null!;

        void Fire()
        {
            effect();

            if (once)
                stopHandle.Dispose();
        }

        void Trigger()
        {
            if (flush == WatchFlush.Post)
            {
                if (scheduled)
                    return;

                scheduled = true;
                NextTick.Run(() =>
                {
                    scheduled = false;
                    Fire();
                });
            }
            else
            {
                Fire();
            }
        }

        void Handler(object? sender, PropertyChangedEventArgs e) => Trigger();

        foreach (var dependency in dependencies)
            dependency.PropertyChanged += Handler;

        stopHandle = new StopHandle(() =>
        {
            foreach (var dependency in dependencies)
                dependency.PropertyChanged -= Handler;
        });

        effect();

        return stopHandle;
    }

    private sealed class StopHandle(Action stop) : IDisposable
    {
        private Action? _stop = stop;

        public void Dispose()
        {
            _stop?.Invoke();
            _stop = null;
        }
    }
}
