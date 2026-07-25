// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.Apsu;

/// <summary>
/// Base class for a Pinia-style store: state lives as ordinary <see cref="ObservableObject"/>
/// properties (see <see cref="ObservableObject.SetProperty{T}(ref T, T, string)"/>), and this
/// type layers on the parts Pinia adds on top of plain reactive state - a batched change
/// notification (<see cref="StateChanged"/>, the <c>$subscribe</c> counterpart), a batched
/// mutation helper (<see cref="Patch"/>, the <c>$patch</c> counterpart), and an explicit reset
/// hook (<see cref="Reset"/>, the <c>$reset</c> counterpart). Register a derived store with
/// <see cref="StoreServiceCollectionExtensions.AddStore{TStore}"/>.
/// </summary>
public abstract class StoreBase : ObservableObject
{
    bool _isPatching;
    bool _patchDirty;

    /// <summary>
    /// Initializes the store and wires <see cref="StateChanged"/> to fire whenever a derived
    /// property setter calls <see cref="ObservableObject.SetProperty{T}(ref T, T, string)"/>,
    /// batched across a <see cref="Patch"/> call.
    /// </summary>
    protected StoreBase()
    {
        PropertyChanged += (_, _) =>
        {
            if (_isPatching)
            {
                _patchDirty = true;
                return;
            }

            StateChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    /// <summary>
    /// Occurs after this store's state changes - once per property set outside a
    /// <see cref="Patch"/> call, or exactly once per <see cref="Patch"/> call no matter how many
    /// properties it touched. The <c>$subscribe</c> counterpart.
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// Applies <paramref name="mutate"/> to this store, batching every property change it makes
    /// into a single <see cref="StateChanged"/> notification fired once <paramref name="mutate"/>
    /// returns. Each individual property still raises <see cref="ObservableObject.PropertyChanged"/>
    /// as it is set - only the coarser <see cref="StateChanged"/> is batched. The <c>$patch</c>
    /// counterpart (the mutator-callback form; there is no separate partial-object-merge form).
    /// </summary>
    /// <param name="mutate">A callback that mutates this store's own properties.</param>
    public void Patch(Action<StoreBase> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        if (_isPatching)
        {
            // A nested Patch call (e.g. an action that calls Patch, invoked from inside another
            // Patch): let the outermost call be the one that fires StateChanged.
            mutate(this);
            return;
        }

        _isPatching = true;
        _patchDirty = false;
        try
        {
            mutate(this);
        }
        finally
        {
            _isPatching = false;
            if (_patchDirty)
                StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Restores this store's state to its initial values. The <c>$reset</c> counterpart -
    /// unlike Pinia (which can only implement this automatically for its Options Store style),
    /// there is no default implementation here: override this method to assign each property
    /// back to its starting value. The default implementation throws, so forgetting to override
    /// it fails loudly instead of silently doing nothing.
    /// </summary>
    /// <exception cref="NotSupportedException">This store does not override <see cref="Reset"/>.</exception>
    public virtual void Reset() =>
        throw new NotSupportedException($"{GetType().Name} does not support Reset(). Override Reset() to restore initial state.");
}
