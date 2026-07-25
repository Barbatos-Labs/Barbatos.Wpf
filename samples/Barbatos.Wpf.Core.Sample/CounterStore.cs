// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Apsu;

namespace Barbatos.Wpf.Core.Sample;

/// <summary>
/// The canonical Pinia "counter store" example, ported to <see cref="StoreBase"/> and registered
/// with <c>builder.Services.AddStore&lt;CounterStore&gt;()</c> in <see cref="WpfProgram"/> -
/// resolved into <see cref="MainViewModel"/> the same way every other service on this page is.
/// Not sealed, and <see cref="Increment"/> is <see langword="virtual"/> and <see cref="ActionAttribute"/>-marked,
/// so <see cref="WpfProgram.CreateWpfApp"/>'s registered <c>SampleActionLoggingInterceptor</c> can
/// observe it - see the "Store" tab's log line each time Increment is clicked.
/// </summary>
public class CounterStore : StoreBase
{
    int _count;
    readonly Getter<string> _doubleCountLabel;

    public CounterStore()
    {
        _doubleCountLabel = new Getter<string>(() => $"Double: {Count * 2}", this);
    }

    /// <summary>State - the <c>count</c> ref in Pinia's own counter store example.</summary>
    public int Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }

    /// <summary>A getter - recomputed lazily, cached until <see cref="Count"/> (or anything
    /// else on this store) changes.</summary>
    public string DoubleCountLabel => _doubleCountLabel.Value;

    /// <summary>An action.</summary>
    [Action]
    public virtual void Increment() => Count++;

    /// <summary>
    /// Deliberately throws, uncaught by its own caller (<see cref="MainViewModel.SimulateCounterError"/>) -
    /// demonstrates that <c>SampleActionLoggingInterceptor</c> observes the error and still lets
    /// it propagate all the way to the app's own last-resort <c>OnDispatcherUnhandledException</c>
    /// lifecycle hook (see <see cref="WpfProgram.CreateWpfApp"/>), which logs it and marks it
    /// handled - the sample keeps running afterward instead of crashing.
    /// </summary>
    [Action]
    public virtual void SimulateError() =>
        throw new InvalidOperationException("Simulated error from CounterStore.SimulateError().");

    /// <inheritdoc />
    public override void Reset() => Count = 0;
}
