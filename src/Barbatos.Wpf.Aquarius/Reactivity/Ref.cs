// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.Aquarius.Reactivity;

/// <summary>
/// A single reactive value - the Aquarius counterpart of Vue's <c>ref()</c>.
/// </summary>
/// <remarks>
/// Built directly on <see cref="ObservableObject"/> (CommunityToolkit.Mvvm): a
/// <see cref="Ref{T}"/> is just an <see cref="ObservableObject"/> with one
/// <see cref="Value"/> property, so it already participates in WPF bindings,
/// <see cref="Watch"/>, and <see cref="Computed{T}"/> like any other observable object.
/// Unlike Vue's <c>ref()</c>, which unwraps automatically inside a template, C# has no
/// template layer to do that unwrapping for you - read and write through
/// <see cref="Value"/> explicitly, the same way Vue's own script code (outside a
/// template) has to use <c>.value</c>. This is intentional: an implicit conversion here
/// would be a silent footgun (e.g. accidental boxing/comparison surprises), so none is
/// provided.
/// </remarks>
public class Ref<T> : ObservableObject
{
    private T _value;

    /// <summary>
    /// Creates a reactive value initialized to <c>default(T)</c>, mirroring
    /// <c>ref()</c> called with no argument.
    /// </summary>
    public Ref() : this(default!)
    {
    }

    /// <summary>
    /// Creates a reactive value initialized to <paramref name="value"/>, mirroring
    /// <c>const x = ref(value)</c>.
    /// </summary>
    public Ref(T value) => _value = value;

    /// <summary>
    /// The current value. Setting it raises <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/>
    /// only when the new value actually differs (default equality comparer), the same
    /// caching behavior Vue's reactivity system provides for a plain <c>ref</c>.
    /// </summary>
    public T Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    /// <inheritdoc/>
    public override string? ToString() => Value?.ToString();
}
